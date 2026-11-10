<script setup lang="ts">
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'spacer'>; ctx: RenderContext }>()
</script>

<template>
  <div
    :style="{ height: `${block.props.height}px` }"
    :class="ctx.editing && 'studio-spacer'"
    aria-hidden="true"
  />
</template>

<style>
.studio-spacer {
  background: repeating-linear-gradient(135deg, transparent 0 6px, rgb(59 130 246 / 0.07) 6px 7px);
}
</style>
