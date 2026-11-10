<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { MousePointerClick, Type } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useStudio } from '@/composables/useStudio'
import { blockDefinition, type Field } from '@/studio/blocks'
import { sectionTypeLabels } from '@/studio/sections'
import { breakpoints } from '@/studio/types'
import FieldControl from './inspector/FieldControl.vue'
import ImageControl from './inspector/ImageControl.vue'
import InspectorGroup from './inspector/InspectorGroup.vue'
import SpacingControl from './inspector/SpacingControl.vue'

const studio = useStudio()
const section = computed(() => studio.selectedSection.value)
const block = computed(() => studio.selectedBlock.value)
const def = computed(() => (block.value ? blockDefinition(block.value.type) : null))

type Tab = 'content' | 'style' | 'advanced'
const tab = ref<Tab>('content')
const tabs = computed<Array<{ value: Tab; label: string }>>(() => [
  { value: 'content', label: block.value ? 'Block' : 'Section' },
  { value: 'style', label: 'Style' },
  { value: 'advanced', label: 'Advanced' },
])
// A newly selected item opens on its main tab.
watch(
  () => (block.value ?? section.value)?.id,
  () => (tab.value = 'content'),
)

/* Reading and writing settings by path */
function get(obj: unknown, path: string): unknown {
  return path.split('.').reduce<unknown>((o, k) => (o as Record<string, unknown> | undefined)?.[k], obj)
}
const sectionValue = (path: string) => get(section.value?.settings, path)
const setSection = (path: string, v: unknown) => section.value && studio.setSectionValue(section.value.id, path, v)
const blockValue = (path: string) => get(block.value, path)
const setBlock = (path: string, v: unknown) => block.value && studio.setBlockValue(block.value.id, path, v)

const opt = <T extends string | number>(pairs: Array<[T, string]>) => pairs.map(([value, label]) => ({ value, label }))

/* Section fields */
const general: Array<[Field, string]> = [
  [{ key: 'type', label: 'Section type', kind: 'select', options: opt(Object.entries(sectionTypeLabels) as Array<[string, string]>) }, 'type'],
  [{ key: 'name', label: 'Name', kind: 'text', placeholder: 'Shown in Layers' }, 'name'],
  [{ key: 'layout', label: 'Layout', kind: 'segmented', options: opt([['stack', 'Stacked'], ['split', 'Two columns']]) }, 'layout'],
]
const splitFields: Array<[Field, string]> = [
  [{ key: 'splitRatio', label: 'First column width', kind: 'slider', min: 20, max: 80, unit: '%' }, 'splitRatio'],
  [{ key: 'verticalAlign', label: 'Vertical alignment', kind: 'segmented', options: opt([['start', 'Top'], ['center', 'Middle'], ['end', 'Bottom']]) }, 'verticalAlign'],
]
const bgKind: Field = { key: 'kind', label: 'Background', kind: 'segmented', options: opt([['none', 'None'], ['color', 'Color'], ['image', 'Image'], ['gradient', 'Gradient']]) }
const bgImage: Array<[Field, string]> = [
  [
    {
      key: 'position',
      label: 'Focus',
      kind: 'select',
      options: opt([['center', 'Center'], ['right center', 'Right'], ['left center', 'Left'], ['center top', 'Top'], ['center bottom', 'Bottom']]),
    },
    'background.position',
  ],
]
const bgGradient: Array<[Field, string]> = [
  [{ key: 'from', label: 'From', kind: 'color' }, 'background.gradientFrom'],
  [{ key: 'to', label: 'To', kind: 'color' }, 'background.gradientTo'],
  [{ key: 'angle', label: 'Angle', kind: 'slider', min: 0, max: 360, step: 5, unit: '°' }, 'background.gradientAngle'],
]
const widthField: Field = { key: 'contentWidth', label: 'Content width', kind: 'select', options: opt([['narrow', 'Narrow'], ['normal', 'Normal'], ['wide', 'Wide'], ['full', 'Full width']]) }
const animation: Array<[Field, string]> = [
  [
    {
      key: 'entrance',
      label: 'Entrance animation',
      kind: 'select',
      options: opt([['none', 'None'], ['fade', 'Fade in'], ['fade-up', 'Fade up'], ['zoom', 'Zoom in'], ['slide-left', 'Slide in']]),
    },
    'animation.entrance',
  ],
  [{ key: 'delay', label: 'Delay', kind: 'slider', min: 0, max: 2, step: 0.1, unit: 's' }, 'animation.delay'],
]
const sectionStyle: Array<[Field, string]> = [
  [{ key: 'theme', label: 'Text colour', kind: 'segmented', options: opt([['light', 'Dark text'], ['dark', 'Light text']]) }, 'theme'],
  [{ key: 'align', label: 'Alignment', kind: 'segmented', options: opt([['left', 'Left'], ['center', 'Center']]) }, 'align'],
  [{ key: 'gap', label: 'Space between blocks', kind: 'slider', min: 0, max: 64, step: 2, unit: 'px' }, 'gap'],
  [{ key: 'minHeight', label: 'Minimum height', kind: 'slider', min: 0, max: 900, step: 10, unit: 'px' }, 'minHeight'],
  [{ key: 'radius', label: 'Corner radius', kind: 'slider', min: 0, max: 40, unit: 'px' }, 'radius'],
]
const advanced: Field[] = [
  { key: 'anchor', label: 'Anchor ID', kind: 'text', placeholder: 'e.g. pricing → /page#pricing' },
  { key: 'className', label: 'CSS classes', kind: 'text', placeholder: 'Space-separated' },
]

