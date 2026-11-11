<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import {
  ArrowLeft,
  ArrowRight,
  Calendar,
  Copy,
  Database,
  Download,
  ExternalLink,
  Globe,
  ImageIcon,
  Maximize,
  Minimize,
  Share2,
  Trash2,
  X,
} from 'lucide-vue-next'
import { FActionButton, FDivider, FIconButton, FMediaChip, FMenu, FModal, FThumbnailRow } from '@forma/design-system'
import type { MediaItem, Website } from '@/data/types'
import { useToast } from '@/composables/useToast'
import { formatBytes } from '@/utils/format'

const open = defineModel<boolean>('open', { required: true })
const props = defineProps<{
  item: MediaItem
  website: Website
  /** Position in the list being browsed, for "3 / 12" and prev/next. */
  index: number
  total: number
  /** While another dialog sits on top, arrow keys belong to it. */
  paused?: boolean
}>()
const emit = defineEmits<{ prev: []; next: []; delete: []; website: [] }>()
const { notify } = useToast()

const loaded = ref(false)
watch(
  () => props.item.id,
  () => (loaded.value = false),
)

const uploadedAt = computed(() => new Intl.DateTimeFormat('en', { dateStyle: 'medium' }).format(props.item.createdAt))
const link = computed(() => new URL(props.item.imageUrl, window.location.origin).href)
const hasPrev = computed(() => props.index > 0)
const hasNext = computed(() => props.index < props.total - 1)

async function copyLink() {
  try {
    await navigator.clipboard.writeText(link.value)
    notify('Link copied')
  } catch {
    notify('Couldn’t copy the link', 'error')
  }
}
async function shareLink() {
  if (!navigator.share) return copyLink()
  try {
    await navigator.share({ title: props.item.fileName, url: link.value })
  } catch {
    /* cancelled */
  }
}

/* Fullscreen preview */
const stage = ref<HTMLElement | null>(null)
const fullscreen = ref(false)
const canFullscreen = typeof document !== 'undefined' && document.fullscreenEnabled
function toggleFullscreen() {
  if (document.fullscreenElement) document.exitFullscreen()
  else stage.value?.requestFullscreen().catch(() => {})
}
const onFullscreenChange = () => (fullscreen.value = document.fullscreenElement === stage.value)

function onKey(e: KeyboardEvent) {
  if (props.paused || (e.target as HTMLElement).closest('input, textarea, select, [role=menu]')) return
  if (e.key === 'ArrowLeft' && hasPrev.value) emit('prev')
  else if (e.key === 'ArrowRight' && hasNext.value) emit('next')
}
onMounted(() => {
  document.addEventListener('keydown', onKey)
  document.addEventListener('fullscreenchange', onFullscreenChange)
})
onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKey)
  document.removeEventListener('fullscreenchange', onFullscreenChange)
  if (document.fullscreenElement) document.exitFullscreen()
})

const moreItems = computed(() => [
  { label: 'Open original', icon: ExternalLink, href: props.item.imageUrl, target: '_blank' },
  { label: 'Copy link', icon: Copy, onSelect: copyLink },
  { label: 'Delete', icon: Trash2, danger: true, onSelect: () => emit('delete') },
])
</script>

