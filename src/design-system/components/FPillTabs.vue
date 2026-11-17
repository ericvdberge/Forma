<script setup lang="ts" generic="T extends string">
import { cn } from '../utils/cn'

/** Outlined pill filters; the selected one fills dark. A `tablist`, so arrow keys move between pills. */
const model = defineModel<T>({ required: true })
defineProps<{
  options: Array<{ label: string; value: T; count?: number }>
  label: string
}>()

function onKeydown(e: KeyboardEvent) {
  if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return
  const tabs = Array.from((e.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('[role=tab]'))
  const i = tabs.indexOf(document.activeElement as HTMLElement)
  const next = tabs[(i + (e.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length]
  next?.focus()
  next?.click()
}
</script>

<template>
  <div role="tablist" :aria-label="label" class="flex gap-2 overflow-x-auto [scrollbar-width:none]" @keydown="onKeydown">
    <button
      v-for="o in options"
      :key="o.value"
      type="button"
      role="tab"
      :aria-selected="model === o.value"
      :tabindex="model === o.value ? 0 : -1"
      :class="
        cn(
          'inline-flex h-10 shrink-0 items-center gap-1.5 rounded-full px-[1.125rem] text-sm font-medium ring-1 transition-colors duration-200',
          model === o.value
            ? 'bg-charcoal text-warm-white ring-charcoal'
            : 'bg-white/70 text-charcoal ring-charcoal/[0.08] hover:ring-charcoal/25',
        )
      "
      @click="model = o.value"
    >
      {{ o.label }}
      <span v-if="o.count != null" :class="cn('text-xs tabular-nums', model === o.value ? 'text-warm-white/60' : 'text-subtle')">{{ o.count }}</span>
    </button>
  </div>
</template>
