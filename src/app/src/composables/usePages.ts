import { computed } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getWebsitesQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { WebsiteDto } from '@/api/generated'
import * as seed from '@/data/seed'
import type { Page, PublishStatus } from '@/data/types'
import { pagesQueryKey, usePagesQuerier } from '@/queriers/usePagesQuerier'
import { useWebsitesQuerier } from '@/queriers/useWebsitesQuerier'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'
import { slugify } from '@/utils/slugify'

/**
 * Seed pages point at the old seed website ids ('studio', …). Re-point them at the API websites
 * with the same domain. Temporary bridge until pages come from the API.
 */
function linkSeedPages(pages: Page[], websites: WebsiteDto[]): Page[] {
  const apiIdBySeedId = new Map<string, string>()
  for (const s of seed.websites) {
    const dto = websites.find((d) => d.domain === s.domain)
    if (dto) apiIdBySeedId.set(s.id, dto.id)
  }
  return pages.map((p) => {
    const apiId = apiIdBySeedId.get(p.websiteId)
    return apiId ? { ...p, websiteId: apiId } : p
  })
}

/**
 * Pages. Nothing is fetched until a page calls `getPages()`.
 * Changes only write to the query cache until the API has a pages endpoint.
 */
export function usePages() {
  const queryClient = useQueryClient()
  const pagesQuerier = usePagesQuerier()
  const websitesQuerier = useWebsitesQuerier()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  function getPages() {
    const query = pagesQuerier.getPages()
    const { data: websiteDtos } = websitesQuerier.getWebsites()
    const pages = computed(() => linkSeedPages(query.data.value ?? [], websiteDtos.value ?? []))
    const pagesOf = (websiteId: string) => pages.value.filter((p) => p.websiteId === websiteId)
    return { ...query, pages, pagesOf }
  }

  /** Current pages from the cache, without subscribing. */
  function cachedPages() {
    return linkSeedPages(
      queryClient.getQueryData<Page[]>(pagesQueryKey) ?? [],
      queryClient.getQueryData<WebsiteDto[]>(getWebsitesQueryKey()) ?? [],
    )
  }

  function updatePages(update: (pages: Page[]) => Page[]) {
    queryClient.setQueryData<Page[]>(pagesQueryKey, (old) => update(old ?? []))
  }

  /** Bump the website's "updated" time in the websites cache when one of its pages changes. */
  function touchWebsite(id: string) {
    queryClient.setQueryData<WebsiteDto[]>(getWebsitesQueryKey(), (old) =>
      old?.map((d) => (d.id === id ? { ...d, updatedAt: new Date().toISOString() } : d)),
    )
  }

  /** Adds a page without logging or notifying, e.g. the Home page of a new website. */
  function addPage(page: Page) {
    updatePages((all) => [...all, page])
  }

  function createPage(websiteId: string, title: string, path?: string) {
    const page: Page = {
      id: crypto.randomUUID(),
      websiteId,
      title,
      path: path?.trim() || `/${slugify(title)}`,
      status: 'draft',
      author: seed.currentUser.name,
      updatedAt: Date.now(),
    }
    updatePages((all) => [page, ...all])
    touchWebsite(websiteId)
    logActivity(`You created the page ‘${title}’`, 'page')
    notify(`Page ‘${title}’ created`)
    return page
  }

  function setPageStatus(id: string, status: PublishStatus) {
    const page = cachedPages().find((p) => p.id === id)
    if (!page) return
    updatePages((all) => all.map((p) => (p.id === id ? { ...p, status, updatedAt: Date.now() } : p)))
    touchWebsite(page.websiteId)
    const verb = { live: 'published', draft: 'unpublished', scheduled: 'scheduled', archived: 'archived' }[status]
    logActivity(`You ${verb} ‘${page.title}’`, 'page')
    notify(`‘${page.title}’ ${verb}`)
  }

  function deletePage(id: string) {
    const page = cachedPages().find((p) => p.id === id)
    if (!page) return
    updatePages((all) => all.filter((p) => p.id !== id))
    notify(`‘${page.title}’ deleted`)
  }

  return {
    getPages,
    addPage,
    createPage,
    setPageStatus,
    deletePage,
  }
}
