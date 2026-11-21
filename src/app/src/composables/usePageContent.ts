import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { pageContentQueryKey, usePageContentQuerier, type StudioPageSeed } from '@/queriers/usePageContentQuerier'
import type { StudioPage } from '@/studio/types'

/**
 * A page's Design Studio content: loaded from GET /pages/{id}/content (the draft, and the live version) into the query
 * cache, where the editor keeps its working copy. Save stores the draft with PUT /pages/{id}/content/draft; nothing
 * changes on the site until it is published.
 */
export function usePageContent(pageId: MaybeRefOrGetter<string>, seed: MaybeRefOrGetter<StudioPageSeed | undefined>) {
  const queryClient = useQueryClient()
  const querier = usePageContentQuerier()

  const query = querier.getPageContent(pageId, seed)
  const page = computed(() => query.data.value)

  /** Changes the cached content of `id` (the open page by default). */
  function writePage(update: (p: StudioPage) => StudioPage, id = toValue(pageId)) {
    queryClient.setQueryData<StudioPage>(pageContentQueryKey(id), (old) => (old ? update(old) : old))
  }

  return {
    query,
    page,
    writePage,
    saveDraft: querier.saveDraft,
    discardDraft: querier.discardDraft,
  }
}
