import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueries, useQuery } from '@tanstack/vue-query'
import {
  createCollectionItem,
  deleteCollectionItem,
  saveCollectionItem,
  type CollectionItemDto,
  type SaveCollectionItemRequest,
} from '@/api/generated'
import { getCollectionItemsOptions } from '@/api/generated/@tanstack/vue-query.gen'

/** The result of saving an item: errors are keyed `slug` or `values.{fieldId}`. */
export type ItemWriteOutcome =
  | { kind: 'saved'; item: CollectionItemDto }
  | { kind: 'invalid'; errors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'failed' }

async function toOutcome(call: Promise<{ data?: CollectionItemDto; error?: unknown; response?: Response }>): Promise<ItemWriteOutcome> {
  try {
    const { data, error, response } = await call
    if (data) return { kind: 'saved', item: data }
    if (response?.status === 400) return { kind: 'invalid', errors: (error as { errors?: Record<string, string[]> })?.errors ?? {} }
    if (response?.status === 404) return { kind: 'not-found' }
    return { kind: 'failed' }
  } catch {
    return { kind: 'failed' }
  }
}

/** TanStack Query calls for a collection's items. Call the `get…` functions during component setup. */
export function useCollectionItemsQuerier() {
  /** GET /collections/{collectionId}/items */
  function getItems(collectionId: MaybeRefOrGetter<string>) {
    return useQuery(
      computed(() => ({
        ...getCollectionItemsOptions({ path: { collectionId: toValue(collectionId) } }),
        enabled: !!toValue(collectionId),
      })),
    )
  }

  /** GET /collections/{collectionId}/items for several collections at once, e.g. every list on a page. */
  function getItemsOfMany(collectionIds: MaybeRefOrGetter<string[]>) {
    return useQueries({
      queries: computed(() =>
        toValue(collectionIds).map((collectionId) => getCollectionItemsOptions({ path: { collectionId } })),
      ),
    })
  }

  /** POST /collections/{collectionId}/items */
  function create(collectionId: string, body: SaveCollectionItemRequest) {
    return toOutcome(createCollectionItem({ path: { collectionId }, body }))
  }

  /** PUT /collections/{collectionId}/items/{itemId} */
  function save(collectionId: string, itemId: string, body: SaveCollectionItemRequest) {
    return toOutcome(saveCollectionItem({ path: { collectionId, itemId }, body }))
  }

  /** DELETE /collections/{collectionId}/items/{itemId}. True when it is gone. */
  async function remove(collectionId: string, itemId: string) {
    try {
      const { response } = await deleteCollectionItem({ path: { collectionId, itemId } })
      return !!response && (response.ok || response.status === 404)
    } catch {
      return false
    }
  }

  return {
    getItems,
    getItemsOfMany,
    create,
    save,
    remove,
  }
}
