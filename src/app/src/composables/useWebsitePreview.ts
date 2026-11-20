import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { usePages } from '@/composables/usePages'
import { useWebsites } from '@/composables/useWebsites'
import type { Page } from '@/data/types'
import { usePageContentQuerier } from '@/queriers/usePageContentQuerier'

/** Slugs a link to `/` lands on, in order; otherwise the website's first page. */
const HOME_SLUGS = ['home', 'index']

/** The page a site path such as `/about` (or `/` for the home page) shows. */
export function pageForPath(pages: Page[], path: string): Page | undefined {
  const slug = path.split(/[?#]/)[0]!.replace(/^\/+|\/+$/g, '').toLowerCase()

  if (slug)
    return pages.find((p) => p.slug.toLowerCase() === slug)

  return HOME_SLUGS.map((s) => pages.find((p) => p.slug === s)).find(Boolean) ?? pages[0]
}

/**
 * A website as visitors will see it: its pages and the published content of the page at `path`.
 * Reads published content only; unpublished studio edits stay in the editor.
 */
export function useWebsitePreview(websiteId: MaybeRefOrGetter<string>, path: MaybeRefOrGetter<string>) {
  const { getWebsites } = useWebsites()
  const { getPages } = usePages()
  const querier = usePageContentQuerier()

  const websitesQuery = getWebsites()
  const website = computed(() => websitesQuery.websites.value.find((w) => w.id === toValue(websiteId)))

  const pagesQuery = getPages(websiteId)
  /** In the order a visitor would expect: home first, then by title. */
  const pages = computed(() => [...pagesQuery.pages.value].sort(byHomeThenTitle))
  const page = computed(() => pageForPath(pages.value, toValue(path)))

  const contentQuery = querier.getPublishedContent(() => page.value?.id)
  const document = computed(() => contentQuery.data.value ?? null)

  const isPending = computed(
    () => websitesQuery.isPending.value || pagesQuery.isPending.value || (!!page.value && contentQuery.isPending.value),
  )

  const isError = computed(() => websitesQuery.isError.value || pagesQuery.isError.value || contentQuery.isError.value)

  function refetch() {
    websitesQuery.refetch()
    pagesQuery.refetch()
    contentQuery.refetch()
  }

  return {
    website,
    pages,
    page,
    document,
    isPending,
    isError,
    refetch,
  }
}

function byHomeThenTitle(a: Page, b: Page) {
  const rank = (p: Page) => (HOME_SLUGS.includes(p.slug) ? 0 : 1)

  return rank(a) - rank(b) || a.title.localeCompare(b.title)
}
