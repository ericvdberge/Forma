<script setup lang="ts">
import { watch } from 'vue'
import { EditorContent, useEditor, type Editor } from '@tiptap/vue-3'
import { Bold, Italic, Link, List, ListOrdered, Quote } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { richTextExtensions } from '@/studio/tiptap'

/** Formatted text for a collection item, with the same markup the studio's text blocks allow. */
const props = defineProps<{ modelValue: string; labelledby: string }>()
const emit = defineEmits<{ 'update:modelValue': [html: string] }>()

// useEditor keeps the toolbar's active states in step with the selection.
const editor = useEditor({
    content: props.modelValue,
    extensions: richTextExtensions('text'),
    editorProps: {
      attributes: {
        class: 'prose-item min-h-40 px-4 py-3 text-[0.9375rem] leading-relaxed outline-none',
        'aria-labelledby': props.labelledby,
        role: 'textbox',
        'aria-multiline': 'true',
      },
    },
    // An empty editor is "not filled in", not an empty paragraph.
  onUpdate: ({ editor: e }) => emit('update:modelValue', e.isEmpty ? '' : e.getHTML()),
})

watch(
  () => props.modelValue,
  (html) => {
    const e = editor.value
    if (e && html !== (e.isEmpty ? '' : e.getHTML())) e.commands.setContent(html, { emitUpdate: false })
  },
)

function setLink() {
  const e = editor.value
  if (!e) return
  const current = e.getAttributes('link').href as string | undefined
  const href = window.prompt('Link to (https://, /page or mailto:)', current ?? 'https://')
  if (href === null) return
  if (!href.trim()) e.chain().focus().unsetLink().run()
  else e.chain().focus().extendMarkRange('link').setLink({ href: href.trim() }).run()
}

const tools = [
  { label: 'Bold', icon: Bold, active: 'bold', run: (e: Editor) => e.chain().focus().toggleBold().run() },
  { label: 'Italic', icon: Italic, active: 'italic', run: (e: Editor) => e.chain().focus().toggleItalic().run() },
  { label: 'Bulleted list', icon: List, active: 'bulletList', run: (e: Editor) => e.chain().focus().toggleBulletList().run() },
  { label: 'Numbered list', icon: ListOrdered, active: 'orderedList', run: (e: Editor) => e.chain().focus().toggleOrderedList().run() },
  { label: 'Quote', icon: Quote, active: 'blockquote', run: (e: Editor) => e.chain().focus().toggleBlockquote().run() },
  { label: 'Link', icon: Link, active: 'link', run: () => setLink() },
]
</script>

<template>
  <div class="overflow-hidden rounded-2xl bg-white ring-1 ring-charcoal/10 focus-within:ring-2 focus-within:ring-accent">
    <div role="toolbar" :aria-labelledby="labelledby" class="flex gap-0.5 border-b border-charcoal/[0.06] p-1.5">
      <button
        v-for="t in tools"
        :key="t.label"
        type="button"
        :title="t.label"
        :aria-pressed="!!editor?.isActive(t.active)"
        :class="cn('grid size-8 place-items-center rounded-lg transition-colors', editor?.isActive(t.active) ? 'bg-charcoal text-warm-white' : 'text-charcoal/70 hover:bg-canvas')"
        @mousedown.prevent
        @click="editor && t.run(editor)"
      >
        <component :is="t.icon" class="size-4" aria-hidden="true" />
        <span class="sr-only">{{ t.label }}</span>
      </button>
    </div>
    <EditorContent :editor="editor" />
  </div>
</template>

<style scoped>
:deep(.prose-item p + p) { margin-top: 0.75em; }
:deep(.prose-item ul) { list-style: disc; padding-left: 1.25rem; margin: 0.5em 0; }
:deep(.prose-item ol) { list-style: decimal; padding-left: 1.25rem; margin: 0.5em 0; }
:deep(.prose-item blockquote) { border-left: 3px solid rgb(11 11 15 / 0.12); padding-left: 0.875rem; color: #6b6a66; margin: 0.5em 0; }
:deep(.prose-item a) { text-decoration: underline; }
</style>
