import { computed } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getWebsitesQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { WebsiteDto } from '@/api/generated'
import { currentUser } from '@/data/seed'
import type { Website } from '@/data/types'
import { useWebsitesQuerier } from '@/queriers/useWebsitesQuerier'
import { useActivity } from '@/composables/useActivity'
import { usePages } from '@/composables/usePages'
import { useToast } from '@/composables/useToast'
import { slugify } from '@/utils/slugify'

/**
 * Websites. Nothing is fetched until a page calls `getWebsites()`.
 * Create/update only write to the query cache until the API has write endpoints, so they reset on reload.
 */
export function useWebsites() {
  const queryClient = useQueryClient()
  const querier = useWebsitesQuerier()
  const { addPage } = usePages()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  function getWebsites() {
    const query = querier.getWebsites()

    const websites = computed<Website[]>(() =>
      (query.data.value ?? []).map((dto, index) => {
        return {
          id: dto.id,
          name: dto.name,
          domain: dto.domain,
          description: dto.description,
          status: dto.status,
          // Presentation-only fields the API doesn't model (yet).
          tone: index % 2 ? 'dark' : 'light',
          collaborators: [currentUser.name],
          updatedAt: Date.parse(dto.updatedAt),
        }
      }),
    )

    return { ...query, websites }
  }

  /** Current websites from the cache, without subscribing. */
  function cachedWebsites() {
    return queryClient.getQueryData<WebsiteDto[]>(getWebsitesQueryKey()) ?? []
  }

  function updateCache(update: (dtos: WebsiteDto[]) => WebsiteDto[]) {
    queryClient.setQueryData<WebsiteDto[]>(getWebsitesQueryKey(), (old) => update(old ?? []))
  }

  function updateWebsite(id: string, patch: Partial<Pick<Website, 'name' | 'domain' | 'description' | 'status'>>) {
    const site = cachedWebsites().find((d) => d.id === id)
    if (!site) return
    const wentLive = patch.status === 'live' && site.status !== 'live'
    updateCache((dtos) => dtos.map((d) => (d.id === id ? { ...d, ...patch, updatedAt: new Date().toISOString() } : d)))
    const name = patch.name ?? site.name
    logActivity(wentLive ? `You published ${name}` : `You updated ${name}`, 'website')
    notify('Website saved')
  }

  /** Creates a website with a draft Home page. */
  function createWebsite(name: string) {
    const now = new Date().toISOString()
    const dto: WebsiteDto = {
      id: crypto.randomUUID(),
      name,
      domain: `${slugify(name) || 'site'}.forma.dev`,
      description: '',
      status: 'draft',
      createdAt: now,
      updatedAt: now,
    }
    addPage(dto.id, 'Home', 'home')
    updateCache((dtos) => [...dtos, dto])
    logActivity(`You created ${name}`, 'website')
    notify(`${name} created`)
    return dto
  }

  return {
    getWebsites,
    updateWebsite,
    createWebsite,
  }
}
