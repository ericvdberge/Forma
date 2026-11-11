<script setup lang="ts">
import type { Component } from 'vue'
import { ArrowDown, ArrowUp } from 'lucide-vue-next'
import { cn } from '../utils/cn'

/** Icon, big number, label and an optional trend. Line several up in an `FStatGroup`. */
withDefaults(
  defineProps<{
    icon: Component
    value: string
    label: string
    /** e.g. "12%" or "+3"; the arrow follows `trend` */
    change?: string
    trend?: 'up' | 'down'
    /** Screen-reader context for the change, e.g. "this week" */
    changeLabel?: string
  }>(),
  { trend: 'up' },
)
</script>

<template>
  <div class="flex min-w-0 items-center gap-3.5">
    <span class="grid size-12 shrink-0 place-items-center rounded-full bg-canvas text-charcoal">
      <component :is="icon" class="size-5" :stroke-width="1.6" aria-hidden="true" />
    </span>
    <div class="min-w-0">
      <div class="flex items-baseline gap-2.5">
        <p class="font-display text-[1.375rem] font-bold leading-tight tracking-[-0.03em] tabular-nums">{{ value }}</p>
        <p v-if="change" :class="cn('inline-flex items-center gap-0.5 text-xs font-medium', trend === 'up' ? 'text-positive' : 'text-danger')">
          <component :is="trend === 'up' ? ArrowUp : ArrowDown" class="size-3" aria-hidden="true" />{{ change }}
          <span v-if="changeLabel" class="sr-only">{{ changeLabel }}</span>
        </p>
      </div>
      <p class="truncate text-[0.8125rem] text-muted">{{ label }}</p>
    </div>
  </div>
</template>
