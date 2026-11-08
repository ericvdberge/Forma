<script setup lang="ts">
import { cn } from '../utils/cn'

withDefaults(defineProps<{ tone?: 'accent' | 'muted' | 'light'; line?: boolean }>(), {
  tone: 'accent',
  line: true,
})

const tones = { accent: 'text-accent', muted: 'text-muted', light: 'text-warm-white/70' }
</script>

<template>
  <p :class="cn('flex items-center gap-3 text-xs font-bold uppercase tracking-[0.14em]', tones[tone])">
    <span v-if="line" class="h-px w-6 bg-current" aria-hidden="true" />
    <slot />
  </p>
</template>
