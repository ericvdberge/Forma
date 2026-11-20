<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight, Bell, Check, CirclePause, LayoutGrid, Puzzle, ShieldCheck, Trash2, TriangleAlert } from 'lucide-vue-next'
import { FMenu, FSwitch, cn, type FMenuItem } from '@forma/design-system'
import type { PluginDto } from '@/api/generated'
import { HOOK_EVENTS, eventLabel } from '@/composables/usePlugins'

/** One installed plugin: what it does, whether it loaded, and the events it runs for. */
const props = defineProps<{ plugin: PluginDto }>()
const emit = defineEmits<{ open: [id: string]; copyId: [id: string]; toggle: [enabled: boolean]; delete: [] }>()

const isHook = computed(() => props.plugin.events.some((e) => HOOK_EVENTS.has(e)))
const icon = computed(() => (!props.plugin.loaded ? TriangleAlert : isHook.value ? ShieldCheck : props.plugin.events.length ? Bell : Puzzle))
const iconTone = computed(() =>
  !props.plugin.loaded ? 'bg-danger/10 text-danger' : isHook.value ? 'bg-sage/20 text-positive' : 'bg-accent-50 text-accent-600',
)

const menu = computed<FMenuItem[]>(() => [
  { label: 'View runs', onSelect: () => emit('open', props.plugin.id) },
  { label: 'Copy plugin id', onSelect: () => emit('copyId', props.plugin.id) },
  { label: 'Delete plugin', icon: Trash2, danger: true, onSelect: () => emit('delete') },
])

const on = computed({
  get: () => props.plugin.loaded && props.plugin.enabled,
  set: (v: boolean) => emit('toggle', v),
})
</script>

<template>
  <article
    :class="
      cn(
        'group relative flex min-h-[188px] flex-col rounded-[1.25rem] bg-white p-5 ring-1 ring-charcoal/[0.06] shadow-card transition hover:shadow-soft',
        plugin.loaded && !plugin.enabled && 'bg-white/60',
      )
    "
  >
    <div class="flex items-start justify-between gap-3">
      <span
        :class="['grid size-12 place-items-center rounded-xl transition', iconTone, plugin.loaded && !plugin.enabled && 'opacity-40 grayscale']"
        aria-hidden="true"
      >
        <component :is="icon" class="size-6" />
      </span>
      <div class="flex items-center gap-1">
        <span :title="plugin.loaded ? undefined : 'It couldn’t be loaded, so it can’t be turned on'">
          <FSwitch v-model="on" :label="`${plugin.name}: ${on ? 'on' : 'off'}`" hide-label :disabled="!plugin.loaded" />
        </span>
        <FMenu :items="menu" :label="`More for ${plugin.name}`" />
      </div>
    </div>

    <h3 class="mt-3 font-display text-lg font-semibold tracking-[-0.02em]">
      {{ plugin.name }}
      <span v-if="plugin.version" class="ml-1 align-middle text-xs font-normal text-subtle">v{{ plugin.version }}</span>
    </h3>
    <p v-if="plugin.loaded" class="mt-1 line-clamp-2 text-sm text-muted">{{ plugin.description || 'No description.' }}</p>
    <p v-else class="mt-1 line-clamp-3 text-sm text-danger" :title="plugin.error ?? undefined">
      Couldn’t load: {{ plugin.error }}
    </p>

    <ul v-if="plugin.events.length || plugin.blocks.length" class="mt-3 flex flex-wrap gap-1.5" aria-label="What it adds">
      <li
        v-for="b in plugin.blocks"
        :key="`block:${b.id}`"
        class="inline-flex items-center gap-1 rounded-full bg-accent-50 px-2 py-0.5 text-[0.6875rem] font-medium text-accent-700 ring-1 ring-accent-200"
        :title="b.description ?? undefined"
      >
        <LayoutGrid class="size-3" aria-hidden="true" /> {{ b.label }} block
      </li>
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
        v-if="plugin.loaded && plugin.enabled"
        class="inline-flex items-center gap-1 rounded-full bg-positive/10 px-2.5 py-1 text-xs font-medium text-positive"
      >
        <Check class="size-3.5" aria-hidden="true" /> Active
      </span>
      <span
        v-else-if="plugin.loaded"
        class="inline-flex items-center gap-1 rounded-full bg-charcoal/[0.06] px-2.5 py-1 text-xs font-medium text-muted"
      >
        <CirclePause class="size-3.5" aria-hidden="true" /> Turned off
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
