import type { Component } from 'vue'
import {
  AlignLeft,
  BookOpen,
  Briefcase,
  Calendar,
  Database,
  GraduationCap,
  Hash,
  Heart,
  Image,
  Link,
  MapPin,
  Music,
  Newspaper,
  ShoppingBag,
  Star,
  ToggleLeft,
  Type,
  Users,
  Utensils,
} from 'lucide-vue-next'
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

/**
 * Icons a collection can be shown with, by the name the API stores. Mirrors `CollectionIcons` in
 * `src/api/Forma.Application/Entities/Collection.cs`.
 */
export const collectionIcons: Record<string, { label: string; icon: Component }> = {
  database: { label: 'Collection', icon: Database },
  newspaper: { label: 'Articles', icon: Newspaper },
  users: { label: 'People', icon: Users },
  briefcase: { label: 'Work', icon: Briefcase },
  calendar: { label: 'Events', icon: Calendar },
  'shopping-bag': { label: 'Products', icon: ShoppingBag },
  star: { label: 'Reviews', icon: Star },
  'book-open': { label: 'Guides', icon: BookOpen },
  image: { label: 'Gallery', icon: Image },
  'map-pin': { label: 'Places', icon: MapPin },
  utensils: { label: 'Menu', icon: Utensils },
  music: { label: 'Music', icon: Music },
  'graduation-cap': { label: 'Courses', icon: GraduationCap },
  heart: { label: 'Favourites', icon: Heart },
}

/** The component for a collection's icon; unknown names fall back to the default. */
export const collectionIcon = (name: string) => (collectionIcons[name] ?? collectionIcons.database!).icon

/** Ready-made field sets to start a collection from. */
export const collectionPresets: Array<{ id: string; name: string; singularName: string; slug: string; icon: string; fields: Array<Omit<CollectionField, 'id'>> }> = [
  {
    id: 'blog',
    name: 'Blog posts',
    singularName: 'Blog post',
    slug: 'blog',
    icon: 'newspaper',
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
    icon: 'users',
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
    icon: 'briefcase',
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
