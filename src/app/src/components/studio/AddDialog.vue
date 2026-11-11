<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, type Component } from 'vue'
import { Bookmark, Search, X } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useStudio, type DragPayload } from '@/composables/useStudio'
import { blockDefinition, libraryBlocks } from '@/studio/blocks'
import { globalElements, sectionTemplates } from '@/studio/sections'
import type { BlockType } from '@/studio/types'

/**
 * A plain-language picker for adding content: ready-made sections first, then single blocks.
 * `section` adds a new section to the page; `block` adds into the selected section.
 */
const props = defineProps<{ mode: 'section' | 'block' }>()
const emit = defineEmits<{ close: []; pick: [payload: DragPayload] }>()
const studio = useStudio()

const blockHelp: Record<BlockType, string> = {
  heading: 'A title or headline',
  text: 'A paragraph of text',
  image: 'One picture',
  gallery: 'A grid of pictures',
  video: 'A YouTube or Vimeo video',
  button: 'A button that links somewhere',
  divider: 'A thin line to separate content',
  spacer: 'Empty space',
  badge: 'A small label, like “New”',
  form: 'A contact form visitors can fill in',
  map: 'A map of an address',
  carousel: 'Pictures that slide one by one',
  faq: 'Questions with answers that open',
  testimonials: 'Quotes from happy customers',
  features: 'A few highlights with icons',
  nav: 'A menu with links',
}

interface Choice {
  key: string
  label: string
  help: string
  icon: Component
  payload: DragPayload
}

const query = ref('')
const matches = (c: Choice) => `${c.label} ${c.help}`.toLowerCase().includes(query.value.trim().toLowerCase())

const sections = computed<Choice[]>(() =>
  props.mode === 'section'
    ? [...sectionTemplates, ...globalElements]
        .map((t) => ({ key: t.id, label: t.label, help: t.description, icon: t.icon, payload: { kind: 'template', id: t.id } }) as Choice)
        .filter(matches)
    : [],
)
const blocks = computed<Choice[]>(() =>
  libraryBlocks
    .map((type) => {
      const def = blockDefinition(type)
      return { key: type, label: def.label, help: blockHelp[type], icon: def.icon, payload: { kind: 'new-block', type } } as Choice
    })
    .filter(matches),
)
const saved = computed<Choice[]>(() =>
  studio.saved.value
    .filter((s) => props.mode === 'section' || s.kind === 'block')
    .map((s) => ({
      key: s.id,
      label: s.name,
      help: s.kind === 'section' ? 'Your saved section' : 'Your saved block',
      icon: Bookmark,
      payload: { kind: 'saved', id: s.id },
    }) as Choice)
    .filter(matches),
)
const groups = computed(() =>
  [
    { title: 'Ready-made sections', choices: sections.value },
    { title: props.mode === 'section' ? 'Single items' : 'What would you like to add?', choices: blocks.value },
    { title: 'Saved by you', choices: saved.value },
  ].filter((g) => g.choices.length),
)

const searchEl = ref<HTMLInputElement | null>(null)
const onKey = (e: KeyboardEvent) => e.key === 'Escape' && emit('close')
onMounted(() => {
  document.addEventListener('keydown', onKey)
  searchEl.value?.focus()
})
onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[60] grid place-items-center bg-charcoal/30 p-4" @click.self="emit('close')">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="add-title"
        class="flex max-h-[88dvh] w-full max-w-3xl flex-col rounded-panel bg-white shadow-float"
      >
        <div class="flex items-center gap-4 border-b border-charcoal/[0.06] px-6 py-5">
          <div class="flex-1">
            <h2 id="add-title" class="text-xl font-bold">{{ mode === 'section' ? 'Add a section' : 'Add to this section' }}</h2>
            <p class="mt-0.5 text-sm text-muted">Pick something to add. You can change all of it afterwards.</p>
          </div>
          <button type="button" class="grid size-10 place-items-center rounded-full hover:bg-canvas" aria-label="Close" @click="emit('close')">
            <X class="size-5" aria-hidden="true" />
          </button>
        </div>

        <div class="px-6 pt-4">
          <div class="relative">
            <Search class="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-subtle" aria-hidden="true" />
            <label for="add-search" class="sr-only">Search</label>
            <input
              id="add-search"
              ref="searchEl"
              v-model="query"
              type="search"
              placeholder="Search, e.g. “picture” or “form”"
              class="h-12 w-full rounded-xl bg-canvas/60 pl-12 pr-4 text-base outline-none ring-1 ring-charcoal/[0.06] placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
            />
          </div>
        </div>

        <div class="overflow-y-auto px-6 pb-6">
          <section v-for="g in groups" :key="g.title" class="mt-5">
            <h3 class="mb-3 text-sm font-medium text-muted">{{ g.title }}</h3>
            <ul class="grid gap-2 sm:grid-cols-2">
              <li v-for="c in g.choices" :key="c.key">
                <button
                  type="button"
                  :class="
                    cn(
                      'flex w-full items-center gap-4 rounded-2xl p-4 text-left ring-1 ring-charcoal/[0.08] transition',
                      'hover:bg-accent-50 hover:ring-accent focus-visible:bg-accent-50',
                    )
                  "
                  @click="emit('pick', c.payload)"
                >
                  <span class="grid size-12 shrink-0 place-items-center rounded-xl bg-canvas">
                    <component :is="c.icon" class="size-6" :stroke-width="1.5" aria-hidden="true" />
                  </span>
                  <span class="min-w-0">
                    <span class="block text-base font-medium">{{ c.label }}</span>
                    <span class="mt-0.5 block text-sm text-muted">{{ c.help }}</span>
                  </span>
                </button>
              </li>
            </ul>
          </section>
          <p v-if="!groups.length" class="py-12 text-center text-muted">Nothing matches “{{ query.trim() }}”.</p>
        </div>
      </div>
    </div>
  </Teleport>
</template>
