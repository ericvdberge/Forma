<script setup lang="ts">
import { computed } from 'vue'
import { SquarePlay } from 'lucide-vue-next'
import { embedUrl, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'video'>; ctx: RenderContext }>()
const src = computed(() => embedUrl(props.block.props.url))
</script>

<template>
  <div class="relative overflow-hidden bg-charcoal" :style="{ aspectRatio: block.props.aspect, borderRadius: `${block.props.radius}px` }">
    <iframe
      v-if="src"
      :src="src"
      title="Embedded video"
      class="absolute inset-0 size-full"
      allow="accelerometer; encrypted-media; picture-in-picture"
      allowfullscreen
    />
    <div v-else class="absolute inset-0 grid place-items-center text-center text-warm-white/70">
      <div>
        <SquarePlay class="mx-auto size-8" :stroke-width="1.4" aria-hidden="true" />
        <p class="mt-2 text-sm">Paste a YouTube or Vimeo link in the inspector</p>
      </div>
    </div>
    <!-- In the editor, clicks select the block instead of playing the video. -->
    <div v-if="ctx.editing" class="absolute inset-0" />
  </div>
</template>