<template>
  <FModal
    v-model:open="open"
    size="xl"
    fullscreen-on-mobile
    labelledby="media-details-title"
    panel-class="lg:h-[min(780px,calc(100dvh-3rem))] lg:flex-row p-0 lg:p-2"
  >
    <!-- Stage -->
    <div
      ref="stage"
      class="relative isolate flex min-h-[46dvh] flex-1 items-center justify-center overflow-hidden bg-canvas lg:min-h-0 lg:rounded-[1.375rem]"
    >
      <img
        :src="item.thumbnailUrl"
        alt=""
        aria-hidden="true"
        class="absolute inset-0 -z-10 size-full scale-110 object-cover opacity-70 blur-2xl"
      />
      <img
        :key="item.id"
        :src="item.imageUrl"
        :alt="item.fileName"
        :width="item.width"
        :height="item.height"
        :class="[
          'max-h-full max-w-full object-contain transition-opacity duration-300',
          fullscreen ? '' : 'p-4 pt-20 pb-16 sm:px-20',
          loaded ? 'opacity-100' : 'opacity-0',
        ]"
        @load="loaded = true"
      />

      <!-- Top bar -->
      <div class="absolute inset-x-4 top-4 flex items-center justify-between gap-2 sm:inset-x-5 sm:top-5">
        <FMediaChip :icon="null" size="md">{{ index + 1 }} / {{ total }}</FMediaChip>
        <div class="flex items-center gap-2">
          <FIconButton
            v-if="canFullscreen"
            :icon="fullscreen ? Minimize : Maximize"
            :label="fullscreen ? 'Exit full screen' : 'Full screen'"
            variant="glass"
            shape="circle"
            class="hidden sm:grid"
            @click="toggleFullscreen"
          />
          <a
            :href="item.imageUrl"
            :download="item.fileName"
            class="grid size-10 place-items-center rounded-full bg-white/75 text-charcoal ring-1 ring-white/60 shadow-soft backdrop-blur-md transition-colors hover:bg-white"
            aria-label="Download"
            title="Download"
          >
            <Download class="size-5" :stroke-width="1.6" aria-hidden="true" />
          </a>
          <FMenu :items="moreItems" label="More actions" variant="glass" shape="circle" size="md" />
          <FIconButton :icon="X" label="Close" variant="glass" shape="circle" class="lg:hidden" @click="open = false" />
        </div>
      </div>

      <!-- Prev / next -->
      <FIconButton
        :icon="ArrowLeft"
        label="Previous image"
        variant="surface"
        shape="circle"
        size="lg"
        class="absolute left-4 top-1/2 -translate-y-1/2 sm:left-5"
        :disabled="!hasPrev"
        @click="emit('prev')"
      />
      <FIconButton
        :icon="ArrowRight"
        label="Next image"
        variant="surface"
        shape="circle"
        size="lg"
        class="absolute right-4 top-1/2 -translate-y-1/2 sm:right-5"
        :disabled="!hasNext"
        @click="emit('next')"
      />

      <FMediaChip size="md" class="absolute bottom-4 left-4 sm:bottom-5 sm:left-5">{{ item.extension }} · {{ formatBytes(item.sizeInBytes) }}</FMediaChip>
    </div>

    <!-- Info -->
    <aside class="relative flex w-full shrink-0 flex-col gap-6 overflow-y-auto px-6 pb-6 pt-7 lg:w-[340px] lg:px-7 lg:pt-16">
      <FIconButton :icon="X" label="Close" variant="outline" shape="circle" class="absolute right-5 top-4 hidden lg:grid" data-autofocus @click="open = false" />

      <div>
        <h2 id="media-details-title" class="break-words font-display text-[1.625rem] font-bold leading-tight tracking-[-0.03em]">{{ item.fileName }}</h2>
        <p class="mt-2 text-[0.9375rem] text-muted">Part of the media library of {{ website.name }}.</p>
      </div>

      <FDivider />

      <ul class="flex flex-col gap-3.5 text-[0.9375rem] text-charcoal/85">
        <li class="flex items-center gap-3"><ImageIcon class="size-[1.125rem] text-charcoal/60" :stroke-width="1.6" aria-hidden="true" />Image · {{ item.extension }}</li>
        <li class="flex items-center gap-3"><Database class="size-[1.125rem] text-charcoal/60" :stroke-width="1.6" aria-hidden="true" />{{ formatBytes(item.sizeInBytes) }}</li>
        <li class="flex items-center gap-3"><Calendar class="size-[1.125rem] text-charcoal/60" :stroke-width="1.6" aria-hidden="true" />Uploaded {{ uploadedAt }}</li>
        <li class="flex items-center gap-3"><Maximize class="size-[1.125rem] text-charcoal/60" :stroke-width="1.6" aria-hidden="true" /><span class="tabular-nums">{{ item.width }} × {{ item.height }} px</span></li>
      </ul>

      <FDivider />

      <section aria-labelledby="media-actions-title">
        <h3 id="media-actions-title" class="mb-3 text-[0.9375rem] font-medium">Actions</h3>
        <div class="flex flex-col gap-2">
          <FActionButton :icon="Download" :href="item.imageUrl" :download="item.fileName" size="sm">Download</FActionButton>
          <FActionButton :icon="Share2" size="sm" @click="shareLink">Share link</FActionButton>
          <FActionButton :icon="Copy" size="sm" @click="copyLink">Copy link</FActionButton>
          <FActionButton :icon="Trash2" size="sm" tone="danger" @click="emit('delete')">Delete</FActionButton>
        </div>
      </section>

      <FThumbnailRow
        class="mt-auto"
        bordered
        interactive
        eyebrow="In website"
        :title="website.name"
        :subtitle="website.domain"
        :icon="Globe"
        @click="emit('website')"
      >
        <template #trailing><ArrowRight class="mr-1 size-4 text-charcoal/60" aria-hidden="true" /></template>
      </FThumbnailRow>
    </aside>
  </FModal>
</template>
