<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { TriangleAlert, X } from 'lucide-vue-next'
import { FButton, FSkeleton, cn } from '@forma/design-system'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { usePages } from '@/composables/usePages'
import { useToast } from '@/composables/useToast'
import { createStudio, provideStudio } from '@/composables/useStudio'
import type { StudioPageSeed } from '@/queriers/useStudioQuerier'
import AddDialog from './AddDialog.vue'
import StudioCanvas from './StudioCanvas.vue'
import StudioInspector from './StudioInspector.vue'
import StudioLibrary from './StudioLibrary.vue'
import StudioToolbar from './StudioToolbar.vue'
import AppToast from '@/components/shell/AppToast.vue'

/** The full-screen page editor. Rendered by StudioView once websites have loaded. */

const route = useRoute()
const router = useRouter()
const { getActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
const { getPages } = usePages()
const { notify } = useToast()
const pagesQuery = getPages(() => activeWebsite.value.id)
const { pages } = pagesQuery

/* The page from the URL. One that isn't in the active website (deleted, or another website's) goes back to Pages. */
const pageId = computed(() => (typeof route.params.pageId === 'string' ? route.params.pageId : ''))
const page = computed(() => pages.value.find((p) => p.id === pageId.value))
watch(
  [pageId, pages, () => pagesQuery.isPending.value],
  () => {
    if (pagesQuery.isPending.value || pagesQuery.isError.value) return
    if (!page.value) {
      notify('That page isn’t in this website')
      router.replace({ name: 'pages' })
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


/*
 * Advanced mode: the library is a column on wide screens and a drawer otherwise.
 * Simple mode: no library; "Add" opens the plain-language Add dialog instead.
 * The inspector is a column on wide screens and a drawer otherwise.
 */
const drawer = ref<null | 'library' | 'inspector'>(null)
const advanced = computed(() => studio.mode.value === 'advanced')
function openAdd() {
  if (advanced.value) return (drawer.value = 'library')
  const d = studio.doc.value
  const sel = studio.selection.value
  const after = sel ? (d?.sections.findIndex((s) => s.id === sel.sectionId) ?? -1) + 1 : 0
  studio.adding.value = { mode: 'section', index: after || (d?.sections.length ?? 0) }
}
// Selecting something on a smaller screen in simple mode opens its settings right away.
watch(
  () => studio.selection.value,
  (sel) => {
    if (sel && !advanced.value && window.innerWidth < 1280 && drawer.value !== 'inspector') drawer.value = 'inspector'
  },
)
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

const loading = computed(() => pagesQuery.isPending.value || !page.value || studio.query.isPending.value)
</script>

<template>
  <div class="flex h-dvh flex-col bg-[#ecebe8]">
    <StudioToolbar
      :page-title="page?.title ?? ''"
      :page-path="page ? `${activeWebsite.domain}/${page.slug}` : ''"
      @library="openAdd"
      @inspector="drawer = 'inspector'"
    />

    <div class="relative flex min-h-0 flex-1">
      <div v-if="drawer" class="fixed inset-0 z-40 bg-charcoal/30 xl:hidden" aria-hidden="true" @click="drawer = null" />

      <!-- Library (advanced mode) -->
      <div
        v-if="advanced"
        :class="
          cn(
            'overflow-hidden bg-white xl:static xl:z-auto xl:flex xl:w-[280px] xl:shrink-0 xl:translate-x-0 xl:border-r xl:border-charcoal/[0.06] xl:shadow-none',
            'fixed inset-y-0 left-0 z-50 w-[320px] max-w-[88vw] shadow-float transition-transform duration-300',
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
      <div class="flex min-w-0 flex-1 flex-col">
        <div v-if="loading" class="flex-1 p-4" aria-busy="true" aria-label="Loading page">
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

      <!-- Settings for the selection -->
      <div
        :class="
          cn(
            'overflow-hidden bg-white xl:static xl:z-auto xl:flex xl:w-[340px] xl:shrink-0 xl:translate-x-0 xl:border-l xl:border-charcoal/[0.06] xl:shadow-none',
            'fixed inset-y-0 right-0 z-50 flex w-[360px] max-w-[92vw] flex-col shadow-float transition-transform duration-300',
            drawer === 'inspector' ? 'translate-x-0' : 'invisible translate-x-full xl:visible',
          )
        "
      >
        <button
          type="button"
          class="absolute right-3 top-3 z-10 grid size-8 place-items-center rounded-full bg-white hover:bg-canvas xl:hidden"
          aria-label="Close settings"
          @click="drawer = null"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
        <StudioInspector class="w-full" />
      </div>
    </div>

    <AddDialog
      v-if="studio.adding.value"
      :mode="studio.adding.value.mode"
      @close="studio.adding.value = null"
      @pick="studio.addPicked($event); drawer = null"
    />
    <AppToast />
  </div>
</template>
