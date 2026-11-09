<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ImagePlus, Images, LoaderCircle, Search, Trash2, TriangleAlert, Upload, X } from 'lucide-vue-next'
import { FButton, FCard, FIconButton, FSelect, cn } from '@forma/design-system'
import PageHeader from '@/components/PageHeader.vue'
import MediaDetails from '@/components/media/MediaDetails.vue'
import MediaGridSkeleton from '@/components/skeletons/MediaGridSkeleton.vue'
import { useWebsites } from '@/composables/useWebsites'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { ACCEPT_ATTRIBUTE, ACCEPTED_TYPES, useMedia } from '@/composables/useMedia'
import type { MediaItem } from '@/data/types'
import { formatBytes } from '@/utils/format'

const { getWebsites } = useWebsites()
const { websites } = getWebsites()
const { getActiveWebsite, setActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
const { getMediaItems, getUploads, uploadFiles, dismissUpload, dismissFailed, deleteMediaItem, isDeleting } = useMedia()
const { media, isPending, isError, isFetching, refetch } = getMediaItems(() => activeWebsite.value.id)
const uploads = getUploads(() => activeWebsite.value.id)
const failedCount = computed(() => uploads.value.filter((u) => u.status === 'failed').length)
const route = useRoute()
const router = useRouter()

/* Website switcher */
const websiteId = computed({
  get: () => activeWebsite.value.id,
  set: (id: string) => setActiveWebsite(id),
})
const websiteOptions = computed(() => websites.value.map((w) => ({ label: w.name, value: w.id })))

/* Filters: file name search, format tabs and sort order */
type Format = 'all' | (typeof ACCEPTED_TYPES)[string]
const format = ref<Format>('all')
const search = ref('')
type Sort = 'newest' | 'oldest' | 'name' | 'largest'
const sort = ref<Sort>('newest')
const sortOptions: Array<{ label: string; value: Sort }> = [
  { label: 'Newest first', value: 'newest' },
  { label: 'Oldest first', value: 'oldest' },
  { label: 'Name A–Z', value: 'name' },
  { label: 'Largest first', value: 'largest' },
]

const searched = computed(() => {
  const q = search.value.trim().toLowerCase()
  return q ? media.value.filter((m) => m.fileName.toLowerCase().includes(q)) : media.value
})
const tabs = computed(() =>
  (['all', ...Object.values(ACCEPTED_TYPES)] as Format[]).map((value) => ({
    value,
    label: value === 'all' ? 'All' : value,
    count: value === 'all' ? searched.value.length : searched.value.filter((m) => m.format === value).length,
  })),
)
const visible = computed(() => {
  const list = format.value === 'all' ? [...searched.value] : searched.value.filter((m) => m.format === format.value)
  const compare: Record<Sort, (a: MediaItem, b: MediaItem) => number> = {
    newest: (a, b) => b.createdAt - a.createdAt,
    oldest: (a, b) => a.createdAt - b.createdAt,
    name: (a, b) => a.fileName.localeCompare(b.fileName, undefined, { numeric: true }),
    largest: (a, b) => b.sizeInBytes - a.sizeInBytes,
  }
  return list.sort(compare[sort.value])
})
const totalSize = computed(() => media.value.reduce((sum, m) => sum + m.sizeInBytes, 0))
const filtered = computed(() => !!search.value.trim() || format.value !== 'all')
function clearFilters() {
  search.value = ''
  format.value = 'all'
}

watch(() => activeWebsite.value.id, clearFilters)

/* Uploading: the button, or files dropped anywhere on the page */
const fileInput = ref<HTMLInputElement | null>(null)
const canUpload = computed(() => !isPending.value && !isError.value)
function browse() {
  fileInput.value?.click()
}
function onPicked(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files?.length) startUpload(input.files)
  input.value = ''
}
function startUpload(files: FileList | File[]) {
  // New uploads show at the top of the grid, so make sure they aren't filtered out of view.
  clearFilters()
  sort.value = 'newest'
  uploadFiles(activeWebsite.value.id, files)
}

