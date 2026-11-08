<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'
import PageHeader from '@/components/PageHeader.vue'
</script>

<!-- Mirrors WebsitesView: website list + detail form. -->
<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Websites" description="Switch between your websites and manage their details.">
      <FSkeleton class="h-11 w-36" rounded="full" />
    </PageHeader>

    <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <div class="flex flex-col gap-3">
        <div
          v-for="i in 3"
          :key="i"
          class="flex items-center gap-4 rounded-card bg-white p-3 ring-1 ring-charcoal/[0.06] shadow-soft"
        >
          <FSkeleton class="h-[4.5rem] w-28 shrink-0" rounded="xl" />
          <div class="flex-1">
            <FSkeleton class="h-4 w-32" />
            <FSkeleton class="mt-2 h-3.5 w-28" />
            <FSkeleton class="mt-2 h-3 w-36" />
          </div>
          <FSkeleton class="mr-2 h-4 w-12" />
        </div>
      </div>

      <div class="self-start overflow-hidden rounded-card bg-white ring-1 ring-charcoal/[0.06] shadow-card">
        <FSkeleton class="h-40 w-full" rounded="none" />
        <div class="flex items-center gap-3 border-b border-charcoal/[0.06] px-5 py-4">
          <FSkeleton class="h-9 w-32" rounded="full" />
          <FSkeleton class="ml-auto h-9 w-32" rounded="full" />
        </div>
        <div class="flex flex-col gap-5 p-5">
          <div v-for="i in 2" :key="i">
            <FSkeleton class="h-3.5 w-24" />
            <FSkeleton class="mt-2 h-12 w-full" rounded="full" />
          </div>
          <div>
            <FSkeleton class="h-3.5 w-24" />
            <FSkeleton class="mt-2 h-24 w-full" rounded="2xl" />
          </div>
          <FSkeleton class="h-16 w-full" rounded="2xl" />
        </div>
      </div>
    </div>
  </div>
</template>
