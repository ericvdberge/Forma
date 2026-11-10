<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { LayoutTemplate, MousePointerClick } from 'lucide-vue-next'
import { FButton, cn } from '@forma/design-system'
import { isSectionPayload, useStudio, type DragPayload, type DropTarget } from '@/composables/useStudio'
import { createBlock } from '@/studio/blocks'
import { frameWidth } from '@/studio/responsive'
import CanvasSection from './CanvasSection.vue'

const studio = useStudio()
const { doc, device, preview, drag, dropTarget, zoom } = studio
const editing = computed(() => !preview.value)

/* Fit the device frame into the available width. */
const viewport = ref<HTMLElement | null>(null)
const available = ref(0)
let resizer: ResizeObserver | undefined
onMounted(() => {
  resizer = new ResizeObserver(([entry]) => (available.value = entry?.contentRect.width ?? 0))
  if (viewport.value) resizer.observe(viewport.value)
})
onBeforeUnmount(() => resizer?.disconnect())

const width = computed(() => frameWidth[device.value])
watch(
  [available, width],
  ([a, w]) => {
    if (a) zoom.value = Math.min(1, Math.max(0.2, (a - 32) / w))
  },
  { immediate: true },
)

/* ---------- Drag and drop ---------- */
const frame = ref<HTMLElement | null>(null)

/** Dragging image files in from the desktop counts as dragging an Image block. */
const isFileDrag = (e: DragEvent) => !drag.value && !!e.dataTransfer?.types.includes('Files')
const payloadOf = (e: DragEvent): DragPayload | null => drag.value ?? (isFileDrag(e) ? { kind: 'new-block', type: 'image' } : null)

function sectionIndexAt(y: number) {
  const els = [...(frame.value?.querySelectorAll<HTMLElement>(':scope > [data-section-id]') ?? [])]
  const i = els.findIndex((el) => {
    const r = el.getBoundingClientRect()
    return y < r.top + r.height / 2
  })
  return i === -1 ? els.length : i
}

function targetAt(e: DragEvent, payload: DragPayload): DropTarget | null {
  const d = doc.value
  if (!d) return null
  const el = e.target as HTMLElement
  const sectionEl = el.closest<HTMLElement>('[data-section-id]')
  const section = sectionEl && d.sections.find((s) => s.id === sectionEl.dataset.sectionId)

  if (!section || !sectionEl || isSectionPayload(payload, studio.saved.value)) {
    if (!sectionEl) return { kind: 'section', index: sectionIndexAt(e.clientY) }
    const i = d.sections.findIndex((s) => s.id === sectionEl.dataset.sectionId)
    const r = sectionEl.getBoundingClientRect()
    return { kind: 'section', index: e.clientY < r.top + r.height / 2 ? i : i + 1 }
  }

  const split = section.settings.layout === 'split'
  const blockEl = el.closest<HTMLElement>('[data-block-id]')
  if (blockEl && sectionEl.contains(blockEl)) {
    const bi = section.blocks.findIndex((b) => b.id === blockEl.dataset.blockId)
    if (bi !== -1) {
      const r = blockEl.getBoundingClientRect()
      const after = blockEl.hasAttribute('data-inline') ? e.clientX > r.left + r.width / 2 : e.clientY > r.top + r.height / 2
      const column = split ? (bi >= section.settings.splitAt ? 1 : 0) : undefined
      return { kind: 'block', sectionId: section.id, index: bi + (after ? 1 : 0), column }
    }
  }

  const columnEl = el.closest<HTMLElement>('[data-column]')
  if (split && columnEl && sectionEl.contains(columnEl)) {
    const column = Number(columnEl.dataset.column) as 0 | 1
    const first = columnEl.querySelector('[data-block-id]')?.getBoundingClientRect()
    const start = column === 0 ? 0 : section.settings.splitAt
    const end = column === 0 ? section.settings.splitAt : section.blocks.length
    return { kind: 'block', sectionId: section.id, index: first && e.clientY < first.top ? start : end, column }
  }

  // Section padding: above the first block inserts at the start, anywhere else appends.
  const first = sectionEl.querySelector('[data-block-id]')?.getBoundingClientRect()
  return {
    kind: 'block',
    sectionId: section.id,
    index: first && e.clientY < first.top ? 0 : section.blocks.length,
    column: split ? (first && e.clientY < first.top ? 0 : 1) : undefined,
  }
}

