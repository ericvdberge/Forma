<script setup lang="ts">
import { useId } from 'vue'
import { cn } from '../utils/cn'

const model = defineModel<string>({ default: '' })
withDefaults(
  defineProps<{
    label: string
    hideLabel?: boolean
    type?: string
    placeholder?: string
    tone?: 'light' | 'dark'
    /** Passed to the input so browsers and password managers can autofill it, e.g. `username`. */
    autocomplete?: string
    required?: boolean
  }>(),
  { hideLabel: false, type: 'text', tone: 'light', required: false },
)
const id = useId()
</script>

<template>
  <div class="flex w-full flex-col gap-1.5">
    <label
      :for="id"
      :class="cn('text-sm font-medium', hideLabel && 'sr-only', tone === 'dark' ? 'text-warm-white' : 'text-charcoal')"
    >
      {{ label }}
    </label>
    <input
      :id="id"
      v-model="model"
      :type="type"
      :placeholder="placeholder"
      :autocomplete="autocomplete"
      :required="required"
      :class="
        cn(
          'h-11 w-full rounded-full px-5 text-[0.9375rem] outline-none transition-shadow',
          tone === 'light'
            ? 'bg-white text-charcoal ring-1 ring-charcoal/10 placeholder:text-subtle focus:ring-2 focus:ring-accent'
            : 'bg-white/[0.06] text-warm-white ring-1 ring-white/15 placeholder:text-white/40 focus:ring-2 focus:ring-accent',
        )
      "
    />
  </div>
</template>
