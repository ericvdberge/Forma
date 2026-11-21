import { computed, type MaybeRefOrGetter } from 'vue'
import type { DraftDto, DraftKind } from '@/api/generated'
import { usePublishingQuerier } from '@/queriers/usePublishingQuerier'

/** A website's drafts (GET /drafts): what isn't live yet, for the top bar, the publish dialog and the editors' status. */
export function useDrafts(websiteId: MaybeRefOrGetter<string>) {
  const query = usePublishingQuerier().getDrafts(websiteId)

  const drafts = computed<DraftDto[]>(() => query.data.value ?? [])
  const count = computed(() => drafts.value.length)

  /** The draft of one collection, page or item, if it has one. */
  function draftOf(kind: DraftKind, id: string | undefined) {
    return drafts.value.find((d) => d.entity.kind === kind && d.entity.id === id)
  }

  return {
    query,
    drafts,
    count,
    draftOf,
  }
}
