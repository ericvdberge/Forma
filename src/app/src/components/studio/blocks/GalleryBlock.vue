<script setup lang="ts">
import { columnsFor, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'gallery'>; ctx: RenderContext }>()
</script>

<template>
  <div
    class="grid"
    :style="{ gridTemplateColumns: `repeat(${columnsFor(block.props.columns, ctx.bp, 2)}, minmax(0, 1fr))`, gap: `${block.props.gap}px` }"
  >
    <img
      v-for="(img, i) in block.props.images"
      :key="i"
      :src="img.src"
      :alt="img.alt"
      class="aspect-square w-full object-cover"
      :style="{ borderRadius: `${block.props.radius}px` }"
      draggable="false"
    />
  </div>
</template>
