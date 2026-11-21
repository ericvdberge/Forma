import { computed, shallowRef, toValue, watch, type MaybeRefOrGetter } from 'vue'
import { useCollections } from '@/composables/useCollections'
import { usePages } from '@/composables/usePages'
import { useStudioCollections } from '@/composables/useStudioCollections'
import { useWebsites } from '@/composables/useWebsites'
import type { Collection, Page } from '@/data/types'
import { usePageContentQuerier } from '@/queriers/usePageContentQuerier'
import type { StudioCollectionData } from '@/studio/inject'
import type { PageDocument } from '@/studio/types'

/** Slugs a link to `/` lands on, in order; otherwise the website's first page. */
const HOME_SLUGS = ['home', 'index']

/** The parts of a site path: `/blog/first-post#top` → `['blog', 'first-post']`. */
export function pathSegments(path: string) {
  return path
    .split(/[?#]/)[0]!
    .split('/')
    .filter(Boolean)
    .map((s) => s.toLowerCase())
}

/** The page a site path such as `/about` (or `/` for the home page) shows. Detail pages are reached through an item. */
export function pageForPath(pages: Page[], path: string): Page | undefined {
  const [slug, ...rest] = pathSegments(path)
  const own = pages.filter((p) => p.kind === 'static')

  if (rest.length)
    return undefined

  if (slug)
    return own.find((p) => p.slug.toLowerCase() === slug)

  return HOME_SLUGS.map((s) => own.find((p) => p.slug === s)).find(Boolean) ?? own[0]
}

/** The collection a path such as `/blog/first-post` shows an item of, and the item's slug. */
function itemPath(collections: Collection[], path: string) {
  const segments = pathSegments(path)

  if (segments.length !== 2)
    return null

  const collection = collections.find((c) => c.slug.toLowerCase() === segments[0])

  return collection ? { collection, itemSlug: segments[1]! } : null
}

/**
 * A website as visitors will see it: its pages and the published content of the page at `path`. A path such as
 * `/blog/first-post` shows the collection's detail page with that published item. Collection cards list published
 * items. Reads published content only; unpublished studio edits stay in the editor.
 */
export function useWebsitePreview(websiteId: MaybeRefOrGetter<string>, path: MaybeRefOrGetter<string>) {
  const { getWebsites } = useWebsites()
  const { getPages } = usePages()
  const querier = usePageContentQuerier()

  const websitesQuery = getWebsites()
  const website = computed(() => websitesQuery.websites.value.find((w) => w.id === toValue(websiteId)))

  const pagesQuery = getPages(websiteId)
  const allPages = computed(() => pagesQuery.pages.value)
  /** The website's own pages, in the order a visitor would expect: home first, then by title. */
  const pages = computed(() => allPages.value.filter((p) => p.kind === 'static').sort(byHomeThenTitle))

  const collectionsQuery = useCollections().getCollections(websiteId)
  const detail = computed(() => itemPath(collectionsQuery.collections.value, toValue(path)))

  /* The cards read the shown document, which is only known once the page is: it is filled in below. */
  const shownDocument = shallowRef<PageDocument | undefined>()
  const studioCollections = useStudioCollections(websiteId, () => detail.value?.collection.id ?? null, shownDocument)

  /** On a detail path: the published item it shows, or undefined when there is none with that slug. */
  const item = computed(() => {
    const d = detail.value

    if (!d)
      return undefined

    return studioCollections.collectionData
      .itemsOf(d.collection.id)
      .find((i) => i.status === 'live' && i.slug.toLowerCase() === d.itemSlug)
  })

  const page = computed(() => {
    const d = detail.value

    if (!d)
      return pageForPath(allPages.value, toValue(path))

    if (!item.value)
      return undefined

    return allPages.value.find((p) => p.id === d.collection.detailPageId)
  })

  const contentQuery = querier.getPublishedContent(() => page.value?.id)
  const document = computed(() => contentQuery.data.value ?? null)

  watch(document, (d) => (shownDocument.value = d ?? undefined), { immediate: true })

  /** What the cards and bound blocks read: the detail page shows the item from the path. */
  const collectionData: StudioCollectionData = {
    ...studioCollections.collectionData,
    previewItem: item,
  }

  const detailPending = computed(() => !!detail.value && studioCollections.collectionData.isLoading(detail.value.collection.id))

  const isPending = computed(
    () =>
      websitesQuery.isPending.value ||
      pagesQuery.isPending.value ||
      collectionsQuery.isPending.value ||
      detailPending.value ||
      (!!page.value && contentQuery.isPending.value),
  )

  const isError = computed(
    () =>
      websitesQuery.isError.value ||
      pagesQuery.isError.value ||
      collectionsQuery.isError.value ||
      contentQuery.isError.value,
  )

  function refetch() {
    websitesQuery.refetch()
    pagesQuery.refetch()
    collectionsQuery.refetch()
    contentQuery.refetch()
  }

  return {
    website,
    pages,
    page,
    item,
    document,
    collectionData,
    isPending,
    isError,
    refetch,
  }
}

function byHomeThenTitle(a: Page, b: Page) {
  const rank = (p: Page) => (HOME_SLUGS.includes(p.slug) ? 0 : 1)

  return rank(a) - rank(b) || a.title.localeCompare(b.title)
}
