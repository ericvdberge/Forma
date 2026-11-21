import { computed, ref, toValue, watch, type MaybeRefOrGetter, type Ref } from 'vue'
import { useCollections } from '@/composables/useCollections'
import { toCollectionItem } from '@/composables/useCollectionItems'
import type { Collection, CollectionItem, CollectionValue } from '@/data/types'
import { useCollectionItemsQuerier } from '@/queriers/useCollectionItemsQuerier'
import type { StudioCollectionData } from '@/studio/inject'
import type { PageDocument } from '@/studio/types'

/** The ids of the collections a document lists with Collection cards. */
function listedCollections(doc: PageDocument | undefined) {
  const ids = new Set<string>()
  for (const section of doc?.sections ?? [])
    for (const block of section.blocks)
      if (block.type === 'collectionCards' && block.props.collectionId) ids.add(block.props.collectionId)
  return ids
}

/** A collection as it will be once its draft is published: the draft's address and fields. */
function asDrafted(collection: Collection): Collection {
  return collection.draft ? { ...collection, slug: collection.draft.slug, fields: collection.draft.fields } : collection
}

/**
 * An item as it will be once its draft, and its collection's draft, are published: the draft's slug and values, with
 * the values of removed fields moved where the collection's draft says (the server does the same when it publishes).
 */
function draftedItem(collection: Collection | undefined, item: CollectionItem): CollectionItem {
  const values: Record<string, CollectionValue> = { ...(item.draft?.values ?? item.values) }
  const drafted = { ...item, slug: item.draft?.slug ?? item.slug, values }
  if (!collection?.draft) return drafted

  const kept = new Set(collection.draft.fields.map((f) => f.id))
  const filled = (v: unknown) => v !== undefined && v !== null && v !== ''
  for (const [from, to] of Object.entries(collection.draft.valueMoves)) {
    if (to && kept.has(to) && !kept.has(from) && filled(values[from]) && !filled(values[to])) values[to] = values[from]!
  }
  for (const id of Object.keys(values)) if (!kept.has(id)) delete values[id]
  return drafted
}

/**
 * The collections the studio (and the website preview) shows: the website's collections, the items of every collection the page lists, and on a
 * collection's detail page the item it is previewed with. Items load as soon as a list or the detail page needs them.
 */
export function useStudioCollections(
  websiteId: MaybeRefOrGetter<string>,
  pageCollectionId: MaybeRefOrGetter<string | null>,
  doc: Ref<PageDocument | undefined>,
  /** The studio designs pages for the site as it will be: collections and items with their drafts. The website preview shows what is live. */
  { drafts = false }: { drafts?: boolean } = {},
) {
  const collectionsQuery = useCollections().getCollections(websiteId)
  const collections = computed(() => (drafts ? collectionsQuery.collections.value.map(asDrafted) : collectionsQuery.collections.value))
  const sourceCollections = computed(() => new Map(collectionsQuery.collections.value.map((c) => [c.id, c])))

  const pageCollection = computed(() => collections.value.find((c) => c.id === toValue(pageCollectionId)))

  const ids = computed(() => {
    const set = listedCollections(doc.value)
    const own = toValue(pageCollectionId)
    if (own) set.add(own)
    return [...set].sort()
  })

  const itemQueries = useCollectionItemsQuerier().getItemsOfMany(ids)

  const itemsById = computed(() => {
    const map = new Map<string, { items: CollectionItem[]; loading: boolean }>()
    ids.value.forEach((id, i) => {
      const q = itemQueries.value[i]
      const items = (q?.data ?? []).map(toCollectionItem)
      map.set(id, { items: drafts ? items.map((i) => draftedItem(sourceCollections.value.get(id), i)) : items, loading: !!q?.isPending })
    })
    return map
  })

  const itemsOf = (collectionId: string) => itemsById.value.get(collectionId)?.items ?? []
  const isLoading = (collectionId: string) => itemsById.value.get(collectionId)?.loading ?? true

  /* ---------- Detail page preview ---------- */
  const previewItems = computed(() => (pageCollection.value ? itemsOf(pageCollection.value.id) : []))
  const previewItemId = ref('')
  const previewItem = computed(() => previewItems.value.find((i) => i.id === previewItemId.value) ?? previewItems.value[0])

  // Start with the newest published item: what most visitors will see.
  watch(
    previewItems,
    (items) => {
      if (!items.some((i) => i.id === previewItemId.value)) previewItemId.value = (items.find((i) => i.status === 'live') ?? items[0])?.id ?? ''
    },
    { immediate: true },
  )

  const data: StudioCollectionData = { collections, itemsOf, isLoading, pageCollection, previewItem }

  return {
    collectionData: data,
    collections,
    collectionsQuery,
    pageCollection,
    previewItems,
    previewItemId,
    previewItem,
  }
}
