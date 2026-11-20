<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { FileQuestion, Pencil, TriangleAlert } from 'lucide-vue-next'
import { FButton, FSkeleton } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
import PreviewFrame from '@/components/preview/PreviewFrame.vue'
import PreviewToolbar from '@/components/preview/PreviewToolbar.vue'
import { pageForPath, useWebsitePreview } from '@/composables/useWebsitePreview'
import type { Page } from '@/data/types'
import type { Breakpoint } from '@/studio/types'

/**
 * Full-screen preview of a website as visitors will see it: each page's published content, with links between the
 * pages working. Unpublished edits stay in the Design Studio until they are published.
 */
const route = useRoute()
const router = useRouter()

const websiteId = computed(() => String(route.params.websiteId))
const path = computed(() => `/${route.params.slug ?? ''}`)

const { website, pages, page, document, isPending, isError, refetch } = useWebsitePreview(websiteId, path)

// On a phone, show the phone layout rather than a tiny desktop.
const device = ref<Breakpoint>(window.innerWidth < 640 ? 'mobile' : 'desktop')

const frame = ref<InstanceType<typeof PreviewFrame> | null>(null)

/** Moves the preview to a site path such as `/about#team`. */
function navigate(href: string) {
  const [pathPart = '', hash = ''] = href.split('#')
  const target = pageForPath(pages.value, pathPart)

  router.push({
    name: 'preview',
    params: { websiteId: websiteId.value, slug: target?.slug ?? pathPart.replace(/^\/+|\/+$/g, '') },
    hash: hash ? `#${hash}` : '',
  })
}

function go(next: Page) {
  navigate(next.path)
}

watch(
  () => route.fullPath,
  async () => {
    await nextTick()
    frame.value?.showFromTop(route.hash.slice(1))
  },
)

/* The tab shows the page and website, after the router has set its own title. */
watch(
  [website, page, () => route.fullPath],
  ([w, p]) => {
    if (w)
      setTitle(p ? `${p.title} · ${w.name}` : w.name)
  },
  { immediate: true, flush: 'post' },
)

function setTitle(title: string) {
  window.document.title = `${title} · Preview · Forma`
}
</script>

<template>
  <div v-if="isPending" class="flex h-dvh flex-col bg-[#ecebe8]" role="status" aria-busy="true">
    <span class="sr-only">Loading the preview…</span>
    <div class="h-16 border-b border-charcoal/[0.06] bg-white" />
    <div class="flex-1 p-4"><FSkeleton class="mx-auto h-full max-w-[1200px]" /></div>
  </div>

  <div v-else-if="isError || !website" role="alert" class="flex h-dvh flex-col items-center justify-center gap-4 p-6 text-center">
    <TriangleAlert class="size-6 text-danger" aria-hidden="true" />
    <p class="font-medium">{{ isError ? 'We couldn’t load this website' : 'This website doesn’t exist' }}</p>
    <FButton v-if="isError" size="sm" variant="secondary" @click="refetch()">Try again</FButton>
    <LinkButton v-else size="sm" to="/websites">Go to websites</LinkButton>
  </div>

  <div v-else class="flex h-dvh flex-col">
    <PreviewToolbar v-model:device="device" :website="website" :pages="pages" :page="page" :path="path" @go="go" />

    <PreviewFrame v-if="document?.sections.length" ref="frame" :document="document" :device="device" @navigate="navigate" />

    <div v-else class="flex flex-1 flex-col items-center justify-center gap-4 bg-[#ecebe8] p-6 text-center">
      <span class="grid size-14 place-items-center rounded-2xl bg-white shadow-soft">
        <FileQuestion class="size-6" :stroke-width="1.6" aria-hidden="true" />
      </span>

      <template v-if="!page">
        <div>
          <p class="text-xl font-bold">{{ pages.length ? `There’s no page at ${path}` : 'This website has no pages yet' }}</p>
          <p class="mt-1 text-muted">Visitors following this link would land on a “not found” page.</p>
        </div>
        <FButton v-if="pages.length" variant="secondary" @click="go(pages[0]!)">Go to {{ pages[0]!.title }}</FButton>
        <LinkButton v-else to="/pages?new=1">Create a page</LinkButton>
      </template>

      <template v-else>
        <div>
          <p class="text-xl font-bold">{{ page.title }} isn’t published yet</p>
          <p class="mt-1 max-w-md text-muted">Open it in the Design Studio and choose Publish. Visitors see the page once it’s published.</p>
        </div>
        <LinkButton :to="{ name: 'studio', params: { pageId: page.id } }">
          <Pencil class="size-4" aria-hidden="true" /> Open in the Design Studio
        </LinkButton>
      </template>
    </div>
  </div>
</template>
