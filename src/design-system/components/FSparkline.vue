<script setup lang="ts">
import { computed } from 'vue'

/** Minimal trend line. Pass raw values; they are scaled to fit. */
const props = withDefaults(defineProps<{ values: number[]; tone?: 'positive' | 'neutral' | 'accent'; height?: number }>(), {
  tone: 'positive',
  height: 32,
})

const W = 200
const strokes = { positive: 'var(--color-positive)', neutral: 'rgb(11 11 15 / 0.35)', accent: 'var(--color-accent)' }

const path = computed(() => {
  const v = props.values
  if (v.length < 2) return ''
  const min = Math.min(...v)
  const max = Math.max(...v)
  const span = max - min || 1
  const pad = 3
  const pts = v.map((y, i) => [(i / (v.length - 1)) * W, pad + (1 - (y - min) / span) * (props.height - pad * 2)] as const)
  // Smooth with simple midpoint curves.
  let d = `M${pts[0]![0]},${pts[0]![1]}`
  for (let i = 1; i < pts.length; i++) {
    const [x0, y0] = pts[i - 1]!
    const [x1, y1] = pts[i]!
    const mx = (x0 + x1) / 2
    d += ` C${mx},${y0} ${mx},${y1} ${x1},${y1}`
  }
  return d
})
</script>

<template>
  <svg :viewBox="`0 0 ${W} ${height}`" preserveAspectRatio="none" class="w-full" :style="{ height: `${height}px` }" aria-hidden="true">
    <path :d="path" fill="none" :stroke="strokes[tone]" stroke-width="1.75" vector-effect="non-scaling-stroke" stroke-linecap="round" />
  </svg>
</template>
