<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight } from 'lucide-vue-next'
import { cn } from '../utils/cn'

const props = withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'accent' | 'ghost' | 'inverse'
    size?: 'sm' | 'md' | 'lg'
    /** Renders an <a> when set */
    href?: string
    type?: 'button' | 'submit' | 'reset'
    /** Show the trailing arrow used across the brand */
    arrow?: boolean
    block?: boolean
    disabled?: boolean
  }>(),
  { variant: 'primary', size: 'md', type: 'button', arrow: false, block: false, disabled: false },
)

const variants = {
  primary: 'bg-charcoal text-warm-white hover:bg-ink shadow-[0_8px_20px_-8px_rgb(11_11_15/0.5)]',
  secondary: 'bg-transparent text-charcoal ring-1 ring-inset ring-charcoal/20 hover:ring-charcoal/60 hover:bg-white',
  accent: 'bg-accent text-white hover:bg-accent-600 shadow-[0_8px_20px_-8px_rgb(59_130_246/0.6)]',
  ghost: 'bg-transparent text-charcoal hover:bg-charcoal/5',
  inverse: 'bg-warm-white text-charcoal hover:bg-white',
}
const sizes = {
  sm: 'h-9 px-4 text-sm gap-1.5',
  md: 'h-11 px-5 text-[0.9375rem] gap-2',
  lg: 'h-14 px-7 text-base gap-2.5',
}

const classes = computed(() =>
  cn(
    'group inline-flex items-center justify-center rounded-full font-medium whitespace-nowrap',
    'transition-all duration-200 ease-out active:scale-[0.98]',
    'disabled:pointer-events-none disabled:opacity-50',
    variants[props.variant],
    sizes[props.size],
    props.block && 'w-full',
  ),
)
</script>

<template>
  <a v-if="href" :href="href" :class="classes">
    <slot />
    <ArrowRight v-if="arrow" class="size-4 transition-transform duration-200 group-hover:translate-x-0.5" aria-hidden="true" />
  </a>
  <button v-else :type="type" :class="classes" :disabled="disabled">
    <slot />
    <ArrowRight v-if="arrow" class="size-4 transition-transform duration-200 group-hover:translate-x-0.5" aria-hidden="true" />
  </button>
</template>
