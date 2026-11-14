<script setup lang="ts">
import { FArchitectureArt } from '@forma/design-system'
import type { MediaItem } from '@/data/types'

/** Three tilted cards with the latest images; artwork fills in while the library is short. */
defineProps<{
  /** Newest first; the first three are shown, the newest in front. */
  items: MediaItem[]
}>()
defineEmits<{ open: [id: string] }>()

/** The two cards behind the front one: position, tilt and look. */
const backCards = [
  {
    index: 1,
    frame: 'left-[6%] top-[28%] h-[52%] w-[34%] -rotate-[9deg] bg-white shadow-float',
    fade: '',
  },
  {
    index: 2,
    frame: 'right-[2%] top-[18%] h-[66%] w-[48%] rotate-[6deg] bg-white/70 shadow-card',
    fade: 'opacity-80',
  },
]
</script>

<template>
  <div class="relative" aria-hidden="true">
    <div
      v-for="card in backCards"
      :key="card.index"
      :class="['absolute overflow-hidden rounded-[1.5rem] p-1.5 ring-1 ring-white', card.frame]"
    >
      <img v-if="items[card.index]" :src="items[card.index]!.thumbnailUrl" alt="" :class="['size-full rounded-[1.125rem] object-cover', card.fade]" />
      <div v-else :class="['size-full overflow-hidden rounded-[1.125rem]', card.fade]"><FArchitectureArt tone="light" /></div>
    </div>
    <button
      type="button"
      tabindex="-1"
      class="absolute left-[24%] top-[6%] h-[80%] w-[56%] rotate-[3deg] overflow-hidden rounded-[1.75rem] bg-white p-2 shadow-float ring-1 ring-white transition-transform duration-500 hover:rotate-[1deg] hover:scale-[1.01] disabled:pointer-events-none"
      :disabled="!items[0]"
      @click="items[0] && $emit('open', items[0].id)"
    >
      <img v-if="items[0]" :src="items[0].thumbnailUrl" alt="" class="size-full rounded-[1.25rem] object-cover" />
      <div v-else class="size-full overflow-hidden rounded-[1.25rem]"><FArchitectureArt /></div>
    </button>
  </div>
</template>
