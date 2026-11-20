import { computed, inject, provide, ref, shallowRef, toValue, watch, type InjectionKey, type MaybeRefOrGetter } from 'vue'
import type { Editor } from '@tiptap/vue-3'
import { usePageContent } from '@/composables/usePageContent'
import { useSavedItems } from '@/composables/useSavedItems'
import { useStudioEditing, type Adding, type DragPayload, type DropTarget } from '@/composables/useStudioEditing'
import { useStudioHistory } from '@/composables/useStudioHistory'
import { useStudioMedia } from '@/composables/useStudioMedia'
import { useStudioPublishing } from '@/composables/useStudioPublishing'
import type { StudioPageSeed } from '@/queriers/usePageContentQuerier'
import { provideCanvas } from '@/studio/canvas'
import { textEditorKey, textPointerKey } from '@/studio/inject'
import type { Breakpoint, Selection } from '@/studio/types'

export { isSectionPayload, type DragPayload, type DropTarget } from '@/composables/useStudioEditing'

export type StudioMode = 'simple' | 'advanced'

/** Simple mode hides the block library and technical settings; remembered per browser. */
const MODE_KEY = 'forma.studioMode'
const mode = ref<StudioMode>(readMode())

function readMode(): StudioMode {
  try {
    return localStorage.getItem(MODE_KEY) === 'advanced' ? 'advanced' : 'simple'
  } catch {
    return 'simple' /* storage unavailable */
  }
}

watch(mode, (m) => {
  try {
    localStorage.setItem(MODE_KEY, m)
  } catch {
    /* storage unavailable */
  }
})

/**
 * The Design Studio editor for one page, put together from one composable per concern: the page's content
 * (usePageContent), the saved library (useSavedItems), media (useStudioMedia), undo (useStudioHistory), editing
 * (useStudioEditing) and publishing (useStudioPublishing). Selection, device, zoom and drag state live here.
 * Edits stay in the browser until the page is published.
 */
export function createStudio(
  pageId: MaybeRefOrGetter<string>,
  seed: MaybeRefOrGetter<StudioPageSeed | undefined>,
  websiteId: MaybeRefOrGetter<string>,
) {
  const content = usePageContent(pageId, seed)
  const library = useSavedItems()
  const studioMedia = useStudioMedia(websiteId)

  const doc = computed(() => content.page.value?.draft)

  /* ---------- Editor state ---------- */
  const selection = ref<Selection>(null)
  // On a phone, start by editing the phone layout rather than a tiny desktop.
  const device = ref<Breakpoint>(typeof window !== 'undefined' && window.innerWidth < 640 ? 'mobile' : 'desktop')
  const preview = ref(false)
  /** How much the canvas frame is scaled down to fit; floating UI inside it counter-scales. */
  const zoom = ref(1)
  const drag = shallowRef<DragPayload | null>(null)
  const dropTarget = ref<DropTarget | null>(null)
  /** The rich-text editor of the text block being edited, for the formatting toolbar. */
  const textEditor = shallowRef<Editor | null>(null)
  const textPointer = shallowRef<{ x: number; y: number } | null>(null)
  const adding = ref<Adding>(null)

  const selectedSection = computed(() => (selection.value ? doc.value?.sections.find((s) => s.id === selection.value!.sectionId) : undefined))
  const selectedBlock = computed(() => {
    const sel = selection.value
    return sel?.kind === 'block' ? selectedSection.value?.blocks.find((b) => b.id === sel.blockId) : undefined
  })

  function select(sel: Selection) {
    selection.value = sel
  }

  /* ---------- Composition ---------- */
  const history = useStudioHistory(doc, (next) => content.writePage((p) => ({ ...p, draft: next, updatedAt: Date.now() })))

  const editing = useStudioEditing({ doc, selection, saved: library.saved, adding, change: history.change })

  const publishing = useStudioPublishing({
    page: content.page,
    writePage: content.writePage,
    publishContent: content.publish,
    change: history.change,
    keepSelectionValid: editing.keepSelectionValid,
    select,
  })

  function undo() {
    const restored = history.undo()
    if (restored) editing.keepSelectionValid(restored)
  }

  function redo() {
    const restored = history.redo()
    if (restored) editing.keepSelectionValid(restored)
  }

  // Another page: start with nothing selected and a fresh history.
  watch(
    () => toValue(pageId),
    () => {
      selection.value = null
      history.reset()
    },
  )

  return {
    query: content.query,
    page: content.page,
    doc,
    media: studioMedia.media,
    mediaQuery: studioMedia.mediaQuery,
    uploadMedia: studioMedia.uploadMedia,
    saved: library.saved,
    saveToLibrary: (sel: Selection = selection.value, name?: string) => library.saveToLibrary(doc.value, sel, name),
    removeSaved: library.removeSaved,
    selection,
    selectedSection,
    selectedBlock,
    device,
    preview,
    mode,
    zoom,
    drag,
    dropTarget,
    textEditor,
    textPointer,
    adding,
    select,
    change: history.change,
    undo,
    redo,
    canUndo: history.canUndo,
    canRedo: history.canRedo,
    ...editing,
    ...publishing,
  }
}

export type Studio = ReturnType<typeof createStudio>

const StudioKey: InjectionKey<Studio> = Symbol('studio')

export function provideStudio(studio: Studio) {
  provide(StudioKey, studio)
  provideCanvas(studio)
  provide(textEditorKey, studio.textEditor)
  provide(textPointerKey, studio.textPointer)
}

/** The studio of the page being edited, for components inside the Design Studio view. */
export function useStudio() {
  const studio = inject(StudioKey)
  if (!studio) throw new Error('useStudio() must be used inside the Design Studio')
  return studio
}
