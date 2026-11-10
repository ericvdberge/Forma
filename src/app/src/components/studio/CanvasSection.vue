<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { EyeOff, Plus } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useStudio, type DropTarget } from '@/composables/useStudio'
import { resolve } from '@/studio/responsive'
import { tone, type RenderContext } from '@/studio/render'
import { sectionTypeLabels } from '@/studio/sections'
import type { Block, Breakpoint, Section } from '@/studio/types'
import CanvasBlock from './CanvasBlock.vue'
import SelectionToolbar from './SelectionToolbar.vue'

const props = defineProps<{ section: Section; bp: Breakpoint; editing: boolean }>()
const studio = useStudio()

const s = computed(() => props.section.settings)
const selected = computed(() => studio.selection.value?.kind === 'section' && studio.selection.value.sectionId === props.section.id)
const hidden = computed(() => !s.value.visibility[props.bp])
const label = computed(() => s.value.name || sectionTypeLabels[s.value.type])
const ctx = computed<Omit<RenderContext, 'selected'>>(() => ({ bp: props.bp, theme: s.value.theme, editing: props.editing }))

const widths = { narrow: '720px', normal: '960px', wide: '1120px', full: 'none' }

const sectionStyle = computed(() => {
  const set = s.value
  const pad = resolve(set.padding, props.bp)
  const bg = set.background
  const t = tone(set.theme)
  const background =
    bg.kind === 'color'
      ? bg.color
      : bg.kind === 'gradient'
        ? `linear-gradient(${bg.gradientAngle}deg, ${bg.gradientFrom}, ${bg.gradientTo})`
        : bg.kind === 'image' && bg.image
          ? `url("${bg.image}") ${bg.position} / cover no-repeat`
          : undefined
  return {
    padding: `${pad.top}px ${pad.right}px ${pad.bottom}px ${pad.left}px`,
    background,
    color: t.text,
    '--studio-muted': t.muted,
    borderRadius: set.radius ? `${set.radius}px` : undefined,
    minHeight: set.minHeight ? `${set.minHeight * (props.bp === 'mobile' ? 0.8 : 1)}px` : undefined,
    justifyContent: { start: 'flex-start', center: 'center', end: 'flex-end' }[set.verticalAlign],
  }
})

const overlay = computed(() => {
  const bg = s.value.background
  return bg.kind === 'image' && bg.overlayOpacity > 0 ? { background: bg.overlayColor, opacity: bg.overlayOpacity / 100 } : null
})

const split = computed(() => s.value.layout === 'split' && props.bp !== 'mobile')
const columns = computed<Block[][]>(() =>
  s.value.layout === 'split'
    ? [props.section.blocks.slice(0, s.value.splitAt), props.section.blocks.slice(s.value.splitAt)]
    : [props.section.blocks],
)

/** Blocks flow in rows: full-width blocks take a row each, inline ones (buttons, badges) share one. */
const flowStyle = computed(() => ({
  rowGap: `${s.value.gap}px`,
  columnGap: '12px',
  justifyContent: s.value.align === 'center' ? 'center' : 'flex-start',
  textAlign: s.value.align,
}))

/* Drop indicator for this section's blocks. `abs` is the block's index in the section. */
function indicatorFor(abs: number, column: number | null, last: boolean): 'before' | 'after' | null {
  const t = studio.dropTarget.value
  if (t?.kind !== 'block' || t.sectionId !== props.section.id) return null
  // Side by side, an index on the column boundary belongs to the column the pointer was over.
  if (column !== null) {
    const targetColumn = t.column ?? (t.index >= s.value.splitAt ? 1 : 0)
    if (targetColumn !== column) return null
  }
  if (t.index === abs) return 'before'
  if (t.index === abs + 1 && last) return 'after'
  return null
}
const emptyColumnTarget = (column: number) => {
  const t = studio.dropTarget.value as DropTarget | null
  return t?.kind === 'block' && t.sectionId === props.section.id && !columns.value[column]!.length && (t.column ?? 0) === column
}

/* Entrance animation: plays on load and whenever its settings change; in preview, when scrolled into view. */
const root = ref<HTMLElement | null>(null)
const revealed = ref(false)
const playKey = ref(0)
let observer: IntersectionObserver | undefined
onMounted(() => {
  observer = new IntersectionObserver(
    ([entry]) => {
      if (entry?.isIntersecting) {
        revealed.value = true
        observer?.disconnect()
      }
    },
    { threshold: 0.15 },
  )
  if (root.value) observer.observe(root.value)
})
onBeforeUnmount(() => observer?.disconnect())
watch(
  () => [s.value.animation.entrance, s.value.animation.delay],
  () => {
    revealed.value = true
    playKey.value++
  },
)
const animationStyle = computed(() => {
  const a = s.value.animation
  if (a.entrance === 'none') return undefined
  if (!revealed.value) return { opacity: 0 }
  return { animation: `studio-${a.entrance} 0.8s cubic-bezier(0.16, 1, 0.3, 1) ${a.delay}s both` }
})

