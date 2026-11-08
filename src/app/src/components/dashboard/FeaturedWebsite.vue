<script setup lang="ts">
import { ArrowRight, Ellipsis, ExternalLink } from 'lucide-vue-next'
import { FArchitectureArt, FAvatar } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { shortAgo } from '@/utils/time'

const { getActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
</script>

<template>
  <section aria-labelledby="featured-title" class="rounded-card bg-white p-5 ring-1 ring-charcoal/[0.06] shadow-soft">
    <div class="flex items-center justify-between border-b border-charcoal/[0.06] pb-4">
      <h2 id="featured-title" class="font-display text-lg font-bold tracking-[-0.02em]">Your Websites</h2>
      <RouterLink to="/websites" class="inline-flex items-center gap-1.5 text-sm font-medium hover:underline">
        View all <ArrowRight class="size-4" aria-hidden="true" />
      </RouterLink>
    </div>

    <div class="mt-4 grid gap-6 md:grid-cols-[minmax(0,1.15fr)_minmax(0,1fr)]">
      <div class="relative aspect-[16/9] overflow-hidden rounded-2xl ring-1 ring-charcoal/[0.06]">
        <FArchitectureArt :tone="activeWebsite.tone" />
        <span
          class="absolute left-3 top-3 rounded-lg bg-white/85 px-2.5 py-1 text-xs font-medium backdrop-blur"
          :class="activeWebsite.status === 'live' ? 'text-positive' : 'text-muted'"
        >
          {{ activeWebsite.status === 'live' ? 'Live' : 'Draft' }}
        </span>
        <RouterLink
          :to="`/websites/${activeWebsite.id}`"
          class="absolute right-3 top-3 grid size-8 place-items-center rounded-lg bg-white/90 text-charcoal backdrop-blur hover:bg-white"
          :aria-label="`Edit ${activeWebsite.name}`"
        >
          <Ellipsis class="size-4" aria-hidden="true" />
        </RouterLink>
      </div>

      <div class="flex min-w-0 flex-col py-1">
        <h3 class="font-display text-xl font-medium tracking-[-0.02em]">{{ activeWebsite.name }}</h3>
        <p class="mt-0.5 inline-flex items-center gap-1.5 text-sm text-muted">
          {{ activeWebsite.domain }} <ExternalLink class="size-3.5" aria-hidden="true" />
        </p>
        <p class="mt-4 text-sm text-charcoal/80">{{ activeWebsite.description || 'No description yet.' }}</p>
        <div class="mt-4 flex items-center -space-x-1.5">
          <FAvatar v-for="c in activeWebsite.collaborators.slice(0, 3)" :key="c" :name="c" size="sm" ring />
          <span
            v-if="activeWebsite.collaborators.length > 3"
            class="grid size-8 place-items-center rounded-full bg-canvas text-[0.6875rem] font-bold ring-2 ring-white"
          >
            +{{ activeWebsite.collaborators.length - 3 }}
          </span>
        </div>
        <div class="mt-auto flex flex-wrap items-center justify-between gap-3 pt-5">
          <span class="inline-flex items-center gap-2 text-xs text-muted">
            <span class="size-2 rounded-full bg-sage" aria-hidden="true" />
            Updated {{ shortAgo(activeWebsite.updatedAt) }}
          </span>
          <LinkButton variant="secondary" size="sm" arrow :to="`/websites/${activeWebsite.id}`">Manage site</LinkButton>
        </div>
      </div>
    </div>
  </section>
</template>
