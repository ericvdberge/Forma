<script setup lang="ts">
import { computed } from 'vue'
import RichText from '../RichText.vue'
import { fluid, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'heading'>; ctx: RenderContext }>()
defineEmits<{ update: [path: string, value: unknown] }>()

const sizes = { xl: [76, 1.02, -0.045], lg: [52, 1.06, -0.04], md: [36, 1.12, -0.03], sm: [24, 1.2, -0.02] } as const
const style = computed(() => {
  const [px, lh, ls] = sizes[props.block.props.size]
  return { fontSize: fluid(px, props.ctx.bp, 20), lineHeight: lh, letterSpacing: `${ls}em` }
})
</script>

<template>
  <RichText
    :tag="`h${block.props.level}`"
    variant="heading"
    class="font-display font-bold"
    :style="style"
    :html="block.props.html"
    :editable="ctx.editing && ctx.selected"
    @update="$emit('update', 'props.html', $event)"
  />
</template>
