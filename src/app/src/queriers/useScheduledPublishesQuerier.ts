import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import {
  cancelScheduledPublish,
  getScheduledPublishes,
  schedulePublish,
  updateScheduledPublish,
  type PublishIssueDto,
  type PublishNeedDto,
  type ScheduledPublishDto,
  type SchedulePublishRequest,
  type UpdateScheduledPublishRequest,
} from '@/api/generated'
import { validationErrors } from '@/utils/validationErrors'

export const scheduledPublishesQueryKey = (websiteId: string) => ['scheduled-publishes', websiteId] as const

export type ScheduleOutcome =
  | { kind: 'scheduled'; publish: ScheduledPublishDto }
  /** The time is in the past, a draft is in another publish, or the check found errors. */
  | { kind: 'invalid'; errors: Record<string, string[]>; issues: PublishIssueDto[]; needs: PublishNeedDto[] }
  | { kind: 'not-found' }
  | { kind: 'failed' }

async function toOutcome(call: Promise<{ data?: ScheduledPublishDto; error?: unknown; response?: Response }>): Promise<ScheduleOutcome> {
  try {
    const { data, error, response } = await call
    if (data) return { kind: 'scheduled', publish: data }
    if (response?.status === 400) {
      const problem = error as { issues?: PublishIssueDto[]; needs?: PublishNeedDto[] }
      return { kind: 'invalid', errors: validationErrors(error), issues: problem?.issues ?? [], needs: problem?.needs ?? [] }
    }
    if (response?.status === 404) return { kind: 'not-found' }
    return { kind: 'failed' }
  } catch {
    return { kind: 'failed' }
  }
}

/** TanStack Query calls for scheduled publishes. */
export function useScheduledPublishesQuerier() {
  /** GET /scheduled-publishes?websiteId=…, refreshed every half minute so a publish that ran shows up. */
  function getScheduledPublishes$(websiteId: MaybeRefOrGetter<string>) {
    return useQuery(
      computed(() => ({
        queryKey: scheduledPublishesQueryKey(toValue(websiteId)),
        queryFn: async ({ signal }: { signal: AbortSignal }) => {
          const { data, response } = await getScheduledPublishes({ query: { websiteId: toValue(websiteId) }, signal })
          if (!response?.ok) throw new Error(`Loading the scheduled publishes failed (${response?.status}).`)
          return data ?? []
        },
        enabled: !!toValue(websiteId),
        refetchInterval: 30_000,
      })),
    )
  }

  /** POST /scheduled-publishes */
  function schedule(body: SchedulePublishRequest) {
    return toOutcome(schedulePublish({ body }))
  }

  /** PUT /scheduled-publishes/{id} */
  function change(id: string, body: UpdateScheduledPublishRequest) {
    return toOutcome(updateScheduledPublish({ path: { id }, body }))
  }

  /** DELETE /scheduled-publishes/{id}. True when it is cancelled. */
  async function cancel(id: string) {
    try {
      const { response } = await cancelScheduledPublish({ path: { id } })
      return !!response && (response.ok || response.status === 404)
    } catch {
      return false
    }
  }

  return {
    getScheduledPublishes: getScheduledPublishes$,
    schedule,
    change,
    cancel,
  }
}
