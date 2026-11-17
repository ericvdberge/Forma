<script setup lang="ts">
import { computed, ref, type Component } from 'vue'
import { Bookmark, PanelTop, Search, Trash2 } from 'lucide-vue-next'
import { FStatus, cn } from '@forma/design-system'
import { useStudio, type DragPayload } from '@/composables/useStudio'
import { blockDefinition, libraryBlocks } from '@/studio/blocks'
import { globalElements, sectionTemplates, sectionTypeLabels } from '@/studio/sections'
import { timeAgo } from '@/utils/time'

defineProps<{ pageTitle: string }>()
const studio = useStudio()

type Tab = 'blocks' | 'templates' | 'saved' | 'layers'
const tab = ref<Tab>('blocks')
const tabs: Array<{ value: Tab; label: string }> = [
  { value: 'blocks', label: 'Blocks' },
  { value: 'templates', label: 'Templates' },
  { value: 'saved', label: 'Saved' },
  { value: 'layers', label: 'Layers' },
]
const query = ref('')
const matches = (text: string) => text.toLowerCase().includes(query.value.trim().toLowerCase())

interface Tile {
  key: string
  label: string
  icon: Component
  payload: DragPayload
}
const blank = sectionTemplates.find((t) => t.id === 'content')!
const blockTiles = computed<Tile[]>(() =>
  [
    { key: 'section', label: 'Section', icon: PanelTop, payload: { kind: 'template', id: blank.id } } as Tile,
    ...libraryBlocks.map((type) => {
      const def = blockDefinition(type)
      return { key: type, label: def.label, icon: def.icon, payload: { kind: 'new-block', type } } as Tile
    }),
  ].filter((t) => matches(t.label)),
)
const globalTiles = computed<Tile[]>(() =>
  globalElements.map((g) => ({ key: g.id, label: g.label, icon: g.icon, payload: { kind: 'template', id: g.id } }) as Tile).filter((t) => matches(t.label)),
)
const templates = computed(() => sectionTemplates.filter((t) => t.id !== 'content' && (matches(t.label) || matches(t.description))))
const saved = computed(() => studio.saved.value.filter((s) => matches(s.name)))

function onDragStart(e: DragEvent, payload: DragPayload, label: string) {
  studio.drag.value = payload
  e.dataTransfer?.setData('text/plain', label)
  if (e.dataTransfer) e.dataTransfer.effectAllowed = 'copy'
}
function onDragEnd() {
  studio.drag.value = null
  studio.dropTarget.value = null
}

const publishState = computed(() => {
  if (!studio.isPublished.value) return { label: 'Not published yet', dot: 'bg-accent' }
  return studio.hasChanges.value ? { label: 'Unpublished changes', dot: 'bg-accent' } : { label: 'All changes published', dot: 'bg-positive' }
})

/* Layers */
const sel = computed(() => studio.selection.value)
</script>

