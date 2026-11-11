<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{ modelValue: string; id?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const text = ref(props.modelValue)
watch(
  () => props.modelValue,
  (v) => (text.value = v),
)
function commit(v: string) {
  text.value = v
  const hex = v.startsWith('#') ? v : `#${v}`
  if (/^#[0-9a-f]{6}$/i.test(hex)) emit('update:modelValue', hex.toLowerCase())
}
</script>

<template>
  <div class="flex h-10 items-center gap-2 rounded-lg bg-white pl-1.5 pr-3 ring-1 ring-charcoal/10 focus-within:ring-2 focus-within:ring-accent">
    <input
      type="color"
      :value="modelValue"
      class="size-7 shrink-0 cursor-pointer rounded-md border-0 bg-transparent p-0"
      aria-label="Pick a colour"
      @input="commit(($event.target as HTMLInputElement).value)"
    />
    <input
      :id="id"
      :value="text.toUpperCase()"
      maxlength="7"
      spellcheck="false"
      class="w-full bg-transparent text-sm uppercase outline-none"
      @input="commit(($event.target as HTMLInputElement).value)"
    />
  </div>
</template>
