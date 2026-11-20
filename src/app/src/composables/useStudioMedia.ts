import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useMedia } from '@/composables/useMedia'
import { useToast } from '@/composables/useToast'
import { mediaSrc } from '@/studio/media'
import type { MediaItem } from '@/data/types'
import type { MediaAsset } from '@/studio/types'

function toAsset(m: MediaItem): MediaAsset {
  // Documents keep the API route, not the URL the browser loads it from: see studio/media.ts.
  return { id: m.id, name: m.fileName, src: mediaSrc(m.id), thumbnailUrl: m.thumbnailUrl }
}

/** The website's media library as the studio uses it: the same images as the Media page (GET /media). */
export function useStudioMedia(websiteId: MaybeRefOrGetter<string>) {
  const mediaApi = useMedia()
  const { notify } = useToast()

  const mediaQuery = mediaApi.getMediaItems(websiteId)
  const media = computed(() => mediaQuery.media.value.map(toAsset))

  /** Uploads images from the user's computer to the website's media library (POST /media). */
  async function uploadMedia(files: FileList | File[]): Promise<MediaAsset[]> {
    const images = [...files].filter((f) => f.type.startsWith('image/'))
    if (!images.length) return []

    const id = toValue(websiteId)
    const items = await mediaApi.uploadFiles(id, images)

    // The studio has no upload list like the Media page, so report a failure here and clear it.
    const failed = mediaApi.getUploads(id).value.find((u) => u.status === 'failed')
    if (failed) {
      notify(failed.error ?? 'The upload failed. Try again.', 'error')
      mediaApi.dismissFailed(id)
    }

    return items.map(toAsset)
  }

  return {
    media,
    mediaQuery,
    uploadMedia,
  }
}
