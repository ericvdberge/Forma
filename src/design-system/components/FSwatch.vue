<script setup lang="ts">
defineProps<{ name: string; hex: string }>()
</script>

<template>
  <figure class="flex flex-col items-center gap-3 text-center">
    <span class="size-16 rounded-full ring-1 ring-charcoal/[0.06] shadow-soft" :style="{ backgroundColor: hex }" />
    <figcaption class="text-[0.8125rem] leading-tight">
      <span class="block text-charcoal">{{ name }}</span>
      <span class="block uppercase text-muted">{{ hex }}</span>
    </figcaption>
  </figure>
</template>
