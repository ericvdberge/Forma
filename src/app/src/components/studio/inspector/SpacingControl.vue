<script setup lang="ts">
import { computed, ref } from 'vue'
import { Link2, Link2Off, Monitor, RotateCcw, Smartphone, Tablet } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { clone } from '@/studio/defaults'
import { useStudio } from '@/composables/useStudio'
import { resolve, sourceOf } from '@/studio/responsive'
import type { Breakpoint, Responsive, Spacing } from '@/studio/types'

/** Padding per device. Tablet and mobile inherit from the larger screen until you change them. */
const props = defineProps<{ modelValue: Responsive<Spacing> }>()
const emit = defineEmits<{ 'update:modelValue': [value: Responsive<Spacing>] }>()
const studio = useStudio()

const devices: Array<{ value: Breakpoint; label: string; icon: typeof Monitor }> = [
  { value: 'desktop', label: 'Desktop', icon: Monitor },
  { value: 'tablet', label: 'Tablet', icon: Tablet },
  { value: 'mobile', label: 'Mobile', icon: Smartphone },
]
const bp = computed(() => studio.device.value)
const current = computed(() => resolve(props.modelValue, bp.value))
// Start linked when the values already match, like the design tools people know.
const linkedY = ref(current.value.top === current.value.bottom)
const linkedX = ref(current.value.left === current.value.right)

function set(keys: Array<keyof Spacing>, v: number) {
  const next = clone(props.modelValue) as Responsive<Spacing>
  for (const key of keys) {
    if (bp.value === 'desktop') next.desktop[key] = v
    else next[bp.value] = { ...next[bp.value], [key]: v }
  }
  emit('update:modelValue', next)
}
const pair = (key: keyof Spacing, other: keyof Spacing, linked: boolean) => (linked ? [key, other] : [key])

const overridden = computed(() => bp.value !== 'desktop' && Object.keys(props.modelValue[bp.value] ?? {}).length > 0)
function reset() {
  const next = clone(props.modelValue) as Responsive<Spacing>
  if (bp.value !== 'desktop') delete next[bp.value]
  emit('update:modelValue', next)
}
const inherited = (key: keyof Spacing) => bp.value !== 'desktop' && sourceOf(props.modelValue, bp.value, key) !== bp.value

const box = 'h-10 w-full rounded-lg bg-white px-3 text-sm tabular-nums ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent'
</script>

<template>
  <div class="flex flex-col gap-3">
    <div class="flex items-center justify-between">
      <span class="text-[0.8125rem] text-charcoal/80">Padding</span>
      <button v-if="overridden" type="button" class="inline-flex items-center gap-1 text-xs text-muted hover:text-charcoal" @click="reset">
        <RotateCcw class="size-3" aria-hidden="true" /> Reset {{ bp }}
      </button>
    </div>
    <div role="radiogroup" aria-label="Device" class="flex gap-1">
      <button
        v-for="d in devices"
        :key="d.value"
        type="button"
        role="radio"
        :aria-checked="bp === d.value"
        :title="`Edit ${d.label.toLowerCase()} padding`"
        :class="cn('grid size-8 place-items-center rounded-lg ring-1', bp === d.value ? 'bg-charcoal/[0.06] ring-charcoal/20' : 'ring-charcoal/[0.08] text-muted hover:text-charcoal')"
        @click="studio.device.value = d.value"
      >
        <component :is="d.icon" class="size-4" :stroke-width="1.6" aria-hidden="true" />
        <span class="sr-only">{{ d.label }}</span>
      </button>
    </div>

    <div v-for="row in [['top', 'bottom'], ['left', 'right']] as const" :key="row[0]" class="flex items-end gap-2">
      <label v-for="(key, k) in row" :key="key" class="flex flex-1 flex-col gap-1 text-xs capitalize text-muted">
        {{ key }}
        <input
          type="number"
          min="0"
          max="400"
          step="4"
          :value="current[key]"
          :class="cn(box, inherited(key) && 'text-subtle')"
          :title="inherited(key) ? 'Inherited from a larger screen' : undefined"
          @input="set(pair(key, row[1 - k]!, row[0] === 'top' ? linkedY : linkedX), Math.max(0, Number(($event.target as HTMLInputElement).value) || 0))"
        />
      </label>
      <button
        type="button"
        :class="cn('grid size-10 shrink-0 place-items-center rounded-lg', (row[0] === 'top' ? linkedY : linkedX) ? 'bg-accent-50 text-accent' : 'text-muted hover:bg-canvas')"
        :aria-pressed="row[0] === 'top' ? linkedY : linkedX"
        :aria-label="`Link ${row[0]} and ${row[1]}`"
        :title="`Link ${row[0]} and ${row[1]}`"
        @click="row[0] === 'top' ? (linkedY = !linkedY) : (linkedX = !linkedX)"
      >
        <component :is="(row[0] === 'top' ? linkedY : linkedX) ? Link2 : Link2Off" class="size-4" aria-hidden="true" />
      </button>
    </div>
    <p v-if="bp !== 'desktop'" class="text-xs text-subtle">
      {{ overridden ? `Custom values for ${bp}.` : `Inherited from a larger screen. Change a value to set it for ${bp} only.` }}
    </p>
  </div>
</template>
