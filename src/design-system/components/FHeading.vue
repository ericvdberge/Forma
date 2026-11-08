<script setup lang="ts">
import { computed } from 'vue'
import { cn } from '../utils/cn'

const props = withDefaults(
  defineProps<{
    as?: 'h1' | 'h2' | 'h3' | 'h4' | 'p'
    size?: 'display' | 'xl' | 'lg' | 'md' | 'sm'
    tone?: 'dark' | 'light'
  }>(),
  { as: 'h2', size: 'lg', tone: 'dark' },
)

const sizes = {
  display: 'text-[clamp(2.75rem,7vw,5.5rem)] leading-[0.95] tracking-[-0.05em]',
  xl: 'text-[clamp(2.25rem,5vw,3.75rem)] leading-[1] tracking-[-0.045em]',
  lg: 'text-[clamp(2rem,4vw,3rem)] leading-[1.02] tracking-[-0.04em]',
  md: 'text-2xl leading-tight tracking-[-0.03em]',
  sm: 'text-lg leading-snug tracking-[-0.02em]',
}

const classes = computed(() =>
  cn('font-display font-bold text-balance', sizes[props.size], props.tone === 'dark' ? 'text-charcoal' : 'text-warm-white'),
)
</script>

<template>
  <component :is="as" :class="classes"><slot /></component>
</template>
