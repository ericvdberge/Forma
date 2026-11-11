<script setup lang="ts">
import { computed } from 'vue'
import { ArrowLeft, ArrowRight, Eye, EyeOff, Monitor, PanelRight, Plus, Redo2, Smartphone, Tablet, Undo2 } from 'lucide-vue-next'
import { FButton, FLogo, cn } from '@forma/design-system'
import { useStudio, type StudioMode } from '@/composables/useStudio'
import type { Breakpoint } from '@/studio/types'

/** The editor's top bar: back to Pages, the page, view controls and publishing. */
defineProps<{ pageTitle: string; pagePath: string }>()
const emit = defineEmits<{ library: []; inspector: [] }>()
const studio = useStudio()

const devices: Array<{ value: Breakpoint; label: string; icon: typeof Monitor }> = [
  { value: 'desktop', label: 'Computer', icon: Monitor },
  { value: 'tablet', label: 'Tablet', icon: Tablet },
  { value: 'mobile', label: 'Phone', icon: Smartphone },
]
const modes: Array<{ value: StudioMode; label: string }> = [
  { value: 'simple', label: 'Simple' },
  { value: 'advanced', label: 'Advanced' },
]

/** A page that was never published can be published as is. */
const canPublish = computed(() => studio.hasChanges.value || !studio.isPublished.value)
const status = computed(() => {
  if (!studio.isPublished.value) return { label: 'Not published yet', dot: 'bg-accent' }
  return studio.hasChanges.value ? { label: 'Changes not published', dot: 'bg-accent' } : { label: 'Published', dot: 'bg-positive' }
})

function discard() {
  if (window.confirm('Throw away all changes since the last publish? You can still undo this.')) studio.discard()
}

const textBtn =
  'inline-flex h-10 shrink-0 items-center gap-2 rounded-xl px-3 text-sm font-medium text-charcoal/80 transition-colors hover:bg-charcoal/5 hover:text-charcoal disabled:pointer-events-none disabled:opacity-35'
</script>

<template>
  <header class="flex flex-wrap items-center gap-x-3 gap-y-2 border-b border-charcoal/[0.06] bg-white px-3 py-2.5 sm:px-4">
    <RouterLink :to="{ name: 'pages' }" :class="textBtn" title="Back to all pages">
      <ArrowLeft class="size-5" :stroke-width="1.6" aria-hidden="true" />
      <span>Pages</span>
    </RouterLink>
    <FLogo variant="mark" class="hidden h-6 lg:block" />

    <div class="min-w-0 border-l border-charcoal/[0.08] pl-3">
      <p class="truncate text-base font-bold leading-tight">{{ pageTitle }}</p>
      <p class="flex items-center gap-1.5 truncate text-xs text-muted" role="status">
        <span :class="cn('size-1.5 shrink-0 rounded-full', status.dot)" aria-hidden="true" />
        {{ status.label }}<span class="hidden md:inline"> · {{ pagePath }}</span>
      </p>
    </div>

    <div class="flex items-center gap-1 sm:ml-4">
      <div role="radiogroup" aria-label="Show the page on" class="flex gap-1 rounded-xl bg-canvas/80 p-1">
        <button
          v-for="d in devices"
          :key="d.value"
          type="button"
          role="radio"
          :aria-checked="studio.device.value === d.value"
          :title="`Show on a ${d.label.toLowerCase()}`"
          :class="
            cn(
              'grid size-9 place-items-center rounded-lg transition-colors',
              studio.device.value === d.value ? 'bg-white text-charcoal shadow-soft' : 'text-muted hover:text-charcoal',
            )
          "
          @click="studio.device.value = d.value"
        >
          <component :is="d.icon" class="size-[18px]" :stroke-width="1.6" aria-hidden="true" />
          <span class="sr-only">{{ d.label }}</span>
        </button>
      </div>
      <button type="button" :class="textBtn" :disabled="!studio.canUndo.value" title="Undo (Ctrl+Z)" @click="studio.undo()">
        <Undo2 class="size-[18px]" :stroke-width="1.6" aria-hidden="true" /><span class="hidden 2xl:inline">Undo</span>
        <span class="sr-only 2xl:hidden">Undo</span>
      </button>
      <button type="button" :class="textBtn" :disabled="!studio.canRedo.value" title="Redo (Ctrl+Shift+Z)" @click="studio.redo()">
        <Redo2 class="size-[18px]" :stroke-width="1.6" aria-hidden="true" /><span class="hidden 2xl:inline">Redo</span>
        <span class="sr-only 2xl:hidden">Redo</span>
      </button>
    </div>

    <div class="ml-auto flex flex-wrap items-center gap-2">
      <div role="radiogroup" aria-label="Editor mode" class="hidden rounded-xl bg-canvas/80 p-1 md:flex" title="Simple hides the technical settings">
        <button
          v-for="m in modes"
          :key="m.value"
          type="button"
          role="radio"
          :aria-checked="studio.mode.value === m.value"
          :class="
            cn(
              'h-8 rounded-lg px-3 text-sm transition-colors',
              studio.mode.value === m.value ? 'bg-white font-medium text-charcoal shadow-soft' : 'text-muted hover:text-charcoal',
            )
          "
          @click="studio.mode.value = m.value"
        >
          {{ m.label }}
        </button>
      </div>
      <button
        v-if="!studio.preview.value"
        type="button"
        :class="cn(textBtn, studio.mode.value === 'advanced' && 'xl:hidden')"
        title="Add sections and blocks"
        @click="emit('library')"
      >
        <Plus class="size-[18px]" :stroke-width="1.8" aria-hidden="true" /> Add
      </button>
      <button
        type="button"
        :class="cn(textBtn, studio.preview.value && 'bg-accent-50 text-accent hover:bg-accent-50 hover:text-accent')"
        :aria-pressed="studio.preview.value"
        :title="studio.preview.value ? 'Back to editing (P)' : 'See the page as visitors will (P)'"
        @click="studio.preview.value = !studio.preview.value"
      >
        <component :is="studio.preview.value ? EyeOff : Eye" class="size-[18px]" :stroke-width="1.6" aria-hidden="true" />
        <span class="hidden sm:inline">{{ studio.preview.value ? 'Back to editing' : 'Preview' }}</span>
        <span class="sr-only sm:hidden">{{ studio.preview.value ? 'Back to editing' : 'Preview' }}</span>
      </button>
      <FButton size="sm" variant="secondary" :disabled="!studio.hasChanges.value" @click="discard">Discard</FButton>
      <FButton size="sm" :disabled="!canPublish" @click="studio.publish()">
        Publish <ArrowRight class="size-4" aria-hidden="true" />
      </FButton>
      <button type="button" :class="cn(textBtn, 'xl:hidden')" aria-label="Show settings" title="Settings for the selection" @click="emit('inspector')">
        <PanelRight class="size-5" :stroke-width="1.6" aria-hidden="true" />
      </button>
    </div>
  </header>
</template>
