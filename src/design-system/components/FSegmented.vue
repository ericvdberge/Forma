<script setup lang="ts" generic="T extends string">
import type { Component } from 'vue'
import { cn } from '../utils/cn'

/** Small icon (or text) toggle, e.g. grid / list view. */
const model = defineModel<T>({ required: true })
defineProps<{
  options: Array<{ value: T; label: string; icon?: Component }>
  label: string
}>()
</script>

<template>
  <div role="radiogroup" :aria-label="label" class="inline-flex gap-0.5 rounded-xl bg-white p-1 ring-1 ring-charcoal/[0.08]">
    <button
      v-for="o in options"
      :key="o.value"
      type="button"
      role="radio"
      :aria-checked="model === o.value"
      :aria-label="o.icon ? o.label : undefined"
      :title="o.icon ? o.label : undefined"
      :class="
        cn(
          'inline-flex h-8 min-w-8 items-center justify-center gap-1.5 rounded-lg px-2 text-sm transition-colors',
          model === o.value ? 'bg-canvas text-charcoal' : 'text-charcoal/50 hover:text-charcoal',
        )
      "
      @click="model = o.value"
    >
      <component :is="o.icon" v-if="o.icon" class="size-4" :stroke-width="1.8" aria-hidden="true" />
      <span v-else>{{ o.label }}</span>
    </button>
  </div>
</template>
