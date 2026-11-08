<script setup lang="ts">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    icon: Component
    title: string
    description: string
    align?: 'center' | 'start'
    tone?: 'dark' | 'light'
  }>(),
  { align: 'center', tone: 'dark' },
)
</script>

<template>
  <div :class="cn('flex flex-col gap-1.5', align === 'center' ? 'items-center text-center' : 'items-start text-left')">
    <component
      :is="icon"
      :class="cn('mb-2 size-6', tone === 'dark' ? 'text-charcoal' : 'text-warm-white')"
      :stroke-width="1.5"
      aria-hidden="true"
    />
    <h3 :class="cn('font-display text-lg font-medium tracking-[-0.02em]', tone === 'dark' ? 'text-charcoal' : 'text-warm-white')">
      {{ title }}
    </h3>
    <p :class="cn('text-sm', tone === 'dark' ? 'text-muted' : 'text-warm-white/60')">{{ description }}</p>
  </div>
</template>
