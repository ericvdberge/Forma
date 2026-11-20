import { useQuery } from '@tanstack/vue-query'
import savedJson from '@/data/studio/saved.json'
import { normalizeDocument, type PageDocumentJson } from '@/studio/sections'
import type { SavedItem } from '@/studio/types'

export const savedItemsQueryKey = ['studio', 'saved'] as const

/** Mock latency: the Saved tab has no API yet. */
const delay = (ms = 250) => new Promise((r) => setTimeout(r, ms))

/** TanStack Query calls for the studio's saved sections and blocks (mock data until it has an API). */
export function useSavedItemsQuerier() {
  function getSavedItems() {
    return useQuery({
      queryKey: savedItemsQueryKey,
      queryFn: async (): Promise<SavedItem[]> => {
        await delay(150)
        const now = Date.now()
        return savedJson.map((item, i): SavedItem => {
          const savedAt = now - (i + 1) * 86_400_000
          // Saved blocks are normalised by wrapping them in a section.
          const json = (item.kind === 'section' ? { sections: [item.section] } : { sections: [{ blocks: [item.block] }] }) as PageDocumentJson
          const section = normalizeDocument(json).sections[0]!
          return item.kind === 'section'
            ? { id: item.id, name: item.name, kind: 'section', section, savedAt }
            : { id: item.id, name: item.name, kind: 'block', block: section.blocks[0]!, savedAt }
        })
      },
      staleTime: Infinity,
      gcTime: Infinity,
    })
  }

  return {
    getSavedItems,
  }
}
