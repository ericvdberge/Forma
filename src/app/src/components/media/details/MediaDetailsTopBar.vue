<script setup lang="ts">
import { computed } from 'vue'
import { Copy, Download, ExternalLink, Maximize, Minimize, Trash2, X } from 'lucide-vue-next'
import { FIconButton, FMediaChip, FMenu } from '@forma/design-system'
import type { MediaItem } from '@/data/types'
import { useMediaLink } from '@/composables/useMediaLink'

/** Position counter and the quick actions laid over the image. */
const { item } = defineProps<{
  item: MediaItem
  index: number
  total: number
  canFullscreen: boolean
  fullscreen: boolean
}>()
const emit = defineEmits<{ toggleFullscreen: []; delete: []; close: [] }>()
const { copyLink } = useMediaLink()

const moreItems = computed(() => [
  { label: 'Open original', icon: ExternalLink, href: item.imageUrl, target: '_blank' },
  { label: 'Copy link', icon: Copy, onSelect: () => copyLink(item) },
  { label: 'Delete', icon: Trash2, danger: true, onSelect: () => emit('delete') },
])
</script>

<template>
  <div class="flex items-center justify-between gap-2">
    <FMediaChip :icon="null" size="md">{{ index + 1 }} / {{ total }}</FMediaChip>
    <div class="flex items-center gap-2">
      <FIconButton
        v-if="canFullscreen"
        :icon="fullscreen ? Minimize : Maximize"
        :label="fullscreen ? 'Exit full screen' : 'Full screen'"
        variant="glass"
        shape="circle"
        class="hidden sm:grid"
        @click="emit('toggleFullscreen')"
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
      <FIconButton :icon="X" label="Close" variant="glass" shape="circle" class="lg:hidden" @click="emit('close')" />
    </div>
  </div>
</template>
