import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { discardPageDraft, getPageContent, savePageDraft, type PageContentDto } from '@/api/generated'
import pageJson from '@/data/studio/page.json'
import type { Collection } from '@/data/types'
import { fromApiDocument, toApiDocument } from '@/studio/contract'
import { detailPageSections } from '@/studio/detailPage'
import { normalizeDocument, type PageDocumentJson } from '@/studio/sections'
import type { PageDocument, StudioPage } from '@/studio/types'
import { validationErrors } from '@/utils/validationErrors'

export const pageContentQueryKey = (pageId: string) => ['page-content', pageId] as const
export const publishedContentQueryKey = (pageId: string) => ['published-page-content', pageId] as const

/** What a new page's starter content is filled in with. */
export interface StudioPageSeed {
  title: string
  websiteName: string
  /** On a collection's detail page: the collection, so the starter design shows its fields. */
  collection?: Collection
}

/** Thrown when the API has no such page (e.g. one created in this browser that the API doesn't have). */
export class StudioPageNotFoundError extends Error {
  constructor() {
    super('The page doesn’t exist on the server.')
  }
}

/**
 * Starter content for a page that has none yet: a header, an intro and a footer. A collection's detail page gets
 * sections that show the item's fields in place of the intro.
 */
export function starterDocument({ title, websiteName, collection }: StudioPageSeed): PageDocument {
  const json = JSON.stringify(pageJson)
    .replaceAll('{{title}}', title.replace(/["\\<>]/g, ''))
    .replaceAll('{{website}}', websiteName.replace(/["\\<>]/g, ''))
  const doc = normalizeDocument(JSON.parse(json) as PageDocumentJson)
  if (!collection) return doc

  const intro = doc.sections.findIndex((s) => s.id === 's_intro')
  doc.sections.splice(intro, 1, ...detailPageSections(collection))
  return doc
}

/**
 * The API's page content as the studio keeps it: the draft (or the live version without one) as the working copy.
 * A page without content starts from the starter page, which isn't saved until Save.
 */
export function toStudioPage(pageId: string, draft: PageContentDto | undefined, live: PageContentDto | undefined, seed: StudioPageSeed): StudioPage {
  const saved = draft ? fromApiDocument(draft.document) : null
  const published = live ? fromApiDocument(live.document) : null
  return {
    pageId,
    draft: saved ?? starterDocument(seed),
    saved,
    hasDraft: !!draft?.hasDraft,
    draftSavedAt: draft?.draftUpdatedAt ? Date.parse(draft.draftUpdatedAt) : null,
    published,
    updatedAt: draft ? Date.parse(draft.updatedAt) : Date.now(),
    publishedAt: draft?.publishedAt ? Date.parse(draft.publishedAt) : null,
  }
}

export type SaveDraftOutcome =
  | { kind: 'saved'; content: PageContentDto }
  /** The server refused the document: something the studio produced that the API doesn't accept. */
  | { kind: 'invalid'; errors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'failed' }

async function read(pageId: string, version: 'draft' | 'live', signal?: AbortSignal) {
  const { data, response } = await getPageContent({ path: { pageId }, query: { version }, signal })
  if (response?.status === 404) throw new StudioPageNotFoundError()
  if (!response?.ok) throw new Error(`Loading the page failed (${response?.status}).`)
  return response.status === 204 ? undefined : (data as PageContentDto)
}

/** TanStack Query calls for a page's content. A page's cache entry is the editor's working copy, so it is never refetched. */
export function usePageContentQuerier() {
  /** GET /pages/{pageId}/content?version=draft, and the live version next to it. */
  function getPageContent$(pageId: MaybeRefOrGetter<string>, seed: MaybeRefOrGetter<StudioPageSeed | undefined>) {
    return useQuery(
      computed(() => ({
        queryKey: pageContentQueryKey(toValue(pageId)),
        queryFn: async ({ signal }: { signal: AbortSignal }): Promise<StudioPage> => {
          const id = toValue(pageId)
          const [draft, live] = await Promise.all([read(id, 'draft', signal), read(id, 'live', signal)])
          return toStudioPage(id, draft, live, toValue(seed)!)
        },
        enabled: !!toValue(pageId) && !!toValue(seed),
        staleTime: Infinity,
        gcTime: Infinity,
        refetchOnWindowFocus: false,
        retry: (count: number, error: Error) => !(error instanceof StudioPageNotFoundError) && count < 2,
      })),
    )
  }

  /**
   * GET /pages/{pageId}/content: only what is live, `null` when the page has none yet. Kept apart from the editor's
   * working copy and refetched each time, so it shows the latest publish.
   */
  function getPublishedContent(pageId: MaybeRefOrGetter<string | undefined>) {
    return useQuery(
      computed(() => ({
        queryKey: publishedContentQueryKey(toValue(pageId) ?? ''),
        queryFn: async ({ signal }: { signal: AbortSignal }): Promise<PageDocument | null> => {
          try {
            const live = await read(toValue(pageId)!, 'live', signal)
            return live ? fromApiDocument(live.document) : null
          } catch (error) {
            if (error instanceof StudioPageNotFoundError) return null
            throw error
          }
        },
        enabled: !!toValue(pageId),
        staleTime: 0,
      })),
    )
  }

  /** PUT /pages/{pageId}/content/draft */
  async function saveDraft(pageId: string, document: PageDocument): Promise<SaveDraftOutcome> {
    try {
      const { data, error, response } = await savePageDraft({ path: { pageId }, body: { document: toApiDocument(document) } })
      if (data) return { kind: 'saved', content: data }
      if (response?.status === 400) return { kind: 'invalid', errors: validationErrors(error) }
      if (response?.status === 404) return { kind: 'not-found' }
      return { kind: 'failed' }
    } catch {
      return { kind: 'failed' }
    }
  }

  /** DELETE /pages/{pageId}/content/draft. True when the draft is gone. */
  async function discardDraft(pageId: string) {
    try {
      const { response } = await discardPageDraft({ path: { pageId } })
      return !!response?.ok
    } catch {
      return false
    }
  }

  return {
    getPageContent: getPageContent$,
    getPublishedContent,
    saveDraft,
    discardDraft,
  }
}
