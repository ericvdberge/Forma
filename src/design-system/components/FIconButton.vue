<script setup lang="ts">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    icon: Component
    /** Accessible name, also used as tooltip */
    label: string
    /** `glass` sits on top of photos; `outline` is a hairline ring */
    variant?: 'ghost' | 'surface' | 'dark' | 'glass' | 'outline'
    size?: 'sm' | 'md' | 'lg'
    shape?: 'rounded' | 'circle'
    /** Small indicator dot, e.g. unread notifications */
    dot?: boolean
    active?: boolean
  }>(),
  { variant: 'ghost', size: 'md', shape: 'rounded', dot: false, active: false },
)

const variants = {
  ghost: 'text-charcoal/75 hover:bg-charcoal/5 hover:text-charcoal',
  surface: 'bg-white text-charcoal ring-1 ring-charcoal/[0.08] shadow-soft hover:ring-charcoal/20',
  dark: 'bg-charcoal text-warm-white hover:bg-ink',
  glass: 'bg-white/75 text-charcoal ring-1 ring-white/60 backdrop-blur-md shadow-soft hover:bg-white',
  outline: 'bg-white text-charcoal ring-1 ring-charcoal/10 hover:ring-charcoal/30',
}
const sizes = { sm: 'size-8', md: 'size-10', lg: 'size-12' }
const radii = { sm: 'rounded-lg', md: 'rounded-xl', lg: 'rounded-2xl' }
</script>

<template>
  <button
    type="button"
    :aria-label="label"
    :title="label"
    :class="
      cn(
        'grid shrink-0 place-items-center transition-colors duration-200 disabled:pointer-events-none disabled:opacity-40',
        variants[variant],
        sizes[size],
        shape === 'circle' ? 'rounded-full' : radii[size],
        dot && 'relative',
        active && 'bg-accent-50 text-accent hover:bg-accent-50 hover:text-accent',
      )
    "
  >
    <component :is="icon" :class="size === 'lg' ? 'size-[1.375rem]' : size === 'sm' ? 'size-4' : 'size-5'" :stroke-width="1.6" aria-hidden="true" />
    <span v-if="dot" class="absolute right-2 top-2 size-2 rounded-full bg-positive ring-2 ring-warm-white" aria-hidden="true" />
  </button>
</template>
