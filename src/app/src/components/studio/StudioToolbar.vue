<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight, Eye, EyeOff, Monitor, PanelLeft, PanelRight, Redo2, Smartphone, Tablet, Undo2 } from 'lucide-vue-next'
import { FButton, cn } from '@forma/design-system'
import { useStudio } from '@/composables/useStudio'
import type { Breakpoint } from '@/studio/types'

defineProps<{ pages: Array<{ label: string; value: string }>; pageId: string }>()
const emit = defineEmits<{ page: [id: string]; library: []; inspector: [] }>()
const studio = useStudio()

const devices: Array<{ value: Breakpoint; label: string; icon: typeof Monitor }> = [
  { value: 'desktop', label: 'Desktop', icon: Monitor },
  { value: 'tablet', label: 'Tablet', icon: Tablet },
  { value: 'mobile', label: 'Mobile', icon: Smartphone },
]

/** A page that was never published can be published as is. */
const canPublish = computed(() => studio.hasChanges.value || !studio.isPublished.value)

function discard() {
  if (window.confirm('Discard all changes since the last publish? You can still undo this.')) studio.discard()
}

const iconBtn =
  'grid size-10 shrink-0 place-items-center rounded-xl text-charcoal/75 transition-colors hover:bg-charcoal/5 hover:text-charcoal disabled:pointer-events-none disabled:opacity-35'
</script>

<template>
  <div class="flex flex-wrap items-center gap-2 border-b border-charcoal/[0.06] bg-white px-3 py-2.5">
    <button type="button" :class="cn(iconBtn, 'xl:hidden')" aria-label="Show library" title="Library" @click="emit('library')">
      <PanelLeft class="size-5" :stroke-width="1.6" aria-hidden="true" />
    </button>

    <div role="radiogroup" aria-label="Preview device" class="flex gap-1 rounded-xl bg-canvas/70 p-1">
      <button
        v-for="d in devices"
        :key="d.value"
        type="button"
        role="radio"
        :aria-checked="studio.device.value === d.value"
        :title="d.label"
        :class="
          cn(
            'grid size-8 place-items-center rounded-lg transition-colors',
            studio.device.value === d.value ? 'bg-white text-charcoal shadow-soft' : 'text-muted hover:text-charcoal',
          )
        "
        @click="studio.device.value = d.value"
      >
        <component :is="d.icon" class="size-4" :stroke-width="1.6" aria-hidden="true" />
        <span class="sr-only">{{ d.label }}</span>
      </button>
    </div>

    <div class="flex">
      <button type="button" :class="iconBtn" :disabled="!studio.canUndo.value" aria-label="Undo" title="Undo (Ctrl+Z)" @click="studio.undo()">
        <Undo2 class="size-[18px]" :stroke-width="1.6" aria-hidden="true" />
      </button>
      <button type="button" :class="iconBtn" :disabled="!studio.canRedo.value" aria-label="Redo" title="Redo (Ctrl+Shift+Z)" @click="studio.redo()">
        <Redo2 class="size-[18px]" :stroke-width="1.6" aria-hidden="true" />
      </button>
    </div>

    <label class="sr-only" for="studio-page">Page</label>
    <select
      id="studio-page"
      :value="pageId"
      class="h-9 w-32 cursor-pointer sm:w-auto sm:max-w-44 rounded-full bg-white pl-3.5 pr-8 text-sm ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent"
      @change="emit('page', ($event.target as HTMLSelectElement).value)"
    >
      <option v-for="p in pages" :key="p.value" :value="p.value">{{ p.label }}</option>
    </select>

    <div class="ml-auto flex shrink-0 items-center gap-2">
      <button
        type="button"
        :class="cn(iconBtn, studio.preview.value && 'bg-accent-50 text-accent hover:bg-accent-50 hover:text-accent')"
        :aria-pressed="studio.preview.value"
        :title="studio.preview.value ? 'Back to editing (P)' : 'Preview (P)'"
        :aria-label="studio.preview.value ? 'Back to editing' : 'Preview'"
        @click="studio.preview.value = !studio.preview.value"
      >
        <component :is="studio.preview.value ? EyeOff : Eye" class="size-[18px]" :stroke-width="1.6" aria-hidden="true" />
      </button>
      <FButton size="sm" variant="secondary" :disabled="!studio.hasChanges.value" @click="discard">Discard</FButton>
      <FButton size="sm" :disabled="!canPublish" @click="studio.publish()">
        <span class="hidden sm:inline">Publish changes</span><span class="sm:hidden">Publish</span>
        <ArrowRight class="size-4" aria-hidden="true" />
      </FButton>
      <button type="button" :class="cn(iconBtn, 'xl:hidden')" aria-label="Show inspector" title="Inspector" @click="emit('inspector')">
        <PanelRight class="size-5" :stroke-width="1.6" aria-hidden="true" />
      </button>
    </div>
  </div>
</template>
