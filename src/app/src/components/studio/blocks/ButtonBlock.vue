<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'button'>; ctx: RenderContext }>()

const classes = computed(() => {
  const { variant, size } = props.block.props
  const dark = props.ctx.theme === 'dark'
  return cn(
    'inline-flex items-center justify-center gap-2 rounded-full font-medium whitespace-nowrap transition-colors',
    { sm: 'h-9 px-4 text-sm', md: 'h-11 px-5 text-[0.9375rem]', lg: 'h-14 px-7 text-base' }[size],
    variant === 'primary' && (dark ? 'bg-warm-white text-charcoal hover:bg-white' : 'bg-charcoal text-warm-white hover:bg-ink'),
    variant === 'secondary' &&
      (dark ? 'ring-1 ring-inset ring-warm-white/30 text-warm-white hover:ring-warm-white/70' : 'bg-white/70 ring-1 ring-inset ring-charcoal/15 hover:ring-charcoal/50'),
    variant === 'accent' && 'bg-accent text-white hover:bg-accent-600',
  )
})
</script>

<template>
  <a :href="ctx.editing ? undefined : block.props.href || undefined" :class="classes" @click="ctx.editing && $event.preventDefault()">
    {{ block.props.label || 'Button' }}
    <ArrowRight v-if="block.props.arrow" class="size-4" aria-hidden="true" />
  </a>
</template>
