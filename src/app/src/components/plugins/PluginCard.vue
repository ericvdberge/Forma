<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight } from 'lucide-vue-next'
import { FMenu, FSwitch, cn, type FMenuItem } from '@forma/design-system'
import type { PluginDto } from '@/api/generated'
import PluginFeatures from './PluginFeatures.vue'
import PluginStatusBadge from './PluginStatusBadge.vue'
import { pluginIcon, pluginIconTone, pluginStatus } from './pluginLook'

/** One installed plugin as a card: what it does, whether it's on, and what it adds. */
const props = defineProps<{ plugin: PluginDto; menu: FMenuItem[] }>()
const emit = defineEmits<{ open: []; toggle: [enabled: boolean] }>()

const off = computed(() => props.plugin.loaded && !props.plugin.enabled)
const on = computed({
  get: () => props.plugin.loaded && props.plugin.enabled,
  set: (v: boolean) => emit('toggle', v),
})
</script>

<template>
  <article
    :class="
      cn(
        'group relative flex h-full min-h-[188px] flex-col rounded-[1.25rem] bg-white p-5 ring-1 ring-charcoal/[0.06] shadow-card transition hover:shadow-soft',
        off && 'bg-white/60',
      )
    "
  >
    <div class="flex items-start justify-between gap-3">
      <span :class="['grid size-12 place-items-center rounded-xl transition', pluginIconTone(plugin), off && 'opacity-40 grayscale']" aria-hidden="true">
        <component :is="pluginIcon(plugin)" class="size-6" />
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
    <p v-else class="mt-1 line-clamp-3 text-sm text-danger" :title="plugin.error ?? undefined">Couldn’t load: {{ plugin.error }}</p>

    <PluginFeatures :plugin="plugin" class="mt-3" />

    <div class="mt-auto flex items-end justify-between gap-3 pt-4">
      <PluginStatusBadge :status="pluginStatus(plugin)" />
      <button
        type="button"
        class="inline-flex h-9 items-center gap-1.5 rounded-full px-4 text-sm font-medium ring-1 ring-inset ring-charcoal/15 transition hover:bg-canvas hover:ring-charcoal/40"
        @click="emit('open')"
      >
        Runs <ArrowRight class="size-4" aria-hidden="true" />
      </button>
    </div>
  </article>
</template>
