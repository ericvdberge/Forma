<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { FileText, Globe, Search } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useCms } from '@/stores/cms'
import { useDismiss } from '@/composables/useDismiss'

const { state, setActiveWebsite } = useCms()
const router = useRouter()

const query = ref('')
const open = ref(false)
const highlighted = ref(0)
const root = ref<HTMLElement | null>(null)
const input = ref<HTMLInputElement | null>(null)
useDismiss(root, () => (open.value = false))

const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.platform)

type Result = { id: string; label: string; hint: string; kind: 'website' | 'page'; run: () => void }

const results = computed<Result[]>(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return []
  const sites: Result[] = state.websites
    .filter((w) => `${w.name} ${w.domain}`.toLowerCase().includes(q))
    .map((w) => ({ id: w.id, label: w.name, hint: w.domain, kind: 'website', run: () => router.push(`/websites/${w.id}`) }))
  const pages: Result[] = state.pages
    .filter((p) => `${p.title} ${p.path}`.toLowerCase().includes(q))
    .map((p) => {
      const site = state.websites.find((w) => w.id === p.websiteId)
      return {
        id: p.id,
        label: p.title,
        hint: `${site?.name ?? ''} · ${p.path}`,
        kind: 'page',
        run: () => {
          if (site && site.id !== state.activeWebsiteId) setActiveWebsite(site.id)
          router.push({ path: '/pages', query: { q: p.title } })
        },
      }
    })
  return [...sites, ...pages].slice(0, 8)
})

function choose(r: Result | undefined) {
  if (!r) return
  r.run()
  query.value = ''
  open.value = false
  input.value?.blur()
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    highlighted.value = Math.min(highlighted.value + 1, results.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    highlighted.value = Math.max(highlighted.value - 1, 0)
  } else if (e.key === 'Enter') {
    choose(results.value[highlighted.value])
  }
}

function onGlobalKey(e: KeyboardEvent) {
  if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    input.value?.focus()
  }
}
onMounted(() => window.addEventListener('keydown', onGlobalKey))
onBeforeUnmount(() => window.removeEventListener('keydown', onGlobalKey))
</script>

<template>
  <div ref="root" class="relative min-w-0 max-w-[490px] flex-1">
    <label class="sr-only" for="global-search">Search websites and pages</label>
    <Search class="pointer-events-none absolute left-5 top-1/2 size-[1.125rem] -translate-y-1/2 text-charcoal/70" aria-hidden="true" />
    <input
      id="global-search"
      ref="input"
      v-model="query"
      type="search"
      autocomplete="off"
      placeholder="Search anything…"
      role="combobox"
      :aria-expanded="open && results.length > 0"
      aria-controls="global-search-results"
      class="h-12 w-full rounded-2xl bg-white/80 pl-13 pr-4 sm:pr-16 text-[0.9375rem] ring-1 ring-charcoal/[0.08] backdrop-blur outline-none transition-shadow placeholder:text-subtle focus:ring-2 focus:ring-accent"
      @focus="open = true"
      @input="open = true; highlighted = 0"
      @keydown="onKeydown"
    />
    <kbd
      class="pointer-events-none absolute right-5 top-1/2 hidden -translate-y-1/2 font-sans text-[0.8125rem] text-subtle sm:block"
      >{{ isMac ? '⌘' : 'Ctrl' }} K</kbd
    >

    <ul
      v-show="open && query.trim()"
      id="global-search-results"
      role="listbox"
      class="absolute inset-x-0 top-full z-50 mt-2 overflow-hidden rounded-2xl bg-white p-2 ring-1 ring-charcoal/[0.06] shadow-float"
    >
      <li v-if="!results.length" class="px-3 py-2.5 text-sm text-muted">No results for “{{ query }}”</li>
      <li
        v-for="(r, i) in results"
        :key="r.kind + r.id"
        role="option"
        :aria-selected="i === highlighted"
        :class="cn('flex cursor-pointer items-center gap-3 rounded-xl px-3 py-2.5', i === highlighted && 'bg-canvas')"
        @mouseenter="highlighted = i"
        @mousedown.prevent="choose(r)"
      >
        <component :is="r.kind === 'website' ? Globe : FileText" class="size-4 text-muted" aria-hidden="true" />
        <span class="text-sm font-medium">{{ r.label }}</span>
        <span class="ml-auto truncate text-xs text-subtle">{{ r.hint }}</span>
      </li>
    </ul>
  </div>
</template>
