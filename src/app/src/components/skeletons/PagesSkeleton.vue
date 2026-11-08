<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'
import PageHeader from '@/components/PageHeader.vue'
</script>

<!-- Mirrors PagesView: header with website switcher, filter bar and pages table. -->
<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Pages">
      <template #meta>
        <FSkeleton class="mt-3 h-5 w-72" />
      </template>
      <FSkeleton class="h-9 w-48" rounded="full" />
      <FSkeleton class="h-11 w-32" rounded="full" />
    </PageHeader>

    <div class="overflow-hidden rounded-card bg-white ring-1 ring-charcoal/[0.06] shadow-card">
      <div class="flex flex-col gap-4 border-b border-charcoal/[0.06] p-4 md:flex-row md:items-center md:justify-between">
        <div class="flex gap-1">
          <FSkeleton v-for="i in 5" :key="i" class="h-9 w-20" rounded="full" />
        </div>
        <FSkeleton class="h-9 w-full md:w-72" rounded="full" />
      </div>
      <div
        v-for="i in 6"
        :key="i"
        class="flex items-center gap-5 border-b border-charcoal/[0.04] px-5 py-3.5 last:border-0"
      >
        <div class="flex flex-1 items-center gap-3">
          <FSkeleton class="size-9 shrink-0" />
          <div>
            <FSkeleton class="h-4 w-32" />
            <FSkeleton class="mt-2 h-3 w-40" />
          </div>
        </div>
        <FSkeleton class="h-4 w-14" />
        <FSkeleton class="hidden h-4 w-28 md:block" />
        <FSkeleton class="h-4 w-20" />
        <FSkeleton class="size-8" rounded="full" />
      </div>
    </div>
  </div>
</template>
