<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'
</script>

<!-- Mirrors MediaView: hero and stats, filter bar and image grid, with the upload panel on the side. -->
<template>
  <div class="mx-auto grid max-w-[1480px] gap-6 xl:grid-cols-[minmax(0,1fr)_300px] 2xl:gap-8 2xl:grid-cols-[minmax(0,1fr)_320px]">
    <div class="@container min-w-0">
      <div class="grid items-center gap-6 pb-6 md:grid-cols-[1.3fr_1fr]">
        <div>
          <FSkeleton class="h-8 w-36" rounded="full" />
          <FSkeleton class="mt-3 h-10 w-96 max-w-full" />
          <FSkeleton class="mt-3 h-5 w-80 max-w-full" />
          <FSkeleton class="mt-4 h-11 w-40" rounded="full" />
        </div>
        <FSkeleton class="hidden h-[clamp(130px,22vh,190px)] md:block" rounded="2xl" />
      </div>
      <FSkeleton class="h-24 w-full" rounded="2xl" />
      <div class="mt-6 flex gap-2">
        <FSkeleton v-for="i in 5" :key="i" class="h-10 w-20" rounded="full" />
      </div>
      <div class="mt-6 grid grid-cols-1 gap-4 @lg:grid-cols-2 @2xl:grid-cols-3 @6xl:grid-cols-4">
        <FSkeleton v-for="i in 8" :key="i" class="aspect-[16/10] w-full" rounded="2xl" />
      </div>
    </div>
    <div class="flex flex-col gap-4">
      <FSkeleton class="h-9 w-full" rounded="full" />
      <FSkeleton class="h-[560px] w-full" rounded="2xl" />
    </div>
  </div>
</template>
