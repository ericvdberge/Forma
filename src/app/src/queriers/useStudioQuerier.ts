import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import homeJson from '@/data/studio/home.json'
import pageJson from '@/data/studio/page.json'
import mediaJson from '@/data/studio/media.json'
import savedJson from '@/data/studio/saved.json'
import { normalizeDocument, type PageDocumentJson } from '@/studio/sections'
import type { MediaAsset, SavedItem, StudioPage } from '@/studio/types'

export const studioPageQueryKey = (pageId: string) => ['studio', 'page', pageId] as const
export const studioMediaQueryKey = ['studio', 'media'] as const
export const studioSavedQueryKey = ['studio', 'saved'] as const

/** Mock latency so loading states are visible, like the real API will be. */
const delay = (ms = 250) => new Promise((r) => setTimeout(r, ms))

/** What a page's mock document is built from. */
export interface StudioPageSeed {
  title: string
  slug: string
  websiteName: string
  published: boolean
}

function mockDocument({ title, slug, websiteName }: StudioPageSeed) {
  if (slug === 'home') return normalizeDocument(homeJson as PageDocumentJson)
  const json = JSON.stringify(pageJson)
    .replaceAll('{{title}}', title.replace(/["\\<>]/g, ''))
    .replaceAll('{{website}}', websiteName.replace(/["\\<>]/g, ''))
  return normalizeDocument(JSON.parse(json) as PageDocumentJson)
}

/**
 * TanStack Query calls for the Design Studio. Served from the JSON files in `src/data/studio`
 * until the API has page-content endpoints. Edits are written straight to these cache entries
 * (see `useStudio`), so they never go stale or get collected while the app is open.
 */
export function useStudioQuerier() {
  function getStudioPage(pageId: MaybeRefOrGetter<string>, seed: MaybeRefOrGetter<StudioPageSeed | undefined>) {
    return useQuery(
      computed(() => ({
        queryKey: studioPageQueryKey(toValue(pageId)),
        queryFn: async (): Promise<StudioPage> => {
          await delay()
          const s = toValue(seed)!
          const doc = mockDocument(s)
          const now = Date.now()
          return {
            pageId: toValue(pageId),
            draft: doc,
            // Pages that are live start with the mock as their published version; drafts have never been published.
            published: structuredClone(doc),
            updatedAt: now,
            publishedAt: s.published ? now : null,
          }
        },
        enabled: !!toValue(pageId) && !!toValue(seed),
        staleTime: Infinity,
        gcTime: Infinity,
      })),
    )
  }

  function getMedia() {
    return useQuery({
      queryKey: studioMediaQueryKey,
      queryFn: async (): Promise<MediaAsset[]> => {
        await delay(150)
        return structuredClone(mediaJson)
      },
      staleTime: Infinity,
      gcTime: Infinity,
    })
  }

  function getSaved() {
    return useQuery({
      queryKey: studioSavedQueryKey,
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
    getStudioPage,
    getMedia,
    getSaved,
  }
}
