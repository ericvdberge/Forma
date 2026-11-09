<script setup lang="ts">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

/** Full-width outlined action with a leading icon, for action lists ("Download", "Copy link", …). */
withDefaults(
  defineProps<{
    icon: Component
    /** Renders an <a> when set */
    href?: string
    download?: string
    target?: string
    tone?: 'default' | 'danger'
    size?: 'sm' | 'md'
    disabled?: boolean
  }>(),
  { tone: 'default', size: 'md', disabled: false },
)

const classes = (tone: string, size: string) =>
  cn(
    'flex w-full items-center gap-3 rounded-xl bg-white text-left font-medium ring-1 ring-charcoal/[0.09] transition-colors duration-200',
    'hover:bg-warm-white hover:ring-charcoal/25 disabled:pointer-events-none disabled:opacity-50',
    size === 'sm' ? 'h-9 px-3.5 text-[0.8125rem]' : 'h-11 px-4 text-sm',
    tone === 'danger' ? 'text-danger hover:ring-danger/40' : 'text-charcoal',
  )
</script>

<template>
  <a v-if="href" :href="href" :download="download" :target="target" :rel="target === '_blank' ? 'noopener' : undefined" :class="classes(tone, size)">
    <component :is="icon" class="size-4 shrink-0" :stroke-width="1.7" aria-hidden="true" />
    <slot />
  </a>
  <button v-else type="button" :disabled="disabled" :class="classes(tone, size)">
    <component :is="icon" class="size-4 shrink-0" :stroke-width="1.7" aria-hidden="true" />
    <slot />
  </button>
</template>
