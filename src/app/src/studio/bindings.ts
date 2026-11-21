import type { Collection, CollectionField, CollectionFieldType, CollectionItem } from '@/data/types'
import { formatDate, plainText } from '@/utils/collections'
import type { Block, BlockType } from './types'

/** Field types that can show as words: in a button label, a badge, alt text. */
const words: CollectionFieldType[] = ['text', 'number', 'date']
/** Field types that can fill rich text (a heading or a paragraph). */
const html: CollectionFieldType[] = ['text', 'richText', 'number', 'date']

/**
 * Which block props can show a collection field, with a label for the inspector and the field types that fit.
 * Mirrors `BindableProps` in `src/api/Forma.Application/Documents/BindableProps.cs`.
 */
export const bindableProps: Partial<Record<BlockType, Array<{ prop: string; label: string; types: CollectionFieldType[] }>>> = {
  heading: [{ prop: 'html', label: 'Text', types: html }],
  text: [{ prop: 'html', label: 'Text', types: html }],
  button: [
    { prop: 'label', label: 'Label', types: words },
    { prop: 'href', label: 'Link', types: ['url'] },
  ],
  badge: [{ prop: 'text', label: 'Text', types: words }],
  image: [
    { prop: 'src', label: 'Image', types: ['image'] },
    { prop: 'alt', label: 'Description', types: words },
    { prop: 'caption', label: 'Caption', types: words },
  ],
  video: [{ prop: 'url', label: 'Video link', types: ['url'] }],
}

const escapeHtml = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

/** A field's value as the prop it is bound to expects, or undefined when the item has none (the placeholder shows). */
function valueFor(prop: string, field: CollectionField, item: CollectionItem): string | undefined {
  const raw = item.values[field.id]
  if (raw === undefined || raw === null || raw === '') return undefined

  const text = field.type === 'date' ? formatDate(String(raw)) : String(raw)

  if (prop !== 'html') return field.type === 'richText' ? plainText(text) : text

  return field.type === 'richText' ? text : `<p>${escapeHtml(text)}</p>`
}

/** The block with its bound props showing `item`'s values; unbound props and empty fields keep their literal value. */
export function resolveBindings<T extends Block>(block: T, collection: Collection | undefined, item: CollectionItem | undefined): T {
  if (!block.bindings || !collection || !item) return block

  const props: Record<string, unknown> = { ...block.props }

  for (const [prop, binding] of Object.entries(block.bindings)) {
    const field = collection.fields.find((f) => f.id === binding.field)
    const value = field ? valueFor(prop, field, item) : undefined
    if (value !== undefined) props[prop] = value
  }

  return { ...block, props } as T
}

/** The labels of the fields a block shows, for the chip on the canvas. */
export function boundFieldLabels(block: Block, collection: Collection | undefined) {
  if (!block.bindings || !collection) return []
  return Object.values(block.bindings).map((b) => collection.fields.find((f) => f.id === b.field)?.label ?? 'Removed field')
}
