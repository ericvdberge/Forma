<script setup lang="ts">
import { computed, ref, useId } from 'vue'
import { ArrowDown, ArrowUp, ChevronDown, Plus, Trash2 } from 'lucide-vue-next'
import { FSwitch, cn } from '@forma/design-system'
import type { Field } from '@/studio/blocks'
import ColorControl from './ColorControl.vue'
import ImageControl from './ImageControl.vue'

/** One inspector field, drawn from its schema. Lists recurse into their item fields. */
const props = defineProps<{ field: Field; modelValue: unknown }>()
const emit = defineEmits<{ 'update:modelValue': [value: unknown] }>()
const id = useId()

const value = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v),
})

const input =
  'h-10 w-full rounded-lg bg-white px-3 text-sm text-charcoal ring-1 ring-charcoal/10 outline-none transition-shadow focus:ring-2 focus:ring-accent'

/* Lists */
const items = computed(() => (Array.isArray(props.modelValue) ? (props.modelValue as Record<string, unknown>[]) : []))
const openItem = ref<number | null>(null)
function setItem(i: number, key: string, v: unknown) {
  emit('update:modelValue', items.value.map((item, j) => (j === i ? { ...item, [key]: v } : item)))
}
function moveItem(i: number, dir: -1 | 1) {
  const next = [...items.value]
  const [item] = next.splice(i, 1)
  next.splice(i + dir, 0, item!)
  emit('update:modelValue', next)
  if (openItem.value === i) openItem.value = i + dir
}
function removeItem(i: number) {
  emit('update:modelValue', items.value.filter((_, j) => j !== i))
  openItem.value = null
}
function addItem() {
  if (props.field.kind !== 'list') return
  emit('update:modelValue', [...items.value, props.field.newItem()])
  openItem.value = items.value.length
}
const itemTitle = (item: Record<string, unknown>, i: number) =>
  (props.field.kind === 'list' && String(item[props.field.titleKey] ?? '').trim()) || `Item ${i + 1}`
</script>

