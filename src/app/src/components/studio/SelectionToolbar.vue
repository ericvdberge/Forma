<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch, type Component } from 'vue'
import {
  ArrowDown,
  ArrowUp,
  Bold,
  BookmarkPlus,
  Copy,
  Ellipsis,
  EyeOff,
  GripVertical,
  Highlighter,
  Italic,
  Link,
  List,
  ListOrdered,
  SquareDashedMousePointer,
  Strikethrough,
  Trash2,
  X,
} from 'lucide-vue-next'
import type { Editor } from '@tiptap/vue-3'
import { cn } from '@forma/design-system'
import { useStudio } from '@/composables/useStudio'

/** Floating actions for the selected section or block, plus text formatting while editing text. */
const props = withDefaults(
  defineProps<{ label: string; placement?: 'above' | 'inside'; richText?: boolean; variant?: 'heading' | 'text' }>(),
  { placement: 'above', richText: false, variant: 'text' },
)

const studio = useStudio()
const sel = computed(() => studio.selection.value)
const menuOpen = ref(false)

/* Re-render active states as the text selection changes. */
const tick = ref(0)
const bump = () => tick.value++
watch(
  () => studio.textEditor.value,
  (e, old) => {
    old?.off('transaction', bump)
    e?.on('transaction', bump)
  },
  { immediate: true },
)
onBeforeUnmount(() => studio.textEditor.value?.off('transaction', bump))

const editor = computed(() => (props.richText ? studio.textEditor.value : null))
function isActive(name: string) {
  void tick.value
  return editor.value?.isActive(name) ?? false
}

type Format = { label: string; icon: Component; mark: string; run: (e: Editor) => void; textOnly?: boolean }
const formats: Format[] = [
  { label: 'Bold', icon: Bold, mark: 'bold', run: (e) => e.chain().focus().toggleBold().run() },
  { label: 'Italic', icon: Italic, mark: 'italic', run: (e) => e.chain().focus().toggleItalic().run() },
  { label: 'Strikethrough', icon: Strikethrough, mark: 'strike', run: (e) => e.chain().focus().toggleStrike().run() },
  { label: 'Soft colour', icon: Highlighter, mark: 'muted', run: (e) => e.chain().focus().toggleMuted().run() },
  { label: 'Link', icon: Link, mark: 'link', run: toggleLink },
  { label: 'Bulleted list', icon: List, mark: 'bulletList', run: (e) => e.chain().focus().toggleBulletList().run(), textOnly: true },
  { label: 'Numbered list', icon: ListOrdered, mark: 'orderedList', run: (e) => e.chain().focus().toggleOrderedList().run(), textOnly: true },
]
const visibleFormats = computed(() => formats.filter((f) => !f.textOnly || props.variant === 'text'))

function toggleLink(e: Editor) {
  if (e.isActive('link')) return e.chain().focus().extendMarkRange('link').unsetLink().run()
  const href = window.prompt('Link to (URL or /page)', 'https://')
  if (href && href !== 'https://') e.chain().focus().extendMarkRange('link').setLink({ href }).run()
}

function onDragStart(e: DragEvent) {
  const s = sel.value
  if (!s) return
  studio.drag.value = s.kind === 'section' ? { kind: 'move-section', sectionId: s.sectionId } : { kind: 'move-block', blockId: s.blockId }
  e.dataTransfer?.setData('text/plain', props.label)
  if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move'
}
function onDragEnd() {
  studio.drag.value = null
  studio.dropTarget.value = null
}

function hideHere() {
  const s = sel.value
  if (!s) return
  const path = `visibility.${studio.device.value}`
  if (s.kind === 'section') studio.setSectionValue(s.sectionId, path, false)
  else studio.setBlockValue(s.blockId, `style.${path}`, false)
  menuOpen.value = false
}

function save() {
  const name = window.prompt('Name this item in your library', props.label)
  if (name !== null) studio.saveToLibrary(sel.value, name.trim() || props.label)
  menuOpen.value = false
}

