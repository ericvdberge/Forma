import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import {
  checkPublish,
  getDrafts,
  publish as publishDrafts,
  type PublishCheckDto,
  type PublishIssueDto,
  type PublishNeedDto,
  type PublishRequest,
  type PublishResultDto,
} from '@/api/generated'

export const draftsQueryKey = (websiteId: string) => ['drafts', websiteId] as const

export type PublishOutcome =
  | { kind: 'published'; result: PublishResultDto }
  /** Something blocks it; nothing was published. */
  | { kind: 'blocked'; issues: PublishIssueDto[]; needs: PublishNeedDto[] }
  | { kind: 'not-found' }
  | { kind: 'failed' }

/** TanStack Query calls for drafts and publishing (GET /drafts, POST /publish/check, POST /publish). */
export function usePublishingQuerier() {
  /** GET /drafts?websiteId=… */
  function getDrafts$(websiteId: MaybeRefOrGetter<string>) {
    return useQuery(
      computed(() => ({
        queryKey: draftsQueryKey(toValue(websiteId)),
        queryFn: async ({ signal }: { signal: AbortSignal }) => {
          const { data, response } = await getDrafts({ query: { websiteId: toValue(websiteId) }, signal })
          if (!response?.ok) throw new Error(`Loading the drafts failed (${response?.status}).`)
          return data ?? []
        },
        enabled: !!toValue(websiteId),
      })),
    )
  }

  /** POST /publish/check. `null` when it couldn't be checked. */
  async function check(body: PublishRequest): Promise<PublishCheckDto | null> {
    try {
      const { data } = await checkPublish({ body })
      return data ?? null
    } catch {
      return null
    }
  }

  /** POST /publish */
  async function publish(body: PublishRequest): Promise<PublishOutcome> {
    try {
      const { data, error, response } = await publishDrafts({ body })
      if (data) return { kind: 'published', result: data }
      if (response?.status === 400) {
        const problem = error as { issues?: PublishIssueDto[]; needs?: PublishNeedDto[] }
        return { kind: 'blocked', issues: problem?.issues ?? [], needs: problem?.needs ?? [] }
      }
      if (response?.status === 404) return { kind: 'not-found' }
      return { kind: 'failed' }
    } catch {
      return { kind: 'failed' }
    }
  }

  return {
    getDrafts: getDrafts$,
    check,
    publish,
  }
}
