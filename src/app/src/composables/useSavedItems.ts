import { computed } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { savedItemsQueryKey, useSavedItemsQuerier } from '@/queriers/useSavedItemsQuerier'
import { useToast } from '@/composables/useToast'
import { clone } from '@/studio/defaults'
import type { PageDocument, SavedItem, Selection } from '@/studio/types'

/** The studio's library of saved sections and blocks. Kept in the query cache until it has an API. */
export function useSavedItems() {
  const queryClient = useQueryClient()
  const { notify } = useToast()

  const query = useSavedItemsQuerier().getSavedItems()
  const saved = computed(() => query.data.value ?? [])

  /** Saves the selected section or block of `doc` under `name`. */
  function saveToLibrary(doc: PageDocument | undefined, sel: Selection, name?: string) {
    const section = sel && doc?.sections.find((s) => s.id === sel.sectionId)
    if (!sel || !section) return

    let item: SavedItem
    if (sel.kind === 'section') {
      item = { id: crypto.randomUUID(), kind: 'section', name: name || section.settings.name || 'Saved section', section: clone(section), savedAt: Date.now() }
    } else {
      const block = section.blocks.find((b) => b.id === sel.blockId)
      if (!block) return
      item = { id: crypto.randomUUID(), kind: 'block', name: name || 'Saved block', block: clone(block), savedAt: Date.now() }
    }

    queryClient.setQueryData<SavedItem[]>(savedItemsQueryKey, (old) => [item, ...(old ?? [])])
    notify(`Saved ‘${item.name}’ to your library`)
  }

  function removeSaved(id: string) {
    queryClient.setQueryData<SavedItem[]>(savedItemsQueryKey, (old) => old?.filter((s) => s.id !== id))
  }

  return {
    saved,
    saveToLibrary,
    removeSaved,
  }
}
