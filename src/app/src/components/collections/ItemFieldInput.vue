<script setup lang="ts">
import { computed, useId } from 'vue'
import { FSwitch, cn } from '@forma/design-system'
import ImageControl from '@/components/studio/inspector/ImageControl.vue'
import type { CollectionField, CollectionValue } from '@/data/types'
import { fieldTypes } from '@/utils/collections'
import RichTextField from './RichTextField.vue'

/** The input for one field of a collection item, picked by the field's type. */
const props = defineProps<{ field: CollectionField; modelValue: CollectionValue | null | undefined; error?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: CollectionValue | null] }>()
const id = useId()

const text = computed(() => (typeof props.modelValue === 'string' ? props.modelValue : props.modelValue == null ? '' : String(props.modelValue)))

const input =
  'h-11 w-full rounded-xl bg-white px-4 text-[0.9375rem] text-charcoal ring-1 ring-charcoal/10 outline-none transition-shadow placeholder:text-subtle focus:ring-2 focus:ring-accent'

function onNumber(raw: string) {
  emit('update:modelValue', raw.trim() === '' ? null : Number(raw))
}
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <div v-if="field.type !== 'boolean'" class="flex items-baseline justify-between gap-3">
      <label :id="`${id}-label`" :for="id" class="text-sm font-medium text-charcoal">
        {{ field.label }}<span v-if="field.required" class="text-danger" aria-hidden="true"> *</span>
        <span v-if="field.required" class="sr-only"> (required)</span>
      </label>
      <span class="flex items-center gap-1 text-xs text-subtle">
        <component :is="fieldTypes[field.type].icon" class="size-3" aria-hidden="true" /> {{ fieldTypes[field.type].label }}
      </span>
    </div>

    <input
      v-if="field.type === 'text'"
      :id="id"
      :value="text"
      maxlength="500"
      :class="cn(input, error && 'ring-danger')"
      :aria-invalid="!!error"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />

    <input
      v-else-if="field.type === 'url'"
      :id="id"
      type="url"
      :value="text"
      placeholder="https:// or /page"
      :class="cn(input, error && 'ring-danger')"
      :aria-invalid="!!error"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />

    <input
      v-else-if="field.type === 'number'"
      :id="id"
      type="number"
      step="any"
      :value="text"
      :class="cn(input, 'sm:w-48', error && 'ring-danger')"
      :aria-invalid="!!error"
      @input="onNumber(($event.target as HTMLInputElement).value)"
    />

    <input
      v-else-if="field.type === 'date'"
      :id="id"
      type="date"
      :value="text"
      :class="cn(input, 'sm:w-56', error && 'ring-danger')"
      :aria-invalid="!!error"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />

    <RichTextField v-else-if="field.type === 'richText'" :labelledby="`${id}-label`" :model-value="text" @update:model-value="emit('update:modelValue', $event)" />

    <div v-else-if="field.type === 'image'" class="max-w-md">
      <ImageControl removable :model-value="text" @update:model-value="emit('update:modelValue', $event)" />
    </div>

    <FSwitch v-else-if="field.type === 'boolean'" :model-value="modelValue === true" :label="field.label" @update:model-value="emit('update:modelValue', $event)" />

    <p v-if="error" class="text-xs text-danger" role="alert">{{ error }}</p>
  </div>
</template>
