import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { getPageContent, savePageDraft, type PageContentDto } from '@/api/generated'
import pageJson from '@/data/studio/page.json'
import savedJson from '@/data/studio/saved.json'
import { fromApiDocument, toApiDocument } from '@/studio/contract'
import { normalizeDocument, type PageDocumentJson } from '@/studio/sections'
import type { PageDocument, SavedItem, StudioPage } from '@/studio/types'

export const studioPageQueryKey = (pageId: string) => ['studio', 'page', pageId] as const
export const studioSavedQueryKey = ['studio', 'saved'] as const

/** Mock latency for the Saved tab, which has no API yet. */
const delay = (ms = 250) => new Promise((r) => setTimeout(r, ms))

/** What a new page's starter content is filled in with. */
export interface StudioPageSeed {
  title: string
  websiteName: string
}

/** Thrown when the API has no such page (e.g. one created in this browser that the API doesn't have). */
export class StudioPageNotFoundError extends Error {
  constructor() {
    super('The page doesn’t exist on the server.')
  }
}

/** Starter content for a page that has none yet: a header, an intro and a footer. */
export function starterDocument({ title, websiteName }: StudioPageSeed): PageDocument {
  const json = JSON.stringify(pageJson)
    .replaceAll('{{title}}', title.replace(/["\\<>]/g, ''))
    .replaceAll('{{website}}', websiteName.replace(/["\\<>]/g, ''))
  return normalizeDocument(JSON.parse(json) as PageDocumentJson)
}

/** The API's page content as the studio keeps it. A page without content starts from the starter page (it saves as an edit). */
export function toStudioPage(dto: PageContentDto, seed?: StudioPageSeed): StudioPage {
  const saved = fromApiDocument(dto.draft)
  const empty = dto.draftRevision === 0 && saved.sections.length === 0
  return {
    pageId: dto.pageId,
    draft: empty && seed ? starterDocument(seed) : saved,
    published: dto.published ? fromApiDocument(dto.published) : null,
    revision: dto.draftRevision,
    savedJson: JSON.stringify(saved),
    updatedAt: dto.draftUpdatedAt ? Date.parse(dto.draftUpdatedAt) : Date.now(),
    publishedAt: dto.publishedAt ? Date.parse(dto.publishedAt) : null,
  }
}

export type SaveDraftOutcome =
  | { kind: 'saved'; revision: number }
  | { kind: 'conflict'; current: PageContentDto }
  | { kind: 'invalid'; errors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'failed' }

/**
 * TanStack Query calls for the Design Studio. Page content comes from the API; the Saved tab is still mock data
 * (phase 3). A page's cache entry is the editor's working copy, so it is never refetched in the background.
 */
export function useStudioQuerier() {
  /** GET /pages/{pageId}/content */
  function getStudioPage(pageId: MaybeRefOrGetter<string>, seed: MaybeRefOrGetter<StudioPageSeed | undefined>) {
    return useQuery(
      computed(() => ({
        queryKey: studioPageQueryKey(toValue(pageId)),
        queryFn: async ({ signal }: { signal: AbortSignal }): Promise<StudioPage> => {
          const { data, response } = await getPageContent({ path: { pageId: toValue(pageId) }, signal })
          if (response?.status === 404) throw new StudioPageNotFoundError()
          if (!data) throw new Error(`Loading the page failed (${response?.status}).`)
          return toStudioPage(data, toValue(seed))
        },
        enabled: !!toValue(pageId) && !!toValue(seed),
        staleTime: Infinity,
        gcTime: Infinity,
        refetchOnWindowFocus: false,
        retry: (count: number, error: Error) => !(error instanceof StudioPageNotFoundError) && count < 2,
      })),
    )
  }

  /** PUT /pages/{pageId}/draft */
  async function saveDraft(pageId: string, document: PageDocument, baseRevision: number): Promise<SaveDraftOutcome> {
    try {
      const { data, error, response } = await savePageDraft({
        path: { pageId },
        body: { document: toApiDocument(document), baseRevision },
      })
      if (data) return { kind: 'saved', revision: data.revision }
      if (response?.status === 409) return { kind: 'conflict', current: error as PageContentDto }
      if (response?.status === 400) return { kind: 'invalid', errors: (error as { errors?: Record<string, string[]> })?.errors ?? {} }
      if (response?.status === 404) return { kind: 'not-found' }
      return { kind: 'failed' }
    } catch {
      return { kind: 'failed' }
    }
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
    saveDraft,
    getSaved,
  }
}
