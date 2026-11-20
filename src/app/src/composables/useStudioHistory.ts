import { computed, shallowRef, type Ref } from 'vue'
import { clone } from '@/studio/defaults'
import type { PageDocument } from '@/studio/types'

const HISTORY_LIMIT = 100
/** Edits to the same field within this window are one undo step (e.g. typing). */
const COALESCE_MS = 1000

/**
 * Undo and redo for the document being edited. `change` is the only way the editor writes the document:
 * it records an undo step and hands the new document to `write`.
 */
export function useStudioHistory(doc: Ref<PageDocument | undefined>, write: (next: PageDocument) => void) {
  const past = shallowRef<PageDocument[]>([])
  const future = shallowRef<PageDocument[]>([])
  let lastCoalesce: { key: string; at: number } | null = null

  /** Applies `mutate` to a copy of the document and stores it, recording an undo step. */
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
    write(next)
  }

  /** Steps back; returns the document it restored. */
  function undo() {
    const prev = past.value.at(-1)
    if (!prev || !doc.value) return

    future.value = [doc.value, ...future.value]
    past.value = past.value.slice(0, -1)
    lastCoalesce = null
    write(prev)
    return prev
  }

  /** Steps forward again; returns the document it restored. */
  function redo() {
    const next = future.value[0]
    if (!next || !doc.value) return

    past.value = [...past.value, doc.value]
    future.value = future.value.slice(1)
    lastCoalesce = null
    write(next)
    return next
  }

  function reset() {
    past.value = []
    future.value = []
    lastCoalesce = null
  }

  return {
    change,
    undo,
    redo,
    reset,
    canUndo: computed(() => past.value.length > 0),
    canRedo: computed(() => future.value.length > 0),
  }
}
