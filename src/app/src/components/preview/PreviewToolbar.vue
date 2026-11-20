<script setup lang="ts">
import { computed } from 'vue'
import { ArrowLeft, Globe, Monitor, Pencil, Smartphone, Tablet } from 'lucide-vue-next'
import { FLogo, FSelect, cn } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
import type { Page, Website } from '@/data/types'
import type { Breakpoint } from '@/studio/types'

/** The website preview's top bar: back to Forma, the address being shown, the page and the device. */
const props = defineProps<{
  website: Website
  pages: Page[]
  page: Page | undefined
  /** The site path being shown, e.g. `/about`, also when no page has it. */
  path: string
}>()

const device = defineModel<Breakpoint>('device', { required: true })
const emit = defineEmits<{ go: [page: Page] }>()

const devices: Array<{ value: Breakpoint; label: string; icon: typeof Monitor }> = [
  { value: 'desktop', label: 'Computer', icon: Monitor },
  { value: 'tablet', label: 'Tablet', icon: Tablet },
  { value: 'mobile', label: 'Phone', icon: Smartphone },
]

const pageOptions = computed(() => props.pages.map((p) => ({ label: p.title, value: p.id })))

const pageId = computed({
  get: () => props.page?.id ?? '',
  set: (id: string) => {
    const next = props.pages.find((p) => p.id === id)

    if (next)
      emit('go', next)
  },
})

const address = computed(() => `${props.website.domain}${props.path}`)

const textBtn =
  'inline-flex h-10 shrink-0 items-center gap-2 rounded-xl px-3 text-sm font-medium text-charcoal/80 transition-colors hover:bg-charcoal/5 hover:text-charcoal'
</script>

<template>
  <header class="flex flex-wrap items-center gap-x-3 gap-y-2 border-b border-charcoal/[0.06] bg-white px-3 py-2.5 sm:px-4">
    <RouterLink :to="{ name: 'pages' }" :class="textBtn" title="Back to all pages">
      <ArrowLeft class="size-5" :stroke-width="1.6" aria-hidden="true" />
      <span>Pages</span>
    </RouterLink>
    <FLogo variant="mark" class="hidden h-6 lg:block" />

    <div class="flex min-w-0 flex-1 items-center gap-2 rounded-xl bg-canvas/80 px-3 py-2 text-sm sm:max-w-md" :title="address">
      <Globe class="size-4 shrink-0 text-muted" :stroke-width="1.6" aria-hidden="true" />
      <span class="truncate"><span class="text-muted">{{ website.domain }}</span>{{ path }}</span>
    </div>

    <FSelect v-if="pages.length" v-model="pageId" label="Page" hide-label size="sm" :options="pageOptions" class="w-40" />

    <div role="radiogroup" aria-label="Show the website on" class="flex gap-1 rounded-xl bg-canvas/80 p-1">
      <button
        v-for="d in devices"
        :key="d.value"
        type="button"
        role="radio"
        :aria-checked="device === d.value"
        :title="`Show on a ${d.label.toLowerCase()}`"
        :class="
          cn(
            'grid size-9 place-items-center rounded-lg transition-colors',
            device === d.value ? 'bg-white text-charcoal shadow-soft' : 'text-muted hover:text-charcoal',
          )
        "
        @click="device = d.value"
      >
        <component :is="d.icon" class="size-[18px]" :stroke-width="1.6" aria-hidden="true" />
        <span class="sr-only">{{ d.label }}</span>
      </button>
    </div>

    <LinkButton v-if="page" :to="{ name: 'studio', params: { pageId: page.id } }" size="sm" variant="secondary" class="ml-auto">
      <Pencil class="size-3.5" aria-hidden="true" /> Edit this page
    </LinkButton>
  </header>
</template>
