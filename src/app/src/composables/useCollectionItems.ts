import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getCollectionItemsQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { CollectionItemDto } from '@/api/generated'
import type { Collection, CollectionItem, CollectionValue, PageStatus } from '@/data/types'
import { useCollectionItemsQuerier, type ItemWriteOutcome } from '@/queriers/useCollectionItemsQuerier'
import { useToast } from '@/composables/useToast'

export function toCollectionItem(dto: CollectionItemDto): CollectionItem {
  return {
    id: dto.id,
    collectionId: dto.collectionId,
    slug: dto.slug,
    status: dto.status === 'published' ? 'live' : 'draft',
    values: dto.values as Record<string, CollectionValue>,
    updatedAt: Date.parse(dto.updatedAt),
    publishedAt: dto.publishedAt ? Date.parse(dto.publishedAt) : null,
  }
}

/** What the item editor sends. Empty values are left out, so they read as "not filled in". */
export interface ItemDraft {
  slug: string
  values: Record<string, CollectionValue | null>
  status: PageStatus
}

function toRequest(collection: Collection, draft: ItemDraft) {
  const known = new Set(collection.fields.map((f) => f.id))
  const values = Object.fromEntries(
    Object.entries(draft.values).filter(([id, v]) => known.has(id) && v !== null && v !== ''),
  )
  return { slug: draft.slug, values, status: draft.status === 'live' ? ('published' as const) : ('draft' as const) }
}

/** The items of a collection (the posts of a blog…), read from and written to the collection items API. */
export function useCollectionItems() {
  const queryClient = useQueryClient()
  const querier = useCollectionItemsQuerier()
  const { notify } = useToast()

  /** A collection's items, newest first. */
  function getItems(collectionId: MaybeRefOrGetter<string>) {
    const query = querier.getItems(collectionId)
    const items = computed(() => (query.data.value ?? []).map(toCollectionItem))
    return { ...query, items }
  }

  function writeCached(collectionId: string, update: (dtos: CollectionItemDto[]) => CollectionItemDto[]) {
    queryClient.setQueryData<CollectionItemDto[]>(getCollectionItemsQueryKey({ path: { collectionId } }), (old) => update(old ?? []))
  }

  function afterSave(collection: Collection, outcome: ItemWriteOutcome, published: boolean) {
    if (outcome.kind === 'saved') {
      const dto = outcome.item
      writeCached(collection.id, (dtos) => [dto, ...dtos.filter((d) => d.id !== dto.id)].sort((a, b) => b.createdAt.localeCompare(a.createdAt)))
      notify(published ? `${collection.singularName} published` : `${collection.singularName} saved`)
    } else if (outcome.kind !== 'invalid') {
      notify(`Saving the ${collection.singularName.toLowerCase()} failed. Try again.`, 'error')
    }
    return outcome
  }

  async function createItem(collection: Collection, draft: ItemDraft) {
    const outcome = await querier.create(collection.id, toRequest(collection, draft))
    return afterSave(collection, outcome, draft.status === 'live')
  }

  async function saveItem(collection: Collection, itemId: string, draft: ItemDraft) {
    const outcome = await querier.save(collection.id, itemId, toRequest(collection, draft))
    return afterSave(collection, outcome, draft.status === 'live')
  }

  async function deleteItem(collection: Collection, item: CollectionItem, title: string) {
    const deleted = await querier.remove(collection.id, item.id)
    if (!deleted) {
      notify(`Deleting the ${collection.singularName.toLowerCase()} failed. Try again.`, 'error')
      return false
    }
    writeCached(collection.id, (dtos) => dtos.filter((d) => d.id !== item.id))
    notify(`‘${title}’ deleted`)
    return true
  }

  return {
    getItems,
    createItem,
    saveItem,
    deleteItem,
  }
}
