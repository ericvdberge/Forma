import { shallowRef, type Component } from 'vue'
import {
  BadgePercent,
  Box,
  CreditCard,
  Gift,
  Package,
  Puzzle,
  ShoppingBag,
  ShoppingCart,
  Sparkles,
  Star,
  Tag,
} from 'lucide-vue-next'
import type { PluginBlockDto, PluginDto } from '@/api/generated'
import type { BlockDefinition, Field } from './blocks'
import { defaultBlockStyle, newId } from './defaults'
import type { Block, PluginBlockValue } from './types'

/**
 * Blocks that installed plugins add to the studio (`blocks` in a plugin's manifest, served by GET /plugins).
 * A plugin block is stored as `{ type: 'plugin', props: { plugin, block, values } }` and drawn from the plugin's
 * template by PluginBlock.vue. Specification: docs/design/plugins.md ("Blocks").
 */
export interface PluginBlockInfo extends PluginBlockDto {
  pluginId: string
  pluginName: string
}

/** The plugin blocks of the loaded plugins, by `{pluginId}/{blockId}`. Filled by the studio from GET /plugins. */
export const pluginBlocks = shallowRef(new Map<string, PluginBlockInfo>())

export function setPluginBlocks(plugins: PluginDto[]) {
  const map = new Map<string, PluginBlockInfo>()
  for (const p of plugins) {
    if (!p.loaded) continue
    for (const b of p.blocks) map.set(`${p.id}/${b.id}`, { ...b, pluginId: p.id, pluginName: p.name })
  }
  pluginBlocks.value = map
}

export const findPluginBlock = (plugin: string, block: string) => pluginBlocks.value.get(`${plugin}/${block}`)

/** Icons a plugin block can pick by Lucide name; anything else gets the puzzle piece. */
const icons: Record<string, Component> = {
  'shopping-bag': ShoppingBag,
  'shopping-cart': ShoppingCart,
  tag: Tag,
  package: Package,
  box: Box,
  gift: Gift,
  'credit-card': CreditCard,
  'badge-percent': BadgePercent,
  star: Star,
  sparkles: Sparkles,
}
export const pluginBlockIcon = (name: string | null | undefined) => (name && icons[name]) || Puzzle

/** The plugin's fields as inspector fields; their values live under `props.values`. */
function toField(f: PluginBlockDto['fields'][number]): Field {
  const base = { key: `values.${f.key}`, label: f.label, advanced: f.advanced || undefined }
  switch (f.kind) {
    case 'number':
    case 'slider':
      return { ...base, kind: f.kind, min: f.min ?? 0, max: f.max ?? 100, step: f.step ?? undefined, unit: f.unit ?? undefined }
    case 'select':
    case 'segmented':
      return { ...base, kind: f.kind, options: f.options ?? [] }
    case 'toggle':
    case 'image':
    case 'color':
      return { ...base, kind: f.kind }
    case 'textarea':
    case 'url':
      return { ...base, kind: f.kind, placeholder: f.placeholder ?? undefined }
    default:
      return { ...base, kind: 'text', placeholder: f.placeholder ?? undefined }
  }
}

/** A block definition (label, icon, inspector fields) for a plugin block; a stand-in when its plugin isn't installed. */
export function pluginBlockDefinition(plugin: string, block: string): BlockDefinition<'plugin'> {
  const info = findPluginBlock(plugin, block)
  return {
    type: 'plugin',
    label: info?.label ?? 'Plugin block',
    icon: pluginBlockIcon(info?.icon),
    props: () => ({ plugin, block, values: info ? defaultValues(info) : {} }),
    fields: info?.fields.map(toField) ?? [],
  }
}

function defaultValues(info: PluginBlockInfo): Record<string, PluginBlockValue> {
  const values: Record<string, PluginBlockValue> = {}
  for (const f of info.fields) {
    const d = f.default as unknown
    if (typeof d === 'string' || typeof d === 'number' || typeof d === 'boolean') values[f.key] = d
    else values[f.key] = f.kind === 'toggle' ? false : f.kind === 'number' || f.kind === 'slider' ? (f.min ?? 0) : ''
  }
  return values
}

export function createPluginBlock(plugin: string, block: string): Block<'plugin'> {
  return { id: newId('b'), type: 'plugin', props: pluginBlockDefinition(plugin, block).props(), style: defaultBlockStyle() }
}

