<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { FileText, TriangleAlert, X } from 'lucide-vue-next'
import { FButton, FSkeleton, cn } from '@forma/design-system'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { usePages } from '@/composables/usePages'
import { createStudio, provideStudio } from '@/composables/useStudio'
import type { StudioPageSeed } from '@/queriers/useStudioQuerier'
import StudioCanvas from '@/components/studio/StudioCanvas.vue'
import StudioInspector from '@/components/studio/StudioInspector.vue'
import StudioLibrary from '@/components/studio/StudioLibrary.vue'
import StudioToolbar from '@/components/studio/StudioToolbar.vue'

const route = useRoute()
const router = useRouter()
const { getActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
const { getPages } = usePages()
const pagesQuery = getPages(() => activeWebsite.value.id)
const { pages } = pagesQuery

/* Which page: the one in the URL, else the website's Home page (or its first page). */
const pageId = computed(() => (typeof route.params.pageId === 'string' ? route.params.pageId : ''))
const page = computed(() => pages.value.find((p) => p.id === pageId.value))
watch(
  [pageId, pages, () => pagesQuery.isPending.value],
  () => {
    if (pagesQuery.isPending.value || !pages.value.length) return
    if (!page.value) {
      const fallback = pages.value.find((p) => p.slug === 'home') ?? pages.value.at(-1)!
      router.replace({ name: 'studio', params: { pageId: fallback.id } })
    }
  },
  { immediate: true },
)
watch(page, (p) => {
  if (p) document.title = `${p.title} · Design Studio · Forma`
})

const seed = computed<StudioPageSeed | undefined>(() =>
  page.value
    ? { title: page.value.title, slug: page.value.slug, websiteName: activeWebsite.value.name, published: page.value.status === 'live' }
    : undefined,
)
const studio = createStudio(() => (page.value ? pageId.value : ''), seed)
provideStudio(studio)

const pageOptions = computed(() =>
  [...pages.value].sort((a, b) => a.title.localeCompare(b.title)).map((p) => ({ label: p.title, value: p.id })),
)

/* Library and inspector are drawers below the xl breakpoint. */
const drawer = ref<null | 'library' | 'inspector'>(null)
watch(
  () => studio.drag.value,
  (d) => {
    if (d && drawer.value === 'library') drawer.value = null
  },
)

/* Keyboard shortcuts */
function isTyping(el: EventTarget | null) {
  const node = el as HTMLElement | null
  return !!node && (node.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(node.tagName))
}
function onKey(e: KeyboardEvent) {
  const mod = e.metaKey || e.ctrlKey
  const inText = (e.target as HTMLElement | null)?.isContentEditable
  // Rich text has no undo of its own: the studio history covers it.
  if (mod && e.key.toLowerCase() === 'z' && (!isTyping(e.target) || inText)) {
    e.preventDefault()
    if (e.shiftKey) studio.redo()
    else studio.undo()
    return
  }
  if (mod && e.key.toLowerCase() === 'y' && (!isTyping(e.target) || inText)) {
    e.preventDefault()
    studio.redo()
    return
  }
  if (e.key === 'Escape') {
    if (inText) (e.target as HTMLElement).blur()
    if (studio.preview.value) studio.preview.value = false
    else studio.select(null)
    return
  }
  if (isTyping(e.target) || studio.preview.value) {
    if (e.key.toLowerCase() === 'p' && !isTyping(e.target)) studio.preview.value = false
    return
  }
  if (!studio.selection.value) {
    if (e.key.toLowerCase() === 'p' && !mod) studio.preview.value = true
    return
  }
  if (e.key === 'Delete' || e.key === 'Backspace') {
    e.preventDefault()
    studio.remove()
  } else if (mod && e.key.toLowerCase() === 'd') {
    e.preventDefault()
    studio.duplicate()
  } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
    e.preventDefault()
    studio.nudge(e.key === 'ArrowUp' ? -1 : 1)
  } else if (e.key.toLowerCase() === 'p' && !mod) {
    studio.preview.value = true
  }
}

/* Drafts live in the query cache, so they survive navigating around the app but not a reload. */
function onBeforeUnload(e: BeforeUnloadEvent) {
  if (studio.hasChanges.value) e.preventDefault()
}
onMounted(() => {
  window.addEventListener('keydown', onKey)
  window.addEventListener('beforeunload', onBeforeUnload)
})
onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKey)
  window.removeEventListener('beforeunload', onBeforeUnload)
})

const loading = computed(() => pagesQuery.isPending.value || (!!page.value && studio.query.isPending.value))
</script>