const btn = 'grid size-8 place-items-center rounded-lg text-charcoal/75 transition-colors hover:bg-charcoal/5 hover:text-charcoal'
</script>

<template>
  <div
    :class="
      cn(
        'absolute z-30 flex items-center gap-0.5 whitespace-nowrap rounded-xl bg-white p-1 text-charcoal shadow-card ring-1 ring-charcoal/[0.08]',
        placement === 'above' ? 'bottom-full left-0 mb-2' : 'left-1/2 top-3 -translate-x-1/2',
      )
    "
    :style="{ zoom: 1 / studio.zoom.value }"
    role="toolbar"
    :aria-label="`${label} actions`"
    @click.stop
    @mousedown.stop
  >
    <span class="px-2 text-xs font-medium text-muted">{{ label }}</span>
    <span
      :class="cn(btn, 'cursor-grab active:cursor-grabbing')"
      draggable="true"
      title="Drag to move"
      aria-label="Drag to move"
      role="button"
      @dragstart="onDragStart"
      @dragend="onDragEnd"
    >
      <GripVertical class="size-4" aria-hidden="true" />
    </span>
    <button type="button" :class="btn" title="Move up" aria-label="Move up" @click="studio.nudge(-1)"><ArrowUp class="size-4" aria-hidden="true" /></button>
    <button type="button" :class="btn" title="Move down" aria-label="Move down" @click="studio.nudge(1)"><ArrowDown class="size-4" aria-hidden="true" /></button>
    <button type="button" :class="btn" title="Duplicate (Ctrl+D)" aria-label="Duplicate" @click="studio.duplicate()"><Copy class="size-4" aria-hidden="true" /></button>
    <button type="button" :class="btn" title="Delete (Del)" aria-label="Delete" @click="studio.remove()"><Trash2 class="size-4" aria-hidden="true" /></button>

    <template v-if="editor">
      <span class="mx-1 h-5 w-px bg-charcoal/10" aria-hidden="true" />
      <button
        v-for="f in visibleFormats"
        :key="f.mark"
        type="button"
        :class="cn(btn, isActive(f.mark) && 'bg-accent-50 text-accent hover:bg-accent-50 hover:text-accent')"
        :title="f.label"
        :aria-label="f.label"
        :aria-pressed="isActive(f.mark)"
        @mousedown.prevent
        @click="f.run(editor)"
      >
        <component :is="f.icon" class="size-4" aria-hidden="true" />
      </button>
    </template>

    <div class="relative">
      <button type="button" :class="btn" title="More" aria-label="More actions" :aria-expanded="menuOpen" @click="menuOpen = !menuOpen">
        <Ellipsis class="size-4" aria-hidden="true" />
      </button>
      <div v-if="menuOpen" role="menu" class="absolute right-0 top-full mt-1 w-52 rounded-xl bg-white p-1 shadow-float ring-1 ring-charcoal/[0.06]">
        <button type="button" role="menuitem" class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm hover:bg-canvas" @click="save">
          <BookmarkPlus class="size-4" aria-hidden="true" /> Save to library
        </button>
        <button type="button" role="menuitem" class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm hover:bg-canvas" @click="hideHere">
          <EyeOff class="size-4" aria-hidden="true" /> Hide on {{ studio.device.value }}
        </button>
        <button
          v-if="sel?.kind === 'block'"
          type="button"
          role="menuitem"
          class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm hover:bg-canvas"
          @click="sel && studio.select({ kind: 'section', sectionId: sel.sectionId })"
        >
          <SquareDashedMousePointer class="size-4" aria-hidden="true" /> Select section
        </button>
        <button type="button" role="menuitem" class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm hover:bg-canvas" @click="studio.select(null)">
          <X class="size-4" aria-hidden="true" /> Deselect
        </button>
      </div>
    </div>
  </div>
</template>