function onClick(e: MouseEvent) {
  if (!props.editing) return
  e.stopPropagation()
  studio.select({ kind: 'section', sectionId: props.section.id })
}
</script>

<template>
  <section
    v-if="editing || !hidden"
    ref="root"
    :id="s.anchor || undefined"
    :data-section-id="section.id"
    :class="
      cn(
        'studio-section relative flex flex-col',
        s.className,
        editing && 'studio-section--editing',
        selected && 'studio-section--selected',
        editing && hidden && 'opacity-40',
      )
    "
    :style="sectionStyle"
    @click="onClick"
  >
    <div v-if="overlay" class="pointer-events-none absolute inset-0" :style="{ ...overlay, borderRadius: 'inherit' }" aria-hidden="true" />

    <div :key="playKey" class="relative w-full" :style="{ maxWidth: widths[s.contentWidth], marginInline: 'auto', ...animationStyle }">
      <div
        v-if="split"
        class="grid gap-12"
        :style="{
          gridTemplateColumns: `${s.splitRatio}fr ${100 - s.splitRatio}fr`,
          alignItems: { start: 'start', center: 'center', end: 'end' }[s.verticalAlign],
          columnGap: bp === 'tablet' ? '32px' : '56px',
        }"
      >
        <div v-for="(col, c) in columns" :key="c" :data-column="c" class="flex min-w-0 flex-wrap content-start" :style="flowStyle">
          <CanvasBlock
            v-for="(block, i) in col"
            :key="block.id"
            :block="block"
            :section="section"
            :ctx="ctx"
            :indicator="indicatorFor((c === 1 ? s.splitAt : 0) + i, c, i === col.length - 1)"
          />
          <div
            v-if="editing && !col.length"
            :class="
              cn(
                'grid min-h-32 w-full place-items-center rounded-xl border border-dashed text-sm opacity-70',
                emptyColumnTarget(c) ? 'border-accent bg-accent-50 text-accent' : 'border-current/20',
              )
            "
          >
            Drop blocks here
          </div>
        </div>
      </div>

      <div v-else class="flex flex-wrap" :style="flowStyle">
        <CanvasBlock
          v-for="(block, i) in section.blocks"
          :key="block.id"
          :block="block"
          :section="section"
          :ctx="ctx"
          :indicator="indicatorFor(i, null, i === section.blocks.length - 1)"
        />
        <div
          v-if="editing && !section.blocks.length"
          :class="
            cn(
              'flex min-h-32 w-full flex-col items-center justify-center gap-2 rounded-xl border border-dashed text-sm',
              emptyColumnTarget(0) ? 'border-accent bg-accent-50 text-accent' : 'border-charcoal/15 text-muted',
            )
          "
        >
          <Plus class="size-5" aria-hidden="true" />
          Drag blocks here, or select this section and click a block in the library
        </div>
      </div>
    </div>

    <template v-if="editing">
      <span
        v-if="!selected"
        class="studio-section__label pointer-events-none absolute left-3 top-3 z-20 rounded-md bg-accent px-2 py-0.5 text-[11px] font-medium text-white"
        :style="{ zoom: 1 / studio.zoom.value }"
      >
        {{ label }}
      </span>
      <span
        v-if="hidden"
        class="pointer-events-none absolute right-3 top-3 z-20 inline-flex items-center gap-1 rounded-full bg-charcoal px-2 py-0.5 text-[11px] font-medium text-warm-white"
        :style="{ zoom: 1 / studio.zoom.value }"
      >
        <EyeOff class="size-3" aria-hidden="true" /> Hidden on {{ bp }}
      </span>
      <SelectionToolbar v-if="selected" :label="label" placement="inside" />
    </template>
  </section>
</template>

<style>
.studio-section--editing {
  outline: 1px solid transparent;
  outline-offset: -1px;
  transition: outline-color 0.15s;
}
.studio-section--editing:hover {
  outline-color: rgb(59 130 246 / 0.35);
}
.studio-section--selected,
.studio-section--selected:hover {
  outline: 2px solid #3b82f6;
  outline-offset: -2px;
}
.studio-section__label {
  opacity: 0;
  transition: opacity 0.15s;
}
.studio-section--editing:hover > .studio-section__label {
  opacity: 1;
}
.studio-block--editing {
  outline: 1px dashed transparent;
  outline-offset: 4px;
  border-radius: 4px;
  cursor: default;
}
.studio-block--editing:hover {
  outline-color: rgb(59 130 246 / 0.6);
}
.studio-block--selected,
.studio-block--selected:hover {
  outline: 2px solid #3b82f6;
  outline-offset: 4px;
}

@keyframes studio-fade {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes studio-fade-up {
  from { opacity: 0; transform: translateY(24px); }
  to { opacity: 1; transform: none; }
}
@keyframes studio-zoom {
  from { opacity: 0; transform: scale(0.96); }
  to { opacity: 1; transform: none; }
}
@keyframes studio-slide-left {
  from { opacity: 0; transform: translateX(32px); }
  to { opacity: 1; transform: none; }
}
</style>
