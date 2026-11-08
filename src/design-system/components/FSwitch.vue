<script setup lang="ts">
import { cn } from '../utils/cn'

const model = defineModel<boolean>({ default: false })
defineProps<{ label: string; hideLabel?: boolean; disabled?: boolean }>()
</script>

<template>
  <label :class="cn('inline-flex cursor-pointer items-center gap-3', disabled && 'cursor-not-allowed opacity-50')">
    <button
      type="button"
      role="switch"
      :aria-checked="model"
      :aria-label="hideLabel ? label : undefined"
      :disabled="disabled"
      :class="
        cn(
          'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors duration-200',
          model ? 'bg-charcoal' : 'bg-charcoal/15',
        )
      "
      @click="model = !model"
    >
      <span
        :class="
          cn(
            'inline-block size-5 rounded-full bg-white shadow-soft transition-transform duration-200',
            model ? 'translate-x-[1.375rem]' : 'translate-x-0.5',
          )
        "
      />
    </button>
    <span v-if="!hideLabel" class="text-sm font-medium text-charcoal">{{ label }}</span>
  </label>
</template>
