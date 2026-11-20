<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useClipboard } from '@vueuse/core'
import { ArrowRight, Check, CircleHelp, ExternalLink, Package, Puzzle, RefreshCw } from 'lucide-vue-next'
import { FArchitectureArt, FBadge, FButton, FPillTabs, FSelect, FSkeleton } from '@forma/design-system'
import type { PluginDto } from '@/api/generated'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import InstallPluginDialog from '@/components/plugins/InstallPluginDialog.vue'
import PluginCard from '@/components/plugins/PluginCard.vue'
import PluginRunsDialog from '@/components/plugins/PluginRunsDialog.vue'
import { DOCS_URL } from '@/components/plugins/links'
import { HOOK_EVENTS, usePlugins } from '@/composables/usePlugins'
import { useToast } from '@/composables/useToast'

const { getPlugins, setEnabled, deletePlugin, isDeleting } = usePlugins()
const { data, isPending, isError, isFetching, refetch } = getPlugins()
const plugins = computed(() => data.value ?? [])
const { notify } = useToast()
const { copy } = useClipboard({ legacy: true })
const route = useRoute()
const router = useRouter()

/* Filters */
type Filter = 'all' | 'blocks' | 'checks' | 'notifications' | 'off' | 'broken'
const filter = ref<Filter>('all')
const matches: Record<Filter, (p: PluginDto) => boolean> = {
  all: () => true,
  blocks: (p) => p.blocks.length > 0,
  checks: (p) => p.events.some((e) => HOOK_EVENTS.has(e)),
  notifications: (p) => p.events.some((e) => !HOOK_EVENTS.has(e)),
  off: (p) => p.loaded && !p.enabled,
  broken: (p) => !p.loaded,
}
const tabs = computed(() => [
  { value: 'all' as const, label: 'All' },
  { value: 'blocks' as const, label: 'Blocks', count: plugins.value.filter(matches.blocks).length },
  { value: 'checks' as const, label: 'Page checks', count: plugins.value.filter(matches.checks).length },
  { value: 'notifications' as const, label: 'Notifications', count: plugins.value.filter(matches.notifications).length },
  ...(plugins.value.some(matches.off)
    ? [{ value: 'off' as const, label: 'Turned off', count: plugins.value.filter(matches.off).length }]
    : []),
  ...(plugins.value.some(matches.broken)
    ? [{ value: 'broken' as const, label: 'Needs attention', count: plugins.value.filter(matches.broken).length }]
    : []),
])

type Sort = 'name' | 'status'
const sort = ref<Sort>('name')
const sortOptions: Array<{ label: string; value: Sort }> = [
  { label: 'Name A–Z', value: 'name' },
  { label: 'Problems first', value: 'status' },
]
const visible = computed(() => {
  const list = plugins.value.filter(matches[filter.value])
  return list.sort((a, b) =>
    sort.value === 'status' && a.loaded !== b.loaded ? (a.loaded ? 1 : -1) : a.name.localeCompare(b.name),
  )
})

/* Runs dialog, kept in the URL (?plugin=id) so it survives a reload. */
const selected = computed(() => plugins.value.find((p) => p.id === route.query.plugin) ?? null)
const openPlugin = (id: string) => router.replace({ query: { ...route.query, plugin: id } })
const closePlugin = () => router.replace({ query: { ...route.query, plugin: undefined } })

/* Delete, after asking */
const pendingDelete = ref<PluginDto | null>(null)
const confirmOpen = computed({
  get: () => !!pendingDelete.value,
  set: (v: boolean) => {
    if (!v) pendingDelete.value = null
  },
})
async function confirmDelete() {
  const plugin = pendingDelete.value
  if (!plugin) return
  if (await deletePlugin(plugin)) {
    pendingDelete.value = null
    if (route.query.plugin === plugin.id) closePlugin()
  }
}

const installOpen = ref(false)
const copyId = async (id: string) => {
  await copy(id)
  notify('Plugin id copied')
}
const scrollToList = () => document.getElementById('installed-plugins')?.scrollIntoView({ behavior: 'smooth' })
const perks = ['Check pages before they go live', 'React when pages are saved or published', 'Run safely in a sandbox']
</script>

