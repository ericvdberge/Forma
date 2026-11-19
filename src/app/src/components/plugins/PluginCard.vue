<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight, Bell, Check, Puzzle, ShieldCheck, TriangleAlert } from 'lucide-vue-next'
import { FMenu, type FMenuItem } from '@forma/design-system'
import type { PluginDto } from '@/api/generated'
import { HOOK_EVENTS, eventLabel } from '@/composables/usePlugins'

/** One installed plugin: what it does, whether it loaded, and the events it runs for. */
const props = defineProps<{ plugin: PluginDto }>()
const emit = defineEmits<{ open: [id: string]; copyId: [id: string] }>()

const isHook = computed(() => props.plugin.events.some((e) => HOOK_EVENTS.has(e)))
const icon = computed(() => (!props.plugin.loaded ? TriangleAlert : isHook.value ? ShieldCheck : props.plugin.events.length ? Bell : Puzzle))
const iconTone = computed(() =>
  !props.plugin.loaded ? 'bg-danger/10 text-danger' : isHook.value ? 'bg-sage/20 text-positive' : 'bg-accent-50 text-accent-600',
)

const menu = computed<FMenuItem[]>(() => [
  { label: 'View runs', onSelect: () => emit('open', props.plugin.id) },
  { label: 'Copy plugin id', onSelect: () => emit('copyId', props.plugin.id) },
])
</script>

<template>
  <article
    class="group relative flex min-h-[188px] flex-col rounded-[1.25rem] bg-white p-5 ring-1 ring-charcoal/[0.06] shadow-card transition-shadow hover:shadow-soft"
  >
    <div class="flex items-start justify-between gap-3">
      <span :class="['grid size-12 place-items-center rounded-xl', iconTone]" aria-hidden="true">
        <component :is="icon" class="size-6" />
      </span>
      <FMenu :items="menu" :label="`More for ${plugin.name}`" />
    </div>

    <h3 class="mt-3 font-display text-lg font-semibold tracking-[-0.02em]">
      {{ plugin.name }}
      <span v-if="plugin.version" class="ml-1 align-middle text-xs font-normal text-subtle">v{{ plugin.version }}</span>
    </h3>
    <p v-if="plugin.loaded" class="mt-1 line-clamp-2 text-sm text-muted">{{ plugin.description || 'No description.' }}</p>
    <p v-else class="mt-1 line-clamp-3 text-sm text-danger" :title="plugin.error ?? undefined">
      Couldn’t load: {{ plugin.error }}
    </p>

    <ul v-if="plugin.events.length" class="mt-3 flex flex-wrap gap-1.5" aria-label="Runs for">
      <li
        v-for="e in plugin.events"
        :key="e"
        class="rounded-full bg-canvas px-2 py-0.5 text-[0.6875rem] font-medium text-muted ring-1 ring-charcoal/[0.05]"
        :title="e"
      >
        {{ eventLabel(e) }}
      </li>
    </ul>

    <div class="mt-auto flex items-end justify-between gap-3 pt-4">
      <span
        v-if="plugin.loaded"
        class="inline-flex items-center gap-1 rounded-full bg-positive/10 px-2.5 py-1 text-xs font-medium text-positive"
      >
        <Check class="size-3.5" aria-hidden="true" /> Installed
      </span>
      <span v-else class="inline-flex items-center gap-1 rounded-full bg-danger/10 px-2.5 py-1 text-xs font-medium text-danger">
        <TriangleAlert class="size-3.5" aria-hidden="true" /> Not loaded
      </span>
      <button
        type="button"
        class="inline-flex h-9 items-center gap-1.5 rounded-full px-4 text-sm font-medium ring-1 ring-inset ring-charcoal/15 transition hover:bg-canvas hover:ring-charcoal/40"
        @click="emit('open', plugin.id)"
      >
        Runs <ArrowRight class="size-4" aria-hidden="true" />
      </button>
    </div>
  </article>
</template>
