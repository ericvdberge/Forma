<script setup lang="ts">
import { computed } from 'vue'
import { Sparkles } from 'lucide-vue-next'
import { featureIcons } from '@/studio/blocks'
import { columnsFor, tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'features'>; ctx: RenderContext }>()
const cols = computed(() => columnsFor(props.block.props.columns, props.ctx.bp))
const t = computed(() => tone(props.ctx.theme))
</script>

<template>
  <div
    class="grid text-left"
    :class="block.props.card ? 'rounded-2xl p-2' : 'gap-6'"
    :style="{
      gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`,
      background: block.props.card ? t.surface : undefined,
      boxShadow: block.props.card ? `inset 0 0 0 1px ${t.line}` : undefined,
    }"
  >
    <div
      v-for="(f, i) in block.props.items"
      :key="i"
      :class="block.props.card && 'p-5'"
      :style="block.props.card && i % cols !== 0 ? { boxShadow: `inset 1px 0 0 ${t.line}` } : undefined"
    >
      <span
        class="grid size-9 place-items-center rounded-lg"
        :style="{ background: ctx.theme === 'dark' ? 'rgb(255 255 255 / 0.08)' : 'rgb(168 181 160 / 0.25)' }"
      >
        <component :is="featureIcons[f.icon] ?? Sparkles" class="size-[18px]" :stroke-width="1.7" aria-hidden="true" />
      </span>
      <p class="mt-4 font-bold">{{ f.title }}</p>
      <p class="mt-1 text-sm leading-relaxed" :style="{ color: t.muted }">{{ f.text }}</p>
    </div>
  </div>
</template>
