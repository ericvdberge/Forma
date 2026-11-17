<script setup lang="ts">
import { cn } from '../utils/cn'

/**
 * The intro at the top of a page: an optional badge, a large two-tone headline, a lead line and
 * actions. Use it so every page opens the same way.
 *
 * `title` is the first line of the headline; `accent` is the second, in a muted tone.
 * `compact` puts both on one smaller line, for working pages where the content matters more.
 */
const { as = 'h1', compact = false } = defineProps<{
  title: string
  accent?: string
  /** The page's main heading by default */
  as?: 'h1' | 'h2'
  compact?: boolean
}>()
</script>

<template>
  <div>
    <div v-if="$slots.badge" :class="compact ? 'mb-3' : 'mb-6'"><slot name="badge" /></div>
    <component
      :is="as"
      :class="
        cn(
          'font-display font-bold text-charcoal',
          compact
            ? 'text-[clamp(1.75rem,min(3vw,5vh),2.75rem)] leading-[1.05] tracking-[-0.04em]'
            : 'text-[clamp(2.5rem,5vw,4rem)] leading-[0.96] tracking-[-0.05em]',
        )
      "
    >
      {{ title }}<template v-if="accent"><br v-if="!compact" /><template v-else>{{ ' ' }}</template><span class="text-charcoal/45">{{ accent }}</span></template>
    </component>
    <p v-if="$slots.default" :class="cn('max-w-lg leading-relaxed text-muted', compact ? 'mt-2 text-[0.9375rem]' : 'mt-5 text-[1.0625rem]')"><slot /></p>
    <div v-if="$slots.actions" :class="cn('flex flex-wrap items-center gap-3', compact ? 'mt-4' : 'mt-7')"><slot name="actions" /></div>
  </div>
</template>
