<script setup lang="ts">
import { computed, inject } from 'vue'
import { Database, EyeOff } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useCanvas } from '@/studio/canvas'
import { blockDefinition } from '@/studio/blocks'
import { boundFieldLabels, resolveBindings } from '@/studio/bindings'
import { collectionDataKey } from '@/studio/inject'
import type { RenderContext } from '@/studio/render'
import type { Block, Section } from '@/studio/types'
import { blockComponents } from './blocks'
import SelectionToolbar from './SelectionToolbar.vue'

const props = defineProps<{
  block: Block
  section: Section
  ctx: Omit<RenderContext, 'selected'>
  /** Drop indicator drawn on this block's edge while dragging. */
  indicator: 'before' | 'after' | null
}>()

const studio = useCanvas()
const def = computed(() => blockDefinition(props.block.type))
const selected = computed(() => studio.selection.value?.kind === 'block' && studio.selection.value.blockId === props.block.id)
const hidden = computed(() => !props.block.style.visibility[props.ctx.bp])
/** Bound rich text shows the item's value, so it isn't typed into on the canvas. */
const boundText = computed(() => !!props.block.bindings?.html)
const ctx = computed<RenderContext>(() => ({ ...props.ctx, selected: selected.value && !boundText.value }))

/* On a collection's detail page, bound props show the preview item's values. Only the studio provides collection
   data; elsewhere (the website preview) blocks show their own values. */
const collectionData = inject(collectionDataKey, null)
const shown = computed(() => resolveBindings(props.block, collectionData?.pageCollection.value, collectionData?.previewItem.value))
const fieldLabels = computed(() => boundFieldLabels(props.block, collectionData?.pageCollection.value))

const maxWidths = { none: undefined, sm: '520px', md: '760px', lg: '960px' }
/** The wrapper always takes a full row (except inline blocks); the content inside can be narrower. */
const style = computed(() => {
  const s = props.block.style
  return {
    marginTop: s.marginTop ? `${s.marginTop}px` : undefined,
    marginBottom: s.marginBottom ? `${s.marginBottom}px` : undefined,
    textAlign: s.align === 'inherit' ? undefined : s.align,
  }
})
const innerStyle = computed(() => {
  const s = props.block.style
  if (def.value.inline || s.maxWidth === 'none') return undefined
  // A narrower block follows the alignment: centred blocks sit in the middle, right-aligned ones on the right.
  const align = s.align === 'inherit' ? props.section.settings.align : s.align
  return { maxWidth: maxWidths[s.maxWidth], marginLeft: align === 'left' ? undefined : 'auto', marginRight: align === 'center' ? 'auto' : undefined }
})

function onClick(e: MouseEvent) {
  if (!props.ctx.editing) return
  e.stopPropagation()
  if (selected.value) return
  if (def.value.richText && !boundText.value) studio.textPointer.value = { x: e.clientX, y: e.clientY }
  studio.select({ kind: 'block', sectionId: props.section.id, blockId: props.block.id })
}
</script>

<template>
  <div
    v-if="ctx.editing || !hidden"
    :id="block.style.anchor || undefined"
    :data-block-id="block.id"
    :data-inline="def.inline ? '' : undefined"
    :class="
      cn(
        'studio-block relative min-w-0',
        def.inline ? 'inline-flex' : 'w-full',
        block.style.className,
        ctx.editing && 'studio-block--editing',
        selected && 'studio-block--selected',
        ctx.editing && fieldLabels.length > 0 && 'studio-block--bound',
        ctx.editing && hidden && 'opacity-40',
      )
    "
    :style="style"
    @click="onClick"
  >
    <div :class="def.inline ? 'contents' : 'w-full'" :style="innerStyle">
      <component
        :is="blockComponents[block.type]"
        :block="shown"
        :ctx="ctx"
        @update="(path: string, value: unknown) => studio.setBlockValue(block.id, path, value)"
      />
    </div>

    <span
      v-if="ctx.editing && hidden"
      class="pointer-events-none absolute right-1 top-1 inline-flex items-center gap-1 rounded-full bg-charcoal px-2 py-0.5 text-[11px] font-medium text-warm-white"
      :style="{ zoom: 1 / studio.zoom.value }"
    >
      <EyeOff class="size-3" aria-hidden="true" /> Hidden on {{ ctx.bp }}
    </span>

    <span
      v-if="ctx.editing && fieldLabels.length"
      class="studio-block__fields pointer-events-none absolute -top-3 left-1 z-10 inline-flex max-w-[90%] items-center gap-1 truncate whitespace-nowrap rounded-full bg-accent px-2 py-0.5 text-[11px] font-medium text-white shadow-soft"
      :style="{ zoom: 1 / studio.zoom.value }"
      :title="`Shows the item’s ${fieldLabels.join(', ')}`"
    >
      <Database class="size-3 shrink-0" aria-hidden="true" /> {{ fieldLabels.join(' · ') }}
    </span>

    <SelectionToolbar v-if="selected && ctx.editing" :label="def.label" :rich-text="def.richText && !boundText" :variant="block.type === 'heading' ? 'heading' : 'text'" />

    <span
      v-if="indicator"
      aria-hidden="true"
      :class="
        cn(
          'pointer-events-none absolute z-20 rounded-full bg-accent',
          def.inline ? 'inset-y-0 w-[3px]' : 'inset-x-0 h-[3px]',
          def.inline ? (indicator === 'before' ? '-left-2' : '-right-2') : indicator === 'before' ? '-top-2' : '-bottom-2',
        )
      "
    />
  </div>
</template>
