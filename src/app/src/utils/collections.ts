import type { Component } from 'vue'
import { AlignLeft, Calendar, Hash, Image, Link, ToggleLeft, Type } from 'lucide-vue-next'
import type { Collection, CollectionField, CollectionFieldType, CollectionItem, CollectionValue } from '@/data/types'

/** How each field type is named and drawn in the collection editor and the studio. */
export const fieldTypes: Record<CollectionFieldType, { label: string; help: string; icon: Component }> = {
  text: { label: 'Text', help: 'A title, name or short line', icon: Type },
  richText: { label: 'Rich text', help: 'Formatted content, like the body of a post', icon: AlignLeft },
  image: { label: 'Image', help: 'A picture from the media library', icon: Image },
  url: { label: 'Link', help: 'A web address or a page of this site', icon: Link },
  number: { label: 'Number', help: 'A price, rating or amount', icon: Hash },
  date: { label: 'Date', help: 'A publish date or an event day', icon: Calendar },
  boolean: { label: 'Yes / no', help: 'A switch, like “Featured”', icon: ToggleLeft },
}

export const fieldTypeOrder: CollectionFieldType[] = ['text', 'richText', 'image', 'date', 'url', 'number', 'boolean']

/** Ready-made field sets to start a collection from. */
export const collectionPresets: Array<{ id: string; name: string; singularName: string; slug: string; fields: Array<Omit<CollectionField, 'id'>> }> = [
  {
    id: 'blog',
    name: 'Blog posts',
    singularName: 'Blog post',
    slug: 'blog',
    fields: [
      { label: 'Title', type: 'text', required: true },
      { label: 'Cover image', type: 'image', required: true },
      { label: 'Summary', type: 'text', required: false },
      { label: 'Content', type: 'richText', required: true },
      { label: 'Published on', type: 'date', required: false },
    ],
  },
  {
    id: 'team',
    name: 'Team members',
    singularName: 'Team member',
    slug: 'team',
    fields: [
      { label: 'Name', type: 'text', required: true },
      { label: 'Photo', type: 'image', required: true },
      { label: 'Role', type: 'text', required: false },
      { label: 'Bio', type: 'richText', required: false },
    ],
  },
  {
    id: 'projects',
    name: 'Projects',
    singularName: 'Project',
    slug: 'projects',
    fields: [
      { label: 'Name', type: 'text', required: true },
      { label: 'Cover image', type: 'image', required: true },
      { label: 'Client', type: 'text', required: false },
      { label: 'Description', type: 'richText', required: false },
      { label: 'Website', type: 'url', required: false },
    ],
  },
]

/** The field an item is named by: the first text field. */
export const titleField = (collection: Collection) => collection.fields.find((f) => f.type === 'text')

/** The item's name for lists and messages: its title field, or its slug. */
export function itemTitle(collection: Collection, item: CollectionItem) {
  const field = titleField(collection)
  const value = field ? item.values[field.id] : undefined
  return typeof value === 'string' && value.trim() ? value.trim() : item.slug
}

/** Rich text as plain text, e.g. for a card's excerpt. */
export function plainText(html: string) {
  const doc = new DOMParser().parseFromString(html, 'text/html')
  return (doc.body.textContent ?? '').replace(/\s+/g, ' ').trim()
}

/** A date value (`yyyy-mm-dd`) as visitors read it, e.g. 12 September 2026. */
export function formatDate(value: string) {
  const date = new Date(`${value}T00:00:00`)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString(undefined, { day: 'numeric', month: 'long', year: 'numeric' })
}

/** A value as one line of text: rich text stripped, dates formatted, booleans as Yes/No. */
export function valueAsText(field: CollectionField, value: CollectionValue | undefined) {
  if (value === undefined || value === '') return ''
  if (field.type === 'richText') return plainText(String(value))
  if (field.type === 'date') return formatDate(String(value))
  if (field.type === 'boolean') return value ? 'Yes' : 'No'
  return String(value)
}
