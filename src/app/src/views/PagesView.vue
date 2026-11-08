<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Archive, ArchiveRestore, Ellipsis, FileText, Plus, Search, Trash2, Upload, Undo2, X } from 'lucide-vue-next'
import { FAvatar, FButton, FCard, FIconButton, FInput, FSelect, FStatus, cn } from '@forma/design-system'
import PageHeader from '@/components/PageHeader.vue'
import { useCms } from '@/stores/cms'
import type { PublishStatus } from '@/data/types'
import { timeAgo } from '@/utils/time'

const cms = useCms()
const { state, activeWebsite } = cms
const route = useRoute()
const router = useRouter()

/* Website switcher */
const websiteId = computed({
  get: () => activeWebsite.value.id,
  set: (id: string) => cms.setActiveWebsite(id),
})
const websiteOptions = computed(() => state.websites.map((w) => ({ label: w.name, value: w.id })))

/* Filters */
type Filter = 'all' | PublishStatus
const filter = ref<Filter>('all')
const search = ref(typeof route.query.q === 'string' ? route.query.q : '')
watch(
  () => route.query.q,
  (q) => {
    if (typeof q === 'string') search.value = q
  },
)

const sitePages = computed(() => cms.pagesFor(activeWebsite.value.id))
const tabs = computed(() =>
  (['all', 'live', 'draft', 'scheduled', 'archived'] as const).map((value) => ({
    value,
    label: value === 'all' ? 'All' : value[0]!.toUpperCase() + value.slice(1),
    count: value === 'all' ? sitePages.value.length : sitePages.value.filter((p) => p.status === value).length,
  })),
)
const visible = computed(() => {
  const q = search.value.trim().toLowerCase()
  return sitePages.value
    .filter((p) => filter.value === 'all' || p.status === filter.value)
    .filter((p) => !q || `${p.title} ${p.path}`.toLowerCase().includes(q))
    .slice()
    .sort((a, b) => b.updatedAt - a.updatedAt)
})

/* New page */
const creating = ref(route.query.new === '1')
const newTitle = ref('')
const newPath = ref('')
const formEl = ref<HTMLElement | null>(null)
function openCreate() {
  creating.value = true
  nextTick(() => formEl.value?.querySelector('input')?.focus())
}
function closeCreate() {
  creating.value = false
  newTitle.value = ''
  newPath.value = ''
  if (route.query.new) router.replace({ query: { ...route.query, new: undefined } })
}
function submitCreate() {
  if (!newTitle.value.trim()) return
  cms.createPage(activeWebsite.value.id, newTitle.value.trim(), newPath.value)
  filter.value = 'all'
  closeCreate()
}
onMounted(() => {
  if (creating.value) openCreate()
})

/* Row menu: fixed-positioned so the table's scroll container doesn't clip it */
const menuFor = ref<string | null>(null)
const menuPos = ref({ top: 0, right: 0 })
function toggleMenu(id: string, e: MouseEvent) {
  if (menuFor.value === id) return (menuFor.value = null)
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  menuPos.value = { top: rect.bottom + 4, right: window.innerWidth - rect.right }
  menuFor.value = id
}
const closeMenu = () => (menuFor.value = null)
const onDocClick = (e: MouseEvent) => {
  if (!(e.target as HTMLElement).closest('[data-row-menu]')) menuFor.value = null
}
const onKey = (e: KeyboardEvent) => {
  if (e.key === 'Escape') menuFor.value = null
}
onMounted(() => {
  document.addEventListener('mousedown', onDocClick)
  document.addEventListener('keydown', onKey)
  document.addEventListener('scroll', closeMenu, true)
  window.addEventListener('resize', closeMenu)
})
onBeforeUnmount(() => {
  document.removeEventListener('mousedown', onDocClick)
  document.removeEventListener('keydown', onKey)
  document.removeEventListener('scroll', closeMenu, true)
  window.removeEventListener('resize', closeMenu)
})

function act(id: string, action: 'publish' | 'unpublish' | 'archive' | 'restore' | 'delete') {
  menuFor.value = null
  if (action === 'delete') {
    const page = state.pages.find((p) => p.id === id)
    if (page && window.confirm(`Delete “${page.title}”? This can’t be undone.`)) cms.deletePage(id)
    return
  }
  const next: Record<typeof action, PublishStatus> = { publish: 'live', unpublish: 'draft', archive: 'archived', restore: 'draft' }
  cms.setPageStatus(id, next[action])
}
</script>

