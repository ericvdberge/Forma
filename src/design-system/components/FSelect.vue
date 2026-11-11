<script setup lang="ts" generic="T extends string">
import { useId } from 'vue'
import { ChevronDown } from 'lucide-vue-next'
import { cn } from '../utils/cn'

const model = defineModel<T>({ required: true })
withDefaults(
  defineProps<{
    label: string
    hideLabel?: boolean
    options: Array<{ label: string; value: T }>
    size?: 'sm' | 'md'
    /** `plain` drops the field chrome, for inline controls like "Newest first ⌄" */
    variant?: 'field' | 'plain'
  }>(),
  { hideLabel: false, size: 'md', variant: 'field' },
)
const id = useId()
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label :for="id" :class="cn('text-sm font-medium text-charcoal', hideLabel && 'sr-only')">{{ label }}</label>
    <div class="relative">
      <select
        :id="id"
        v-model="model"
        :class="
          cn(
            'w-full cursor-pointer appearance-none rounded-full pl-4 pr-10 text-charcoal outline-none transition-shadow focus:ring-2 focus:ring-accent',
            variant === 'field' ? 'bg-white ring-1 ring-charcoal/10' : 'bg-transparent font-medium hover:bg-charcoal/5',
            size === 'sm' ? 'h-9 text-sm' : 'h-11 text-[0.9375rem]',
          )
        "
      >
        <option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option>
      </select>
      <ChevronDown class="pointer-events-none absolute right-3.5 top-1/2 size-4 -translate-y-1/2 text-muted" aria-hidden="true" />
    </div>
  </div>
</template>
