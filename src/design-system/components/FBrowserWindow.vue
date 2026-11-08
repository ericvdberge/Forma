<script setup lang="ts">
import { cn } from '../utils/cn'

withDefaults(defineProps<{ url?: string; tone?: 'light' | 'dark'; dots?: 'color' | 'mono' }>(), {
  tone: 'light',
  dots: 'color',
})
</script>

<template>
  <div
    :class="
      cn(
        'overflow-hidden rounded-[1.25rem] ring-1 shadow-float',
        tone === 'light' ? 'bg-warm-white ring-charcoal/[0.08]' : 'bg-ink ring-white/10',
      )
    "
  >
    <div
      :class="
        cn('flex h-11 items-center gap-2 px-4', tone === 'light' ? 'border-b border-charcoal/[0.06]' : 'border-b border-white/10')
      "
    >
      <span :class="cn('size-3 rounded-full', dots === 'color' ? 'bg-[#ff5f57]' : 'bg-charcoal/15')" />
      <span :class="cn('size-3 rounded-full', dots === 'color' ? 'bg-[#febc2e]' : 'bg-charcoal/15')" />
      <span :class="cn('size-3 rounded-full', dots === 'color' ? 'bg-[#28c840]' : 'bg-charcoal/15')" />
      <span
        v-if="url"
        :class="
          cn(
            'mx-auto rounded-full px-4 py-1 text-[0.6875rem]',
            tone === 'light' ? 'bg-charcoal/[0.04] text-muted' : 'bg-white/5 text-white/50',
          )
        "
      >
        {{ url }}
      </span>
    </div>
    <slot />
  </div>
</template>