function onDragOver(e: DragEvent) {
  if (!editing.value) return
  const payload = payloadOf(e)
  if (!payload) return
  e.preventDefault()
  if (e.dataTransfer) e.dataTransfer.dropEffect = drag.value?.kind.startsWith('move') ? 'move' : 'copy'
  const t = targetAt(e, payload)
  if (JSON.stringify(t) !== JSON.stringify(dropTarget.value)) dropTarget.value = t
}

function onDragLeave(e: DragEvent) {
  if (!frame.value?.contains(e.relatedTarget as Node)) dropTarget.value = null
}

function onDrop(e: DragEvent) {
  const payload = payloadOf(e)
  const target = dropTarget.value
  e.preventDefault()
  dropTarget.value = null
  drag.value = null
  if (!payload || !target) return
  if (isFileDrag(e) && e.dataTransfer) {
    // Each dropped image becomes an Image block, in order.
    const assets = studio.uploadMedia(e.dataTransfer.files)
    assets.reverse().forEach((a) => {
      const block = createBlock('image', { src: a.src, alt: a.name.replace(/\.[^.]+$/, '') })
      if (target.kind === 'block') studio.addBlock(block, target.sectionId, target.index, target.column)
      else studio.addBlockAsSection(block, target.index)
    })
    return
  }
  studio.drop(payload, target)
}

/* Bring the selection into view, e.g. when picked from the Layers list or added from the library. */
watch(
  () => studio.selection.value,
  async (sel) => {
    if (!sel) return
    await nextTick()
    const selector = sel.kind === 'block' ? `[data-block-id="${sel.blockId}"]` : `[data-section-id="${sel.sectionId}"]`
    const el = frame.value?.querySelector<HTMLElement>(selector)
    const box = viewport.value?.getBoundingClientRect()
    const r = el?.getBoundingClientRect()
    if (!el || !box || !r) return
    if (r.bottom < box.top + 40 || r.top > box.bottom - 40) el.scrollIntoView({ block: 'center', behavior: 'smooth' })
  },
)

const sectionIndicator = (i: number) => dropTarget.value?.kind === 'section' && dropTarget.value.index === i

function addHero() {
  studio.insert({ kind: 'template', id: 'hero' })
}
</script>

<template>
  <div ref="viewport" class="studio-canvas min-h-0 flex-1 overflow-auto bg-[#ecebe8] p-4" @click="editing && studio.select(null)">
    <div
      class="mx-auto origin-top transition-[width] duration-300"
      :style="{ width: `${width}px`, zoom }"
    >
      <div
        ref="frame"
        :class="
          cn(
            'relative flex min-h-[640px] flex-col gap-3 bg-[#f7f7f5] p-3 font-sans text-charcoal',
            device !== 'desktop' && 'rounded-[28px] shadow-card ring-1 ring-charcoal/[0.06]',
            device === 'desktop' && 'rounded-2xl shadow-soft',
          )
        "
        :data-studio-editing="editing ? '' : undefined"
        @dragover="onDragOver"
        @dragleave="onDragLeave"
        @drop="onDrop"
      >
        <template v-if="doc?.sections.length">
          <template v-for="(section, i) in doc.sections" :key="section.id">
            <div v-if="sectionIndicator(i)" class="-my-[7.5px] h-[3px] shrink-0 rounded-full bg-accent" aria-hidden="true" />
            <CanvasSection :section="section" :bp="device" :editing="editing" />
          </template>
          <div v-if="sectionIndicator(doc.sections.length)" class="-my-[7.5px] h-[3px] shrink-0 rounded-full bg-accent" aria-hidden="true" />
        </template>

        <div
          v-else
          :class="
            cn(
              'flex flex-1 flex-col items-center justify-center gap-4 rounded-2xl border-2 border-dashed p-10 text-center',
              dropTarget ? 'border-accent bg-accent-50' : 'border-charcoal/10',
            )
          "
        >
          <span class="grid size-14 place-items-center rounded-2xl bg-white shadow-soft">
            <MousePointerClick class="size-6" :stroke-width="1.6" aria-hidden="true" />
          </span>
          <div>
            <p class="text-xl font-bold">This page is empty</p>
            <p class="mt-1 text-muted">Drag a block or template from the library, or start with a hero.</p>
          </div>
          <FButton v-if="editing" variant="secondary" @click.stop="addHero"><LayoutTemplate class="size-4" aria-hidden="true" /> Add a hero section</FButton>
        </div>
      </div>
    </div>
  </div>
</template>
