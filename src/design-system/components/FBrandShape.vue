<script setup lang="ts">
import { cn } from '../utils/cn'

/** The four brand elements: leaf, circle, wedge and gradient tile. */
withDefaults(defineProps<{ shape: 'leaf' | 'circle' | 'wedge' | 'gradient'; size?: number }>(), { size: 48 })
</script>

<template>
  <span
    aria-hidden="true"
    :style="{ width: `${size}px`, height: `${size}px` }"
    :class="
      cn(
        'inline-block',
        shape === 'leaf' && 'rounded-[60%_15%_60%_15%] bg-charcoal',
        shape === 'circle' && 'rounded-full bg-charcoal',
        shape === 'wedge' && 'bg-charcoal [clip-path:polygon(100%_0,100%_100%,0_100%)]',
        shape === 'gradient' && 'rounded-xl bg-gradient-to-br from-accent-200 via-[#d9d4f7] to-accent-100 ring-1 ring-white',
      )
    "
  />
</template>
