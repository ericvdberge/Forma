import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getCollectionsQueryKey, getPagesQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { CollectionDto, PageDto } from '@/api/generated'
import type { Collection, CollectionField } from '@/data/types'
import { useCollectionsQuerier, type CollectionWriteOutcome } from '@/queriers/useCollectionsQuerier'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'

export function toCollection(dto: CollectionDto): Collection {
  return {
    id: dto.id,
    websiteId: dto.websiteId,
    name: dto.name,
    singularName: dto.singularName,
    slug: dto.slug,
    fields: dto.fields.map((f) => ({ ...f })),
    detailPageId: dto.detailPageId ?? null,
    updatedAt: Date.parse(dto.updatedAt),
  }
}

/** What the collection editor sends: names, slug and fields (new fields have no id yet). */
export interface CollectionDraft {
  name: string
  singularName: string
  slug: string
  fields: CollectionField[]
}

const toRequestFields = (fields: CollectionField[]) =>
  fields.map((f) => ({ id: f.id || null, label: f.label, type: f.type, required: f.required }))

/** The detail page as GET /pages describes it, so the studio can open it without reloading the pages. */
function detailPageDto(dto: CollectionDto, old?: PageDto): PageDto | undefined {
  if (!dto.detailPageId) return undefined
  const now = new Date().toISOString()
  return {
    id: dto.detailPageId,
    websiteId: dto.websiteId,
    title: `${dto.singularName} page`,
    slug: `${dto.slug}/{item}`,
    status: old?.status ?? 'draft',
    kind: 'collectionTemplate',
    collectionId: dto.id,
    createdAt: old?.createdAt ?? now,
    updatedAt: now,
  }
}

/**
 * A website's collections (Blog posts, Team members…), read from GET /collections and written through the
 * collections API. Nothing is fetched until a view calls `getCollections()`.
 */
export function useCollections() {
  const queryClient = useQueryClient()
  const querier = useCollectionsQuerier()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  /** The website's collections, by name. */
  function getCollections(websiteId: MaybeRefOrGetter<string>) {
    const query = querier.getCollections(websiteId)
    const collections = computed(() => (query.data.value ?? []).map(toCollection))
    return { ...query, collections }
  }

  /** One collection of the website, from the same list. */
  function getCollection(websiteId: MaybeRefOrGetter<string>, collectionId: MaybeRefOrGetter<string>) {
    const query = getCollections(websiteId)
    const collection = computed(() => query.collections.value.find((c) => c.id === toValue(collectionId)))
    return { ...query, collection }
  }

  function writeCached(dto: CollectionDto, { remove = false } = {}) {
    queryClient.setQueryData<CollectionDto[]>(getCollectionsQueryKey({ query: { websiteId: dto.websiteId } }), (old) => {
      const others = (old ?? []).filter((d) => d.id !== dto.id)
      return remove ? others : [...others, dto].sort((a, b) => a.name.localeCompare(b.name))
    })

    queryClient.setQueryData<PageDto[]>(getPagesQueryKey({ query: { websiteId: dto.websiteId } }), (old) => {
      if (!old) return old
      const existing = old.find((p) => p.id === dto.detailPageId)
      const others = old.filter((p) => p.id !== dto.detailPageId)
      const page = remove ? undefined : detailPageDto(dto, existing)
      return page ? [page, ...others] : others
    })
  }

  function afterWrite(outcome: CollectionWriteOutcome, message: string) {
    if (outcome.kind !== 'saved') {
      if (outcome.kind !== 'invalid') notify('Saving the collection failed. Try again.', 'error')
      return outcome
    }
    writeCached(outcome.collection)
    logActivity(message, 'page')
    notify(message.replace(/^You /, '').replace(/^./, (c) => c.toUpperCase()))
    return outcome
  }

  async function createCollection(websiteId: string, draft: CollectionDraft) {
    const outcome = await querier.create({ websiteId, ...draft, fields: toRequestFields(draft.fields) })
    return afterWrite(outcome, `You created the collection ‘${draft.name}’`)
  }

  async function updateCollection(collection: Collection, draft: CollectionDraft) {
    const outcome = await querier.update(collection.id, { ...draft, fields: toRequestFields(draft.fields) })
    return afterWrite(outcome, `You saved the collection ‘${draft.name}’`)
  }

  async function deleteCollection(collection: Collection) {
    const deleted = await querier.remove(collection.id)
    if (!deleted) {
      notify('Deleting the collection failed. Try again.', 'error')
      return false
    }
    const dto = queryClient
      .getQueryData<CollectionDto[]>(getCollectionsQueryKey({ query: { websiteId: collection.websiteId } }))
      ?.find((d) => d.id === collection.id)
    if (dto) writeCached(dto, { remove: true })
    notify(`‘${collection.name}’ deleted`)
    return true
  }

  return {
    getCollections,
    getCollection,
    createCollection,
    updateCollection,
    deleteCollection,
  }
}
