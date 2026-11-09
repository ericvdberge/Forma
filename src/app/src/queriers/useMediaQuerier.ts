import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useMutation, useQuery } from '@tanstack/vue-query'
import { deleteMediaMutation, getMediaItemsOptions } from '@/api/generated/@tanstack/vue-query.gen'
import type { MediaItemDto } from '@/api/generated'
import { apiUrl } from '@/api/url'

export interface UploadMediaVariables {
  websiteId: string
  file: File
  /** Called with 0–100 while the bytes are sent. */
  onProgress?: (percent: number) => void
}

/**
 * POST /media through XMLHttpRequest rather than the generated (fetch) client: fetch can't
 * report upload progress. Errors are thrown the way the generated client throws them: the
 * parsed problem details, or a TypeError when the API can't be reached.
 */
function postMedia({ websiteId, file, onProgress }: UploadMediaVariables) {
  return new Promise<MediaItemDto>((resolve, reject) => {
    const body = new FormData()
    body.append('websiteId', websiteId)
    body.append('file', file)
    const xhr = new XMLHttpRequest()
    xhr.open('POST', apiUrl('/media'))
    xhr.responseType = 'json'
    xhr.upload.onprogress = (e) => e.lengthComputable && onProgress?.((e.loaded / e.total) * 100)
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) resolve(xhr.response as MediaItemDto)
      else reject({ status: xhr.status, ...(xhr.response ?? {}) })
    }
    xhr.onerror = () => reject(new TypeError('Network request failed'))
    xhr.send(body)
  })
}

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

  /** POST /media (multipart: websiteId + file), reporting progress. */
  function uploadMedia() {
    return useMutation({ mutationFn: postMedia })
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
