<script setup lang="ts">
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    as?: string
    variant?: 'surface' | 'outline' | 'muted' | 'dark' | 'glass'
    padding?: 'none' | 'sm' | 'md' | 'lg'
    interactive?: boolean
  }>(),
  { as: 'div', variant: 'surface', padding: 'md', interactive: false },
)

const variants = {
  surface: 'bg-white ring-1 ring-charcoal/[0.06] shadow-card',
  outline: 'bg-transparent ring-1 ring-line',
  muted: 'bg-canvas ring-1 ring-charcoal/[0.04]',
  dark: 'bg-charcoal text-warm-white ring-1 ring-white/10',
  glass: 'bg-white/60 ring-1 ring-white/70 backdrop-blur-xl shadow-soft',
}
const paddings = { none: '', sm: 'p-4', md: 'p-6', lg: 'p-8 md:p-10' }
</script>

<template>
  <component
    :is="as"
    :class="
      cn(
        'relative overflow-hidden rounded-card',
        variants[variant],
        paddings[padding],
        interactive && 'transition-all duration-300 hover:-translate-y-1 hover:shadow-float',
      )
    "
  >
    <slot />
  </component>
</template>
