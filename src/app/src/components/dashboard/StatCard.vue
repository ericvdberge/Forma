<script setup lang="ts">
import type { Component } from 'vue'
import { ArrowUp } from 'lucide-vue-next'
import { FSparkline } from '@forma/design-system'
import type { Stat } from '@/data/types'

defineProps<{ stat: Stat; icon: Component }>()
</script>

<template>
  <article class="rounded-card bg-white/85 p-5 ring-1 ring-charcoal/[0.06] shadow-soft backdrop-blur">
    <div class="flex items-start gap-3.5">
      <span class="grid size-11 shrink-0 place-items-center rounded-xl bg-[#eef1ea] text-charcoal">
        <component :is="icon" class="size-5" :stroke-width="1.6" aria-hidden="true" />
      </span>
      <div class="min-w-0 flex-1">
        <p class="truncate text-[0.8125rem] text-muted">{{ stat.label }}</p>
        <div class="flex items-baseline justify-between gap-2">
          <p class="font-display text-2xl font-bold tracking-[-0.03em]">{{ stat.value }}</p>
          <p class="inline-flex items-center gap-0.5 text-xs font-medium text-positive">
            <ArrowUp class="size-3.5" aria-hidden="true" />+{{ stat.change }}%
            <span class="sr-only">this month</span>
          </p>
        </div>
      </div>
    </div>
    <FSparkline class="mt-3" :values="stat.trend" :tone="stat.change >= 5 ? 'positive' : 'neutral'" />
  </article>
</template>
