<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'
</script>

<template>
  <div class="flex h-[calc(100dvh-8.5rem)] gap-4">
    <div class="hidden w-[272px] shrink-0 flex-col gap-4 rounded-panel bg-white p-5 xl:flex">
      <FSkeleton class="h-6 w-40" />
      <FSkeleton class="h-4 w-52" />
      <div class="mt-4 grid grid-cols-3 gap-2">
        <FSkeleton v-for="i in 12" :key="i" class="aspect-square" />
      </div>
    </div>
    <div class="flex flex-1 flex-col gap-4 rounded-panel bg-white p-4">
      <FSkeleton class="h-10" />
      <FSkeleton class="flex-1" />
    </div>
    <div class="hidden w-[300px] shrink-0 flex-col gap-4 rounded-panel bg-white p-5 xl:flex">
      <FSkeleton class="h-8" />
      <FSkeleton class="h-40" />
      <FSkeleton class="h-40" />
    </div>
  </div>
</template>
