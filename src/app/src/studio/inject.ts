import type { ComputedRef, InjectionKey, ShallowRef } from 'vue'
import type { Editor } from '@tiptap/vue-3'
import type { Collection, CollectionItem } from '@/data/types'

/** The rich-text editor currently being typed in, shared with the studio's formatting toolbar. */
export const textEditorKey: InjectionKey<ShallowRef<Editor | null>> = Symbol('studio-text-editor')

/** Where the user last clicked on a text block, so the text editor can put the cursor there right away. */
export const textPointerKey: InjectionKey<ShallowRef<{ x: number; y: number } | null>> = Symbol('studio-text-pointer')

/** The collection data blocks show: lists read items, and a detail page's bound props read the preview item. */
export interface StudioCollectionData {
  collections: ComputedRef<Collection[]>
  /** The items of a collection, newest first, once they have loaded. */
  itemsOf: (collectionId: string) => CollectionItem[]
  /** Whether a collection's items are still loading. */
  isLoading: (collectionId: string) => boolean
  /** The collection a detail page shows, or undefined on other pages. */
  pageCollection: ComputedRef<Collection | undefined>
  /** The item a detail page is previewed with. */
  previewItem: ComputedRef<CollectionItem | undefined>
}

export const collectionDataKey: InjectionKey<StudioCollectionData> = Symbol('studio-collection-data')
