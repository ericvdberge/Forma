import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getCollectionItemsQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { CollectionItemDto } from '@/api/generated'
import type { Collection, CollectionItem, CollectionValue } from '@/data/types'
import { useCollectionItemsQuerier, type ItemWriteOutcome } from '@/queriers/useCollectionItemsQuerier'
import { useToast } from '@/composables/useToast'

export function toCollectionItem(dto: CollectionItemDto): CollectionItem {
  return {
    id: dto.id,
    collectionId: dto.collectionId,
    slug: dto.slug,
    status: dto.publishedAt ? 'live' : 'draft',
    values: dto.values as Record<string, CollectionValue>,
    draft: dto.draft
      ? {
          slug: dto.draft.slug,
          values: dto.draft.values as Record<string, CollectionValue>,
          updatedAt: dto.draft.updatedAt ? Date.parse(dto.draft.updatedAt) : null,
        }
      : null,
    updatedAt: Date.parse(dto.updatedAt),
    publishedAt: dto.publishedAt ? Date.parse(dto.publishedAt) : null,
  }
}

/** What the item editor sends. Empty values are left out, so they read as "not filled in". */
export interface ItemDraft {
  slug: string
  values: Record<string, CollectionValue | null>
}

/** The fields the item editor shows: the collection's draft fields while it has a draft, else its live ones. */
export function editorFields(collection: Collection) {
  return collection.draft?.fields ?? collection.fields
}

function toRequest(collection: Collection, draft: ItemDraft) {
  const known = new Set([...editorFields(collection), ...collection.fields].map((f) => f.id))
  const values = Object.fromEntries(
    Object.entries(draft.values).filter(([id, v]) => known.has(id) && v !== null && v !== ''),
  )
  return { slug: draft.slug, values }
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

  function afterSave(collection: Collection, outcome: ItemWriteOutcome, message: string | null) {
    if (outcome.kind === 'saved') {
      const dto = outcome.item
      writeCached(collection.id, (dtos) => [dto, ...dtos.filter((d) => d.id !== dto.id)].sort((a, b) => b.createdAt.localeCompare(a.createdAt)))
      void queryClient.invalidateQueries({ queryKey: ['drafts'] })
      if (message) notify(message)
    } else if (outcome.kind !== 'invalid') {
      notify(`Saving the ${collection.singularName.toLowerCase()} failed. Try again.`, 'error')
    }
    return outcome
  }

  /** Creates the item as a draft: visitors don't see it until it is published. */
  async function createItem(collection: Collection, draft: ItemDraft, { quiet = false } = {}) {
    const outcome = await querier.create(collection.id, toRequest(collection, draft))
    return afterSave(collection, outcome, quiet ? null : 'Saved as draft')
  }

  /** Saves the item's draft. Nothing changes on the site. */
  async function saveItem(collection: Collection, itemId: string, draft: ItemDraft, { quiet = false } = {}) {
    const outcome = await querier.saveDraft(collection.id, itemId, toRequest(collection, draft))
    return afterSave(collection, outcome, quiet ? null : 'Saved as draft')
  }

  /** Throws the draft away: the item goes back to its live version. */
  async function discardItem(collection: Collection, itemId: string, title: string) {
    const outcome = await querier.discardDraft(collection.id, itemId)
    return afterSave(collection, outcome, `Changes to ${title} discarded`)
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
    discardItem,
    deleteItem,
  }
}
