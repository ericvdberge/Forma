<script setup lang="ts">
defineProps<{ title: string; description?: string }>()
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4 pb-8 pt-2">
    <div>
      <h1 class="font-display text-[clamp(2rem,4vw,2.75rem)] font-bold leading-none tracking-[-0.045em]">{{ title }}</h1>
      <p v-if="description" class="mt-3 text-[0.9375rem] text-muted">{{ description }}</p>
      <slot name="meta" />
    </div>
    <div class="flex flex-wrap items-center gap-3"><slot /></div>
  </div>
</template>
