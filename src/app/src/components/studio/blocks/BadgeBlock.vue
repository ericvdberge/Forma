<script setup lang="ts">
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'badge'>; ctx: RenderContext }>()
</script>

<template>
  <span
    :class="[
      'inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-[0.8125rem] font-medium',
      ctx.theme === 'dark' ? 'bg-white/10 text-warm-white' : 'bg-charcoal/[0.06] text-charcoal',
    ]"
  >
    <span class="size-1.5 rounded-full bg-sage" aria-hidden="true" />
    {{ block.props.text }}
  </span>
</template>
