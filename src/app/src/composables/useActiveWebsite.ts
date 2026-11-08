import { computed, readonly, ref, watch } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getWebsitesQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { WebsiteDto } from '@/api/generated'
import { useWebsites } from '@/composables/useWebsites'
import { useToast } from '@/composables/useToast'

/** The website the CMS is working on, remembered per browser. */
const ACTIVE_KEY = 'forma.activeWebsite'

const activeWebsiteId = ref(readStored())

function readStored(): string {
  try {
    return localStorage.getItem(ACTIVE_KEY) ?? ''
  } catch {
    return '' /* storage unavailable */
  }
}

watch(activeWebsiteId, (id) => {
  try {
    if (id) localStorage.setItem(ACTIVE_KEY, id)
  } catch {
    /* storage unavailable */
  }
})

/** Nothing is fetched until a page calls `getActiveWebsite()`. */
export function useActiveWebsite() {
  const queryClient = useQueryClient()
  const { getWebsites } = useWebsites()
  const { notify } = useToast()

  /** Only read once websites have loaded — AppShell holds the views back until then. */
  function getActiveWebsite() {
    const { websites } = getWebsites()
    return computed(() => websites.value.find((w) => w.id === activeWebsiteId.value) ?? websites.value[0]!)
  }

  function setActiveWebsite(id: string, { silent = false } = {}) {
    activeWebsiteId.value = id
    const site = queryClient.getQueryData<WebsiteDto[]>(getWebsitesQueryKey())?.find((d) => d.id === id)
    if (site && !silent) notify(`Switched to ${site.name}`)
  }

  return {
    activeWebsiteId: readonly(activeWebsiteId),
    getActiveWebsite,
    setActiveWebsite,
  }
}
