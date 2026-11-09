<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { Check, ChevronLeft, ChevronRight, Copy, ExternalLink, Trash2, X } from 'lucide-vue-next'
import { FButton, FIconButton } from '@forma/design-system'
import type { MediaItem } from '@/data/types'
import { formatBytes } from '@/utils/format'

const props = defineProps<{
  item: MediaItem
  /** Position in the list the user is browsing, for "3 of 12" and prev/next. */
  index: number
  total: number
  deleting?: boolean
}>()
const emit = defineEmits<{ close: []; prev: []; next: []; delete: [] }>()

const panel = ref<HTMLElement | null>(null)
const loaded = ref(false)
watch(
  () => props.item.id,
  () => {
    loaded.value = false
    copied.value = false
  },
)

const uploadedAt = computed(() =>
  new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short' }).format(props.item.createdAt),
)
const absoluteUrl = computed(() => new URL(props.item.imageUrl, window.location.origin).href)

const copied = ref(false)
let copiedTimer: ReturnType<typeof setTimeout> | undefined
async function copyUrl() {
  try {
    await navigator.clipboard.writeText(absoluteUrl.value)
    copied.value = true
    clearTimeout(copiedTimer)
    copiedTimer = setTimeout(() => (copied.value = false), 1800)
  } catch {
    /* clipboard unavailable: the URL field can still be selected by hand */
  }
}

function onKey(e: KeyboardEvent) {
  if ((e.target as HTMLElement).closest('input, textarea, select')) return
  if (e.key === 'Escape') emit('close')
  else if (e.key === 'ArrowLeft' && props.index > 0) emit('prev')
  else if (e.key === 'ArrowRight' && props.index < props.total - 1) emit('next')
}

/* Keep focus inside the dialog and give it back to whatever opened it. */
let opener: HTMLElement | null = null
function trapFocus(e: KeyboardEvent) {
  if (e.key !== 'Tab' || !panel.value) return
  const focusable = panel.value.querySelectorAll<HTMLElement>('button:not([disabled]), a[href], input')
  const first = focusable[0]
  const last = focusable[focusable.length - 1]
  if (e.shiftKey && document.activeElement === first) {
    e.preventDefault()
    last?.focus()
  } else if (!e.shiftKey && document.activeElement === last) {
    e.preventDefault()
    first?.focus()
  }
}
onMounted(() => {
  opener = document.activeElement as HTMLElement | null
  document.addEventListener('keydown', onKey)
  nextTick(() => panel.value?.querySelector<HTMLElement>('[data-autofocus]')?.focus())
})
onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKey)
  clearTimeout(copiedTimer)
  opener?.focus?.()
})
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-stretch justify-center bg-charcoal/60 backdrop-blur-sm sm:p-6" @click.self="emit('close')">
    <div
      ref="panel"
      role="dialog"
      aria-modal="true"
      aria-labelledby="media-details-title"
      class="flex w-full max-w-[1100px] flex-col overflow-hidden bg-white shadow-float sm:rounded-card lg:flex-row"
      @keydown="trapFocus"
    >
      <!-- Preview -->
      <div class="relative flex min-h-[260px] flex-1 items-center justify-center bg-canvas p-4 sm:p-8">
        <div
          class="absolute inset-0 opacity-60 [background-image:conic-gradient(#e7e4de_25%,transparent_0_50%,#e7e4de_0_75%,transparent_0)] [background-size:20px_20px]"
          aria-hidden="true"
        />
        <img
          :key="item.id"
          :src="item.imageUrl"
          :alt="item.fileName"
          :width="item.width"
          :height="item.height"
          :class="[
            'relative max-h-[45vh] w-auto max-w-full rounded-lg object-contain shadow-card transition-opacity duration-300 lg:max-h-[calc(100dvh-8rem)]',
            loaded ? 'opacity-100' : 'opacity-0',
          ]"
          @load="loaded = true"
        />
        <img
          v-if="!loaded"
          :src="item.thumbnailUrl"
          alt=""
          aria-hidden="true"
          class="absolute max-h-[45vh] max-w-[calc(100%-2rem)] rounded-lg object-contain blur-sm lg:max-h-[calc(100dvh-8rem)]"
        />

        <div class="absolute inset-x-4 bottom-4 flex items-center justify-between sm:inset-x-6">
          <FIconButton :icon="ChevronLeft" label="Previous image" variant="surface" :disabled="index === 0" class="disabled:opacity-40" @click="emit('prev')" />
          <span class="rounded-full bg-white/90 px-3 py-1 text-xs font-medium text-charcoal shadow-soft">{{ index + 1 }} of {{ total }}</span>
          <FIconButton :icon="ChevronRight" label="Next image" variant="surface" :disabled="index >= total - 1" class="disabled:opacity-40" @click="emit('next')" />
        </div>
      </div>

      <!-- Info -->
      <aside class="flex w-full shrink-0 flex-col gap-6 overflow-y-auto p-6 lg:w-[340px]">
        <div class="flex items-start justify-between gap-3">
          <div class="min-w-0">
            <p class="text-xs font-medium uppercase tracking-[0.08em] text-subtle">{{ item.format }} image</p>
            <h2 id="media-details-title" class="mt-1 break-words font-display text-xl font-bold tracking-[-0.02em]">{{ item.fileName }}</h2>
          </div>
          <FIconButton :icon="X" label="Close" size="sm" data-autofocus @click="emit('close')" />
        </div>

        <dl class="grid grid-cols-2 gap-x-4 gap-y-4 text-sm">
          <div>
            <dt class="text-xs text-muted">Dimensions</dt>
            <dd class="mt-0.5 font-medium tabular-nums">{{ item.width }} × {{ item.height }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted">File size</dt>
            <dd class="mt-0.5 font-medium tabular-nums">{{ formatBytes(item.sizeInBytes) }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted">Format</dt>
            <dd class="mt-0.5 font-medium">{{ item.format }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted">Uploaded</dt>
            <dd class="mt-0.5 font-medium">{{ uploadedAt }}</dd>
          </div>
        </dl>

        <div>
          <label for="media-url" class="text-xs text-muted">Image URL</label>
          <div class="mt-1.5 flex items-center gap-2">
            <input
              id="media-url"
              :value="absoluteUrl"
              readonly
              class="h-9 min-w-0 flex-1 rounded-full bg-canvas px-4 text-xs text-charcoal/80 outline-none focus:ring-2 focus:ring-accent"
              @focus="($event.target as HTMLInputElement).select()"
            />
            <FIconButton :icon="copied ? Check : Copy" :label="copied ? 'Copied' : 'Copy URL'" size="sm" variant="surface" @click="copyUrl" />
          </div>
          <p class="sr-only" aria-live="polite">{{ copied ? 'Image URL copied' : '' }}</p>
        </div>

        <div class="mt-auto flex flex-col gap-2 pt-2">
          <FButton :href="item.imageUrl" variant="secondary" size="sm" target="_blank" rel="noopener">
            <ExternalLink class="size-4" aria-hidden="true" /> Open original
          </FButton>
          <button
            type="button"
            class="inline-flex h-9 items-center justify-center gap-1.5 rounded-full px-4 text-sm font-medium text-danger transition-colors hover:bg-danger/5 disabled:pointer-events-none disabled:opacity-50"
            :disabled="deleting"
            @click="emit('delete')"
          >
            <Trash2 class="size-4" aria-hidden="true" /> {{ deleting ? 'Deleting…' : 'Delete image' }}
          </button>
        </div>
      </aside>
    </div>
  </div>
</template>
