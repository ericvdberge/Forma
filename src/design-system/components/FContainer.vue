<script setup lang="ts">
import { cn } from '../utils/cn'

withDefaults(defineProps<{ as?: string; size?: 'md' | 'lg' | 'xl' }>(), { as: 'div', size: 'xl' })
const sizes = { md: 'max-w-3xl', lg: 'max-w-5xl', xl: 'max-w-[1240px]' }
</script>

<template>
  <component :is="as" :class="cn('mx-auto w-full px-4 sm:px-6 lg:px-10', sizes[size])"><slot /></component>
</template>
