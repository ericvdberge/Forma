import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { getPageContent, publishPageContent, type ContentIssueDto, type PageContentDto } from '@/api/generated'
import pageJson from '@/data/studio/page.json'
import { fromApiDocument, toApiDocument } from '@/studio/contract'
import { normalizeDocument, type PageDocumentJson } from '@/studio/sections'
import type { PageDocument, StudioPage } from '@/studio/types'

export const pageContentQueryKey = (pageId: string) => ['page-content', pageId] as const

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

/** The API's page content as the studio keeps it. A page without content starts from the starter page. */
export function toStudioPage(pageId: string, dto: PageContentDto | undefined, seed: StudioPageSeed): StudioPage {
  const document = dto ? fromApiDocument(dto.document) : starterDocument(seed)
  const published = dto?.status === 'published'
  return {
    pageId,
    draft: document,
    published: published ? document : null,
    updatedAt: dto ? Date.parse(dto.updatedAt) : Date.now(),
    publishedAt: published ? Date.parse(dto.updatedAt) : null,
  }
}

export type PublishOutcome =
  | { kind: 'published'; content: PageContentDto; warnings: ContentIssueDto[] }
  /** The server refused the document: `issues` are the errors to fix, empty when the document itself is malformed. */
  | { kind: 'invalid'; issues: ContentIssueDto[]; errors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'failed' }

/** TanStack Query calls for a page's content. A page's cache entry is the editor's working copy, so it is never refetched. */
export function usePageContentQuerier() {
  /** GET /pages/{pageId}/content */
  function getPageContent$(pageId: MaybeRefOrGetter<string>, seed: MaybeRefOrGetter<StudioPageSeed | undefined>) {
    return useQuery(
      computed(() => ({
        queryKey: pageContentQueryKey(toValue(pageId)),
        queryFn: async ({ signal }: { signal: AbortSignal }): Promise<StudioPage> => {
          const id = toValue(pageId)
          const { data, response } = await getPageContent({ path: { pageId: id }, signal })
          if (response?.status === 404) throw new StudioPageNotFoundError()
          if (!response?.ok) throw new Error(`Loading the page failed (${response?.status}).`)
          return toStudioPage(id, response.status === 204 ? undefined : (data as PageContentDto), toValue(seed)!)
        },
        enabled: !!toValue(pageId) && !!toValue(seed),
        staleTime: Infinity,
        gcTime: Infinity,
        refetchOnWindowFocus: false,
        retry: (count: number, error: Error) => !(error instanceof StudioPageNotFoundError) && count < 2,
      })),
    )
  }

  /** PUT /pages/{pageId}/content */
  async function publish(pageId: string, document: PageDocument): Promise<PublishOutcome> {
    try {
      const { data, error, response } = await publishPageContent({ path: { pageId }, body: { document: toApiDocument(document) } })
      if (data) return { kind: 'published', content: data.content, warnings: data.warnings }
      if (response?.status === 400) {
        const problem = error as { errors?: Record<string, string[]>; issues?: ContentIssueDto[] }
        return { kind: 'invalid', issues: problem?.issues ?? [], errors: problem?.errors ?? {} }
      }
      if (response?.status === 404) return { kind: 'not-found' }
      return { kind: 'failed' }
    } catch {
      return { kind: 'failed' }
    }
  }

  return {
    getPageContent: getPageContent$,
    publish,
  }
}
