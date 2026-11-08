<script setup lang="ts">
import { ArrowRight } from 'lucide-vue-next'
import FLogo from './FLogo.vue'

/** Dark brand banner: "forma. — Turn your ideas into a stunning website. →" */
defineProps<{ href: string }>()
</script>

<template>
  <a
    :href="href"
    class="group relative isolate flex items-center gap-6 overflow-hidden rounded-card bg-charcoal px-7 py-6 text-warm-white ring-1 ring-white/10 sm:gap-10 sm:px-9"
  >
    <span
      aria-hidden="true"
      class="absolute -right-16 -top-24 -z-10 size-80 rounded-full bg-[radial-gradient(circle_at_30%_30%,rgb(255_255_255/0.22),rgb(255_255_255/0.04)_55%,transparent_70%)] transition-transform duration-700 group-hover:scale-110"
    />
    <FLogo tone="light" size="md" />
    <span class="text-sm leading-snug text-warm-white/85"><slot /></span>
    <ArrowRight class="ml-auto size-5 shrink-0 transition-transform group-hover:translate-x-1" aria-hidden="true" />
  </a>
</template>
