import { computed } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import type { Activity } from '@/data/types'
import { activityQueryKey, useActivityQuerier } from '@/queriers/useActivityQuerier'

/** Recent activity. Nothing is fetched until a page calls `getActivity()`. */
export function useActivity() {
  const queryClient = useQueryClient()
  const querier = useActivityQuerier()

  function getActivity() {
    const query = querier.getActivity()
    const activity = computed(() => query.data.value ?? [])
    return { ...query, activity }
  }

  function logActivity(message: string, kind: Activity['kind']) {
    queryClient.setQueryData<Activity[]>(activityQueryKey, (old) => [
      { id: crypto.randomUUID(), message, at: Date.now(), kind },
      ...(old ?? []),
    ])
  }

  return {
    getActivity,
    logActivity,
  }
}