<template>
  <!-- Toggle -->
  <div v-if="field.kind === 'toggle'" class="flex items-center justify-between gap-3">
    <span class="text-[0.8125rem] text-charcoal/80">{{ field.label }}</span>
    <FSwitch :model-value="!!value" :label="field.label" hide-label @update:model-value="value = $event" />
  </div>

  <!-- List -->
  <div v-else-if="field.kind === 'list'" class="flex flex-col gap-2">
    <div class="flex items-center justify-between">
      <span class="text-[0.8125rem] text-charcoal/80">{{ field.label }}</span>
      <span class="text-xs text-subtle">{{ items.length }}</span>
    </div>
    <ul class="flex flex-col gap-1.5">
      <li v-for="(item, i) in items" :key="i" class="rounded-xl ring-1 ring-charcoal/[0.08]">
        <div class="flex items-center gap-1 pl-3 pr-1">
          <button
            type="button"
            class="flex min-w-0 flex-1 items-center gap-2 py-2.5 text-left text-[0.8125rem] font-medium"
            :aria-expanded="openItem === i"
            @click="openItem = openItem === i ? null : i"
          >
            <ChevronDown :class="cn('size-3.5 shrink-0 text-muted transition-transform', openItem === i && 'rotate-180')" aria-hidden="true" />
            <span class="truncate">{{ itemTitle(item, i) }}</span>
          </button>
          <button type="button" class="grid size-7 place-items-center rounded-md text-muted hover:bg-canvas disabled:opacity-30" :disabled="i === 0" aria-label="Move up" @click="moveItem(i, -1)">
            <ArrowUp class="size-3.5" aria-hidden="true" />
          </button>
          <button
            type="button"
            class="grid size-7 place-items-center rounded-md text-muted hover:bg-canvas disabled:opacity-30"
            :disabled="i === items.length - 1"
            aria-label="Move down"
            @click="moveItem(i, 1)"
          >
            <ArrowDown class="size-3.5" aria-hidden="true" />
          </button>
          <button type="button" class="grid size-7 place-items-center rounded-md text-muted hover:bg-canvas hover:text-danger" aria-label="Remove" @click="removeItem(i)">
            <Trash2 class="size-3.5" aria-hidden="true" />
          </button>
        </div>
        <div v-if="openItem === i" class="flex flex-col gap-3 border-t border-charcoal/[0.06] p-3">
          <FieldControl
            v-for="sub in field.itemFields"
            :key="sub.key"
            :field="sub"
            :model-value="item[sub.key]"
            @update:model-value="setItem(i, sub.key, $event)"
          />
        </div>
      </li>
    </ul>
    <button
      type="button"
      class="flex h-9 items-center justify-center gap-1.5 rounded-xl border border-dashed border-charcoal/15 text-[0.8125rem] text-muted hover:border-charcoal/40 hover:text-charcoal"
      @click="addItem"
    >
      <Plus class="size-3.5" aria-hidden="true" /> Add
    </button>
  </div>

  <!-- Labelled inputs -->
  <div v-else class="flex flex-col gap-1.5">
    <label :for="id" class="text-[0.8125rem] text-charcoal/80">{{ field.label }}</label>

    <input
      v-if="field.kind === 'text' || field.kind === 'url'"
      :id="id"
      :type="field.kind === 'url' ? 'url' : 'text'"
      :value="value"
      :placeholder="field.placeholder"
      :class="input"
      @input="value = ($event.target as HTMLInputElement).value"
    />

    <textarea
      v-else-if="field.kind === 'textarea'"
      :id="id"
      :value="value as string"
      :placeholder="field.placeholder"
      rows="3"
      :class="cn(input, 'h-auto resize-y py-2 leading-relaxed')"
      @input="value = ($event.target as HTMLTextAreaElement).value"
    />

    <div v-else-if="field.kind === 'number'" class="relative">
      <input
        :id="id"
        type="number"
        :value="value"
        :min="field.min"
        :max="field.max"
        :step="field.step ?? 1"
        :class="cn(input, field.unit && 'pr-9')"
        @input="value = Number(($event.target as HTMLInputElement).value) || 0"
      />
      <span v-if="field.unit" class="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs text-subtle">{{ field.unit }}</span>
    </div>

    <div v-else-if="field.kind === 'slider'" class="flex items-center gap-3">
      <input
        :id="id"
        type="range"
        :value="value"
        :min="field.min"
        :max="field.max"
        :step="field.step ?? 1"
        class="h-1 flex-1 cursor-pointer accent-charcoal"
        @input="value = Number(($event.target as HTMLInputElement).value)"
      />
      <span class="w-14 shrink-0 rounded-md bg-canvas px-2 py-1 text-right text-xs tabular-nums">{{ value }}{{ field.unit ?? '' }}</span>
    </div>

    <div v-else-if="field.kind === 'select'" class="relative">
      <select :id="id" :value="value" :class="cn(input, 'cursor-pointer appearance-none pr-9')" @change="value = field.options.find((o) => String(o.value) === ($event.target as HTMLSelectElement).value)?.value">
        <option v-for="o in field.options" :key="o.value" :value="o.value">{{ o.label }}</option>
      </select>
      <ChevronDown class="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-muted" aria-hidden="true" />
    </div>

    <div v-else-if="field.kind === 'segmented'" :id="id" role="radiogroup" :aria-label="field.label" class="flex rounded-lg bg-canvas p-1">
      <button
        v-for="o in field.options"
        :key="o.value"
        type="button"
        role="radio"
        :aria-checked="value === o.value"
        :class="
          cn(
            'h-8 flex-1 rounded-md px-2 text-[0.8125rem] transition-colors',
            value === o.value ? 'bg-white font-medium text-charcoal shadow-soft' : 'text-muted hover:text-charcoal',
          )
        "
        @click="value = o.value"
      >
        {{ o.label }}
      </button>
    </div>

    <ColorControl v-else-if="field.kind === 'color'" :id="id" :model-value="String(value ?? '')" @update:model-value="value = $event" />

    <ImageControl v-else-if="field.kind === 'image'" :model-value="String(value ?? '')" @update:model-value="value = $event" />
  </div>
</template>
