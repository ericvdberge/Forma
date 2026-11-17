<script setup lang="ts">
import { ImageOff } from 'lucide-vue-next'
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'image'>; ctx: RenderContext }>()
</script>

<template>
  <figure>
    <img
      v-if="block.props.src"
      :src="block.props.src"
      :alt="block.props.alt"
      class="w-full object-cover"
      :style="{ aspectRatio: block.props.aspect === 'auto' ? undefined : block.props.aspect, borderRadius: `${block.props.radius}px` }"
      draggable="false"
    />
    <div
      v-else
      class="grid aspect-video w-full place-items-center bg-canvas text-subtle"
      :style="{ borderRadius: `${block.props.radius}px` }"
    >
      <ImageOff class="size-6" aria-hidden="true" />
    </div>
    <figcaption v-if="block.props.caption" class="mt-2 text-sm opacity-60">{{ block.props.caption }}</figcaption>
  </figure>
</template>
