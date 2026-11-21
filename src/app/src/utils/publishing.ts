import type { RouteLocationRaw } from 'vue-router'
import type { EntityRefDto } from '@/api/generated'

/** Where a collection, page or item is edited: what an issue's **Open** link goes to. */
export function editorRoute(entity: EntityRefDto): RouteLocationRaw {
  if (entity.kind === 'page') return { name: 'studio', params: { pageId: entity.id } }
  if (entity.kind === 'collection') return { name: 'collection-settings', params: { collectionId: entity.id } }
  return { name: 'collection-item', params: { collectionId: entity.collectionId ?? '', itemId: entity.id } }
}

/** The browser's time zone, e.g. `Europe/Amsterdam`. */
export const timeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone

export const sameEntity = (a: EntityRefDto, b: EntityRefDto) => a.kind === b.kind && a.id === b.id
