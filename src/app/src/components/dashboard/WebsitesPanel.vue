<script setup lang="ts">
import { ArrowRight, ChevronRight, FilePlus2, Grid2x2Plus, LayoutGrid, Lightbulb, MonitorSmartphone } from 'lucide-vue-next'
import { FArchitectureArt, FStatus, cn } from '@forma/design-system'
import { useCms } from '@/stores/cms'

const { state, activeWebsite, setActiveWebsite } = useCms()

const quickActions = [
  { label: 'Create a new page', icon: FilePlus2, to: { path: '/pages', query: { new: '1' } } },
  { label: 'Browse templates', icon: LayoutGrid, to: '/templates' },
  { label: 'Add a block', icon: Grid2x2Plus, to: '/blocks' },
  { label: 'View your site', icon: MonitorSmartphone, to: '/websites' },
]
</script>

<template>
  <section
    aria-labelledby="panel-websites"
    class="rounded-card bg-white/90 p-3 ring-1 ring-charcoal/[0.06] shadow-card backdrop-blur-xl"
  >
    <div class="flex items-center justify-between px-3 pb-2 pt-2">
      <h2 id="panel-websites" class="text-[0.9375rem] font-bold">Your websites</h2>
      <RouterLink to="/websites" class="inline-flex items-center gap-1 text-xs font-medium hover:underline">
        View all <ArrowRight class="size-3.5" aria-hidden="true" />
      </RouterLink>
    </div>

    <ul class="flex flex-col gap-1">
      <li v-for="site in state.websites.slice(0, 3)" :key="site.id">
        <button
          type="button"
          :aria-pressed="site.id === activeWebsite.id"
          :class="
            cn(
              'flex w-full items-center gap-3.5 rounded-2xl p-2 text-left transition-colors',
              site.id === activeWebsite.id ? 'bg-canvas' : 'hover:bg-canvas/60',
            )
          "
          @click="setActiveWebsite(site.id)"
        >
          <span class="h-12 w-20 shrink-0 overflow-hidden rounded-lg ring-1 ring-charcoal/[0.06]">
            <FArchitectureArt :tone="site.tone" />
          </span>
          <span class="min-w-0 flex-1">
            <span class="block truncate text-sm font-medium">{{ site.name }}</span>
            <span class="block truncate text-xs text-muted">{{ site.domain }}</span>
          </span>
          <FStatus :status="site.status" class="pr-2" />
        </button>
      </li>
    </ul>

    <div class="mx-3 my-3 h-px bg-charcoal/[0.06]" />

    <h3 class="px-3 text-[0.9375rem] font-bold">Quick actions</h3>
    <ul class="mt-1">
      <li v-for="a in quickActions" :key="a.label">
        <RouterLink :to="a.to" class="flex items-center gap-3.5 rounded-xl px-4 py-2.5 text-[0.9375rem] hover:bg-canvas">
          <component :is="a.icon" class="size-5" :stroke-width="1.6" aria-hidden="true" />
          {{ a.label }}
        </RouterLink>
      </li>
    </ul>

    <RouterLink
      to="/templates"
      class="mt-2 flex items-center gap-3 rounded-2xl bg-canvas px-4 py-3 text-xs leading-snug text-muted transition-colors hover:bg-[#efeee9]"
    >
      <Lightbulb class="size-6 shrink-0 text-positive" :stroke-width="1.5" aria-hidden="true" />
      <span><b class="font-bold text-charcoal">Tip:</b> Use Templates to quickly build beautiful pages.</span>
      <ChevronRight class="ml-auto size-4 shrink-0 text-charcoal" aria-hidden="true" />
    </RouterLink>
  </section>
</template>