const dragging = ref(false)
let dragDepth = 0
const hasFiles = (e: DragEvent) => !!e.dataTransfer?.types.includes('Files')
function onDragEnter(e: DragEvent) {
  if (!hasFiles(e) || !canUpload.value) return
  dragDepth++
  dragging.value = true
}
function onDragOver(e: DragEvent) {
  if (!hasFiles(e) || !canUpload.value) return
  e.preventDefault()
  e.dataTransfer!.dropEffect = 'copy'
}
function onDragLeave(e: DragEvent) {
  if (!hasFiles(e)) return
  dragDepth = Math.max(0, dragDepth - 1)
  if (!dragDepth) dragging.value = false
}
function onDrop(e: DragEvent) {
  if (!hasFiles(e)) return
  e.preventDefault()
  dragDepth = 0
  dragging.value = false
  if (canUpload.value && e.dataTransfer?.files.length) startUpload(e.dataTransfer.files)
}
/** Pasting an image (e.g. a screenshot) uploads it too. */
function onPaste(e: ClipboardEvent) {
  if ((e.target as HTMLElement).closest('input, textarea') || !canUpload.value) return
  const files = Array.from(e.clipboardData?.files ?? [])
  if (files.length) startUpload(files)
}
onMounted(() => {
  document.addEventListener('dragenter', onDragEnter)
  document.addEventListener('dragover', onDragOver)
  document.addEventListener('dragleave', onDragLeave)
  document.addEventListener('drop', onDrop)
  document.addEventListener('paste', onPaste)
})
onBeforeUnmount(() => {
  document.removeEventListener('dragenter', onDragEnter)
  document.removeEventListener('dragover', onDragOver)
  document.removeEventListener('dragleave', onDragLeave)
  document.removeEventListener('drop', onDrop)
  document.removeEventListener('paste', onPaste)
})

/* Details: the open item lives in the URL (?item=…) so it can be linked to and survives a reload */
const openId = computed(() => (typeof route.query.item === 'string' ? route.query.item : null))
const openIndex = computed(() => visible.value.findIndex((m) => m.id === openId.value))
const openItem = computed(() => visible.value[openIndex.value] ?? media.value.find((m) => m.id === openId.value))
function open(id: string | null) {
  router.replace({ query: { ...route.query, item: id ?? undefined } })
}
function step(by: number) {
  const next = visible.value[openIndex.value + by]
  if (next) open(next.id)
}
// Drop a stale ?item= once the list has loaded (deleted, or from another website).
watch([openId, isPending, media], () => {
  if (openId.value && !isPending.value && !isError.value && !openItem.value) open(null)
})

async function remove(item: MediaItem) {
  if (!window.confirm(`Delete “${item.fileName}”? Pages that use it will show a broken image. This can’t be undone.`)) return
  const index = visible.value.findIndex((m) => m.id === item.id)
  const neighbour = visible.value[index + 1] ?? visible.value[index - 1]
  const wasOpen = openId.value === item.id
  if (await deleteMediaItem(item)) {
    if (wasOpen) open(neighbour?.id ?? null)
  }
}
</script>

