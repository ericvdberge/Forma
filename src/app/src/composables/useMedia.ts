import { computed, reactive, readonly, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getMediaItemsQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { HttpValidationProblemDetails, MediaItemDto } from '@/api/generated'
import { apiUrl } from '@/api/url'
import type { MediaItem } from '@/data/types'
import { useMediaQuerier } from '@/queriers/useMediaQuerier'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'

/** Mirrors the API's upload rules, so obviously wrong files fail before they are sent. */
export const MAX_UPLOAD_BYTES = 10 * 1024 * 1024
export const ACCEPTED_TYPES: Record<string, string> = {
  'image/jpeg': 'JPEG',
  'image/png': 'PNG',
  'image/gif': 'GIF',
  'image/webp': 'WebP',
}
const ACCEPTED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.webp']
/** For `<input type="file" accept>`. */
export const ACCEPT_ATTRIBUTE = [...Object.keys(ACCEPTED_TYPES), ...ACCEPTED_EXTENSIONS].join(',')

/** How many files are sent at the same time. */
const PARALLEL_UPLOADS = 3

function toMediaItem(dto: MediaItemDto): MediaItem {
  return {
    id: dto.id,
    websiteId: dto.websiteId,
    fileName: dto.fileName,
    contentType: dto.contentType,
    format: ACCEPTED_TYPES[dto.contentType] ?? dto.contentType.replace(/^image\//, '').toUpperCase(),
    sizeInBytes: Number(dto.sizeInBytes),
    width: Number(dto.width),
    height: Number(dto.height),
    imageUrl: apiUrl(dto.imageUrl),
    thumbnailUrl: apiUrl(dto.thumbnailUrl),
    createdAt: Date.parse(dto.createdAt),
  }
}

/** Why a file can't be uploaded, or undefined when it looks fine. */
export function checkFile(file: File): string | undefined {
  const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
  if (!ACCEPTED_EXTENSIONS.includes(ext) || (file.type && !ACCEPTED_TYPES[file.type]))
    return 'Only JPEG, PNG, GIF and WebP images are allowed.'
  if (file.size === 0) return 'The file is empty.'
  if (file.size > MAX_UPLOAD_BYTES) return 'The file is larger than 10 MB.'
}

/** Reads the message out of whatever the API client threw. */
function uploadErrorMessage(error: unknown): string {
  if (error instanceof TypeError) return 'Couldn’t reach the Forma API.'
  const problem = error as HttpValidationProblemDetails | undefined
  const first = problem?.errors && Object.values(problem.errors).flat()[0]
  if (first) return first
  if (problem?.status === 404 || problem?.status === '404') return 'This website no longer exists.'
  return 'The upload failed. Try again.'
}

export interface PendingUpload {
  id: string
  websiteId: string
  fileName: string
  sizeInBytes: number
  /** Local object URL, shown until the server's thumbnail is ready. */
  previewUrl: string
  status: 'queued' | 'uploading' | 'failed'
  error?: string
}

/** Uploads in flight or failed, shared across views so navigating away doesn't lose them. */
const uploads = reactive<PendingUpload[]>([])

/** Media items of a website, read from GET /media. Nothing is fetched until a page calls `getMediaItems()`. */
export function useMedia() {
  const queryClient = useQueryClient()
  const querier = useMediaQuerier()
  const { mutateAsync: upload } = querier.uploadMedia()
  const { mutateAsync: remove, isPending: isDeleting } = querier.deleteMedia()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  /** Media of one website, newest first. */
  function getMediaItems(websiteId: MaybeRefOrGetter<string>) {
    const query = querier.getMediaItems(websiteId)
    const media = computed(() =>
      (query.data.value ?? [])
        .filter((d) => d.websiteId === toValue(websiteId))
        .map(toMediaItem)
        .sort((a, b) => b.createdAt - a.createdAt),
    )
    return { ...query, media }
  }

  /** Uploads that belong to one website. */
  function getUploads(websiteId: MaybeRefOrGetter<string>) {
    return computed(() => uploads.filter((u) => u.websiteId === toValue(websiteId)))
  }

  function updateCachedMedia(websiteId: string, update: (dtos: MediaItemDto[]) => MediaItemDto[]) {
    queryClient.setQueryData<MediaItemDto[]>(getMediaItemsQueryKey({ query: { websiteId } }), (old) =>
      old ? update(old) : old,
    )
  }

  function forget(id: string) {
    const i = uploads.findIndex((u) => u.id === id)
    if (i === -1) return
    URL.revokeObjectURL(uploads[i]!.previewUrl)
    uploads.splice(i, 1)
  }

  async function send(entry: PendingUpload, file: File) {
    entry.status = 'uploading'
    try {
      const dto = await upload({ body: { websiteId: entry.websiteId, file } })
      updateCachedMedia(entry.websiteId, (dtos) => [dto, ...dtos.filter((d) => d.id !== dto.id)])
      forget(entry.id)
      return true
    } catch (error) {
      entry.status = 'failed'
      entry.error = uploadErrorMessage(error)
      return false
    }
  }

  /** Checks and uploads files to a website, a few at a time. Files that fail stay listed with the reason. */
  async function uploadFiles(websiteId: string, files: Iterable<File>) {
    const work: Array<[PendingUpload, File]> = []
    for (const file of files) {
      const error = checkFile(file)
      const entry: PendingUpload = {
        id: crypto.randomUUID(),
        websiteId,
        fileName: file.name,
        sizeInBytes: file.size,
        previewUrl: error ? '' : URL.createObjectURL(file),
        status: error ? 'failed' : 'queued',
        error,
      }
      uploads.unshift(entry)
      // Work on the reactive copy so status changes show up.
      if (!error) work.push([uploads[0]!, file])
    }
    if (!work.length) return

    let uploaded = 0
    const queue = [...work]
    await Promise.all(
      Array.from({ length: Math.min(PARALLEL_UPLOADS, queue.length) }, async () => {
        for (let next = queue.shift(); next; next = queue.shift()) {
          if (await send(...next)) uploaded++
        }
      }),
    )

    if (!uploaded) return
    const first = work.find(([entry]) => !uploads.includes(entry))?.[1].name
    const label = uploaded === 1 ? `‘${first}’` : `${uploaded} images`
    logActivity(`You uploaded ${label}`, 'media')
    notify(work.length === 1 ? `${label} uploaded` : `${uploaded} of ${work.length} images uploaded`)
  }

  /** Clears one failed upload. */
  const dismissUpload = forget
  /** Clears all failed uploads of a website. */
  function dismissFailed(websiteId: string) {
    for (const u of uploads.filter((u) => u.websiteId === websiteId && u.status === 'failed')) forget(u.id)
  }

  /** Deletes the image and its thumbnail. Resolves to false when the API refused. */
  async function deleteMediaItem(item: Pick<MediaItem, 'id' | 'websiteId' | 'fileName'>) {
    try {
      await remove({ path: { id: item.id } })
    } catch {
      notify(`Couldn’t delete ‘${item.fileName}’`, 'error')
      // It may already be gone (e.g. deleted in another tab): reload the list to find out.
      queryClient.invalidateQueries({ queryKey: getMediaItemsQueryKey({ query: { websiteId: item.websiteId } }) })
      return false
    }
    updateCachedMedia(item.websiteId, (dtos) => dtos.filter((d) => d.id !== item.id))
    logActivity(`You deleted ‘${item.fileName}’`, 'media')
    notify(`‘${item.fileName}’ deleted`)
    return true
  }

  return {
    getMediaItems,
    getUploads,
    uploads: readonly(uploads),
    uploadFiles,
    dismissUpload,
    dismissFailed,
    deleteMediaItem,
    isDeleting,
  }
}
