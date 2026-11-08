<script setup lang="ts">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    items: Array<{ icon: Component; label: string; active?: boolean }>
    orientation?: 'vertical' | 'horizontal'
  }>(),
  { orientation: 'vertical' },
)
</script>

<template>
  <div
    role="toolbar"
    :aria-orientation="orientation"
    :class="
      cn(
        'inline-flex gap-1 rounded-2xl bg-white/90 p-1.5 ring-1 ring-charcoal/[0.06] shadow-card backdrop-blur',
        orientation === 'vertical' ? 'flex-col' : 'flex-row',
      )
    "
  >
    <button
      v-for="item in items"
      :key="item.label"
      type="button"
      :aria-label="item.label"
      :title="item.label"
      :class="
        cn(
          'grid size-10 place-items-center rounded-xl transition-colors',
          item.active ? 'bg-accent-50 text-accent' : 'text-charcoal/70 hover:bg-charcoal/5 hover:text-charcoal',
        )
      "
    >
      <component :is="item.icon" class="size-[1.125rem]" :stroke-width="1.6" />
    </button>
  </div>
</template>
