import { useClipboard, useShare } from '@vueuse/core'
import type { MediaItem } from '@/data/types'
import { useToast } from '@/composables/useToast'

/** Copy or share the public link of an image. */
export function useMediaLink() {
  const { copy } = useClipboard({ legacy: true })
  const { share, isSupported: canShare } = useShare()
  const { notify } = useToast()

  const linkOf = (item: Pick<MediaItem, 'imageUrl'>) => new URL(item.imageUrl, window.location.origin).href

  async function copyLink(item: Pick<MediaItem, 'imageUrl'>) {
    await copy(linkOf(item))
    notify('Link copied')
  }

  /** Opens the system share sheet, or copies the link where there is none. */
  async function shareLink(item: Pick<MediaItem, 'imageUrl' | 'fileName'>) {
    if (!canShare.value) return copyLink(item)
    try {
      await share({ title: item.fileName, url: linkOf(item) })
    } catch {
      /* cancelled */
    }
  }

  return { copyLink, shareLink }
}
