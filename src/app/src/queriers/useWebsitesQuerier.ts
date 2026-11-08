import { useQuery } from '@tanstack/vue-query'
import { getWebsitesOptions } from '@/api/generated/@tanstack/vue-query.gen'

/** TanStack Query calls for the websites API. Call the sub-functions during component setup. */
export function useWebsitesQuerier() {
  /** GET /websites. Every caller shares one cached query. */
  function getWebsites() {
    return useQuery({
      ...getWebsitesOptions(),
      // Local edits live in the cache until write endpoints exist; don't let a refetch overwrite them.
      staleTime: Infinity,
    })
  }

  return {
    getWebsites,
  }
}
