import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getPagesQueryKey, getWebsitesQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { PageDto, WebsiteDto } from '@/api/generated'
import type { Page, PageStatus } from '@/data/types'
import { allPagesQueryKey, usePagesQuerier, type GetPagesOptions } from '@/queriers/usePagesQuerier'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'
import { slugify } from '@/utils/slugify'

function toPage(dto: PageDto): Page {
  return {
    id: dto.id,
    websiteId: dto.websiteId,
    title: dto.title,
    slug: dto.slug,
    path: `/${dto.slug}`,
    status: dto.status === 'published' ? 'live' : 'draft',
    updatedAt: Date.parse(dto.updatedAt),
  }
}

/** Optional client-side filters on a website's pages. Empty values are ignored. */
export interface PageFilters {
  /** Title contains this text (case-insensitive). */
  title?: string
  /** Slug is exactly this. */
  slug?: string
  status?: PageStatus
}

export function filterPages(pages: Page[], { title, slug, status }: PageFilters) {
  const t = title?.trim().toLowerCase()
  const s = slug?.trim().replace(/^\//, '')
  return pages.filter(
    (p) => (!t || p.title.toLowerCase().includes(t)) && (!s || p.slug === s) && (!status || p.status === status),
  )
}

/**
 * Pages, read from GET /pages. Nothing is fetched until a page calls `getPages()`.
 * Changes only write to the query cache until the API has write endpoints, so they reset on reload.
 */
export function usePages() {
  const queryClient = useQueryClient()
  const querier = usePagesQuerier()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  /** Pages of one website, newest first. */
  function getPages(websiteId: MaybeRefOrGetter<string>, options?: GetPagesOptions) {
    const query = querier.getPages(websiteId, options)
    const pages = computed(() =>
      (query.data.value ?? [])
        .filter((d) => d.websiteId === toValue(websiteId))
        .map(toPage)
        .sort((a, b) => b.updatedAt - a.updatedAt),
    )
    return { ...query, pages }
  }

  function updateCachedPages(websiteId: string, update: (dtos: PageDto[]) => PageDto[]) {
    queryClient.setQueryData<PageDto[]>(getPagesQueryKey({ query: { websiteId } }), (old) => update(old ?? []))
  }

  function findCached(id: string) {
    for (const [, dtos] of queryClient.getQueriesData<PageDto[]>({ queryKey: allPagesQueryKey })) {
      const dto = dtos?.find((d) => d.id === id)
      if (dto) return dto
    }
  }

  /** Bump the website's "updated" time in the websites cache when one of its pages changes. */
  function touchWebsite(id: string) {
    queryClient.setQueryData<WebsiteDto[]>(getWebsitesQueryKey(), (old) =>
      old?.map((d) => (d.id === id ? { ...d, updatedAt: new Date().toISOString() } : d)),
    )
  }

  /** Adds a draft page without logging or notifying, e.g. the Home page of a new website. */
  function addPage(websiteId: string, title: string, slug: string) {
    const now = new Date().toISOString()
    const dto: PageDto = { id: crypto.randomUUID(), websiteId, title, slug, status: 'draft', createdAt: now, updatedAt: now }
    updateCachedPages(websiteId, (dtos) => [dto, ...dtos])
    return toPage(dto)
  }

  function createPage(websiteId: string, title: string, path?: string) {
    const page = addPage(websiteId, title, slugify(path?.trim() || title) || 'page')
    touchWebsite(websiteId)
    logActivity(`You created the page ‘${title}’`, 'page')
    notify(`Page ‘${title}’ created`)
    return page
  }

  function setPageStatus(id: string, status: PageStatus) {
    const dto = findCached(id)
    if (!dto) return
    const now = new Date().toISOString()
    updateCachedPages(dto.websiteId, (dtos) =>
      dtos.map((d) => (d.id === id ? { ...d, status: status === 'live' ? 'published' : 'draft', updatedAt: now } : d)),
    )
    touchWebsite(dto.websiteId)
    const verb = status === 'live' ? 'published' : 'unpublished'
    logActivity(`You ${verb} ‘${dto.title}’`, 'page')
    notify(`‘${dto.title}’ ${verb}`)
  }

  function deletePage(id: string) {
    const dto = findCached(id)
    if (!dto) return
    updateCachedPages(dto.websiteId, (dtos) => dtos.filter((d) => d.id !== id))
    notify(`‘${dto.title}’ deleted`)
  }

  return {
    getPages,
    addPage,
    createPage,
    setPageStatus,
    deletePage,
  }
}
