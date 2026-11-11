<script setup lang="ts">
import { computed } from 'vue'
import RichText from '../RichText.vue'
import { fluid, tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'text'>; ctx: RenderContext }>()
defineEmits<{ update: [path: string, value: unknown] }>()

const style = computed(() => ({
  fontSize: fluid({ lg: 19, md: 16, sm: 13 }[props.block.props.size], props.ctx.bp, 13),
  lineHeight: 1.6,
  color: props.block.props.tone === 'muted' ? tone(props.ctx.theme).muted : undefined,
}))
</script>

<template>
  <RichText
    tag="div"
    variant="text"
    :style="style"
    :html="block.props.html"
    :editable="ctx.editing && ctx.selected"
    @update="$emit('update', 'props.html', $event)"
  />
</template>
