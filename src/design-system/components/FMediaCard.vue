<script setup lang="ts">
import { ref, type Component } from 'vue'
import { ImageOff } from 'lucide-vue-next'
import FMediaChip from './FMediaChip.vue'
import { cn } from '../utils/cn'

/**
 * A photo tile: the image fills the card, with a frosted meta chip bottom-left and room for
 * actions (e.g. an `FMenu`) top-right. The whole card is one button.
 */
withDefaults(
  defineProps<{
    src: string
    alt: string
    /** Shown in the chip, e.g. "JPG · 2.4 MB" */
    meta?: string
    metaIcon?: Component
    /** Tailwind aspect class */
    aspect?: string
    selected?: boolean
  }>(),
  { aspect: 'aspect-[16/10]', selected: false },
)
const emit = defineEmits<{ open: [] }>()
const failed = ref(false)
</script>

<template>
  <div
    :class="
      cn(
        'group relative overflow-hidden rounded-card bg-canvas ring-1 transition-shadow duration-300 hover:shadow-card',
        selected ? 'ring-2 ring-accent' : 'ring-charcoal/[0.06]',
        aspect,
      )
    "
  >
    <button type="button" class="absolute inset-0 size-full outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-accent" :aria-label="alt" @click="emit('open')">
      <img
        v-if="!failed"
        :src="src"
        alt=""
        loading="lazy"
        decoding="async"
        class="size-full object-cover transition-transform duration-500 ease-out group-hover:scale-[1.04]"
        @error="failed = true"
      />
      <span v-else class="grid size-full place-items-center text-subtle"><ImageOff class="size-6" :stroke-width="1.5" aria-hidden="true" /></span>
      <span class="pointer-events-none absolute inset-x-0 bottom-0 h-1/3 bg-gradient-to-t from-charcoal/25 to-transparent" aria-hidden="true" />
    </button>
    <FMediaChip v-if="meta" :icon="metaIcon" class="pointer-events-none absolute bottom-3 left-3">{{ meta }}</FMediaChip>
    <div v-if="$slots.actions" class="absolute right-2.5 top-2.5">
      <slot name="actions" />
    </div>
    <slot />
  </div>
</template>
