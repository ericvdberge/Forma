import { useQueryClient } from '@tanstack/vue-query'
import type { ContentIssueDto, PluginDto } from '@/api/generated'
import { getPluginsQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import { useToast } from '@/composables/useToast'
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

  const queryClient = useQueryClient()
  const { notify } = useToast()
  const { mutateAsync: putEnabled } = querier.setPluginEnabled()
  const { mutateAsync: remove, isPending: isDeleting } = querier.deletePlugin()

  const updateCache = (update: (list: PluginDto[]) => PluginDto[]) =>
    queryClient.setQueryData<PluginDto[]>(getPluginsQueryKey(), (old) => (old ? update(old) : old))

  /** 409 means the API can't write its plugins folder (e.g. a read-only Docker mount). */
  const failure = (e: unknown, what: string) =>
    (e as { status?: number })?.status === 409 || /writable/i.test(JSON.stringify(e ?? ''))
      ? `Couldn’t ${what}: the server can’t write to its plugins folder.`
      : `Couldn’t ${what}. Try again.`

  /** Turns a plugin on or off. The switch moves at once; it moves back if the server says no. */
  async function setEnabled(plugin: PluginDto, enabled: boolean) {
    updateCache((list) => list.map((p) => (p.id === plugin.id ? { ...p, enabled } : p)))
    try {
      const saved = await putEnabled({ path: { pluginId: plugin.id }, body: { enabled } })
      updateCache((list) => list.map((p) => (p.id === plugin.id ? saved : p)))
      notify(enabled ? `${plugin.name} is on` : `${plugin.name} is off`)
    } catch (e) {
      updateCache((list) => list.map((p) => (p.id === plugin.id ? { ...p, enabled: plugin.enabled } : p)))
      notify(failure(e, `turn ${plugin.name} ${enabled ? 'on' : 'off'}`), 'error')
    } finally {
      queryClient.invalidateQueries({ queryKey: getPluginsQueryKey() })
    }
  }

  /** Deletes a plugin's files on the server. */
  async function deletePlugin(plugin: PluginDto) {
    try {
      await remove({ path: { pluginId: plugin.id } })
      updateCache((list) => list.filter((p) => p.id !== plugin.id))
      notify(`${plugin.name} deleted`)
      return true
    } catch (e) {
      notify(failure(e, `delete ${plugin.name}`), 'error')
      return false
    } finally {
      queryClient.invalidateQueries({ queryKey: getPluginsQueryKey() })
    }
  }

  return { getPlugins, getPluginRuns: querier.getPluginRuns, setEnabled, deletePlugin, isDeleting }
}
