<script setup lang="ts">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

/**
 * Thumbnail, title and a muted line, with an optional trailing slot (a menu, a chevron…).
 * Renders a link with `href`, a button when it has a click listener, otherwise a plain row.
 */
const props = withDefaults(
  defineProps<{
    title: string
    subtitle?: string
    /** Image URL; falls back to `icon` on a canvas square */
    src?: string
    icon?: Component
    /** Small label above the title, e.g. "In website" */
    eyebrow?: string
    href?: string
    interactive?: boolean
    bordered?: boolean
  }>(),
  { interactive: false, bordered: false },
)
const tag = props.href ? 'a' : props.interactive ? 'button' : 'div'
</script>

<template>
  <component
    :is="tag"
    :href="href"
    :type="tag === 'button' ? 'button' : undefined"
    :class="
      cn(
        'flex w-full items-center gap-3 rounded-2xl p-2 text-left',
        (href || interactive) && 'transition-colors hover:bg-canvas/70',
        bordered && 'bg-white ring-1 ring-charcoal/[0.08] p-2.5',
      )
    "
  >
    <span class="grid h-12 w-14 shrink-0 place-items-center overflow-hidden rounded-xl bg-canvas text-charcoal/70">
      <img v-if="src" :src="src" alt="" loading="lazy" class="size-full object-cover" />
      <component :is="icon" v-else-if="icon" class="size-5" :stroke-width="1.6" aria-hidden="true" />
    </span>
    <span class="min-w-0 flex-1">
      <span v-if="eyebrow" class="block text-[0.6875rem] text-muted">{{ eyebrow }}</span>
      <span class="block truncate text-sm font-medium text-charcoal">{{ title }}</span>
      <span v-if="subtitle" class="block truncate text-xs text-muted">{{ subtitle }}</span>
    </span>
    <slot name="trailing" />
  </component>
</template>
