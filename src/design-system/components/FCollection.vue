<script setup lang="ts" generic="T">
import { cn } from '../utils/cn'
import type { FCollectionView } from './FViewSwitch.vue'

/**
 * The same items as cards or as a list (a table), switched with `FViewSwitch`.
 *
 * - Cards: the `card` slot draws one item; the grid is responsive (`gridClass` changes the columns).
 * - List: one row per item, one cell per column. A cell shows `item[key]` unless a `cell-{key}` slot draws it.
 *   Clicking a row emits `open`; cells with `stop` (e.g. a menu or a switch) don't.
 */
export interface FCollectionColumn {
  key: string
  label: string
  /** Keeps the header for screen readers only, e.g. an actions column. */
  hideLabel?: boolean
  /** Clicks inside the cell don't open the row: for menus, switches and buttons. */
  stop?: boolean
  align?: 'left' | 'right'
  /** Extra classes for the cell, e.g. a width. */
  class?: string
}

const {
  view,
  items,
  itemKey,
  columns,
  label,
  selectedKey,
  gridClass = 'grid-cols-1 sm:grid-cols-2 xl:grid-cols-3',
  minTableWidth = '640px',
} = defineProps<{
  view: FCollectionView
  items: T[]
  /** A stable key per item. */
  itemKey: (item: T) => string
  /** List columns, in order. */
  columns: FCollectionColumn[]
  /** Accessible name of the collection, e.g. "Images". */
  label: string
  /** The item shown as selected (its row is highlighted). */
  selectedKey?: string | null
  gridClass?: string
  /** The table scrolls sideways below this width instead of squashing its columns. */
  minTableWidth?: string
}>()
const emit = defineEmits<{ open: [item: T] }>()

defineSlots<
  {
    card(props: { item: T }): unknown
  } & { [K in `cell-${string}`]?: (props: { item: T }) => unknown }
>()

const cellValue = (item: T, key: string) => {
  const v = (item as Record<string, unknown>)[key]
  return v === null || v === undefined ? '' : String(v)
}
</script>

<template>
  <ul v-if="view === 'grid'" :class="cn('grid gap-4', gridClass)" :aria-label="label">
    <li v-for="item in items" :key="itemKey(item)" class="min-w-0">
      <slot name="card" :item="item" />
    </li>
  </ul>

  <div v-else class="overflow-x-auto rounded-panel bg-white ring-1 ring-charcoal/[0.06] shadow-soft">
    <table class="w-full text-left text-sm" :style="{ minWidth: minTableWidth }" :aria-label="label">
      <thead class="text-xs text-muted">
        <tr class="border-b border-charcoal/[0.06]">
          <th
            v-for="c in columns"
            :key="c.key"
            scope="col"
            :class="cn('px-5 py-3 font-medium', c.align === 'right' && 'text-right', c.class)"
          >
            <span :class="c.hideLabel && 'sr-only'">{{ c.label }}</span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="item in items"
          :key="itemKey(item)"
          :class="
            cn(
              'cursor-pointer border-b border-charcoal/[0.04] transition-colors last:border-0 hover:bg-warm-white',
              selectedKey != null && selectedKey === itemKey(item) && 'bg-accent-50/60 hover:bg-accent-50',
            )
          "
          @click="emit('open', item)"
        >
          <td
            v-for="c in columns"
            :key="c.key"
            :class="cn('px-5 py-3 align-middle', c.align === 'right' && 'text-right', c.class)"
            @click="c.stop && $event.stopPropagation()"
          >
            <slot :name="`cell-${c.key}`" :item="item">{{ cellValue(item, c.key) }}</slot>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
