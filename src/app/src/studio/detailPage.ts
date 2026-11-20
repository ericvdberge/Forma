import type { Collection, CollectionFieldType } from '@/data/types'
import { createBlock } from './blocks'
import { responsivePadding } from './defaults'
import { createSection } from './sections'
import type { Block, BlockType, Section } from './types'

/** A block whose `prop` shows the collection's field `fieldId`. */
function bound<T extends BlockType>(block: Block<T>, prop: string, fieldId: string | undefined): Block<T> {
  return fieldId ? { ...block, bindings: { [prop]: { scope: 'page', field: fieldId } } } : block
}

/**
 * The starter design of a collection's detail page, built from its fields: the item's title (and date and summary when
 * there are such fields) in a hero, its first image wide, then its rich text. Every value is connected to its field.
 */
export function detailPageSections(collection: Collection): Section[] {
  const fieldsOf = (type: CollectionFieldType) => collection.fields.filter((f) => f.type === type)
  const [title, summary] = fieldsOf('text')
  const [image] = fieldsOf('image')
  const [body] = fieldsOf('richText')
  const [date] = fieldsOf('date')

  const hero = createSection(
    { type: 'hero', align: 'center', contentWidth: 'normal', gap: 16, padding: responsivePadding(96, 48), name: `${collection.singularName} title` },
    [
      ...(date ? [bound(createBlock('badge', { text: 'Date' }), 'text', date.id)] : []),
      bound(createBlock('heading', { html: `<p>${title?.label ?? collection.singularName}</p>`, level: 1, size: 'xl' }), 'html', title?.id),
      ...(summary
        ? [bound(createBlock('text', { html: `<p>${summary.label}</p>`, size: 'lg', tone: 'muted' }, { maxWidth: 'md' }), 'html', summary.id)]
        : []),
    ],
  )

  const cover = image
    ? [
        createSection({ type: 'content', contentWidth: 'wide', padding: responsivePadding(0, 48) }, [
          bound(createBlock('image', { src: '/media/facade.svg', alt: '', aspect: '16/9', radius: 24 }), 'src', image.id),
        ]),
      ]
    : []

  const content = body
    ? [
        createSection({ type: 'content', contentWidth: 'narrow', padding: responsivePadding(32, 96) }, [
          bound(createBlock('text', { html: `<p>${body.label}</p>`, size: 'lg', tone: 'default' }), 'html', body.id),
        ]),
      ]
    : []

  return [hero, ...cover, ...content]
}
