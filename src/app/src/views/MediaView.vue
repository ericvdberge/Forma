<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Clock,
  Copy,
  Database,
  Download,
  Expand,
  ImageIcon,
  ImagePlus,
  Images,
  LayoutGrid,
  List,
  Scale,
  Search,
  Trash2,
  TriangleAlert,
} from 'lucide-vue-next'
import {
  FButton,
  FMediaCard,
  FMenu,
  FPillTabs,
  FSegmented,
  FSelect,
  FSkeleton,
  FStatGroup,
  FStatTile,
  type FMenuItem,
} from '@forma/design-system'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import MediaDetails from '@/components/media/MediaDetails.vue'
import MediaHero from '@/components/media/MediaHero.vue'
import MediaSidePanel from '@/components/media/MediaSidePanel.vue'
import { useWebsites } from '@/composables/useWebsites'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { ACCEPTED_TYPES, useMedia } from '@/composables/useMedia'
import { useToast } from '@/composables/useToast'
import type { MediaItem } from '@/data/types'
import { formatBytes } from '@/utils/format'
import { shortAgo } from '@/utils/time'

const { getWebsites } = useWebsites()
const { websites } = getWebsites()
const { getActiveWebsite, setActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
const { getMediaItems, getUploads, uploadFiles, dismissUpload, dismissFailed, deleteMediaItem, isDeleting } = useMedia()
const { media, isPending, isError, isFetching, refetch } = getMediaItems(() => activeWebsite.value.id)
const uploads = getUploads(() => activeWebsite.value.id)
const { notify } = useToast()
const route = useRoute()
const router = useRouter()

/* Website switcher */
const websiteId = computed({
  get: () => activeWebsite.value.id,
  set: (id: string) => setActiveWebsite(id),
})
const websiteOptions = computed(() => websites.value.map((w) => ({ label: w.name, value: w.id })))
const goToWebsite = () => router.push(`/websites/${activeWebsite.value.id}`)

/* Stats */
const WEEK = 7 * 86_400_000
const stats = computed(() => {
  const all = media.value
  const total = all.reduce((s, m) => s + m.sizeInBytes, 0)
  const recent = all.filter((m) => Date.now() - m.createdAt < WEEK)
  const recentSize = recent.reduce((s, m) => s + m.sizeInBytes, 0)
  const growth = (added: number, now: number) => {
    if (!added) return undefined
    const before = now - added
    return before > 0 ? `${Math.round((added / before) * 100)}%` : undefined
  }
  return {
    count: all.length.toLocaleString('en'),
    countChange: growth(recent.length, all.length),
    size: formatBytes(total),
    sizeChange: growth(recentSize, total),
    average: all.length ? formatBytes(total / all.length) : '—',
    latest: all[0] ? shortAgo(all[0].createdAt) : '—',
  }
})

/* Filters */
type Format = 'all' | string
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
type View = 'grid' | 'list'
const VIEW_KEY = 'forma.mediaView'
const view = ref<View>(readView())
function readView(): View {
  try {
    return localStorage.getItem(VIEW_KEY) === 'list' ? 'list' : 'grid'
  } catch {
    return 'grid'
  }
}
watch(view, (v) => {
  try {
    localStorage.setItem(VIEW_KEY, v)
  } catch {
    /* storage unavailable */
  }
})
const viewOptions = [
  { value: 'grid' as const, label: 'Grid view', icon: LayoutGrid },
  { value: 'list' as const, label: 'List view', icon: List },
]

const searched = computed(() => {
  const q = search.value.trim().toLowerCase()
  return q ? media.value.filter((m) => m.fileName.toLowerCase().includes(q)) : media.value
})
const tabs = computed(() =>
  ['all', ...Object.values(ACCEPTED_TYPES)].map((value) => ({
    value,
    label: value === 'all' ? 'All' : value,
    count: value === 'all' ? undefined : searched.value.filter((m) => m.format === value).length,
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
function clearFilters() {
  search.value = ''
  format.value = 'all'
}
watch(() => activeWebsite.value.id, clearFilters)

/* Uploading: the buttons, the dropzone, files dropped anywhere on the page, or a paste */
const sidePanel = ref<InstanceType<typeof MediaSidePanel> | null>(null)
const canUpload = computed(() => !isPending.value && !isError.value)
const activeUploads = computed(() => uploads.value.filter((u) => u.status !== 'failed'))
const browse = () => sidePanel.value?.browse()
function startUpload(files: FileList | File[]) {
  // New uploads land at the top of the grid, so make sure they aren't filtered out of view.
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
  dragDepth = 0
  dragging.value = false
  // The side panel's dropzone handles its own drops.
  if (e.defaultPrevented) return
  e.preventDefault()
  if (canUpload.value && e.dataTransfer?.files.length) startUpload(e.dataTransfer.files)
}
function onPaste(e: ClipboardEvent) {
  if ((e.target as HTMLElement).closest('input, textarea') || !canUpload.value) return
  const files = Array.from(e.clipboardData?.files ?? [])
  if (files.length) startUpload(files)
}
/** "Paste from clipboard" button: reads images through the async clipboard API. */
async function pasteFromClipboard() {
  try {
    const files: File[] = []
    for (const item of await navigator.clipboard.read()) {
      const type = item.types.find((t) => ACCEPTED_TYPES[t])
      if (!type) continue
      const blob = await item.getType(type)
      files.push(new File([blob], `pasted-${Date.now()}.${type.split('/')[1]!.replace('jpeg', 'jpg')}`, { type }))
    }
    if (files.length) startUpload(files)
    else notify('There’s no image on the clipboard', 'error')
  } catch {
    notify('Couldn’t read the clipboard', 'error')
  }
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
const detailsOpen = computed({
  get: () => !!openItem.value,
  set: (v) => {
    if (!v) open(null)
  },
})
function open(id: string | null) {
  router.replace({ query: { ...route.query, item: id ?? undefined } })
}
function step(by: number) {
  const next = visible.value[openIndex.value + by]
  if (next) open(next.id)
}
watch([openId, isPending, media], () => {
  if (openId.value && !isPending.value && !isError.value && !openItem.value) open(null)
})

/* Delete, after confirming */
const pendingDelete = ref<MediaItem | null>(null)
const confirmOpen = computed({
  get: () => !!pendingDelete.value,
  set: (v) => {
    if (!v) pendingDelete.value = null
  },
})
async function confirmDelete() {
  const item = pendingDelete.value
  if (!item) return
  const index = visible.value.findIndex((m) => m.id === item.id)
  const neighbour = visible.value[index + 1] ?? visible.value[index - 1]
  const wasOpen = openId.value === item.id
  if (await deleteMediaItem(item)) {
    pendingDelete.value = null
    if (wasOpen) open(neighbour?.id ?? null)
  }
}

async function copyLink(item: MediaItem) {
  try {
    await navigator.clipboard.writeText(new URL(item.imageUrl, window.location.origin).href)
    notify('Link copied')
  } catch {
    notify('Couldn’t copy the link', 'error')
  }
}
function menuFor(item: MediaItem): FMenuItem[] {
  return [
    { label: 'View details', icon: Expand, onSelect: () => open(item.id) },
    { label: 'Download', icon: Download, href: item.imageUrl, download: item.fileName },
    { label: 'Copy link', icon: Copy, onSelect: () => copyLink(item) },
    { label: 'Delete', icon: Trash2, danger: true, onSelect: () => (pendingDelete.value = item) },
  ]
}
const uploadedOn = (ts: number) => new Intl.DateTimeFormat('en', { dateStyle: 'medium' }).format(ts)
</script>

<template>
  <div class="relative mx-auto max-w-[1480px]">
    <!-- Soft architectural wash behind the hero -->
    <div
      class="pointer-events-none absolute -right-8 -top-24 -z-0 hidden h-[520px] w-[60%] rounded-bl-[200px] bg-[radial-gradient(ellipse_at_top_right,#dfe3e8_0%,transparent_65%)] md:block"
      aria-hidden="true"
    />

    <div class="relative grid gap-8 xl:grid-cols-[minmax(0,1fr)_320px]">
      <div class="min-w-0">
        <MediaHero
          :latest="media.slice(0, 3)"
          :upload="activeUploads[0]"
          :more-uploading="Math.max(0, activeUploads.length - 1)"
          :can-upload="canUpload"
          @upload="browse"
          @open="open"
        />

        <!-- Stats -->
        <div v-if="isPending" class="grid grid-cols-2 gap-5 rounded-panel bg-white/80 p-5 ring-1 ring-charcoal/[0.06] lg:grid-cols-4">
          <div v-for="i in 4" :key="i" class="flex items-center gap-3.5">
            <FSkeleton class="size-12 shrink-0" rounded="full" />
            <div><FSkeleton class="h-6 w-16" /><FSkeleton class="mt-1.5 h-3 w-20" /></div>
          </div>
        </div>
        <FStatGroup v-else-if="!isError">
          <FStatTile :icon="Images" :value="stats.count" label="Total media" :change="stats.countChange" change-label="this week" />
          <FStatTile :icon="Database" :value="stats.size" label="Storage used" :change="stats.sizeChange" change-label="this week" />
          <FStatTile :icon="Scale" :value="stats.average" label="Average size" />
          <FStatTile :icon="Clock" :value="stats.latest" label="Last upload" />
        </FStatGroup>

        <!-- Toolbar -->
        <div class="mt-8 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <FPillTabs v-model="format" :options="tabs" label="Filter by format" />
          <div class="flex flex-wrap items-center gap-2">
            <div class="relative min-w-0 flex-1 sm:w-52 sm:flex-none">
              <Search class="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-subtle" aria-hidden="true" />
              <label for="media-search" class="sr-only">Search media by file name</label>
              <input
                id="media-search"
                v-model="search"
                type="search"
                placeholder="Search media…"
                class="h-10 w-full rounded-full bg-white/70 pl-10 pr-4 text-sm outline-none ring-1 ring-charcoal/[0.08] transition-shadow placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
              />
            </div>
            <FSelect v-model="sort" label="Sort" hide-label size="sm" variant="plain" :options="sortOptions" class="w-[9.5rem]" />
            <FSegmented v-model="view" :options="viewOptions" label="Layout" />
          </div>
        </div>

        <!-- Loading -->
        <div v-if="isPending" class="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4" aria-busy="true" aria-label="Loading media">
          <FSkeleton v-for="i in 8" :key="i" class="aspect-[16/10] w-full" rounded="2xl" />
        </div>

        <!-- Error -->
        <div v-else-if="isError" class="mt-6 flex flex-col items-center gap-3 rounded-panel bg-white px-6 py-16 text-center ring-1 ring-charcoal/[0.06]">
          <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><TriangleAlert class="size-5" aria-hidden="true" /></span>
          <p class="font-medium">Couldn’t load the media</p>
          <p class="max-w-xs text-sm text-muted">Check that the Forma API is running, then try again.</p>
          <FButton size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
        </div>

        <!-- Empty library -->
        <button
          v-else-if="!media.length"
          type="button"
          class="mt-6 flex w-full flex-col items-center gap-3 rounded-panel border-[1.5px] border-dashed border-charcoal/15 bg-white/50 px-6 py-20 text-center transition-colors hover:border-charcoal/30 hover:bg-white"
          @click="browse"
        >
          <span class="grid size-14 place-items-center rounded-2xl bg-white shadow-soft"><ImagePlus class="size-6" :stroke-width="1.5" aria-hidden="true" /></span>
          <span class="mt-1 font-display text-lg font-bold tracking-[-0.02em]">No images yet</span>
          <span class="max-w-sm text-sm text-muted">
            Drop images anywhere on this page, paste one, or <span class="font-medium text-charcoal underline underline-offset-2">browse your files</span>.
          </span>
        </button>

        <!-- Nothing matches -->
        <div v-else-if="!visible.length" class="mt-6 flex flex-col items-center gap-3 rounded-panel bg-white/60 px-6 py-16 text-center ring-1 ring-charcoal/[0.06]">
          <span class="grid size-12 place-items-center rounded-2xl bg-white shadow-soft"><Images class="size-5" aria-hidden="true" /></span>
          <p class="font-medium">No images match</p>
          <p class="max-w-xs text-sm text-muted">
            {{ search.trim() ? `No file name contains “${search.trim()}”` : `There are no ${format} images` }}{{ search.trim() && format !== 'all' ? ` among the ${format} images` : '' }}.
          </p>
          <FButton size="sm" variant="secondary" @click="clearFilters">Clear filters</FButton>
        </div>

        <!-- Grid -->
        <ul v-else-if="view === 'grid'" class="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4" aria-label="Images">
          <li v-for="m in visible" :key="m.id">
            <FMediaCard
              :src="m.thumbnailUrl"
              :alt="`${m.fileName}, ${m.width} × ${m.height}, ${formatBytes(m.sizeInBytes)}`"
              :meta="`${m.extension} · ${formatBytes(m.sizeInBytes)}`"
              :selected="openId === m.id"
              @open="open(m.id)"
            >
              <template #actions>
                <FMenu :items="menuFor(m)" :label="`Actions for ${m.fileName}`" variant="glass" />
              </template>
            </FMediaCard>
          </li>
        </ul>

        <!-- List -->
        <div v-else class="mt-6 overflow-x-auto rounded-panel bg-white ring-1 ring-charcoal/[0.06] shadow-soft">
          <table class="w-full min-w-[640px] text-left text-sm">
            <thead class="text-xs text-muted">
              <tr class="border-b border-charcoal/[0.06]">
                <th scope="col" class="px-5 py-3 font-medium">File</th>
                <th scope="col" class="px-5 py-3 font-medium">Dimensions</th>
                <th scope="col" class="px-5 py-3 font-medium">Size</th>
                <th scope="col" class="px-5 py-3 font-medium">Uploaded</th>
                <th scope="col" class="w-14 px-5 py-3"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="m in visible"
                :key="m.id"
                class="cursor-pointer border-b border-charcoal/[0.04] transition-colors last:border-0 hover:bg-warm-white"
                @click="open(m.id)"
              >
                <td class="px-5 py-3">
                  <div class="flex items-center gap-3">
                    <img :src="m.thumbnailUrl" alt="" loading="lazy" class="h-11 w-16 shrink-0 rounded-lg bg-canvas object-cover" />
                    <div class="min-w-0">
                      <button type="button" class="block max-w-[260px] truncate text-left font-medium hover:underline" @click.stop="open(m.id)">{{ m.fileName }}</button>
                      <p class="flex items-center gap-1 text-xs text-muted"><ImageIcon class="size-3" aria-hidden="true" />{{ m.extension }}</p>
                    </div>
                  </div>
                </td>
                <td class="whitespace-nowrap px-5 py-3 tabular-nums text-muted">{{ m.width }} × {{ m.height }}</td>
                <td class="whitespace-nowrap px-5 py-3 tabular-nums text-muted">{{ formatBytes(m.sizeInBytes) }}</td>
                <td class="whitespace-nowrap px-5 py-3 text-muted">{{ uploadedOn(m.createdAt) }}</td>
                <td class="px-5 py-3" @click.stop>
                  <FMenu :items="menuFor(m)" :label="`Actions for ${m.fileName}`" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Side panel -->
      <div class="flex flex-col gap-4 xl:sticky xl:top-4 xl:self-start">
        <FSelect v-model="websiteId" label="Website" hide-label size="sm" :options="websiteOptions" />
        <MediaSidePanel
          ref="sidePanel"
          :media="media"
          :uploads="uploads"
          :can-upload="canUpload"
          @files="startUpload"
          @dismiss="dismissUpload"
          @dismiss-failed="dismissFailed(activeWebsite.id)"
          @format="(f) => (format = f)"
          @paste="pasteFromClipboard"
          @website="goToWebsite"
        />
      </div>
    </div>

    <!-- Drop overlay -->
    <Transition enter-from-class="opacity-0" leave-to-class="opacity-0" enter-active-class="transition duration-150" leave-active-class="transition duration-150">
      <div v-if="dragging" class="pointer-events-none fixed inset-0 z-50 grid place-items-center bg-charcoal/40 p-6 backdrop-blur-sm">
        <div class="flex flex-col items-center gap-3 rounded-panel border-2 border-dashed border-white/70 bg-charcoal/80 px-12 py-10 text-center text-warm-white">
          <ImagePlus class="size-7" aria-hidden="true" />
          <p class="font-display text-xl font-bold tracking-[-0.02em]">Drop to upload</p>
          <p class="text-sm text-warm-white/70">to {{ activeWebsite.name }} · JPEG, PNG, GIF or WebP, up to 10 MB</p>
        </div>
      </div>
    </Transition>

    <MediaDetails
      v-if="openItem"
      v-model:open="detailsOpen"
      :item="openItem"
      :website="activeWebsite"
      :index="Math.max(0, openIndex)"
      :total="Math.max(visible.length, 1)"
      :paused="confirmOpen"
      @prev="step(-1)"
      @next="step(1)"
      @delete="pendingDelete = openItem"
      @website="goToWebsite"
    />

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="`Delete “${pendingDelete?.fileName ?? ''}”?`"
      message="The image and its thumbnail are removed for good. Pages that use it will show a broken image."
      :busy="isDeleting"
      @confirm="confirmDelete"
    />
  </div>
</template>