<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Pages">
      <template #meta>
        <p class="mt-3 text-[0.9375rem] text-muted">
          Every page of <span class="font-medium text-charcoal">{{ activeWebsite.name }}</span>, from draft to live.
        </p>
      </template>
      <FSelect v-model="websiteId" label="Website" hide-label size="sm" :options="websiteOptions" class="w-48" />
      <FButton @click="openCreate"><Plus class="size-4" aria-hidden="true" /> New page</FButton>
    </PageHeader>

    <!-- Create -->
    <Transition enter-from-class="opacity-0 -translate-y-2" enter-active-class="transition duration-200">
      <FCard v-if="creating" padding="none" class="mb-6">
        <form ref="formEl" class="flex flex-col gap-4 p-5 sm:flex-row sm:items-end" @submit.prevent="submitCreate">
          <FInput v-model="newTitle" label="Page title" placeholder="e.g. Pricing" />
          <FInput v-model="newPath" label="URL path" :placeholder="`/${newTitle.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-') || 'pricing'}`" />
          <div class="flex shrink-0 gap-2">
            <FButton type="submit" :disabled="!newTitle.trim()">Create page</FButton>
            <FIconButton :icon="X" label="Cancel" @click="closeCreate" />
          </div>
        </form>
      </FCard>
    </Transition>

    <FCard padding="none">
      <!-- Toolbar -->
      <div class="flex flex-col gap-4 border-b border-charcoal/[0.06] p-4 md:flex-row md:items-center md:justify-between">
        <div role="tablist" aria-label="Filter by status" class="flex gap-1 overflow-x-auto">
          <button
            v-for="t in tabs"
            :key="t.value"
            type="button"
            role="tab"
            :aria-selected="filter === t.value"
            :class="
              cn(
                'inline-flex h-9 shrink-0 items-center gap-2 rounded-full px-4 text-sm transition-colors',
                filter === t.value ? 'bg-charcoal text-warm-white' : 'text-charcoal/70 hover:bg-charcoal/5 hover:text-charcoal',
              )
            "
            @click="filter = t.value"
          >
            {{ t.label }}
            <span :class="cn('text-xs', filter === t.value ? 'text-warm-white/60' : 'text-subtle')">{{ t.count }}</span>
          </button>
        </div>
        <div class="relative md:w-72">
          <Search class="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-subtle" aria-hidden="true" />
          <label for="page-search" class="sr-only">Search pages</label>
          <input
            id="page-search"
            v-model="search"
            type="search"
            placeholder="Search pages"
            class="h-9 w-full rounded-full bg-canvas pl-10 pr-4 text-sm outline-none ring-1 ring-transparent transition-shadow placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
          />
        </div>
      </div>

      <!-- Table -->
      <div class="overflow-x-auto">
        <table class="w-full min-w-[640px] text-left text-sm">
          <thead class="text-xs text-muted">
            <tr class="border-b border-charcoal/[0.06]">
              <th scope="col" class="px-5 py-3 font-medium">Page</th>
              <th scope="col" class="px-5 py-3 font-medium">Status</th>
              <th scope="col" class="hidden px-5 py-3 font-medium md:table-cell">Author</th>
              <th scope="col" class="px-5 py-3 font-medium">Updated</th>
              <th scope="col" class="w-14 px-5 py-3"><span class="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="p in visible"
              :key="p.id"
              class="border-b border-charcoal/[0.04] transition-colors last:border-0 hover:bg-warm-white"
            >
              <td class="px-5 py-3.5">
                <div class="flex items-center gap-3">
                  <span class="grid size-9 shrink-0 place-items-center rounded-lg bg-canvas text-charcoal/70">
                    <FileText class="size-4" :stroke-width="1.6" aria-hidden="true" />
                  </span>
                  <div class="min-w-0">
                    <p class="truncate font-medium">{{ p.title }}</p>
                    <p class="truncate text-xs text-muted">{{ activeWebsite.domain }}{{ p.path === '/' ? '' : p.path }}</p>
                  </div>
                </div>
              </td>
              <td class="px-5 py-3.5"><FStatus :status="p.status" /></td>
              <td class="hidden px-5 py-3.5 md:table-cell">
                <span class="inline-flex items-center gap-2"><FAvatar :name="p.author" size="xs" /> {{ p.author }}</span>
              </td>
              <td class="whitespace-nowrap px-5 py-3.5 text-muted">{{ timeAgo(p.updatedAt) }}</td>
              <td class="px-5 py-3.5">
                <div data-row-menu class="flex justify-end">
                  <FIconButton
                    :icon="Ellipsis"
                    :label="`Actions for ${p.title}`"
                    size="sm"
                    :aria-expanded="menuFor === p.id"
                    @click="toggleMenu(p.id, $event)"
                  />
                  <div
                    v-if="menuFor === p.id"
                    role="menu"
                    :style="{ top: `${menuPos.top}px`, right: `${menuPos.right}px` }"
                    class="fixed z-40 w-48 rounded-2xl bg-white p-1.5 ring-1 ring-charcoal/[0.06] shadow-float"
                  >
                    <button
                      v-if="p.status !== 'live'"
                      role="menuitem"
                      class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-sm hover:bg-canvas"
                      @click="act(p.id, 'publish')"
                    >
                      <Upload class="size-4" aria-hidden="true" /> Publish
                    </button>
                    <button
                      v-if="p.status === 'live' || p.status === 'scheduled'"
                      role="menuitem"
                      class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-sm hover:bg-canvas"
                      @click="act(p.id, 'unpublish')"
                    >
                      <Undo2 class="size-4" aria-hidden="true" /> Revert to draft
                    </button>
                    <button
                      v-if="p.status !== 'archived'"
                      role="menuitem"
                      class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-sm hover:bg-canvas"
                      @click="act(p.id, 'archive')"
                    >
                      <Archive class="size-4" aria-hidden="true" /> Archive
                    </button>
                    <button
                      v-else
                      role="menuitem"
                      class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-sm hover:bg-canvas"
                      @click="act(p.id, 'restore')"
                    >
                      <ArchiveRestore class="size-4" aria-hidden="true" /> Restore
                    </button>
                    <div class="my-1 h-px bg-charcoal/[0.06]" />
                    <button
                      role="menuitem"
                      class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-sm text-danger hover:bg-canvas"
                      @click="act(p.id, 'delete')"
                    >
                      <Trash2 class="size-4" aria-hidden="true" /> Delete
                    </button>
                  </div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="!visible.length" class="flex flex-col items-center gap-3 px-6 py-16 text-center">
        <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><FileText class="size-5" aria-hidden="true" /></span>
        <p class="font-medium">No pages here yet</p>
        <p class="max-w-xs text-sm text-muted">
          {{ search ? 'Try a different search.' : 'Create a page to start building out this website.' }}
        </p>
        <FButton v-if="!search" size="sm" variant="secondary" @click="openCreate">New page</FButton>
      </div>
    </FCard>
  </div>
</template>
