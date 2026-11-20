import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { pageContentQueryKey, usePageContentQuerier, type StudioPageSeed } from '@/queriers/usePageContentQuerier'
import type { StudioPage } from '@/studio/types'

/**
 * A page's Design Studio content: loaded from GET /pages/{id}/content into the query cache, where the editor keeps its
 * working copy, and published with PUT /pages/{id}/content. Nothing is sent to the server until Publish.
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
    publish: querier.publish,
  }
}
