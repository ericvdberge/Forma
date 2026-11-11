<script setup lang="ts">
import type { Component } from 'vue'
import { Image } from 'lucide-vue-next'
import { cn } from '../utils/cn'

/** Small frosted label laid over a photo, e.g. "JPG · 2.4 MB". */
withDefaults(
  defineProps<{
    icon?: Component | null
    tone?: 'dark' | 'light'
    size?: 'sm' | 'md'
  }>(),
  { icon: () => Image, tone: 'dark', size: 'sm' },
)
</script>

<template>
  <span
    :class="
      cn(
        'inline-flex items-center gap-1.5 rounded-full font-medium tabular-nums backdrop-blur-md',
        tone === 'dark' ? 'bg-charcoal/55 text-white ring-1 ring-white/10' : 'bg-white/80 text-charcoal ring-1 ring-white/60 shadow-soft',
        size === 'sm' ? 'h-6 px-2.5 text-[0.6875rem]' : 'h-9 px-3.5 text-[0.8125rem]',
      )
    "
  >
    <component :is="icon" v-if="icon" :class="size === 'sm' ? 'size-3' : 'size-4'" :stroke-width="1.8" aria-hidden="true" />
    <slot />
  </span>
</template>
