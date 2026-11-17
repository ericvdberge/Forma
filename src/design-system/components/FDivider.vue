<script setup lang="ts">
import { cn } from '../utils/cn'

const { orientation = 'horizontal', tone = 'dark' } = defineProps<{
  orientation?: 'horizontal' | 'vertical'
  tone?: 'dark' | 'light'
}>()
</script>

<template>
  <span
  role="separator"
  :aria-orientation="orientation"
  :class="
    cn(
      'block shrink-0',
      orientation === 'horizontal' ? 'h-px w-full' : 'w-px self-stretch',
      tone === 'dark' ? 'bg-charcoal/[0.08]' : 'bg-white/10',
    )
  "
  />
</template>
