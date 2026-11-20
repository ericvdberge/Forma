<script setup lang="ts">
import { LayoutGrid } from 'lucide-vue-next'
import type { PluginDto } from '@/api/generated'
import { eventLabel } from '@/composables/usePlugins'

/** What a plugin adds: its blocks and the events it runs for, as small chips. */
defineProps<{ plugin: PluginDto }>()
</script>

<template>
  <ul v-if="plugin.events.length || plugin.blocks.length" class="flex flex-wrap gap-1.5" aria-label="What it adds">
    <li
      v-for="b in plugin.blocks"
      :key="`block:${b.id}`"
      class="inline-flex items-center gap-1 whitespace-nowrap rounded-full bg-accent-50 px-2 py-0.5 text-[0.6875rem] font-medium text-accent-700 ring-1 ring-accent-200"
      :title="b.description ?? undefined"
    >
      <LayoutGrid class="size-3" aria-hidden="true" /> {{ b.label }} block
    </li>
    <li
      v-for="e in plugin.events"
      :key="e"
      class="whitespace-nowrap rounded-full bg-canvas px-2 py-0.5 text-[0.6875rem] font-medium text-muted ring-1 ring-charcoal/[0.05]"
      :title="e"
    >
      {{ eventLabel(e) }}
    </li>
  </ul>
</template>
