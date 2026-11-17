<script setup lang="ts">
import { ref } from 'vue'
import { ImagePlus, X } from 'lucide-vue-next'
import MediaPicker from './MediaPicker.vue'

defineProps<{ modelValue: string; removable?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const picking = ref(false)
</script>

<template>
  <div class="flex flex-col gap-2">
    <div class="relative overflow-hidden rounded-xl bg-canvas ring-1 ring-charcoal/[0.06]">
      <img v-if="modelValue" :src="modelValue" alt="" class="aspect-[16/7] w-full object-cover" />
      <button v-else type="button" class="grid aspect-[16/7] w-full place-items-center text-muted hover:text-charcoal" @click="picking = true">
        <span class="flex flex-col items-center gap-1 text-xs"><ImagePlus class="size-5" aria-hidden="true" /> Choose an image</span>
      </button>
      <button
        v-if="modelValue && removable"
        type="button"
        class="absolute right-2 top-2 grid size-6 place-items-center rounded-full bg-white/90 text-charcoal shadow-soft hover:bg-white"
        aria-label="Remove image"
        @click="emit('update:modelValue', '')"
      >
        <X class="size-3.5" aria-hidden="true" />
      </button>
    </div>
    <button type="button" class="h-9 rounded-lg bg-canvas text-[0.8125rem] font-medium hover:bg-charcoal/[0.06]" @click="picking = true">
      {{ modelValue ? 'Change image' : 'Choose image' }}
    </button>
    <MediaPicker v-if="picking" @close="picking = false" @select="emit('update:modelValue', $event); picking = false" />
  </div>
</template>
