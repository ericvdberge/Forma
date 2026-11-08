import { useQuery } from '@tanstack/vue-query'
import * as seed from '@/data/seed'

export const pagesQueryKey = ['pages'] as const

/** TanStack Query calls for pages. Served from seed data until the API has a pages endpoint. */
export function usePagesQuerier() {
  function getPages() {
    return useQuery({
      queryKey: pagesQueryKey,
      queryFn: async () => structuredClone(seed.pages),
      staleTime: Infinity,
      gcTime: Infinity,
    })
  }

  return {
    getPages,
  }
}
