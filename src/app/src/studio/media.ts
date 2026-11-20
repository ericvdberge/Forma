import { apiUrl } from '@/api/url'

/**
 * Documents store media-library images as the API's route, `/media/{id}/image`, so pages don't depend on where the API
 * runs. Built-in demo images (`/media/hero.svg`) are files of the app itself and stay as they are.
 */
const MEDIA_ROUTE = /^\/media\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/image$/i

export const mediaSrc = (mediaId: string) => `/media/${mediaId}/image`

/** The URL the browser loads for an image source stored in a document. */
export const imageUrl = (src: string) => (MEDIA_ROUTE.test(src) ? apiUrl(src) : src)
