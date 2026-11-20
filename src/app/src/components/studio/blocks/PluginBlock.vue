<script setup lang="ts">
import { computed, ref, watchEffect } from 'vue'
import { Puzzle } from 'lucide-vue-next'
import type { RenderContext } from '@/studio/render'
import { findPluginBlock, renderPluginTemplate } from '@/studio/pluginBlocks'
import type { Block } from '@/studio/types'

/** A block from a plugin, drawn from the plugin's template (see studio/pluginBlocks.ts for what a template may do). */
const props = defineProps<{ block: Block<'plugin'>; ctx: RenderContext }>()

const info = computed(() => findPluginBlock(props.block.props.plugin, props.block.props.block))
const host = ref<HTMLElement | null>(null)

watchEffect(() => {
  if (!host.value || !info.value) return
  host.value.replaceChildren(renderPluginTemplate(info.value.template, props.block.props.values))
})

/** Links don't navigate while editing, like the built-in blocks. */
function onClick(e: MouseEvent) {
  if (props.ctx.editing && (e.target as HTMLElement).closest('a')) e.preventDefault()
}
</script>

<template>
  <div v-if="info" ref="host" class="studio-plugin-block" :data-plugin="info.pluginId" @click="onClick" />
  <div
    v-else
    class="flex items-center gap-3 rounded-xl border border-dashed border-current/25 px-4 py-5 text-sm opacity-70"
    role="note"
  >
    <Puzzle class="size-5 shrink-0" aria-hidden="true" />
    <span>
      This block comes from the plugin “{{ block.props.plugin || 'unknown' }}”, which is turned off or isn’t installed.
      It won’t show on the page until the plugin is back on.
    </span>
  </div>
</template>
