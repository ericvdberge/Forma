import type { ContentIssueDto, PluginDto } from '@/api/generated'
import { usePluginsQuerier } from '@/queriers/usePluginsQuerier'

/** Hook events run during a request and can return issues; the others are notifications that run afterwards. */
export const HOOK_EVENTS = new Set(['page.check'])

/** What each event means, for people. */
export const EVENT_LABELS: Record<string, string> = {
  'page.check': 'Checks pages',
  'page.draftSaved': 'After a draft save',
  'page.published': 'After publishing',
}

export const eventLabel = (event: string) => EVENT_LABELS[event] ?? event

/**
 * The plugin that raised an issue, if any. Plugin issue codes are namespaced `{pluginId}:{code}`;
 * a crashing hook is reported by the API as `plugin.failed`.
 */
export function issuePluginId(issue: Pick<ContentIssueDto, 'code'>): string | null {
  const i = issue.code.indexOf(':')
  return i > 0 ? issue.code.slice(0, i) : null
}

/** Installed plugins and their runs (docs/design/plugins.md). */
export function usePlugins() {
  const querier = usePluginsQuerier()

  function getPlugins() {
    const query = querier.getPlugins()
    /** Name of the plugin with this id, falling back to the id. */
    const pluginName = (id: string) => query.data.value?.find((p: PluginDto) => p.id === id)?.name ?? id
    return { ...query, pluginName }
  }

  return { getPlugins, getPluginRuns: querier.getPluginRuns }
}
