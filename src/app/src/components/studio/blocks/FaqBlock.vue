<script setup lang="ts">
import { Plus } from 'lucide-vue-next'
import { tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

defineProps<{ block: Block<'faq'>; ctx: RenderContext }>()
</script>

<template>
  <div>
    <details
      v-for="(item, i) in block.props.items"
      :key="i"
      class="group border-b py-4 text-left"
      :style="{ borderColor: tone(ctx.theme).line }"
      :open="i === 0"
    >
      <summary class="flex cursor-pointer list-none items-center justify-between gap-4 text-[1.0625rem] font-medium">
        {{ item.question }}
        <Plus class="size-4 shrink-0 transition-transform group-open:rotate-45" aria-hidden="true" />
      </summary>
      <p class="mt-2 leading-relaxed" :style="{ color: tone(ctx.theme).muted }">{{ item.answer }}</p>
    </details>
  </div>
</template>
