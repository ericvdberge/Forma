<script setup lang="ts">
import { Check, CirclePause, TriangleAlert } from 'lucide-vue-next'
import type { PluginStatus } from './pluginLook'

/** Active, Turned off or Not loaded. */
defineProps<{ status: PluginStatus }>()
</script>

<template>
  <span
    v-if="status === 'active'"
    class="inline-flex items-center gap-1 whitespace-nowrap rounded-full bg-positive/10 px-2.5 py-1 text-xs font-medium text-positive"
  >
    <Check class="size-3.5" aria-hidden="true" /> Active
  </span>
  <span
    v-else-if="status === 'off'"
    class="inline-flex items-center gap-1 whitespace-nowrap rounded-full bg-charcoal/[0.06] px-2.5 py-1 text-xs font-medium text-muted"
  >
    <CirclePause class="size-3.5" aria-hidden="true" /> Turned off
  </span>
  <span
    v-else
    class="inline-flex items-center gap-1 whitespace-nowrap rounded-full bg-danger/10 px-2.5 py-1 text-xs font-medium text-danger"
  >
    <TriangleAlert class="size-3.5" aria-hidden="true" /> Not loaded
  </span>
</template>
