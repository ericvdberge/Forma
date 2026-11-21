<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { cn } from '@forma/design-system'
import CanvasSection from '@/components/studio/CanvasSection.vue'
import { provideCanvas, readOnlyCanvas } from '@/studio/canvas'
import { frameWidth } from '@/studio/responsive'
import type { Breakpoint, PageDocument } from '@/studio/types'

/**
 * One page as visitors see it, drawn by the studio's own section renderers in a device-sized frame.
 * Links to the website's own pages (`/about`) are handed to the parent instead of leaving the preview.
 */
const props = defineProps<{ document: PageDocument; device: Breakpoint }>()
const emit = defineEmits<{ navigate: [path: string] }>()

const zoom = ref(1)
provideCanvas(readOnlyCanvas(zoom))

/* Fit the device frame into the available width. */
const viewport = ref<HTMLElement | null>(null)
const available = ref(0)
let resizer: ResizeObserver | undefined

onMounted(() => {
  resizer = new ResizeObserver(([entry]) => (available.value = entry?.contentRect.width ?? 0))

  if (viewport.value)
    resizer.observe(viewport.value)
})

onBeforeUnmount(() => resizer?.disconnect())

/* Desktop fills the window like a real browser; tablet and phone are device-sized frames. */
const fullWidth = computed(() => props.device === 'desktop')

const width = computed(() => (fullWidth.value ? '100%' : `${frameWidth[props.device]}px`))

watch(
  [available, () => props.device],
  ([a, device]) => {
    if (fullWidth.value)
      zoom.value = 1
    else if (a)
      zoom.value = Math.min(1, Math.max(0.2, (a - 32) / frameWidth[device]))
  },
  { immediate: true },
)

/** Another page of this site: tell the parent. An anchor on this page: scroll to it. Anything else: a new tab. */
function onClick(e: MouseEvent) {
  const link = (e.target as HTMLElement).closest('a')
  const href = link?.getAttribute('href')

  if (!link || !href || e.defaultPrevented)
    return

  if (href.startsWith('#')) {
    e.preventDefault()
    scrollToAnchor(href.slice(1))
    return
  }

  if (isSitePath(href)) {
    e.preventDefault()
    emit('navigate', href)
    return
  }

  link.target = '_blank'
  link.rel = 'noopener noreferrer'
}

/** A link to one of the website's pages, like `/about` (not `//other.host`). */
const isSitePath = (href: string) => href.startsWith('/') && !href.startsWith('//')

function scrollToAnchor(id: string) {
  if (!id)
    return viewport.value?.scrollTo({ top: 0, behavior: 'smooth' })

  viewport.value?.querySelector(`[id="${CSS.escape(id)}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

/** Start each page at its top, and at the anchor when a link pointed to one. */
function showFromTop(hash = '') {
  viewport.value?.scrollTo({ top: 0 })

  if (hash)
    requestAnimationFrame(() => scrollToAnchor(hash))
}

defineExpose({ showFromTop })
</script>

<template>
  <div ref="viewport" :class="cn('flex min-h-0 flex-1 flex-col overflow-auto', fullWidth ? 'bg-[#f7f7f5]' : 'bg-[#ecebe8] p-4')">
    <div :class="cn('mx-auto origin-top', fullWidth && 'flex-1')" :style="{ width, zoom }">
      <div
        :class="
          cn(
            'relative flex flex-col gap-3 bg-[#f7f7f5] p-3 font-sans text-charcoal',
            fullWidth ? 'min-h-full' : 'min-h-[640px] rounded-[28px] shadow-card ring-1 ring-charcoal/[0.06]',
          )
        "
        @click="onClick"
      >
        <CanvasSection v-for="section in document.sections" :key="section.id" :section="section" :bp="device" :editing="false" />
      </div>
    </div>
  </div>
</template>
