<script setup lang="ts">
import { ArrowRight } from 'lucide-vue-next'
import { cn } from '../utils/cn'

withDefaults(defineProps<{ href: string; tone?: 'dark' | 'light' | 'accent' }>(), { tone: 'dark' })
const tones = { dark: 'text-charcoal', light: 'text-warm-white', accent: 'text-accent' }
</script>

<template>
  <a :href="href" :class="cn('group inline-flex items-center gap-2 text-[0.9375rem] font-bold', tones[tone])">
    <span
      class="bg-[linear-gradient(currentColor,currentColor)] bg-[length:0%_1px] bg-left-bottom bg-no-repeat pb-0.5 transition-[background-size] duration-300 group-hover:bg-[length:100%_1px]"
    >
      <slot />
    </span>
    <ArrowRight class="size-4 transition-transform duration-200 group-hover:translate-x-1" aria-hidden="true" />
  </a>
</template>
