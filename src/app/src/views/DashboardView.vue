<script setup lang="ts">
import { Eye, Layers, LayoutGrid, Users } from 'lucide-vue-next'
import { FArchitectureArt, FPageHero } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
import { currentUser, stats } from '@/data/seed'
import StatCard from '@/components/dashboard/StatCard.vue'
import WebsitesPanel from '@/components/dashboard/WebsitesPanel.vue'
import FeaturedWebsite from '@/components/dashboard/FeaturedWebsite.vue'
import RecentActivity from '@/components/dashboard/RecentActivity.vue'

const statIcons = [Eye, LayoutGrid, Layers, Users]
</script>

<template>
  <!-- Pull up under the transparent top bar so the hero image reaches the top edge. -->
  <div class="relative -mt-20 pt-20">
    <div
      aria-hidden="true"
      class="pointer-events-none absolute -right-4 top-0 h-[500px] w-[85%] sm:-right-8 sm:w-[72%] [mask-image:linear-gradient(to_right,transparent,black_35%),linear-gradient(to_bottom,black_70%,transparent)] [mask-composite:intersect]"
    >
      <FArchitectureArt tone="light" />
    </div>

    <div class="relative grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
      <div class="flex min-w-0 flex-col gap-10">
        <FPageHero class="pt-2 sm:pt-6" title="Build something" accent="extraordinary.">
          <template #badge>
            <span
              class="inline-flex items-center gap-2 rounded-full bg-white/80 px-3.5 py-1.5 text-[0.8125rem] font-medium ring-1 ring-charcoal/[0.06] shadow-soft backdrop-blur"
            >
              <span class="size-2.5 rounded-full bg-sage" aria-hidden="true" />
              Welcome back, {{ currentUser.firstName }}! 👋
            </span>
          </template>
          Your creative journey starts here. Design, build and publish stunning websites with Forma.
          <template #actions>
            <LinkButton to="/pages" arrow>Edit your pages</LinkButton>
            <LinkButton to="/templates" variant="secondary" class="bg-white/60 backdrop-blur">Browse Templates</LinkButton>
          </template>
        </FPageHero>

        <section aria-label="Statistics" class="grid gap-4 sm:grid-cols-2 2xl:grid-cols-4">
          <StatCard v-for="(s, i) in stats" :key="s.label" :stat="s" :icon="statIcons[i]!" />
        </section>
      </div>

      <WebsitesPanel class="self-start xl:mt-0" />
    </div>

    <div class="relative mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_minmax(0,1fr)]">
      <FeaturedWebsite />
      <RecentActivity />
    </div>
  </div>
</template>