/* Block style fields */
const blockStyle: Field[] = [
  { key: 'align', label: 'Alignment', kind: 'segmented', options: opt([['inherit', 'Auto'], ['left', 'Left'], ['center', 'Center'], ['right', 'Right']]) },
  { key: 'maxWidth', label: 'Maximum width', kind: 'select', options: opt([['none', 'Full'], ['sm', 'Small (520px)'], ['md', 'Medium (760px)'], ['lg', 'Large (960px)']]) },
  { key: 'marginTop', label: 'Space above', kind: 'slider', min: 0, max: 160, step: 4, unit: 'px' },
  { key: 'marginBottom', label: 'Space below', kind: 'slider', min: 0, max: 160, step: 4, unit: 'px' },
]

const deviceLabels = { desktop: 'Desktop', tablet: 'Tablet', mobile: 'Mobile' }
</script>

<template>
  <aside class="flex h-full min-h-0 flex-col bg-white" aria-label="Inspector">
    <template v-if="section">
      <div role="tablist" aria-label="Inspector" class="flex gap-1 border-b border-charcoal/[0.06] pl-3 pr-12 pt-4 xl:pr-3">
        <button
          v-for="t in tabs"
          :key="t.value"
          type="button"
          role="tab"
          :aria-selected="tab === t.value"
          :class="
            cn(
              '-mb-px flex-1 border-b-2 px-2 pb-3 text-[0.8125rem] transition-colors',
              tab === t.value ? 'border-charcoal font-medium text-charcoal' : 'border-transparent text-muted hover:text-charcoal',
            )
          "
          @click="tab = t.value"
        >
          {{ t.label }}
        </button>
      </div>

      <div class="min-h-0 flex-1 overflow-y-auto">
        <!-- ===== Block ===== -->
        <template v-if="block && def">
          <template v-if="tab === 'content'">
            <InspectorGroup :title="def.label">
              <p v-if="def.richText" class="flex gap-2 rounded-xl bg-accent-50 p-3 text-xs leading-relaxed text-accent-700">
                <Type class="mt-0.5 size-3.5 shrink-0" aria-hidden="true" />
                Type directly on the canvas. Select text to make it bold, italic, a link or a softer colour.
              </p>
              <FieldControl
                v-for="f in def.fields"
                :key="f.key"
                :field="f"
                :model-value="blockValue(`props.${f.key}`)"
                @update:model-value="setBlock(`props.${f.key}`, $event)"
              />
            </InspectorGroup>
          </template>
          <InspectorGroup v-else-if="tab === 'style'" title="Layout">
            <FieldControl
              v-for="f in blockStyle"
              :key="f.key"
              :field="f"
              :model-value="blockValue(`style.${f.key}`)"
              @update:model-value="setBlock(`style.${f.key}`, $event)"
            />
          </InspectorGroup>
          <template v-else>
            <InspectorGroup title="Attributes">
              <FieldControl
                v-for="f in advanced"
                :key="f.key"
                :field="f"
                :model-value="blockValue(`style.${f.key}`)"
                @update:model-value="setBlock(`style.${f.key}`, $event)"
              />
            </InspectorGroup>
            <InspectorGroup title="Visibility">
              <FieldControl
                v-for="bp in breakpoints"
                :key="bp"
                :field="{ key: bp, label: `Show on ${deviceLabels[bp].toLowerCase()}`, kind: 'toggle' }"
                :model-value="blockValue(`style.visibility.${bp}`)"
                @update:model-value="setBlock(`style.visibility.${bp}`, $event)"
              />
            </InspectorGroup>
          </template>
        </template>

        <!-- ===== Section ===== -->
        <template v-else>
          <template v-if="tab === 'content'">
            <InspectorGroup title="General">
              <FieldControl v-for="[f, path] in general" :key="path" :field="f" :model-value="sectionValue(path)" @update:model-value="setSection(path, $event)" />
              <template v-if="section.settings.layout === 'split'">
                <FieldControl v-for="[f, path] in splitFields" :key="path" :field="f" :model-value="sectionValue(path)" @update:model-value="setSection(path, $event)" />
                <p class="text-xs leading-relaxed text-subtle">The first {{ section.settings.splitAt }} block(s) go in the left column. Drag blocks between columns to change that. Columns stack on mobile.</p>
              </template>
              <SpacingControl :model-value="section.settings.padding" @update:model-value="setSection('padding', $event)" />
            </InspectorGroup>

            <InspectorGroup title="Background">
              <FieldControl :field="bgKind" :model-value="sectionValue('background.kind')" @update:model-value="setSection('background.kind', $event)" />
              <FieldControl
                v-if="section.settings.background.kind === 'color'"
                :field="{ key: 'color', label: 'Colour', kind: 'color' }"
                :model-value="sectionValue('background.color')"
                @update:model-value="setSection('background.color', $event)"
              />
              <template v-else-if="section.settings.background.kind === 'image'">
                <ImageControl removable :model-value="section.settings.background.image" @update:model-value="setSection('background.image', $event)" />
                <FieldControl v-for="[f, path] in bgImage" :key="path" :field="f" :model-value="sectionValue(path)" @update:model-value="setSection(path, $event)" />
                <div class="grid grid-cols-[1fr_96px] items-end gap-2">
                  <FieldControl
                    :field="{ key: 'overlay', label: 'Overlay', kind: 'color' }"
                    :model-value="sectionValue('background.overlayColor')"
                    @update:model-value="setSection('background.overlayColor', $event)"
                  />
                  <FieldControl
                    :field="{ key: 'opacity', label: 'Opacity', kind: 'number', min: 0, max: 100, step: 5, unit: '%' }"
                    :model-value="sectionValue('background.overlayOpacity')"
                    @update:model-value="setSection('background.overlayOpacity', Math.min(100, Math.max(0, Number($event))))"
                  />
                </div>
              </template>
              <template v-else-if="section.settings.background.kind === 'gradient'">
                <FieldControl v-for="[f, path] in bgGradient" :key="path" :field="f" :model-value="sectionValue(path)" @update:model-value="setSection(path, $event)" />
              </template>
              <FieldControl :field="widthField" :model-value="sectionValue('contentWidth')" @update:model-value="setSection('contentWidth', $event)" />
            </InspectorGroup>

            <InspectorGroup title="Animation">
              <FieldControl v-for="[f, path] in animation" :key="path" :field="f" :model-value="sectionValue(path)" @update:model-value="setSection(path, $event)" />
            </InspectorGroup>
          </template>

          <InspectorGroup v-else-if="tab === 'style'" title="Appearance">
            <FieldControl v-for="[f, path] in sectionStyle" :key="path" :field="f" :model-value="sectionValue(path)" @update:model-value="setSection(path, $event)" />
          </InspectorGroup>

          <template v-else>
            <InspectorGroup title="Attributes">
              <FieldControl v-for="f in advanced" :key="f.key" :field="f" :model-value="sectionValue(f.key)" @update:model-value="setSection(f.key, $event)" />
            </InspectorGroup>
            <InspectorGroup title="Visibility">
              <FieldControl
                v-for="bp in breakpoints"
                :key="bp"
                :field="{ key: bp, label: `Show on ${deviceLabels[bp].toLowerCase()}`, kind: 'toggle' }"
                :model-value="sectionValue(`visibility.${bp}`)"
                @update:model-value="setSection(`visibility.${bp}`, $event)"
              />
            </InspectorGroup>
          </template>
        </template>
      </div>
    </template>

    <div v-else class="flex flex-1 flex-col items-center justify-center gap-3 px-8 text-center">
      <span class="grid size-12 place-items-center rounded-2xl bg-canvas">
        <MousePointerClick class="size-5" :stroke-width="1.6" aria-hidden="true" />
      </span>
      <p class="font-medium">Nothing selected</p>
      <p class="text-sm leading-relaxed text-muted">Click a section or block on the canvas to edit its content, style and behaviour.</p>
      <ul class="mt-2 w-full space-y-1.5 text-left text-xs text-muted">
        <li class="flex justify-between"><span>Undo / redo</span><kbd class="font-sans">Ctrl Z / Ctrl ⇧ Z</kbd></li>
        <li class="flex justify-between"><span>Duplicate</span><kbd class="font-sans">Ctrl D</kbd></li>
        <li class="flex justify-between"><span>Delete</span><kbd class="font-sans">Del</kbd></li>
        <li class="flex justify-between"><span>Move up / down</span><kbd class="font-sans">Alt ↑ / Alt ↓</kbd></li>
        <li class="flex justify-between"><span>Deselect</span><kbd class="font-sans">Esc</kbd></li>
      </ul>
    </div>
  </aside>
</template>
