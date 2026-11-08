<script setup lang="ts">
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    tone?: 'neutral' | 'accent' | 'dark'
    /** Leading status dot */
    dot?: boolean
  }>(),
  { tone: 'neutral', dot: true },
)

const tones = {
  neutral: 'bg-white/70 text-charcoal ring-charcoal/[0.08] backdrop-blur',
  accent: 'bg-accent-50 text-accent-700 ring-accent-200',
  dark: 'bg-charcoal text-warm-white ring-white/10',
}
</script>

<template>
  <span
    :class="cn('inline-flex items-center gap-2 rounded-full px-3.5 py-1.5 text-[0.8125rem] font-medium ring-1 shadow-soft', tones[tone])"
  >
    <span v-if="dot" class="relative flex size-2.5" aria-hidden="true">
      <span class="absolute inset-0 animate-ping rounded-full bg-accent opacity-30" />
      <span class="relative size-2.5 rounded-full bg-accent" />
    </span>
    <slot />
  </span>
</template>
