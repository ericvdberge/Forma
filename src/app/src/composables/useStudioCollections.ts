import { computed, ref, toValue, watch, type MaybeRefOrGetter, type Ref } from 'vue'
import { useCollections } from '@/composables/useCollections'
import { toCollectionItem } from '@/composables/useCollectionItems'
import type { CollectionItem } from '@/data/types'
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

/**
 * The collections the studio (and the website preview) shows: the website's collections, the items of every collection the page lists, and on a
 * collection's detail page the item it is previewed with. Items load as soon as a list or the detail page needs them.
 */
export function useStudioCollections(
  websiteId: MaybeRefOrGetter<string>,
  pageCollectionId: MaybeRefOrGetter<string | null>,
  doc: Ref<PageDocument | undefined>,
) {
  const collectionsQuery = useCollections().getCollections(websiteId)
  const collections = collectionsQuery.collections

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
      map.set(id, { items: (q?.data ?? []).map(toCollectionItem), loading: !!q?.isPending })
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
