<script setup lang="ts">
import { ArrowRight, Settings } from 'lucide-vue-next'
import { FArchitectureArt } from '@forma/design-system'
import { useActivity } from '@/composables/useActivity'
import { timeAgo } from '@/utils/time'

const { getActivity } = useActivity()
const { activity } = getActivity()
</script>

<template>
  <section aria-labelledby="activity-title" class="rounded-card bg-white p-5 ring-1 ring-charcoal/[0.06] shadow-soft">
    <div class="flex items-center justify-between pb-2">
      <h2 id="activity-title" class="font-display text-lg font-bold tracking-[-0.02em]">Recent activity</h2>
      <RouterLink to="/pages" class="inline-flex items-center gap-1.5 text-sm font-medium hover:underline">
        View all <ArrowRight class="size-4" aria-hidden="true" />
      </RouterLink>
    </div>

    <ol class="relative mt-3">
      <span class="absolute bottom-4 left-[5px] top-4 w-px bg-charcoal/10" aria-hidden="true" />
      <li v-for="a in activity.slice(0, 3)" :key="a.id" class="relative flex items-center gap-5 py-2.5 pl-8">
        <span class="absolute left-0 top-1/2 size-[11px] -translate-y-1/2 rounded-full bg-charcoal ring-4 ring-white" aria-hidden="true" />
        <div class="min-w-0 flex-1">
          <p class="truncate text-[0.9375rem]">{{ a.message }}</p>
          <p class="text-xs text-subtle">{{ timeAgo(a.at) }}</p>
        </div>
        <span
          v-if="a.kind === 'styles'"
          class="grid h-11 w-[4.5rem] shrink-0 place-items-center rounded-lg bg-canvas text-charcoal"
          aria-hidden="true"
        >
          <Settings class="size-5" :stroke-width="1.6" />
        </span>
        <span v-else class="h-11 w-[4.5rem] shrink-0 overflow-hidden rounded-lg ring-1 ring-charcoal/[0.06]" aria-hidden="true">
          <FArchitectureArt tone="light" />
        </span>
      </li>
    </ol>
  </section>
</template>
