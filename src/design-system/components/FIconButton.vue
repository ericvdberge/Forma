<script setup lang="ts">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    icon: Component
    /** Accessible name, also used as tooltip */
    label: string
    variant?: 'ghost' | 'surface' | 'dark'
    size?: 'sm' | 'md'
    /** Small indicator dot, e.g. unread notifications */
    dot?: boolean
    active?: boolean
  }>(),
  { variant: 'ghost', size: 'md', dot: false, active: false },
)

const variants = {
  ghost: 'text-charcoal/75 hover:bg-charcoal/5 hover:text-charcoal',
  surface: 'bg-white text-charcoal ring-1 ring-charcoal/[0.08] shadow-soft hover:ring-charcoal/20',
  dark: 'bg-charcoal text-warm-white hover:bg-ink',
}
const sizes = { sm: 'size-8 rounded-lg', md: 'size-10 rounded-xl' }
</script>

<template>
  <button
    type="button"
    :aria-label="label"
    :title="label"
    :class="
      cn(
        'relative grid shrink-0 place-items-center transition-colors duration-200',
        variants[variant],
        sizes[size],
        active && 'bg-accent-50 text-accent hover:bg-accent-50 hover:text-accent',
      )
    "
  >
    <component :is="icon" :class="size === 'sm' ? 'size-4' : 'size-5'" :stroke-width="1.6" aria-hidden="true" />
    <span v-if="dot" class="absolute right-2 top-2 size-2 rounded-full bg-positive ring-2 ring-warm-white" aria-hidden="true" />
  </button>
</template>
