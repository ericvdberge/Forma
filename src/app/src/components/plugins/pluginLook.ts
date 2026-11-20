import { Bell, Puzzle, ShieldCheck, Trash2, TriangleAlert } from 'lucide-vue-next'
import type { FMenuItem } from '@forma/design-system'
import type { PluginDto } from '@/api/generated'
import { HOOK_EVENTS } from '@/composables/usePlugins'

/** How a plugin looks in its card and in its list row, so both views agree. */

const isHook = (p: PluginDto) => p.events.some((e) => HOOK_EVENTS.has(e))

export const pluginIcon = (p: PluginDto) => (!p.loaded ? TriangleAlert : isHook(p) ? ShieldCheck : p.events.length ? Bell : Puzzle)

export const pluginIconTone = (p: PluginDto) =>
  !p.loaded ? 'bg-danger/10 text-danger' : isHook(p) ? 'bg-sage/20 text-positive' : 'bg-accent-50 text-accent-600'

export type PluginStatus = 'active' | 'off' | 'broken'
export const pluginStatus = (p: PluginDto): PluginStatus => (!p.loaded ? 'broken' : p.enabled ? 'active' : 'off')

export interface PluginActions {
  open: (p: PluginDto) => void
  copyId: (p: PluginDto) => void
  delete: (p: PluginDto) => void
}

/** The "More" menu of a plugin. */
export const pluginMenu = (p: PluginDto, on: PluginActions): FMenuItem[] => [
  { label: 'View runs', onSelect: () => on.open(p) },
  { label: 'Copy plugin id', onSelect: () => on.copyId(p) },
  { label: 'Delete plugin', icon: Trash2, danger: true, onSelect: () => on.delete(p) },
]
