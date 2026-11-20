import type { Ref } from 'vue'
import { createBlock } from '@/studio/blocks'
import { createSection, globalElements, sectionTemplates, withFreshIds } from '@/studio/sections'
import type { Block, BlockType, PageDocument, SavedItem, Section, Selection } from '@/studio/types'

/** Something being dragged onto or around the canvas. */
export type DragPayload =
  | { kind: 'new-block'; type: BlockType }
  | { kind: 'template'; id: string }
  | { kind: 'saved'; id: string }
  | { kind: 'move-block'; blockId: string }
  | { kind: 'move-section'; sectionId: string }

/** Where a drop would land: between sections, or at an index inside a section (and column, for split layouts). */
export type DropTarget = { kind: 'section'; index: number } | { kind: 'block'; sectionId: string; index: number; column?: 0 | 1 }

/** The Add dialog: a new section at an index, or a block into the selected section. */
export type Adding = null | { mode: 'section'; index: number } | { mode: 'block' }

/** Whether a drag payload is placed as a section (not as a block inside one). */
export function isSectionPayload(p: DragPayload, saved: SavedItem[]) {
  if (p.kind === 'template' || p.kind === 'move-section') return true
  if (p.kind === 'saved') return saved.find((s) => s.id === p.id)?.kind === 'section'
  return false
}

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

const templateById = (id: string) => [...sectionTemplates, ...globalElements].find((t) => t.id === id)

interface EditingContext {
  doc: Ref<PageDocument | undefined>
  selection: Ref<Selection>
  saved: Ref<SavedItem[]>
  adding: Ref<Adding>
  change: (mutate: (d: PageDocument) => void, coalesceKey?: string) => void
}

/** Edits to the document: values, adding, moving, duplicating and removing sections and blocks, and drag and drop. */
export function useStudioEditing({ doc, selection, saved, adding, change }: EditingContext) {
  /** Keeps the selection pointing at something that exists in `d`, e.g. after undo. */
  function keepSelectionValid(d: PageDocument) {
    const sel = selection.value
    if (!sel) return
    const section = d.sections.find((s) => s.id === sel.sectionId)
    if (!section) selection.value = null
    else if (sel.kind === 'block' && !section.blocks.some((b) => b.id === sel.blockId)) selection.value = { kind: 'section', sectionId: section.id }
  }

  /* ---------- Values ---------- */
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

  /* ---------- Adding and moving ---------- */
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
      return
    }

    const found = locateBlock(d, sel.blockId)
    if (!found) return
    const to = found.index + dir
    if (to < 0 || to >= found.section.blocks.length) return
    // Stay in the same column when moving within a split section.
    moveBlock(sel.blockId, found.section.id, dir === 1 ? to + 1 : to, columnOf(found.section, found.index))
  }

  function duplicate(sel: Selection = selection.value) {
    const d = doc.value
    if (!sel || !d) return

    if (sel.kind === 'section') {
      const i = d.sections.findIndex((s) => s.id === sel.sectionId)
      if (i !== -1) addSection(withFreshIds(d.sections[i]!), i + 1)
      return
    }

    const found = locateBlock(d, sel.blockId)
    if (found) addBlock(withFreshIds(found.block), found.section.id, found.index + 1, columnOf(found.section, found.index))
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

  /* ---------- Library and drag and drop ---------- */
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

  /** A block dropped between sections gets a section of its own. */
  function moveBlockToNewSection(blockId: string, index: number) {
    const found = doc.value && locateBlock(doc.value, blockId)
    if (!found) return
    const block = found.block
    change((draft) => {
      const f = locateBlock(draft, blockId)!
      removeFrom(f.section, f.index)
      draft.sections.splice(index, 0, createSection({}, [block]))
    })
    const section = doc.value?.sections[index]
    if (section) selection.value = { kind: 'block', sectionId: section.id, blockId: block.id }
  }

  /** Places a library item or moves a canvas item to `target`. */
  function drop(p: DragPayload, target: DropTarget) {
    if (p.kind === 'move-section') {
      if (target.kind === 'section') moveSection(p.sectionId, target.index)
      return
    }

    if (p.kind === 'move-block') {
      if (target.kind === 'block') moveBlock(p.blockId, target.sectionId, target.index, target.column)
      else moveBlockToNewSection(p.blockId, target.index)
      return
    }

    const section = sectionFromPayload(p)
    if (section) {
      // Sections dropped inside a section go after it.
      const index = target.kind === 'section' ? target.index : (doc.value?.sections.findIndex((s) => s.id === target.sectionId) ?? -1) + 1
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

    if (isSectionPayload(p, saved.value)) return drop(p, { kind: 'section', index: sectionIndex === -1 ? d.sections.length : sectionIndex + 1 })

    if (sel?.kind === 'block') {
      const found = locateBlock(d, sel.blockId)
      if (found) return drop(p, { kind: 'block', sectionId: found.section.id, index: found.index + 1, column: columnOf(found.section, found.index) })
    }

    if (sel?.kind === 'section') return drop(p, { kind: 'block', sectionId: sel.sectionId, index: d.sections[sectionIndex]!.blocks.length })

    drop(p, { kind: 'section', index: d.sections.length })
  }

  /** Adds what was picked in the Add dialog where it was opened. */
  function addPicked(p: DragPayload) {
    const a = adding.value
    adding.value = null
    if (!a) return
    if (a.mode === 'section') drop(p, { kind: 'section', index: a.index })
    else insert(p)
  }

  return {
    keepSelectionValid,
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
    addPicked,
  }
}
