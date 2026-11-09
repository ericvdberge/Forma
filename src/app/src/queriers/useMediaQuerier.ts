import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useMutation, useQuery } from '@tanstack/vue-query'
import {
  deleteMediaMutation,
  getMediaItemsOptions,
  uploadMediaMutation,
} from '@/api/generated/@tanstack/vue-query.gen'

/** TanStack Query calls for the media API. Call the sub-functions during component setup. */
export function useMediaQuerier() {
  /** GET /media?websiteId=… Refetches when the (reactive) website id changes. */
  function getMediaItems(websiteId: MaybeRefOrGetter<string>) {
    return useQuery(
      computed(() => ({
        ...getMediaItemsOptions({ query: { websiteId: toValue(websiteId) } }),
        enabled: !!toValue(websiteId),
      })),
    )
  }

  /** POST /media (multipart: websiteId + file). */
  function uploadMedia() {
    return useMutation(uploadMediaMutation())
  }

  /** DELETE /media/{id} */
  function deleteMedia() {
    return useMutation(deleteMediaMutation())
  }

  return {
    getMediaItems,
    uploadMedia,
    deleteMedia,
  }
}
