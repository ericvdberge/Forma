import { useQuery } from '@tanstack/vue-query'
import * as seed from '@/data/seed'

export const activityQueryKey = ['activity'] as const

/** TanStack Query calls for recent activity. Served from seed data until the API has an endpoint. */
export function useActivityQuerier() {
  function getActivity() {
    return useQuery({
      queryKey: activityQueryKey,
      queryFn: async () => structuredClone(seed.activity),
      staleTime: Infinity,
      gcTime: Infinity,
    })
  }

  return {
    getActivity,
  }
}
