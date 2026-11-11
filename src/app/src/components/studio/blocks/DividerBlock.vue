<script setup lang="ts">
import { tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'divider'>; ctx: RenderContext }>()
</script>

<template>
  <div class="py-2">
    <hr :style="{ borderTop: `${block.props.thickness}px ${block.props.style} ${tone(ctx.theme).line}` }" />
  </div>
</template>
