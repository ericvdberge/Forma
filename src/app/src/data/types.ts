export type PublishStatus = 'live' | 'draft' | 'scheduled' | 'archived'

/** The statuses the pages API knows: `published` maps to `live`. */
export type PageStatus = Extract<PublishStatus, 'live' | 'draft'>

export interface Website {
  id: string
  name: string
  domain: string
  description: string
  status: 'live' | 'draft'
  tone: 'light' | 'dark'
  collaborators: string[]
  updatedAt: number
}

export interface Page {
  id: string
  websiteId: string
  title: string
  slug: string
  /** `/` + slug, for display. */
  path: string
  status: PageStatus
  /** `collectionTemplate`: the detail page every item of `collectionId` is shown with. */
  kind: 'static' | 'collectionTemplate'
  collectionId: string | null
  updatedAt: number
}

export type CollectionFieldType = 'text' | 'richText' | 'image' | 'url' | 'number' | 'date' | 'boolean'

export interface CollectionField {
  /** Stable id (`fld_…`); empty for a field that isn't saved yet. */
  id: string
  label: string
  type: CollectionFieldType
  required: boolean
}

/** A website's own content type, such as Blog posts. */
export interface Collection {
  id: string
  websiteId: string
  name: string
  singularName: string
  slug: string
  fields: CollectionField[]
  /** The page every item is shown with; designed in the studio. */
  detailPageId: string | null
  updatedAt: number
}

/** A value of an item's field: a string (text, rich text, image, link, date), a number or a boolean. */
export type CollectionValue = string | number | boolean

export interface CollectionItem {
  id: string
  collectionId: string
  slug: string
  status: PageStatus
  values: Record<string, CollectionValue>
  updatedAt: number
  publishedAt: number | null
}

export interface MediaItem {
  id: string
  websiteId: string
  fileName: string
  contentType: string
  /** Short format label, e.g. "JPEG". */
  format: string
  /** File-extension style label for chips, e.g. "JPG". */
  extension: string
  sizeInBytes: number
  width: number
  height: number
  /** Absolute URLs (API base included), ready for <img src>. */
  imageUrl: string
  thumbnailUrl: string
  createdAt: number
}

export interface Activity {
  id: string
  message: string
  at: number
  kind: 'page' | 'template' | 'styles' | 'website' | 'media'
}

export interface Stat {
  label: string
  value: string
  change: number
  trend: number[]
}
