import { computed, type MaybeRefOrGetter } from 'vue'
import type { DraftKind, EntityRefDto, ScheduledPublishDto } from '@/api/generated'
import { useScheduledPublishesQuerier } from '@/queriers/useScheduledPublishesQuerier'

/** A website's upcoming and failed scheduled publishes (GET /scheduled-publishes). */
export function useScheduledPublishes(websiteId: MaybeRefOrGetter<string>) {
  const query = useScheduledPublishesQuerier().getScheduledPublishes(websiteId)

  const publishes = computed<ScheduledPublishDto[]>(() => query.data.value ?? [])
  const pending = computed(() => publishes.value.filter((p) => p.status === 'pending'))
  const failed = computed(() => publishes.value.filter((p) => p.status === 'failed'))

  const has = (p: ScheduledPublishDto, kind: DraftKind, id: string | undefined) => p.entries.some((e) => e.kind === kind && e.id === id)

  /** The pending publish a collection, page or item is in. */
  function pendingFor(kind: DraftKind, id: string | undefined) {
    return pending.value.find((p) => has(p, kind, id))
  }

  /** The failed publish a collection, page or item is in, so its editor can say it didn't go live. */
  function failedFor(kind: DraftKind, id: string | undefined) {
    return failed.value.find((p) => has(p, kind, id))
  }

  return {
    query,
    publishes,
    pending,
    failed,
    pendingFor,
    failedFor,
  }
}

/** The ids of a publish's entries, grouped as the API takes them. */
export function selectionOf(entries: EntityRefDto[]) {
  return {
    collections: entries.filter((e) => e.kind === 'collection').map((e) => e.id),
    pages: entries.filter((e) => e.kind === 'page').map((e) => e.id),
    items: entries.filter((e) => e.kind === 'item').map((e) => e.id),
  }
}
