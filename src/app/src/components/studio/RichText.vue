<script setup lang="ts">
import { computed, inject, onBeforeUnmount, shallowRef, watch } from 'vue'
import { generateHTML, generateJSON } from '@tiptap/core'
import { Editor, EditorContent } from '@tiptap/vue-3'
import { richTextExtensions } from '@/studio/tiptap'
import { textEditorKey } from '@/studio/inject'

/**
 * Rich text that becomes editable in place (Tiptap) while its block is selected in the studio,
 * and is plain HTML otherwise. Emits the new HTML as the user types.
 */
const props = defineProps<{ html: string; editable: boolean; variant: 'heading' | 'text'; tag: string }>()
const emit = defineEmits<{ update: [html: string] }>()

/** Lets the floating toolbar format the text being edited. */
const activeEditor = inject(textEditorKey, null)
const editor = shallowRef<Editor | null>(null)

function create() {
  const e = new Editor({
    content: props.html,
    extensions: richTextExtensions(props.variant),
    editorProps: { attributes: { class: 'outline-none', spellcheck: 'true' } },
    onUpdate: ({ editor: ed }) => emit('update', ed.getHTML()),
  })
  editor.value = e
  if (activeEditor) activeEditor.value = e
}

function destroy() {
  if (activeEditor && activeEditor.value === editor.value) activeEditor.value = null
  editor.value?.destroy()
  editor.value = null
}

watch(
  () => props.editable,
  (on) => (on ? create() : destroy()),
  { immediate: true },
)

// Undo/redo and inspector changes replace the HTML from outside; don't fight the cursor while typing.
watch(
  () => props.html,
  (html) => {
    const e = editor.value
    if (e && html !== e.getHTML()) e.commands.setContent(html, { emitUpdate: false })
  },
)

onBeforeUnmount(destroy)

/** Round-tripped through the editor schema, so only the marks and nodes it allows are rendered. */
const safeHtml = computed(() => {
  const extensions = richTextExtensions(props.variant)
  return generateHTML(generateJSON(props.html, extensions), extensions)
})
</script>

<template>
  <component :is="tag" class="studio-rich">
    <EditorContent v-if="editor" :editor="editor" />
    <div v-else v-html="safeHtml" />
  </component>
</template>

<style>
.studio-rich p {
  margin: 0;
}
.studio-rich p + p {
  margin-top: 0.75em;
}
.studio-rich ul,
.studio-rich ol {
  margin: 0.5em 0;
  padding-left: 1.25em;
}
.studio-rich ul {
  list-style: disc;
}
.studio-rich ol {
  list-style: decimal;
}
.studio-rich blockquote {
  border-left: 2px solid currentColor;
  padding-left: 1em;
  opacity: 0.8;
}
.studio-rich a {
  text-decoration: underline;
  text-underline-offset: 3px;
}
.studio-rich [data-muted] {
  color: var(--studio-muted);
}
.studio-rich .ProseMirror {
  cursor: text;
}
</style>
