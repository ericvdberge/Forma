<script setup lang="ts">
import { cn } from '../utils/cn'

/** Placeholder block shown while content loads. Size it with classes (h-*, w-*); shape it with `rounded`. */
withDefaults(
  defineProps<{
    rounded?: 'none' | 'md' | 'lg' | 'xl' | '2xl' | 'full'
    tone?: 'default' | 'light'
  }>(),
  { rounded: 'lg', tone: 'default' },
)

const radii = {
  none: 'rounded-none',
  md: 'rounded-md',
  lg: 'rounded-lg',
  xl: 'rounded-xl',
  '2xl': 'rounded-2xl',
  full: 'rounded-full',
}
</script>

<template>
  <div
    aria-hidden="true"
    :class="cn('animate-pulse motion-reduce:animate-none', radii[rounded], tone === 'light' ? 'bg-white/70' : 'bg-charcoal/[0.07]')"
  />
</template>
