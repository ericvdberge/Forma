<script setup lang="ts">
import { ref, useTemplateRef, watch } from 'vue'
import { useFullscreen } from '@vueuse/core'
import { ArrowLeft, ArrowRight } from 'lucide-vue-next'
import { FIconButton, FMediaChip } from '@forma/design-system'
import type { MediaItem } from '@/data/types'
import { formatBytes } from '@/utils/format'
import MediaDetailsTopBar from './MediaDetailsTopBar.vue'

/** The image on a blurred backdrop of itself, with the top bar and prev/next buttons over it. */
const { item } = defineProps<{
  item: MediaItem
  index: number
  total: number
  hasPrev: boolean
  hasNext: boolean
}>()
const emit = defineEmits<{ prev: []; next: []; delete: []; close: [] }>()

const stage = useTemplateRef('stage')
const fullscreen = useFullscreen(stage, { autoExit: true })

// Fade each image in once it has loaded instead of showing it paint in.
const loaded = ref(false)
watch(
  () => item.id,
  () => (loaded.value = false),
)
</script>

<template>
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
        fullscreen.isFullscreen.value ? '' : 'p-4 pt-20 pb-16 sm:px-20',
        loaded ? 'opacity-100' : 'opacity-0',
      ]"
      @load="loaded = true"
    />

    <MediaDetailsTopBar
      class="absolute inset-x-4 top-4 sm:inset-x-5 sm:top-5"
      :item="item"
      :index="index"
      :total="total"
      :can-fullscreen="fullscreen.isSupported.value"
      :fullscreen="fullscreen.isFullscreen.value"
      @toggle-fullscreen="fullscreen.toggle()"
      @delete="emit('delete')"
      @close="emit('close')"
    />

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
</template>
