<script setup lang="ts">
import { computed } from 'vue'
import { cn } from '../utils/cn'

const props = withDefaults(
  defineProps<{
    /** `wordmark` = "forma.", `mark` = "f." monogram */
    variant?: 'wordmark' | 'mark'
    tone?: 'dark' | 'light'
    size?: 'sm' | 'md' | 'lg' | 'xl'
    /** Colour the full stop in the accent blue */
    accentDot?: boolean
  }>(),
  { variant: 'wordmark', tone: 'dark', size: 'md', accentDot: false },
)

const sizes = { sm: 'text-xl', md: 'text-[1.75rem]', lg: 'text-5xl', xl: 'text-7xl md:text-8xl' }
const classes = computed(() =>
  cn(
    'inline-flex items-baseline font-display font-bold leading-none tracking-[-0.06em] select-none',
    sizes[props.size],
    props.tone === 'dark' ? 'text-charcoal' : 'text-warm-white',
  ),
)
</script>

<template>
  <span :class="classes" role="img" aria-label="Forma">
    <span aria-hidden="true">{{ variant === 'wordmark' ? 'forma' : 'f' }}</span
    ><span aria-hidden="true" :class="accentDot && 'text-accent'">.</span>
  </span>
</template>
