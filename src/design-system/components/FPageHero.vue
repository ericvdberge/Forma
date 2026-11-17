<script setup lang="ts">
/**
 * The intro at the top of a page: an optional badge, a large two-tone headline, a lead line and
 * actions. Use it so every page opens the same way.
 *
 * `title` is the first line of the headline; `accent` is the second, in a muted tone.
 */
const { as = 'h1' } = defineProps<{
  title: string
  accent?: string
  /** The page's main heading by default */
  as?: 'h1' | 'h2'
}>()
</script>

<template>
  <div>
    <div v-if="$slots.badge" class="mb-6"><slot name="badge" /></div>
    <component :is="as" class="font-display text-[clamp(2.5rem,5vw,4rem)] font-bold leading-[0.96] tracking-[-0.05em] text-charcoal">
      {{ title }}<template v-if="accent"><br /><span class="text-charcoal/45">{{ accent }}</span></template>
    </component>
    <p v-if="$slots.default" class="mt-5 max-w-lg text-[1.0625rem] leading-relaxed text-muted"><slot /></p>
    <div v-if="$slots.actions" class="mt-7 flex flex-wrap items-center gap-3"><slot name="actions" /></div>
  </div>
</template>
