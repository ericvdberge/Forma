<script setup lang="ts">
import { ref, useId } from 'vue'
import { CloudUpload } from 'lucide-vue-next'
import { cn } from '../utils/cn'

/** Dashed drop target with a "browse" fallback. Emits the chosen or dropped files. */
withDefaults(
  defineProps<{
    title?: string
    /** Line under the box, e.g. "Supports images up to 10 MB" */
    hint?: string
    /** Format pills under the hint, e.g. ['JPG', 'PNG'] */
    formats?: string[]
    /** `<input accept>` */
    accept?: string
    multiple?: boolean
    disabled?: boolean
  }>(),
  { title: 'Drag & drop your files here', multiple: true, disabled: false, formats: () => [] },
)
const emit = defineEmits<{ files: [files: File[]] }>()

const input = ref<HTMLInputElement | null>(null)
const over = ref(false)
const hintId = useId()
let depth = 0

function browse() {
  input.value?.click()
}
function onChange(e: Event) {
  const el = e.target as HTMLInputElement
  if (el.files?.length) emit('files', Array.from(el.files))
  el.value = ''
}
function onEnter() {
  depth++
  over.value = true
}
function onLeave() {
  depth = Math.max(0, depth - 1)
  if (!depth) over.value = false
}
function onDrop(e: DragEvent) {
  depth = 0
  over.value = false
  const files = Array.from(e.dataTransfer?.files ?? [])
  if (files.length) emit('files', files)
}

defineExpose({ browse })
</script>

<template>
  <div>
    <button
      type="button"
      :disabled="disabled"
      :aria-describedby="hint || formats.length ? hintId : undefined"
      :class="
        cn(
          'flex w-full flex-col items-center justify-center gap-2 rounded-2xl border-[1.5px] border-dashed px-5 py-9 text-center transition-colors duration-200',
          'disabled:pointer-events-none disabled:opacity-50',
          over ? 'border-accent bg-accent-50/60' : 'border-charcoal/15 hover:border-charcoal/35 hover:bg-warm-white',
        )
      "
      @click="browse"
      @dragenter.prevent="onEnter"
      @dragover.prevent
      @dragleave.prevent="onLeave"
      @drop.prevent="onDrop"
    >
      <CloudUpload :class="cn('size-9 transition-transform duration-200', over && '-translate-y-0.5 text-accent')" :stroke-width="1.4" aria-hidden="true" />
      <span class="mt-1 text-[0.9375rem] font-medium text-charcoal">{{ title }}</span>
      <span class="text-sm text-muted">or <span class="font-medium text-positive underline-offset-2 hover:underline">browse</span> your device</span>
    </button>
    <input ref="input" type="file" class="sr-only" tabindex="-1" aria-hidden="true" :accept="accept" :multiple="multiple" @change="onChange" />
    <div v-if="hint || formats.length" :id="hintId" class="mt-3 flex flex-col items-center gap-2.5">
      <p v-if="hint" class="text-xs text-muted">{{ hint }}</p>
      <ul v-if="formats.length" class="flex flex-wrap justify-center gap-1.5" aria-label="Supported formats">
        <li v-for="f in formats" :key="f" class="rounded-md bg-canvas px-2 py-0.5 text-[0.6875rem] font-medium text-charcoal/70">{{ f }}</li>
      </ul>
    </div>
  </div>
</template>
