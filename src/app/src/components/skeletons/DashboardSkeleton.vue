<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'
</script>

<!-- Mirrors DashboardView: hero + stats, websites panel, featured website + activity. -->
<template>
  <div class="relative grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
    <div class="flex min-w-0 flex-col gap-10">
      <section class="pt-2 sm:pt-6">
        <FSkeleton class="h-8 w-52" rounded="full" />
        <FSkeleton class="mt-6 h-14 w-full max-w-md" rounded="2xl" />
        <FSkeleton class="mt-3 h-14 w-2/3 max-w-sm" rounded="2xl" />
        <FSkeleton class="mt-6 h-5 w-full max-w-lg" />
        <FSkeleton class="mt-2 h-5 w-3/4 max-w-sm" />
        <div class="mt-7 flex gap-3">
          <FSkeleton class="h-11 w-48" rounded="full" />
          <FSkeleton class="h-11 w-40" rounded="full" />
        </div>
      </section>

      <section class="grid gap-4 sm:grid-cols-2 2xl:grid-cols-4">
        <div v-for="i in 4" :key="i" class="rounded-card bg-white/85 p-5 ring-1 ring-charcoal/[0.06] shadow-soft">
          <div class="flex items-start gap-3.5">
            <FSkeleton class="size-11 shrink-0" rounded="xl" />
            <div class="flex-1">
              <FSkeleton class="h-3.5 w-20" />
              <FSkeleton class="mt-2.5 h-6 w-16" />
            </div>
          </div>
          <FSkeleton class="mt-4 h-8 w-full" />
        </div>
      </section>
    </div>

    <section class="self-start rounded-card bg-white/90 p-3 ring-1 ring-charcoal/[0.06] shadow-card">
      <div class="flex items-center justify-between px-3 py-2">
        <FSkeleton class="h-4 w-28" />
        <FSkeleton class="h-3 w-14" />
      </div>
      <div v-for="i in 3" :key="i" class="flex items-center gap-3.5 p-2">
        <FSkeleton class="h-12 w-20 shrink-0" />
        <div class="flex-1">
          <FSkeleton class="h-3.5 w-28" />
          <FSkeleton class="mt-2 h-3 w-24" />
        </div>
      </div>
      <div class="mx-3 my-3 h-px bg-charcoal/[0.06]" />
      <FSkeleton class="mx-3 h-4 w-28" />
      <div v-for="i in 4" :key="i" class="flex items-center gap-3.5 px-4 py-2.5">
        <FSkeleton class="size-5" rounded="md" />
        <FSkeleton class="h-4 w-36" />
      </div>
    </section>
  </div>

  <div class="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_minmax(0,1fr)]">
    <div class="h-72 rounded-card bg-white p-3 ring-1 ring-charcoal/[0.06] shadow-card">
      <FSkeleton class="h-full w-full" rounded="2xl" />
    </div>
    <div class="flex flex-col gap-4 rounded-card bg-white p-6 ring-1 ring-charcoal/[0.06] shadow-card">
      <FSkeleton class="h-4 w-32" />
      <div v-for="i in 4" :key="i" class="flex items-center gap-3">
        <FSkeleton class="size-8 shrink-0" rounded="full" />
        <div class="flex-1">
          <FSkeleton class="h-3.5 w-3/4" />
          <FSkeleton class="mt-2 h-3 w-16" />
        </div>
      </div>
    </div>
  </div>
</template>
