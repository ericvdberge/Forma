import { computed, inject, provide, ref, shallowRef, toValue, watch, type InjectionKey, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import type { Editor } from '@tiptap/vue-3'
import { studioMediaQueryKey, studioPageQueryKey, studioSavedQueryKey, useStudioQuerier, type StudioPageSeed } from '@/queriers/useStudioQuerier'
import { usePages } from '@/composables/usePages'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'
import { clone } from '@/studio/defaults'
import { createBlock } from '@/studio/blocks'
import { createSection, globalElements, sectionTemplates, withFreshIds } from '@/studio/sections'
import { textEditorKey } from '@/studio/inject'
import type { Block, BlockType, Breakpoint, MediaAsset, PageDocument, SavedItem, Section, Selection, StudioPage } from '@/studio/types'

/** Something being dragged onto or around the canvas. */
export type DragPayload =
  | { kind: 'new-block'; type: BlockType }
  | { kind: 'template'; id: string }
  | { kind: 'saved'; id: string }
  | { kind: 'move-block'; blockId: string }
  | { kind: 'move-section'; sectionId: string }

/** Where a drop would land: between sections, or at an index inside a section (and column, for split layouts). */
export type DropTarget = { kind: 'section'; index: number } | { kind: 'block'; sectionId: string; index: number; column?: 0 | 1 }

const HISTORY_LIMIT = 100
/** Edits to the same field within this window are one undo step (e.g. typing). */
const COALESCE_MS = 1000

function setPath(target: Record<string, unknown>, path: string, value: unknown) {
  const keys = path.split('.')
  let obj = target
  for (const k of keys.slice(0, -1)) {
    if (typeof obj[k] !== 'object' || obj[k] === null) obj[k] = {}
    obj = obj[k] as Record<string, unknown>
  }
  const last = keys.at(-1)!
  if (value === undefined) delete obj[last]
  else obj[last] = value
}

/** Whether a drag payload is placed as a section (not as a block inside one). */
export function isSectionPayload(p: DragPayload, saved: SavedItem[]) {
  if (p.kind === 'template' || p.kind === 'move-section') return true
  if (p.kind === 'saved') return saved.find((s) => s.id === p.id)?.kind === 'section'
  return false
}

/**
 * The Design Studio editor for one page. The page's draft and published documents live in the
 * vue-query cache (`['studio', 'page', id]`); every edit writes a new draft there, so leaving and
 * coming back to the studio keeps unpublished work. Selection, device and history are editor state.
 */
export function createStudio(pageId: MaybeRefOrGetter<string>, seed: MaybeRefOrGetter<StudioPageSeed | undefined>) {
  const queryClient = useQueryClient()
  const querier = useStudioQuerier()
  const { setPageStatus } = usePages()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  const query = querier.getStudioPage(pageId, seed)
  const mediaQuery = querier.getMedia()
  const savedQuery = querier.getSaved()

  const page = computed(() => query.data.value)
  const doc = computed(() => page.value?.draft)
  const media = computed(() => mediaQuery.data.value ?? [])
  const saved = computed(() => savedQuery.data.value ?? [])

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

  const past = shallowRef<PageDocument[]>([])
  const future = shallowRef<PageDocument[]>([])
  let lastCoalesce: { key: string; at: number } | null = null

  watch(
    () => toValue(pageId),
    () => {
      selection.value = null
      past.value = []
      future.value = []
      lastCoalesce = null
    },
  )

  const selectedSection = computed(() => (selection.value ? doc.value?.sections.find((s) => s.id === selection.value!.sectionId) : undefined))
  const selectedBlock = computed(() => {
    const sel = selection.value
    return sel?.kind === 'block' ? selectedSection.value?.blocks.find((b) => b.id === sel.blockId) : undefined
  })

  const hasChanges = computed(() => !!page.value && JSON.stringify(page.value.draft) !== JSON.stringify(page.value.published))
  const isPublished = computed(() => !!page.value?.publishedAt)

  /* ---------- Cache writes ---------- */
  function writePage(update: (p: StudioPage) => StudioPage) {
    queryClient.setQueryData<StudioPage>(studioPageQueryKey(toValue(pageId)), (old) => (old ? update(old) : old))
  }

  /** Applies `mutate` to a copy of the draft and stores it, recording an undo step. */
  function change(mutate: (d: PageDocument) => void, coalesceKey?: string) {
    const current = doc.value
    if (!current) return
    const now = Date.now()
    const coalesce = coalesceKey && lastCoalesce?.key === coalesceKey && now - lastCoalesce.at < COALESCE_MS
    if (!coalesce) past.value = [...past.value.slice(-HISTORY_LIMIT + 1), current]
    future.value = []
    lastCoalesce = coalesceKey ? { key: coalesceKey, at: now } : null
    const next = clone(current)
    mutate(next)
    writePage((p) => ({ ...p, draft: next, updatedAt: now }))
  }

  function undo() {
    const prev = past.value.at(-1)
    if (!prev || !doc.value) return
    future.value = [doc.value, ...future.value]
    past.value = past.value.slice(0, -1)
    lastCoalesce = null
    writePage((p) => ({ ...p, draft: prev, updatedAt: Date.now() }))
    keepSelectionValid(prev)
  }

  function redo() {
    const next = future.value[0]
    if (!next || !doc.value) return
    past.value = [...past.value, doc.value]
    future.value = future.value.slice(1)
    lastCoalesce = null
    writePage((p) => ({ ...p, draft: next, updatedAt: Date.now() }))
    keepSelectionValid(next)
  }

  function keepSelectionValid(d: PageDocument) {
    const sel = selection.value
    if (!sel) return
    const section = d.sections.find((s) => s.id === sel.sectionId)
    if (!section) selection.value = null
    else if (sel.kind === 'block' && !section.blocks.some((b) => b.id === sel.blockId)) selection.value = { kind: 'section', sectionId: section.id }
  }

  /* ---------- Lookups ---------- */
  function locateBlock(d: PageDocument, blockId: string) {
    for (const section of d.sections) {
      const index = section.blocks.findIndex((b) => b.id === blockId)
      if (index !== -1) return { section, index, block: section.blocks[index]! }
    }
  }

  /** Which column of a split section a block index is in. */
  const columnOf = (section: Section, index: number): 0 | 1 => (section.settings.layout === 'split' && index >= section.settings.splitAt ? 1 : 0)

  function insertInto(section: Section, index: number, block: Block, column?: 0 | 1) {
    const s = section.settings
    if (s.layout === 'split' && (index < s.splitAt || (index === s.splitAt && column === 0))) s.splitAt++
    section.blocks.splice(index, 0, block)
  }

  function removeFrom(section: Section, index: number) {
    const s = section.settings
    if (s.layout === 'split' && index < s.splitAt) s.splitAt--
    return section.blocks.splice(index, 1)[0]!
  }

  /* ---------- Editing ---------- */
  function select(sel: Selection) {
    selection.value = sel
  }

  function setSectionValue(sectionId: string, path: string, value: unknown) {
    change((d) => {
      const section = d.sections.find((s) => s.id === sectionId)
      if (section) setPath(section.settings as unknown as Record<string, unknown>, path, value)
    }, `${sectionId}:${path}`)
  }

  /** `path` starts with `props.` or `style.` */
  function setBlockValue(blockId: string, path: string, value: unknown) {
    change((d) => {
      const found = locateBlock(d, blockId)
      if (found) setPath(found.block as unknown as Record<string, unknown>, path, value)
    }, `${blockId}:${path}`)
  }

  function addSection(section: Section, index?: number) {
    change((d) => d.sections.splice(index ?? d.sections.length, 0, section))
    selection.value = { kind: 'section', sectionId: section.id }
  }

  function addBlock(block: Block, sectionId: string, index?: number, column?: 0 | 1) {
    change((d) => {
      const section = d.sections.find((s) => s.id === sectionId)
      if (section) insertInto(section, index ?? section.blocks.length, block, column)
    })
    selection.value = { kind: 'block', sectionId, blockId: block.id }
  }

  /** A block on its own: wraps it in a new blank section at `index`. */
  function addBlockAsSection(block: Block, index?: number) {
    const section = createSection({}, [block])
    change((d) => d.sections.splice(index ?? d.sections.length, 0, section))
    selection.value = { kind: 'block', sectionId: section.id, blockId: block.id }
  }

  function moveSection(sectionId: string, toIndex: number) {
    change((d) => {
      const from = d.sections.findIndex((s) => s.id === sectionId)
      if (from === -1) return
      const [section] = d.sections.splice(from, 1)
      d.sections.splice(toIndex > from ? toIndex - 1 : toIndex, 0, section!)
    })
  }

  function moveBlock(blockId: string, toSectionId: string, toIndex: number, column?: 0 | 1) {
    change((d) => {
      const found = locateBlock(d, blockId)
      const target = d.sections.find((s) => s.id === toSectionId)
      if (!found || !target) return
      const block = removeFrom(found.section, found.index)
      const index = found.section === target && toIndex > found.index ? toIndex - 1 : toIndex
      insertInto(target, index, block, column)
    })
    selection.value = { kind: 'block', sectionId: toSectionId, blockId }
  }

  /** Moves the selection one step up (-1) or down (1). */
  function nudge(dir: -1 | 1) {
    const sel = selection.value
    const d = doc.value
    if (!sel || !d) return
    if (sel.kind === 'section') {
      const i = d.sections.findIndex((s) => s.id === sel.sectionId)
      const to = i + dir
      if (i !== -1 && to >= 0 && to < d.sections.length) moveSection(sel.sectionId, dir === 1 ? to + 1 : to)
    } else {
      const found = locateBlock(d, sel.blockId)
      if (!found) return
      const to = found.index + dir
      if (to < 0 || to >= found.section.blocks.length) return
      // Stay in the same column when moving within a split section.
      moveBlock(sel.blockId, found.section.id, dir === 1 ? to + 1 : to, columnOf(found.section, found.index))
    }
  }

  function duplicate(sel: Selection = selection.value) {
    const d = doc.value
    if (!sel || !d) return
    if (sel.kind === 'section') {
      const i = d.sections.findIndex((s) => s.id === sel.sectionId)
      if (i !== -1) addSection(withFreshIds(d.sections[i]!), i + 1)
    } else {
      const found = locateBlock(d, sel.blockId)
      if (found) addBlock(withFreshIds(found.block), found.section.id, found.index + 1, columnOf(found.section, found.index))
    }
  }

  function remove(sel: Selection = selection.value) {
    if (!sel) return
    change((d) => {
      if (sel.kind === 'section') d.sections = d.sections.filter((s) => s.id !== sel.sectionId)
      else {
        const found = locateBlock(d, sel.blockId)
        if (found) removeFrom(found.section, found.index)
      }
    })
    selection.value = sel.kind === 'block' ? { kind: 'section', sectionId: sel.sectionId } : null
  }

  /* ---------- Library ---------- */
  const templateById = (id: string) => [...sectionTemplates, ...globalElements].find((t) => t.id === id)

  function sectionFromPayload(p: DragPayload): Section | undefined {
    if (p.kind === 'template') return templateById(p.id)?.create()
    if (p.kind === 'saved') {
      const item = saved.value.find((s) => s.id === p.id)
      if (item?.kind === 'section') return withFreshIds(item.section)
    }
  }

  function blockFromPayload(p: DragPayload): Block | undefined {
    if (p.kind === 'new-block') return createBlock(p.type)
    if (p.kind === 'saved') {
      const item = saved.value.find((s) => s.id === p.id)
      if (item?.kind === 'block') return withFreshIds(item.block)
    }
  }

  /** Places a library item or moves a canvas item to `target`. */
  function drop(p: DragPayload, target: DropTarget) {
    if (p.kind === 'move-section') {
      if (target.kind === 'section') moveSection(p.sectionId, target.index)
      return
    }
    if (p.kind === 'move-block') {
      if (target.kind === 'block') moveBlock(p.blockId, target.sectionId, target.index, target.column)
      else {
        const d = doc.value
        const found = d && locateBlock(d, p.blockId)
        if (!found) return
        // Dropped between sections: give the block a section of its own.
        const block = found.block
        change((draft) => {
          const f = locateBlock(draft, p.blockId)!
          removeFrom(f.section, f.index)
          draft.sections.splice(target.index, 0, createSection({}, [block]))
        })
        const section = doc.value?.sections[target.index]
        if (section) selection.value = { kind: 'block', sectionId: section.id, blockId: block.id }
      }
      return
    }
    const section = sectionFromPayload(p)
    if (section) {
      const d = doc.value
      // Sections dropped inside a section go after it.
      const index = target.kind === 'section' ? target.index : (d?.sections.findIndex((s) => s.id === target.sectionId) ?? -1) + 1
      addSection(section, index)
      return
    }
    const block = blockFromPayload(p)
    if (!block) return
    if (target.kind === 'block') addBlock(block, target.sectionId, target.index, target.column)
    else addBlockAsSection(block, target.index)
  }

  /** Adds a library item next to the current selection (click instead of drag). */
  function insert(p: DragPayload) {
    const d = doc.value
    if (!d) return
    const sel = selection.value
    const sectionIndex = sel ? d.sections.findIndex((s) => s.id === sel.sectionId) : -1
    if (isSectionPayload(p, saved.value)) {
      drop(p, { kind: 'section', index: sectionIndex === -1 ? d.sections.length : sectionIndex + 1 })
      return
    }
    if (sel?.kind === 'block') {
      const found = locateBlock(d, sel.blockId)
      if (found) return drop(p, { kind: 'block', sectionId: found.section.id, index: found.index + 1, column: columnOf(found.section, found.index) })
    }
    if (sel?.kind === 'section') return drop(p, { kind: 'block', sectionId: sel.sectionId, index: d.sections[sectionIndex]!.blocks.length })
    drop(p, { kind: 'section', index: d.sections.length })
  }

  function saveToLibrary(sel: Selection = selection.value, name?: string) {
    const d = doc.value
    if (!sel || !d) return
    const section = d.sections.find((s) => s.id === sel.sectionId)
    if (!section) return
    let item: SavedItem
    if (sel.kind === 'section') {
      item = { id: crypto.randomUUID(), kind: 'section', name: name || section.settings.name || 'Saved section', section: clone(section), savedAt: Date.now() }
    } else {
      const block = section.blocks.find((b) => b.id === sel.blockId)
      if (!block) return
      item = { id: crypto.randomUUID(), kind: 'block', name: name || 'Saved block', block: clone(block), savedAt: Date.now() }
    }
    queryClient.setQueryData<SavedItem[]>(studioSavedQueryKey, (old) => [item, ...(old ?? [])])
    notify(`Saved ‘${item.name}’ to your library`)
  }

  function removeSaved(id: string) {
    queryClient.setQueryData<SavedItem[]>(studioSavedQueryKey, (old) => old?.filter((s) => s.id !== id))
  }

  /** Adds images from the user's computer to the media library for this session. */
  function uploadMedia(files: FileList | File[]) {
    const assets: MediaAsset[] = [...files]
      .filter((f) => f.type.startsWith('image/'))
      .map((f) => ({ id: crypto.randomUUID(), name: f.name, src: URL.createObjectURL(f), uploaded: true }))
    if (assets.length) queryClient.setQueryData<MediaAsset[]>(studioMediaQueryKey, (old) => [...assets, ...(old ?? [])])
    return assets
  }

  /* ---------- Publishing ---------- */
  function publish() {
    const p = page.value
    if (!p) return
    writePage((old) => ({ ...old, published: clone(old.draft), publishedAt: Date.now() }))
    setPageStatus(p.pageId, 'live')
  }

  function discard() {
    const p = page.value
    if (!p || !hasChanges.value) return
    change((d) => Object.assign(d, clone(p.published)))
    keepSelectionValid(p.published)
    logActivity('You discarded unpublished changes', 'page')
    notify('Changes discarded')
  }

  return {
    query,
    page,
    doc,
    media,
    saved,
    selection,
    selectedSection,
    selectedBlock,
    device,
    preview,
    zoom,
    drag,
    dropTarget,
    textEditor,
    hasChanges,
    isPublished,
    canUndo: computed(() => past.value.length > 0),
    canRedo: computed(() => future.value.length > 0),
    select,
    change,
    undo,
    redo,
    setSectionValue,
    setBlockValue,
    addSection,
    addBlock,
    addBlockAsSection,
    moveSection,
    moveBlock,
    nudge,
    duplicate,
    remove,
    drop,
    insert,
    saveToLibrary,
    removeSaved,
    uploadMedia,
    publish,
    discard,
  }
}

export type Studio = ReturnType<typeof createStudio>

const StudioKey: InjectionKey<Studio> = Symbol('studio')

export function provideStudio(studio: Studio) {
  provide(StudioKey, studio)
  provide(textEditorKey, studio.textEditor)
}

/** The studio of the page being edited, for components inside the Design Studio view. */
export function useStudio() {
  const studio = inject(StudioKey)
  if (!studio) throw new Error('useStudio() must be used inside the Design Studio')
  return studio
}