<template>
  <div class="mx-auto flex max-w-[1240px] flex-col gap-6 pt-2">
    <!-- Hero -->
    <section
      class="relative isolate overflow-hidden rounded-[1.75rem] bg-gradient-to-br from-white via-canvas to-[#ecebe6] ring-1 ring-charcoal/[0.05]"
    >
      <div class="pointer-events-none absolute inset-y-0 right-0 -z-10 hidden w-[58%] md:block" aria-hidden="true">
        <FArchitectureArt tone="light" class="[mask-image:linear-gradient(to_right,transparent,black_30%)]" />
      </div>
      <div class="flex max-w-xl flex-col gap-5 p-7 sm:p-10">
        <FBadge dot-tone="positive" class="self-start">Extend Forma</FBadge>
        <h1 class="font-display text-[clamp(2rem,4.4vw,3.25rem)] font-bold leading-[1.02] tracking-[-0.045em]">
          Supercharge your site with powerful <span class="text-positive">plugins.</span>
        </h1>
        <p class="max-w-md text-[0.9375rem] text-muted sm:text-base">
          Add checks and automations that run whenever pages are saved, checked or published — safely, in a sandbox.
        </p>
        <div class="flex flex-wrap gap-3">
          <FButton arrow @click="installOpen = true">Install a plugin</FButton>
          <FButton variant="secondary" @click="scrollToList">Manage installed</FButton>
        </div>
      </div>
    </section>

    <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_280px]">
      <!-- Installed plugins -->
      <section id="installed-plugins" class="flex min-w-0 scroll-mt-24 flex-col gap-5" aria-label="Installed plugins">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <FPillTabs v-model="filter" :options="tabs" label="Filter plugins" />
          <div class="flex items-center gap-1">
            <button
              type="button"
              class="grid size-9 place-items-center rounded-full text-muted hover:bg-white hover:text-charcoal"
              aria-label="Reload plugins"
              title="Reload plugins"
              @click="refetch()"
            >
              <RefreshCw :class="['size-4', isFetching && 'animate-spin']" aria-hidden="true" />
            </button>
            <FSelect v-model="sort" label="Sort" hide-label size="sm" variant="plain" :options="sortOptions" class="w-[9.5rem]" />
          </div>
        </div>

        <div v-if="isPending" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <FSkeleton v-for="i in 3" :key="i" class="h-[188px] rounded-[1.25rem]" />
        </div>

        <div v-else-if="isError" class="rounded-[1.25rem] bg-white p-8 text-center ring-1 ring-charcoal/[0.06]">
          <p class="font-medium">Couldn’t load the plugins.</p>
          <FButton variant="secondary" size="sm" class="mt-4" @click="refetch()">Try again</FButton>
        </div>

        <div
          v-else-if="!plugins.length"
          class="flex flex-col items-center gap-3 rounded-[1.25rem] bg-white px-6 py-12 text-center ring-1 ring-charcoal/[0.06]"
        >
          <span class="grid size-12 place-items-center rounded-xl bg-canvas" aria-hidden="true"><Puzzle class="size-6" /></span>
          <p class="font-display text-lg font-semibold">No plugins installed yet</p>
          <p class="max-w-sm text-sm text-muted">Add one to the server’s plugins folder and restart the API.</p>
          <FButton size="sm" arrow @click="installOpen = true">How to install</FButton>
        </div>

        <p v-else-if="!visible.length" class="rounded-[1.25rem] bg-white px-6 py-10 text-center text-sm text-muted ring-1 ring-charcoal/[0.06]">
          No plugins match this filter.
        </p>

        <div v-else class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <PluginCard
            v-for="p in visible"
            :key="p.id"
            :plugin="p"
            @open="openPlugin"
            @copy-id="copyId"
            @toggle="setEnabled(p, $event)"
            @delete="pendingDelete = p"
          />
        </div>
      </section>

      <!-- Side -->
      <aside class="flex flex-col gap-4">
        <div
          class="relative overflow-hidden rounded-[1.25rem] bg-gradient-to-b from-sage/15 to-white p-6 text-center ring-1 ring-charcoal/[0.05]"
        >
          <span class="mx-auto grid size-12 place-items-center rounded-xl bg-white shadow-soft" aria-hidden="true">
            <Package class="size-6" />
          </span>
          <h2 class="mt-4 font-display text-xl font-bold tracking-[-0.02em]">Build without limits</h2>
          <p class="mt-1.5 text-sm text-muted">Write your own plugin in any language that compiles to WebAssembly.</p>
          <ul class="mt-4 flex flex-col gap-2 text-left text-sm">
            <li v-for="p in perks" :key="p" class="flex items-start gap-2">
              <Check class="mt-0.5 size-4 shrink-0 text-positive" aria-hidden="true" /> {{ p }}
            </li>
          </ul>
          <FButton size="sm" class="mt-5" :href="DOCS_URL" target="_blank">
            Build a plugin <ArrowRight class="size-4" aria-hidden="true" />
          </FButton>
        </div>

        <a
          :href="DOCS_URL"
          target="_blank"
          rel="noopener"
          class="flex items-center gap-3 rounded-[1.25rem] bg-white p-4 ring-1 ring-charcoal/[0.06] transition hover:ring-charcoal/20"
        >
          <span class="grid size-10 shrink-0 place-items-center rounded-xl bg-canvas" aria-hidden="true">
            <CircleHelp class="size-5" />
          </span>
          <span class="min-w-0 flex-1">
            <span class="block text-sm font-medium">Need help?</span>
            <span class="block text-xs text-muted">Read how plugins work.</span>
          </span>
          <ExternalLink class="size-4 shrink-0 text-subtle" aria-hidden="true" />
        </a>
      </aside>
    </div>

    <PluginRunsDialog :plugin="selected" @close="closePlugin" />
    <InstallPluginDialog v-model:open="installOpen" />
    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="`Delete “${pendingDelete?.name ?? ''}”?`"
      message="Its files are removed from the server, and it stops running at once. Pages that use its blocks keep them, but they won’t show until it’s installed again."
      confirm-label="Delete plugin"
      :busy="isDeleting"
      @confirm="confirmDelete"
    />
  </div>
</template>
