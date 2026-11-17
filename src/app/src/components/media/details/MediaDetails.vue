<script setup lang="ts">
import { computed } from 'vue'
import { onKeyStroke } from '@vueuse/core'
import { FModal } from '@forma/design-system'
import type { MediaItem, Website } from '@/data/types'
import MediaDetailsInfo from './MediaDetailsInfo.vue'
import MediaDetailsStage from './MediaDetailsStage.vue'

/** Full-size preview of one image with its details, browsable with the arrow keys. */
const open = defineModel<boolean>('open', { required: true })
const { index, total, paused } = defineProps<{
  item: MediaItem
  website: Website
  /** Position in the list being browsed, for "3 / 12" and prev/next. */
  index: number
  total: number
  /** While another dialog sits on top, arrow keys belong to it. */
  paused?: boolean
}>()
const emit = defineEmits<{ prev: []; next: []; delete: []; website: [] }>()

const hasPrev = computed(() => index > 0)
const hasNext = computed(() => index < total - 1)

onKeyStroke(['ArrowLeft', 'ArrowRight'], (e) => {
  if (paused || (e.target as HTMLElement).closest('input, textarea, select, [role=menu]')) return
  if (e.key === 'ArrowLeft' && hasPrev.value) emit('prev')
  else if (e.key === 'ArrowRight' && hasNext.value) emit('next')
})
</script>

<template>
  <FModal
    v-model:open="open"
    size="xl"
    fullscreen-on-mobile
    labelledby="media-details-title"
    panel-class="lg:h-[min(780px,calc(100dvh-3rem))] lg:flex-row p-0 lg:p-2"
  >
    <MediaDetailsStage
      :item="item"
      :index="index"
      :total="total"
      :has-prev="hasPrev"
      :has-next="hasNext"
      @prev="emit('prev')"
      @next="emit('next')"
      @delete="emit('delete')"
      @close="open = false"
    />
    <MediaDetailsInfo
      title-id="media-details-title"
      :item="item"
      :website="website"
      @delete="emit('delete')"
      @website="emit('website')"
      @close="open = false"
    />
  </FModal>
</template>
