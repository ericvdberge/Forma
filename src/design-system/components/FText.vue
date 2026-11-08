<script setup lang="ts">
import { cn } from '../utils/cn'

withDefaults(
  defineProps<{
    as?: 'p' | 'span' | 'div'
    size?: 'lead' | 'body' | 'small' | 'caption'
    tone?: 'default' | 'muted' | 'light' | 'light-muted'
  }>(),
  { as: 'p', size: 'body', tone: 'muted' },
)

const sizes = {
  lead: 'text-lg md:text-xl leading-relaxed',
  body: 'text-base leading-relaxed',
  small: 'text-sm leading-relaxed',
  caption: 'text-[0.8125rem] leading-snug',
}
const tones = {
  default: 'text-charcoal',
  muted: 'text-muted',
  light: 'text-warm-white',
  'light-muted': 'text-warm-white/65',
}
</script>

<template>
  <component :is="as" :class="cn('text-pretty', sizes[size], tones[tone])"><slot /></component>
</template>
