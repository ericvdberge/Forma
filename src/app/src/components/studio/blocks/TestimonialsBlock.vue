<script setup lang="ts">
import { FAvatar } from '@forma/design-system'
import { columnsFor, tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'testimonials'>; ctx: RenderContext }>()
</script>

<template>
  <div class="grid gap-4" :style="{ gridTemplateColumns: `repeat(${columnsFor(block.props.columns, ctx.bp)}, minmax(0, 1fr))` }">
    <figure
      v-for="(t, i) in block.props.items"
      :key="i"
      class="flex flex-col justify-between gap-6 rounded-2xl p-6 text-left"
      :style="{ background: tone(ctx.theme).surface, boxShadow: `inset 0 0 0 1px ${tone(ctx.theme).line}` }"
    >
      <blockquote class="text-[1.0625rem] leading-relaxed">“{{ t.quote }}”</blockquote>
      <figcaption class="flex items-center gap-3">
        <FAvatar :name="t.name" size="sm" />
        <span>
          <span class="block text-sm font-medium">{{ t.name }}</span>
          <span class="block text-xs" :style="{ color: tone(ctx.theme).muted }">{{ t.role }}</span>
        </span>
      </figcaption>
    </figure>
  </div>
</template>
