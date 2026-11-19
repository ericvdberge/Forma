import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { getPluginRunsOptions, getPluginsOptions } from '@/api/generated/@tanstack/vue-query.gen'

/** TanStack Query calls for the plugins API. Call the sub-functions during component setup. */
export function usePluginsQuerier() {
  /** GET /plugins: every installed plugin, including ones that couldn't be loaded. */
  function getPlugins() {
    return useQuery(getPluginsOptions())
  }

  /** GET /plugins/{id}/runs: the last 25 runs, newest first. Off while `pluginId` is empty. */
  function getPluginRuns(pluginId: MaybeRefOrGetter<string | null | undefined>) {
    return useQuery(
      computed(() => ({
        ...getPluginRunsOptions({ path: { pluginId: toValue(pluginId) ?? '' } }),
        enabled: !!toValue(pluginId),
        refetchInterval: 5_000,
      })),
    )
  }

  return { getPlugins, getPluginRuns }
}
