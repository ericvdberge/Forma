import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import {
  createCollection,
  deleteCollection,
  discardCollectionDraft,
  saveCollectionDraft,
  updateCollection,
  type CollectionDto,
  type CreateCollectionRequest,
  type SaveCollectionDraftRequest,
  type UpdateCollectionRequest,
} from '@/api/generated'
import { getCollectionFieldUsagesOptions, getCollectionsOptions } from '@/api/generated/@tanstack/vue-query.gen'
import { validationErrors } from '@/utils/validationErrors'

/** Matches every cached GET /collections query, whatever website it is for. */
export const allCollectionsQueryKey = [{ _id: 'getCollections' }] as const

/** The result of a write: errors are keyed by property (`name`, `slug`, `fields[0].label`…). */
export type CollectionWriteOutcome =
  | { kind: 'saved'; collection: CollectionDto }
  | { kind: 'invalid'; errors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'failed' }

async function toOutcome(call: Promise<{ data?: CollectionDto; error?: unknown; response?: Response }>): Promise<CollectionWriteOutcome> {
  try {
    const { data, error, response } = await call
    if (data) return { kind: 'saved', collection: data }
    if (response?.status === 400) return { kind: 'invalid', errors: validationErrors(error) }
    if (response?.status === 404) return { kind: 'not-found' }
    return { kind: 'failed' }
  } catch {
    return { kind: 'failed' }
  }
}

/** TanStack Query calls for the collections API. Call `getCollections` during component setup. */
export function useCollectionsQuerier() {
  /** GET /collections?websiteId=… Refetches when the (reactive) website id changes. */
  function getCollections(websiteId: MaybeRefOrGetter<string>) {
    return useQuery(
      computed(() => ({
        ...getCollectionsOptions({ query: { websiteId: toValue(websiteId) } }),
        enabled: !!toValue(websiteId),
      })),
    )
  }

  /** GET /collections/{collectionId}/usages: which live pages show which fields. */
  function getUsages(collectionId: MaybeRefOrGetter<string>) {
    return useQuery(
      computed(() => ({
        ...getCollectionFieldUsagesOptions({ path: { collectionId: toValue(collectionId) } }),
        enabled: !!toValue(collectionId),
      })),
    )
  }

  /** POST /collections */
  function create(body: CreateCollectionRequest) {
    return toOutcome(createCollection({ body }))
  }

  /** PUT /collections/{collectionId} */
  function update(collectionId: string, body: UpdateCollectionRequest) {
    return toOutcome(updateCollection({ path: { collectionId }, body }))
  }

  /** PUT /collections/{collectionId}/draft */
  function saveDraft(collectionId: string, body: SaveCollectionDraftRequest) {
    return toOutcome(saveCollectionDraft({ path: { collectionId }, body }))
  }

  /** DELETE /collections/{collectionId}/draft */
  function discardDraft(collectionId: string) {
    return toOutcome(discardCollectionDraft({ path: { collectionId } }))
  }

  /** DELETE /collections/{collectionId}. True when it is gone. */
  async function remove(collectionId: string) {
    try {
      const { response } = await deleteCollection({ path: { collectionId } })
      return !!response && (response.ok || response.status === 404)
    } catch {
      return false
    }
  }

  return {
    getCollections,
    getUsages,
    create,
    update,
    saveDraft,
    discardDraft,
    remove,
  }
}