<template>
  <div class="-mx-4 -mb-10 h-[calc(100dvh-6rem)] sm:mx-0 sm:mb-0 sm:h-[calc(100dvh-8.5rem)]">
    <!-- No pages -->
    <div v-if="!pagesQuery.isPending.value && !pages.length" class="flex h-full flex-col items-center justify-center gap-3 text-center">
      <span class="grid size-12 place-items-center rounded-2xl bg-canvas">
        <component :is="pagesQuery.isError.value ? TriangleAlert : FileText" class="size-5" aria-hidden="true" />
      </span>
      <p class="font-medium">{{ pagesQuery.isError.value ? 'Couldn’t load the pages' : `${activeWebsite.name} has no pages yet` }}</p>
      <p class="max-w-xs text-sm text-muted">
        {{ pagesQuery.isError.value ? 'Check that the Forma API is running, then try again.' : 'Create a page first, then design it here.' }}
      </p>
      <FButton v-if="pagesQuery.isError.value" size="sm" variant="secondary" @click="pagesQuery.refetch()">Try again</FButton>
      <FButton v-else size="sm" @click="router.push({ name: 'pages', query: { new: '1' } })">New page</FButton>
    </div>

    <div v-else class="flex h-full gap-4">
      <!-- Library: a column on wide screens, a drawer otherwise -->
      <div
        v-if="drawer"
        class="fixed inset-0 z-40 bg-charcoal/30 xl:hidden"
        aria-hidden="true"
        @click="drawer = null"
      />
      <div
        :class="
          cn(
            'overflow-hidden bg-white xl:static xl:z-auto xl:flex xl:w-[272px] xl:shrink-0 xl:translate-x-0 xl:rounded-panel xl:shadow-soft xl:ring-1 xl:ring-charcoal/[0.06]',
            'fixed inset-y-0 left-0 z-50 w-[300px] max-w-[88vw] shadow-float transition-transform duration-300',
            drawer === 'library' ? 'translate-x-0' : 'invisible -translate-x-full xl:visible',
          )
        "
      >
        <button
          type="button"
          class="absolute right-3 top-4 z-10 grid size-8 place-items-center rounded-full hover:bg-canvas xl:hidden"
          aria-label="Close library"
          @click="drawer = null"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
        <StudioLibrary class="w-full" :page-title="page?.title ?? '…'" />
      </div>

      <!-- Canvas -->
      <div class="flex min-w-0 flex-1 flex-col overflow-hidden bg-white sm:rounded-panel sm:shadow-soft sm:ring-1 sm:ring-charcoal/[0.06]">
        <StudioToolbar
          :pages="pageOptions"
          :page-id="pageId"
          @page="router.push({ name: 'studio', params: { pageId: $event } })"
          @library="drawer = 'library'"
          @inspector="drawer = 'inspector'"
        />
        <div v-if="loading" class="flex-1 bg-[#ecebe8] p-4" aria-busy="true" aria-label="Loading page">
          <div class="mx-auto flex max-w-[1200px] flex-col gap-3 rounded-2xl bg-[#f7f7f5] p-3">
            <FSkeleton class="h-[420px]" />
            <FSkeleton class="h-28" />
            <FSkeleton class="h-64" />
          </div>
        </div>
        <div v-else-if="studio.query.isError.value" class="flex flex-1 flex-col items-center justify-center gap-3 text-center">
          <TriangleAlert class="size-5" aria-hidden="true" />
          <p class="font-medium">Couldn’t open this page</p>
          <FButton size="sm" variant="secondary" @click="studio.query.refetch()">Try again</FButton>
        </div>
        <StudioCanvas v-else />
      </div>

      <!-- Inspector -->
      <div
        :class="
          cn(
            'overflow-hidden bg-white xl:static xl:z-auto xl:flex xl:w-[300px] xl:shrink-0 xl:translate-x-0 xl:rounded-panel xl:shadow-soft xl:ring-1 xl:ring-charcoal/[0.06]',
            'fixed inset-y-0 right-0 z-50 flex w-[320px] max-w-[88vw] flex-col shadow-float transition-transform duration-300',
            drawer === 'inspector' ? 'translate-x-0' : 'invisible translate-x-full xl:visible',
          )
        "
      >
        <button
          type="button"
          class="absolute right-3 top-3 z-10 grid size-8 place-items-center rounded-full bg-white hover:bg-canvas xl:hidden"
          aria-label="Close inspector"
          @click="drawer = null"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
        <StudioInspector class="w-full" />
      </div>
    </div>
  </div>
</template>
