<script setup lang="ts">
import { cn } from '../utils/cn'

/** Thin progress bar. Leave `value` out for an indeterminate shimmer. */
withDefaults(
  defineProps<{
    /** 0–100 */
    value?: number | null
    label?: string
    tone?: 'positive' | 'accent' | 'danger'
  }>(),
  { value: null, label: 'Progress', tone: 'positive' },
)
const tones = { positive: 'bg-positive', accent: 'bg-accent', danger: 'bg-danger' }
</script>

<template>
  <div
    role="progressbar"
    :aria-label="label"
    aria-valuemin="0"
    aria-valuemax="100"
    :aria-valuenow="value ?? undefined"
    class="relative h-1 w-full overflow-hidden rounded-full bg-charcoal/[0.08]"
  >
    <div
      v-if="value != null"
      :class="cn('h-full rounded-full transition-[width] duration-300 ease-out', tones[tone])"
      :style="{ width: `${Math.max(0, Math.min(100, value))}%` }"
    />
    <div v-else :class="cn('absolute inset-y-0 w-1/3 rounded-full animate-indeterminate', tones[tone])" />
  </div>
</template>

