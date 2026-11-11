<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Ellipsis, FileText, Pencil, Plus, Search, Trash2, TriangleAlert, Upload, Undo2, X } from 'lucide-vue-next'
import { FButton, FCard, FIconButton, FInput, FSelect, FSkeleton, FStatus, cn } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useWebsites } from '@/composables/useWebsites'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { filterPages, usePages } from '@/composables/usePages'
import type { PageStatus } from '@/data/types'
import { timeAgo } from '@/utils/time'

const { getWebsites } = useWebsites()
const { websites } = getWebsites()
const { getActiveWebsite, setActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
const { getPages, createPage, setPageStatus, deletePage } = usePages()
const { pages, isPending, isError, isFetching, refetch } = getPages(() => activeWebsite.value.id)
const route = useRoute()
const router = useRouter()

/* Website switcher */
const websiteId = computed({
  get: () => activeWebsite.value.id,
  set: (id: string) => setActiveWebsite(id),
})
const websiteOptions = computed(() => websites.value.map((w) => ({ label: w.name, value: w.id })))

/* Filters: title search, then status tabs */
type Filter = 'all' | PageStatus
const filter = ref<Filter>('all')
const search = ref(typeof route.query.q === 'string' ? route.query.q : '')
watch(
  () => route.query.q,
  (q) => {
    if (typeof q === 'string') search.value = q
  },
)

/** Pages whose title matches the search; the tab counts are within these. */
const searched = computed(() => filterPages(pages.value, { title: search.value }))
const tabs = computed(() =>
  (['all', 'live', 'draft'] as const).map((value) => ({
    value,
    label: { all: 'All', live: 'Published', draft: 'Draft' }[value],
    count: value === 'all' ? searched.value.length : filterPages(searched.value, { status: value }).length,
  })),
)
const visible = computed(() =>
  filter.value === 'all' ? searched.value : filterPages(searched.value, { status: filter.value }),
)

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
  createPage(activeWebsite.value.id, newTitle.value.trim(), newPath.value)
  filter.value = 'all'
  search.value = ''
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

function act(id: string, action: 'publish' | 'unpublish' | 'delete') {
  menuFor.value = null
  if (action === 'delete') {
    const page = pages.value.find((p) => p.id === id)
    if (page && window.confirm(`Delete “${page.title}”? This can’t be undone.`)) deletePage(id)
    return
  }
  setPageStatus(id, action === 'publish' ? 'live' : 'draft')
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
      <FButton :disabled="isPending || isError" @click="openCreate"><Plus class="size-4" aria-hidden="true" /> New page</FButton>
    </PageHeader>

    <!-- Create -->
    <Transition enter-from-class="opacity-0 -translate-y-2" enter-active-class="transition duration-200">
      <FCard v-if="creating && !isPending && !isError" padding="none" class="mb-6">
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
          <label for="page-search" class="sr-only">Search pages by title</label>
          <input
            id="page-search"
            v-model="search"
            type="search"
            placeholder="Search by title"
            class="h-9 w-full rounded-full bg-canvas pl-10 pr-4 text-sm outline-none ring-1 ring-transparent transition-shadow placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
          />
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isPending" aria-busy="true" aria-label="Loading pages">
        <div
          v-for="i in 4"
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
          <FSkeleton class="h-4 w-20" />
          <FSkeleton class="size-8" rounded="full" />
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="isError" class="flex flex-col items-center gap-3 px-6 py-16 text-center">
        <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><TriangleAlert class="size-5" aria-hidden="true" /></span>
        <p class="font-medium">Couldn’t load the pages</p>
        <p class="max-w-xs text-sm text-muted">Check that the Forma API is running, then try again.</p>
        <FButton size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
      </div>

      <template v-else>
        <!-- Table -->
        <div class="overflow-x-auto">
          <table class="w-full min-w-[560px] text-left text-sm">
            <thead class="text-xs text-muted">
              <tr class="border-b border-charcoal/[0.06]">
                <th scope="col" class="px-5 py-3 font-medium">Page</th>
                <th scope="col" class="px-5 py-3 font-medium">Status</th>
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
                      <RouterLink :to="{ name: 'studio', params: { pageId: p.id } }" class="block truncate font-medium hover:underline">
                        {{ p.title }}
                      </RouterLink>
                      <p class="truncate text-xs text-muted">{{ activeWebsite.domain }}{{ p.path }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-5 py-3.5"><FStatus :status="p.status" :label="p.status === 'live' ? 'Published' : undefined" /></td>
                <td class="whitespace-nowrap px-5 py-3.5 text-muted">{{ timeAgo(p.updatedAt) }}</td>
                <td class="px-5 py-3.5">
                  <div data-row-menu class="flex items-center justify-end gap-1">
                    <LinkButton
                      :to="{ name: 'studio', params: { pageId: p.id } }"
                      size="sm"
                      variant="secondary"
                      :aria-label="`Edit ${p.title}`"
                    >
                      <Pencil class="size-3.5" aria-hidden="true" /> Edit
                    </LinkButton>
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
                        v-else
                        role="menuitem"
                        class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-sm hover:bg-canvas"
                        @click="act(p.id, 'unpublish')"
                      >
                        <Undo2 class="size-4" aria-hidden="true" /> Revert to draft
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
          <p class="font-medium">{{ search ? 'No pages match' : 'No pages here yet' }}</p>
          <p class="max-w-xs text-sm text-muted">
            {{ search ? `No page title contains “${search.trim()}”.` : 'Create a page to start building out this website.' }}
          </p>
          <FButton v-if="!search" size="sm" variant="secondary" @click="openCreate">New page</FButton>
        </div>
      </template>
    </FCard>
  </div>
</template>
