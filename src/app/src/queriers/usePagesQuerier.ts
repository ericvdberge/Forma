import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { getPagesOptions } from '@/api/generated/@tanstack/vue-query.gen'

/** Matches every cached GET /pages query, whatever website it is for. */
export const allPagesQueryKey = [{ _id: 'getPages' }] as const

export interface GetPagesOptions {
  /** When false, nothing is fetched (e.g. until the user starts searching). Defaults to true. */
  enabled?: MaybeRefOrGetter<boolean>
}

/** TanStack Query calls for the pages API. Call the sub-functions during component setup. */
export function usePagesQuerier() {
  /** GET /pages?websiteId=… Refetches when the (reactive) website id changes. */
  function getPages(websiteId: MaybeRefOrGetter<string>, options: GetPagesOptions = {}) {
    return useQuery(
      computed(() => ({
        ...getPagesOptions({ query: { websiteId: toValue(websiteId) } }),
        enabled: toValue(options.enabled) ?? true,
        // Local edits live in the cache until write endpoints exist; don't let a refetch overwrite them.
        staleTime: Infinity,
      })),
    )
  }

  return {
    getPages,
  }
}