<template>
  <aside class="flex h-full min-h-0 flex-col bg-white" aria-label="Library">
    <div class="px-5 pt-5">
      <div class="flex items-center gap-2.5">
        <h1 class="text-xl font-bold tracking-[-0.02em]">Design Studio</h1>
        <span class="rounded-full bg-canvas px-2 py-0.5">
          <FStatus :status="studio.isPublished.value ? 'live' : 'draft'" />
        </span>
      </div>
      <p class="mt-2 inline-flex items-center gap-1.5 text-xs text-muted" role="status">
        <span :class="cn('size-1.5 rounded-full', publishState.dot)" aria-hidden="true" />
        {{ publishState.label }}
      </p>
      <p class="mt-1.5 text-[0.8125rem] leading-relaxed text-muted">
        Editing <span class="font-medium text-charcoal">{{ pageTitle }}</span>. Drag blocks onto the page, or click one to add it after the selection.
      </p>
    </div>

    <div role="tablist" aria-label="Library" class="mt-4 flex border-b border-charcoal/[0.06] px-3">
      <button
        v-for="t in tabs"
        :key="t.value"
        type="button"
        role="tab"
        :aria-selected="tab === t.value"
        :class="
          cn(
            '-mb-px flex-1 border-b-2 px-1 pb-2.5 pt-1 text-[0.8125rem] transition-colors',
            tab === t.value ? 'border-charcoal font-medium text-charcoal' : 'border-transparent text-muted hover:text-charcoal',
          )
        "
        @click="tab = t.value"
      >
        {{ t.label }}
      </button>
    </div>

    <div class="min-h-0 flex-1 overflow-y-auto px-4 pb-6 pt-4">
      <div v-if="tab !== 'layers'" class="relative mb-4">
        <Search class="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-subtle" aria-hidden="true" />
        <label for="studio-library-search" class="sr-only">Search the library</label>
        <input
          id="studio-library-search"
          v-model="query"
          type="search"
          :placeholder="`Search ${tab}…`"
          class="h-10 w-full rounded-xl bg-canvas/60 pl-10 pr-3 text-sm outline-none ring-1 ring-charcoal/[0.06] placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
        />
      </div>

      <!-- Blocks -->
      <template v-if="tab === 'blocks'">
        <ul class="grid grid-cols-3 gap-2">
          <li v-for="t in blockTiles" :key="t.key">
            <button
              type="button"
              draggable="true"
              class="flex aspect-square w-full cursor-grab flex-col items-center justify-center gap-2 rounded-xl bg-white text-[0.75rem] text-charcoal/80 ring-1 ring-charcoal/[0.08] transition hover:text-charcoal hover:shadow-soft hover:ring-charcoal/20 active:cursor-grabbing"
              :title="`Add ${t.label}`"
              @dragstart="onDragStart($event, t.payload, t.label)"
              @dragend="onDragEnd"
              @click="studio.insert(t.payload)"
            >
              <component :is="t.icon" class="size-5" :stroke-width="1.5" aria-hidden="true" />
              {{ t.label }}
            </button>
          </li>
        </ul>
        <template v-if="globalTiles.length">
          <h2 class="mb-3 mt-6 text-sm font-medium">Global elements</h2>
          <ul class="grid grid-cols-3 gap-2">
            <li v-for="t in globalTiles" :key="t.key">
              <button
                type="button"
                draggable="true"
                class="flex aspect-square w-full cursor-grab flex-col items-center justify-center gap-2 rounded-xl bg-white text-[0.75rem] text-charcoal/80 ring-1 ring-charcoal/[0.08] transition hover:text-charcoal hover:shadow-soft hover:ring-charcoal/20"
                :title="`Add ${t.label}`"
                @dragstart="onDragStart($event, t.payload, t.label)"
                @dragend="onDragEnd"
                @click="studio.insert(t.payload)"
              >
                <component :is="t.icon" class="size-5" :stroke-width="1.5" aria-hidden="true" />
                {{ t.label }}
              </button>
            </li>
          </ul>
        </template>
        <p v-if="!blockTiles.length && !globalTiles.length" class="py-8 text-center text-sm text-muted">No blocks match “{{ query.trim() }}”.</p>
      </template>

      <!-- Templates -->
      <ul v-else-if="tab === 'templates'" class="flex flex-col gap-2">
        <li v-for="t in templates" :key="t.id">
          <button
            type="button"
            draggable="true"
            class="flex w-full cursor-grab items-start gap-3 rounded-xl bg-white p-3 text-left ring-1 ring-charcoal/[0.08] transition hover:shadow-soft hover:ring-charcoal/20"
            @dragstart="onDragStart($event, { kind: 'template', id: t.id }, t.label)"
            @dragend="onDragEnd"
            @click="studio.insert({ kind: 'template', id: t.id })"
          >
            <span class="grid size-10 shrink-0 place-items-center rounded-lg bg-canvas">
              <component :is="t.icon" class="size-5" :stroke-width="1.5" aria-hidden="true" />
            </span>
            <span>
              <span class="block text-sm font-medium">{{ t.label }}</span>
              <span class="mt-0.5 block text-xs leading-relaxed text-muted">{{ t.description }}</span>
            </span>
          </button>
        </li>
        <li v-if="!templates.length" class="py-8 text-center text-sm text-muted">No templates match “{{ query.trim() }}”.</li>
      </ul>

      <!-- Saved -->
      <template v-else-if="tab === 'saved'">
        <ul v-if="saved.length" class="flex flex-col gap-2">
          <li v-for="item in saved" :key="item.id" class="group relative">
            <button
              type="button"
              draggable="true"
              class="flex w-full cursor-grab items-center gap-3 rounded-xl bg-white p-3 pr-12 text-left ring-1 ring-charcoal/[0.08] transition hover:shadow-soft hover:ring-charcoal/20"
              @dragstart="onDragStart($event, { kind: 'saved', id: item.id }, item.name)"
              @dragend="onDragEnd"
              @click="studio.insert({ kind: 'saved', id: item.id })"
            >
              <span class="grid size-10 shrink-0 place-items-center rounded-lg bg-canvas">
                <component
                  :is="item.kind === 'section' ? PanelTop : blockDefinition(item.block.type).icon"
                  class="size-5"
                  :stroke-width="1.5"
                  aria-hidden="true"
                />
              </span>
              <span class="min-w-0">
                <span class="block truncate text-sm font-medium">{{ item.name }}</span>
                <span class="mt-0.5 block text-xs text-muted">
                  {{ item.kind === 'section' ? 'Section' : blockDefinition(item.block.type).label }} · {{ timeAgo(item.savedAt) }}
                </span>
              </span>
            </button>
            <button
              type="button"
              class="absolute right-2 top-1/2 grid size-8 -translate-y-1/2 place-items-center rounded-lg text-muted opacity-0 transition hover:bg-canvas hover:text-danger focus-visible:opacity-100 group-hover:opacity-100"
              :aria-label="`Remove ${item.name} from your library`"
              @click="studio.removeSaved(item.id)"
            >
              <Trash2 class="size-4" aria-hidden="true" />
            </button>
          </li>
        </ul>
        <div v-else class="flex flex-col items-center gap-2 py-10 text-center">
          <Bookmark class="size-5 text-subtle" aria-hidden="true" />
          <p class="text-sm font-medium">{{ query ? 'Nothing matches' : 'Nothing saved yet' }}</p>
          <p class="max-w-52 text-xs text-muted">Select a section or block and choose “Save to library” from its ••• menu.</p>
        </div>
      </template>

      <!-- Layers -->
      <ol v-else class="flex flex-col gap-1 text-sm" aria-label="Page structure">
        <li v-for="section in studio.doc.value?.sections ?? []" :key="section.id">
          <button
            type="button"
            :class="
              cn(
                'flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-left font-medium',
                sel?.kind === 'section' && sel.sectionId === section.id ? 'bg-accent-50 text-accent-700' : 'hover:bg-canvas',
              )
            "
            @click="studio.select({ kind: 'section', sectionId: section.id })"
          >
            <PanelTop class="size-4 shrink-0" :stroke-width="1.6" aria-hidden="true" />
            <span class="truncate">{{ section.settings.name || sectionTypeLabels[section.settings.type] }}</span>
          </button>
          <ol class="ml-4 border-l border-charcoal/[0.06] pl-2">
            <li v-for="block in section.blocks" :key="block.id">
              <button
                type="button"
                :class="
                  cn(
                    'flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-[0.8125rem]',
                    sel?.kind === 'block' && sel.blockId === block.id ? 'bg-accent-50 text-accent-700' : 'text-charcoal/75 hover:bg-canvas',
                  )
                "
                @click="studio.select({ kind: 'block', sectionId: section.id, blockId: block.id })"
              >
                <component :is="blockDefinition(block.type).icon" class="size-3.5 shrink-0" :stroke-width="1.6" aria-hidden="true" />
                {{ blockDefinition(block.type).label }}
              </button>
            </li>
          </ol>
        </li>
      </ol>
    </div>
  </aside>
</template>
