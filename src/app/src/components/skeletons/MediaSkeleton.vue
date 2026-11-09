<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'
import PageHeader from '@/components/PageHeader.vue'
import MediaGridSkeleton from './MediaGridSkeleton.vue'
</script>

<!-- Mirrors MediaView: header with website switcher and upload button, filter bar and image grid. -->
<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Media">
      <template #meta>
        <FSkeleton class="mt-3 h-5 w-80" />
      </template>
      <FSkeleton class="h-9 w-48" rounded="full" />
      <FSkeleton class="h-11 w-32" rounded="full" />
    </PageHeader>

    <div class="overflow-hidden rounded-card bg-white ring-1 ring-charcoal/[0.06] shadow-card">
      <div class="flex flex-col gap-4 border-b border-charcoal/[0.06] p-4 md:flex-row md:items-center md:justify-between">
        <div class="flex gap-1">
          <FSkeleton v-for="i in 5" :key="i" class="h-9 w-16" rounded="full" />
        </div>
        <FSkeleton class="h-9 w-full md:w-96" rounded="full" />
      </div>
      <MediaGridSkeleton />
    </div>
  </div>
</template>