/* ── Template rendering ─────────────────────────────────────────────────────
 * The API refuses templates outside these rules when the plugin loads; they are applied again here, so the studio
 * never trusts a template or a value on its own. Mirrors PluginBlockTemplate in Forma.Infrastructure. */

const ALLOWED_TAGS = new Set(['DIV', 'SPAN', 'P', 'H2', 'H3', 'H4', 'STRONG', 'EM', 'SMALL', 'S', 'BR', 'HR', 'IMG', 'A', 'UL', 'OL', 'LI', 'FIGURE', 'FIGCAPTION'])
const ALLOWED_ATTRIBUTES = new Set(['style', 'href', 'src', 'alt', 'title', 'data-if'])
const SLOT = /\{\{\s*([a-zA-Z][a-zA-Z0-9_]{0,39})\s*\}\}/g
const SINGLE_SLOT = /^\{\{\s*([a-zA-Z][a-zA-Z0-9_]{0,39})\s*\}\}$/
const UNSAFE_CSS = /url\s*\(|@import|expression\s*\(|\\|javascript:|</i
/** Values allowed into a `style` slot: colours and plain numbers. */
const CSS_VALUE = /^(#[0-9a-f]{3,8}|-?\d+(\.\d+)?)$/i

const text = (v: PluginBlockValue | undefined) => (v === undefined || v === null ? '' : String(v))
const present = (v: PluginBlockValue | undefined) => (typeof v === 'string' ? v.trim().length > 0 : typeof v === 'number' ? true : !!v)

function safeHref(url: string) {
  return /^(https?:|mailto:|tel:)/i.test(url) || url.startsWith('#') || (url.startsWith('/') && !url.startsWith('//'))
}
function safeSrc(url: string) {
  return /^https:/i.test(url) || (url.startsWith('/') && !url.startsWith('//'))
}

/**
 * Builds the block's DOM from its template and values. Tags and attributes outside the allowlist are removed,
 * `data-if="key"` drops an element when that value is empty or off, and slots in `href`/`src`/`style` only take
 * values that are safe there.
 */
export function renderPluginTemplate(template: string, values: Record<string, PluginBlockValue>): DocumentFragment {
  const doc = new DOMParser().parseFromString(`<!doctype html><body>${template}</body>`, 'text/html')
  const fill = (s: string) => s.replace(SLOT, (_, key: string) => text(values[key]))

  for (const el of Array.from(doc.body.querySelectorAll('*'))) {
    if (!el.isConnected) continue
    if (!ALLOWED_TAGS.has(el.tagName)) {
      el.remove()
      continue
    }
    const condition = el.getAttribute('data-if')
    if (condition !== null && !present(values[condition])) {
      el.remove()
      continue
    }
    for (const { name, value } of Array.from(el.attributes)) {
      if (!ALLOWED_ATTRIBUTES.has(name) || name === 'data-if') {
        el.removeAttribute(name)
        continue
      }
      if (name === 'href' || name === 'src') {
        const slot = SINGLE_SLOT.exec(value)
        const url = slot ? text(values[slot[1]!]).trim() : value
        const ok = name === 'href' ? el.tagName === 'A' && safeHref(url) : el.tagName === 'IMG' && safeSrc(url)
        if (ok && url) el.setAttribute(name, url)
        else el.removeAttribute(name)
      } else if (name === 'style') {
        const css = value.replace(SLOT, (_, key: string) => {
          const v = text(values[key]).trim()
          return CSS_VALUE.test(v) ? v : ''
        })
        if (UNSAFE_CSS.test(css)) el.removeAttribute(name)
        else el.setAttribute(name, css)
      } else {
        el.setAttribute(name, fill(value))
      }
    }
  }

  // Slots in text become plain text (textContent), never markup.
  const walker = doc.createTreeWalker(doc.body, NodeFilter.SHOW_TEXT)
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    if (SLOT.test(node.textContent ?? '')) node.textContent = fill(node.textContent ?? '')
    SLOT.lastIndex = 0
  }

  const fragment = document.createDocumentFragment()
  fragment.append(...Array.from(doc.body.childNodes).map((n) => document.importNode(n, true)))
  return fragment
}
