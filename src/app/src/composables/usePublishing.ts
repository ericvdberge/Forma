import { useQueryClient } from '@tanstack/vue-query'
import type { EntityRefDto, PublishIssueDto } from '@/api/generated'
import { useActivity } from '@/composables/useActivity'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { useToast } from '@/composables/useToast'
import { pageContentQueryKey, publishedContentQueryKey } from '@/queriers/usePageContentQuerier'
import { usePublishingQuerier, type PublishOutcome } from '@/queriers/usePublishingQuerier'
import { useScheduledPublishesQuerier, type ScheduleOutcome } from '@/queriers/useScheduledPublishesQuerier'
import { selectionOf } from '@/composables/useScheduledPublishes'

/** What publishing one thing came to. */
export type PublishOneOutcome =
  | { kind: 'published'; warnings: PublishIssueDto[] }
  /** It needs other drafts: the "Publish together?" prompt is open. */
  | { kind: 'needs-others' }
  /** Something blocks it that no other draft fixes. */
  | { kind: 'blocked'; issues: PublishIssueDto[] }
  | { kind: 'failed' }

/** `Mon 24 Nov, 09:00` in the browser's time zone. */
export function formatWhen(at: string | number) {
  return new Date(at).toLocaleString(undefined, { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

/** `Mon 24 Nov at 09:00`, for sentences. */
export function formatWhenLong(at: string | number) {
  const d = new Date(at)
  const day = d.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
  const time = d.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  return `${day} at ${time}`
}

/** "Blog post page is live", "3 changes are live". */
function liveMessage(entities: EntityRefDto[]) {
  if (entities.length === 1) return `${entities[0]!.name} is live`
  return `${entities.length} changes are live`
}

/**
 * Putting drafts live (POST /publish) and scheduling them (POST /scheduled-publishes), with the toasts that say what
 * happened. Publishing one thing checks it first: when it needs other drafts, it asks "Publish together?" instead of failing.
 */
export function usePublishing() {
  const queryClient = useQueryClient()
  const querier = usePublishingQuerier()
  const scheduledQuerier = useScheduledPublishesQuerier()
  const { together, published } = usePublishDialog()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  /** Everything a publish can change: drafts, scheduled publishes, collections, items, pages and their live content. */
  async function refresh(entities: EntityRefDto[] = []) {
    for (const e of entities.filter((e) => e.kind === 'page')) {
      queryClient.removeQueries({ queryKey: pageContentQueryKey(e.id), type: 'inactive' })
      queryClient.invalidateQueries({ queryKey: publishedContentQueryKey(e.id) })
    }

    published.value++

    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['drafts'] }),
      queryClient.invalidateQueries({ queryKey: ['scheduled-publishes'] }),
      queryClient.invalidateQueries({ queryKey: [{ _id: 'getCollections' }] }),
      queryClient.invalidateQueries({ queryKey: [{ _id: 'getCollectionItems' }] }),
      queryClient.invalidateQueries({ queryKey: [{ _id: 'getCollectionFieldUsages' }] }),
      queryClient.invalidateQueries({ queryKey: [{ _id: 'getPages' }] }),
    ])
  }

  /** Publishes these drafts now. Toasts what happened, except when it is blocked: the caller shows why. */
  async function publishNow(websiteId: string, entities: EntityRefDto[]): Promise<PublishOutcome> {
    const outcome = await querier.publish({ websiteId, ...selectionOf(entities) })

    if (outcome.kind === 'published') {
      await refresh(entities)
      logActivity(`You published ${entities.length === 1 ? `‘${entities[0]!.name}’` : `${entities.length} changes`}`, 'page')
      notify(liveMessage(outcome.result.entities.length ? outcome.result.entities : entities))
    } else if (outcome.kind === 'not-found') {
      notify('Some of these changes don’t exist anymore. Reload and try again.', 'error')
    } else if (outcome.kind === 'failed') {
      notify('Couldn’t publish. Nothing went live. Try again.', 'error')
    }

    return outcome
  }

  /** Publish from an editor: this one thing now, or "Publish together?" when it needs other drafts. */
  async function publishOne(websiteId: string, entity: EntityRefDto): Promise<PublishOneOutcome> {
    const check = await querier.check({ websiteId, ...selectionOf([entity]) })

    if (!check) {
      notify('Couldn’t check the changes. Try again.', 'error')
      return { kind: 'failed' }
    }

    if (!check.canPublish && check.needs.length) {
      together.value = { websiteId, entity, issues: check.issues.filter((i) => i.severity === 'error'), needs: check.needs }
      return { kind: 'needs-others' }
    }

    if (!check.canPublish)
      return { kind: 'blocked', issues: check.issues }

    const outcome = await publishNow(websiteId, [entity])

    if (outcome.kind === 'published') return { kind: 'published', warnings: outcome.result.warnings }
    if (outcome.kind === 'blocked') return { kind: 'blocked', issues: outcome.issues }
    return { kind: 'failed' }
  }

  /** Schedules drafts for a time. */
  async function schedule(websiteId: string, entities: EntityRefDto[], publishAt: Date): Promise<ScheduleOutcome> {
    const outcome = await scheduledQuerier.schedule({ websiteId, publishAt: publishAt.toISOString(), ...selectionOf(entities) })
    afterSchedule(outcome, entities)
    return outcome
  }

  /** Changes when a scheduled publish runs; a failed one is scheduled again. */
  async function reschedule(publishId: string, publishAt: Date): Promise<ScheduleOutcome> {
    const outcome = await scheduledQuerier.change(publishId, { publishAt: publishAt.toISOString(), collections: null, pages: null, items: null })
    afterSchedule(outcome, [])
    return outcome
  }

  function afterSchedule(outcome: ScheduleOutcome, entities: EntityRefDto[]) {
    if (outcome.kind === 'scheduled') {
      void refresh(entities)
      notify(`Scheduled for ${formatWhen(outcome.publish.publishAt)}`)
    } else if (outcome.kind !== 'invalid') {
      notify('Couldn’t schedule it. Try again.', 'error')
    }
  }

  /** Cancels a scheduled publish. Its drafts stay drafts. */
  async function cancel(publishId: string, { quiet = false } = {}) {
    const cancelled = await scheduledQuerier.cancel(publishId)
    if (!cancelled) {
      notify('Couldn’t cancel the scheduled publish. Try again.', 'error')
      return false
    }
    await refresh()
    if (!quiet) notify('Scheduled publish cancelled. The changes stay as drafts.')
    return true
  }

  return {
    publishNow,
    publishOne,
    schedule,
    reschedule,
    cancel,
    refresh,
  }
}