<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Media">
      <template #meta>
        <p class="mt-3 text-[0.9375rem] text-muted">
          Images for <span class="font-medium text-charcoal">{{ activeWebsite.name }}</span>. JPEG, PNG, GIF or WebP, up to 10 MB each.
        </p>
      </template>
      <FSelect v-model="websiteId" label="Website" hide-label size="sm" :options="websiteOptions" class="w-48" />
      <FButton :disabled="!canUpload" @click="browse"><Upload class="size-4" aria-hidden="true" /> Upload</FButton>
      <input ref="fileInput" type="file" multiple :accept="ACCEPT_ATTRIBUTE" class="sr-only" tabindex="-1" aria-hidden="true" @change="onPicked" />
    </PageHeader>

    <FCard padding="none">
      <!-- Toolbar -->
      <div class="flex flex-col gap-4 border-b border-charcoal/[0.06] p-4 lg:flex-row lg:items-center lg:justify-between">
        <div role="tablist" aria-label="Filter by format" class="flex gap-1 overflow-x-auto">
          <button
            v-for="t in tabs"
            :key="t.value"
            type="button"
            role="tab"
            :aria-selected="format === t.value"
            :class="
              cn(
                'inline-flex h-9 shrink-0 items-center gap-2 rounded-full px-4 text-sm transition-colors',
                format === t.value ? 'bg-charcoal text-warm-white' : 'text-charcoal/70 hover:bg-charcoal/5 hover:text-charcoal',
              )
            "
            @click="format = t.value"
          >
            {{ t.label }}
            <span :class="cn('text-xs', format === t.value ? 'text-warm-white/60' : 'text-subtle')">{{ t.count }}</span>
          </button>
        </div>
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
          <div class="relative sm:w-64">
            <Search class="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-subtle" aria-hidden="true" />
            <label for="media-search" class="sr-only">Search media by file name</label>
            <input
              id="media-search"
              v-model="search"
              type="search"
              placeholder="Search by file name"
              class="h-9 w-full rounded-full bg-canvas pl-10 pr-4 text-sm outline-none ring-1 ring-transparent transition-shadow placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
            />
          </div>
          <FSelect v-model="sort" label="Sort" hide-label size="sm" :options="sortOptions" class="sm:w-40" />
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isPending" aria-busy="true" aria-label="Loading media">
        <MediaGridSkeleton />
      </div>

      <!-- Error -->
      <div v-else-if="isError" class="flex flex-col items-center gap-3 px-6 py-16 text-center">
        <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><TriangleAlert class="size-5" aria-hidden="true" /></span>
        <p class="font-medium">Couldn’t load the media</p>
        <p class="max-w-xs text-sm text-muted">Check that the Forma API is running, then try again.</p>
        <FButton size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
      </div>

      <template v-else>
        <!-- Failed uploads summary -->
        <div
          v-if="failedCount > 1"
          class="flex items-center justify-between gap-3 border-b border-charcoal/[0.06] bg-danger/[0.04] px-5 py-2.5 text-sm"
          role="status"
        >
          <span class="text-danger">{{ failedCount }} files couldn’t be uploaded.</span>
          <button type="button" class="font-medium hover:underline" @click="dismissFailed(activeWebsite.id)">Dismiss all</button>
        </div>

        <!-- Empty library: one big drop target -->
        <div v-if="!media.length && !uploads.length" class="p-4">
          <button
            type="button"
            class="flex w-full flex-col items-center gap-3 rounded-2xl border-2 border-dashed border-charcoal/10 px-6 py-16 text-center transition-colors hover:border-charcoal/25 hover:bg-warm-white"
            @click="browse"
          >
            <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><ImagePlus class="size-5" aria-hidden="true" /></span>
            <span class="font-medium">No images yet</span>
            <span class="max-w-sm text-sm text-muted">
              Drop images here, paste one, or <span class="font-medium text-charcoal underline underline-offset-2">browse your files</span>.
            </span>
          </button>
        </div>

        <!-- Grid -->
        <ul
          v-else-if="visible.length || (!filtered && uploads.length)"
          class="grid grid-cols-2 gap-4 p-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5"
          aria-label="Images"
        >
          <!-- In-flight and failed uploads -->
          <li v-for="u in filtered ? [] : uploads" :key="u.id" class="min-w-0">
            <div
              :class="
                cn(
                  'relative aspect-square overflow-hidden rounded-xl bg-canvas',
                  u.status === 'failed' && 'ring-2 ring-inset ring-danger/40',
                )
              "
            >
              <img v-if="u.previewUrl" :src="u.previewUrl" alt="" :class="cn('size-full object-cover', u.status !== 'failed' && 'opacity-60')" />
              <div v-if="u.status !== 'failed'" class="absolute inset-0 grid place-items-center bg-charcoal/20">
                <span class="inline-flex items-center gap-2 rounded-full bg-white/95 px-3 py-1.5 text-xs font-medium shadow-soft">
                  <LoaderCircle v-if="u.status === 'uploading'" class="size-3.5 animate-spin" aria-hidden="true" />
                  {{ u.status === 'uploading' ? 'Uploading…' : 'Waiting…' }}
                </span>
              </div>
              <div v-else class="absolute inset-0 flex flex-col items-center justify-center gap-2 bg-white/85 p-3 text-center">
                <TriangleAlert class="size-5 text-danger" aria-hidden="true" />
                <p class="text-xs font-medium text-danger">{{ u.error }}</p>
              </div>
              <div v-if="u.status === 'failed'" class="absolute right-2 top-2">
                <FIconButton :icon="X" :label="`Dismiss ${u.fileName}`" size="sm" variant="surface" @click="dismissUpload(u.id)" />
              </div>
            </div>
            <p class="mt-2.5 truncate text-sm font-medium" :title="u.fileName">{{ u.fileName }}</p>
            <p class="text-xs text-muted">{{ formatBytes(u.sizeInBytes) }}</p>
          </li>

          <!-- Library -->
          <li v-for="m in visible" :key="m.id" class="group relative min-w-0">
            <button
              type="button"
              class="block w-full text-left outline-none"
              :aria-label="`${m.fileName}, ${m.width} by ${m.height}, ${formatBytes(m.sizeInBytes)}`"
              @click="open(m.id)"
            >
              <span
                class="block aspect-square overflow-hidden rounded-xl bg-canvas ring-1 ring-charcoal/[0.06] transition group-hover:ring-charcoal/20 group-focus-within:ring-2 group-focus-within:ring-accent"
              >
                <img
                  :src="m.thumbnailUrl"
                  :alt="m.fileName"
                  loading="lazy"
                  decoding="async"
                  class="size-full object-cover transition-transform duration-300 group-hover:scale-[1.03]"
                />
              </span>
              <span class="mt-2.5 block truncate text-sm font-medium" :title="m.fileName">{{ m.fileName }}</span>
              <span class="block text-xs tabular-nums text-muted">{{ m.width }} × {{ m.height }} · {{ formatBytes(m.sizeInBytes) }}</span>
            </button>
            <div
              class="absolute right-2 top-2 opacity-0 transition-opacity focus-within:opacity-100 group-hover:opacity-100 [@media(hover:none)]:opacity-100"
            >
              <FIconButton :icon="Trash2" :label="`Delete ${m.fileName}`" size="sm" variant="surface" :disabled="isDeleting" @click="remove(m)" />
            </div>
          </li>
        </ul>

        <!-- Nothing matches the filters -->
        <div v-else class="flex flex-col items-center gap-3 px-6 py-16 text-center">
          <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><Images class="size-5" aria-hidden="true" /></span>
          <p class="font-medium">No images match</p>
          <p class="max-w-xs text-sm text-muted">
            {{ search.trim() ? `No file name contains “${search.trim()}”` : `There are no ${format} images` }}{{ search.trim() && format !== 'all' ? ` among the ${format} images` : '' }}.
          </p>
          <FButton size="sm" variant="secondary" @click="clearFilters">Clear filters</FButton>
        </div>

        <p v-if="media.length" class="border-t border-charcoal/[0.06] px-5 py-3 text-xs text-muted">
          {{ media.length }} {{ media.length === 1 ? 'image' : 'images' }} · {{ formatBytes(totalSize) }} in total
          <span class="hidden sm:inline">· Drop or paste images anywhere on this page to upload them</span>
        </p>
      </template>
    </FCard>

    <!-- Drop overlay -->
    <Transition enter-from-class="opacity-0" leave-to-class="opacity-0" enter-active-class="transition duration-150" leave-active-class="transition duration-150">
      <div v-if="dragging" class="pointer-events-none fixed inset-0 z-50 grid place-items-center bg-charcoal/40 p-6 backdrop-blur-sm">
        <div class="flex flex-col items-center gap-3 rounded-card border-2 border-dashed border-white/70 bg-charcoal/80 px-12 py-10 text-center text-warm-white">
          <ImagePlus class="size-7" aria-hidden="true" />
          <p class="font-display text-xl font-bold tracking-[-0.02em]">Drop to upload</p>
          <p class="text-sm text-warm-white/70">to {{ activeWebsite.name }} · JPEG, PNG, GIF or WebP, up to 10 MB</p>
        </div>
      </div>
    </Transition>

    <MediaDetails
      v-if="openItem"
      :item="openItem"
      :index="Math.max(0, openIndex)"
      :total="Math.max(visible.length, 1)"
      :deleting="isDeleting"
      @close="open(null)"
      @prev="step(-1)"
      @next="step(1)"
      @delete="remove(openItem)"
    />
  </div>
</template>
