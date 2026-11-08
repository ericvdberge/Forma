<script setup lang="ts">
import { computed } from 'vue'
import { cn } from '../utils/cn'

const props = withDefaults(
  defineProps<{
    name: string
    /** Optional image; initials are shown otherwise */
    src?: string
    size?: 'xs' | 'sm' | 'md' | 'lg'
    /** Adds a light ring so stacked avatars separate cleanly */
    ring?: boolean
  }>(),
  { size: 'md', ring: false },
)

const sizes = { xs: 'size-6 text-[0.5625rem]', sm: 'size-8 text-[0.6875rem]', md: 'size-10 text-sm', lg: 'size-14 text-lg' }
// Warm, on-brand fills picked deterministically from the name.
const fills = ['bg-stone text-charcoal', 'bg-sage text-charcoal', 'bg-charcoal text-warm-white', 'bg-accent-100 text-accent-700', 'bg-canvas text-charcoal']

const initials = computed(() =>
  props.name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]!.toUpperCase())
    .join(''),
)
const fill = computed(() => fills[[...props.name].reduce((a, c) => a + c.charCodeAt(0), 0) % fills.length])
</script>

<template>
  <span
    :class="
      cn(
        'inline-grid shrink-0 place-items-center overflow-hidden rounded-full font-bold tracking-[-0.02em] select-none',
        sizes[size],
        !src && fill,
        ring && 'ring-2 ring-white',
      )
    "
    role="img"
    :aria-label="name"
  >
    <img v-if="src" :src="src" alt="" class="size-full object-cover" />
    <span v-else aria-hidden="true">{{ initials }}</span>
  </span>
</template>
