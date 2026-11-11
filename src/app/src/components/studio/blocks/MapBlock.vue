<script setup lang="ts">
import { computed } from 'vue'
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'map'>; ctx: RenderContext }>()
const src = computed(
  () => `https://maps.google.com/maps?q=${encodeURIComponent(props.block.props.address)}&z=${props.block.props.zoom}&output=embed`,
)
</script>

<template>
  <div class="relative overflow-hidden bg-canvas" :style="{ height: `${block.props.height}px`, borderRadius: `${block.props.radius}px` }">
    <iframe :src="src" :title="`Map of ${block.props.address}`" class="absolute inset-0 size-full border-0" loading="lazy" />
    <div v-if="ctx.editing" class="absolute inset-0" />
  </div>
</template>
