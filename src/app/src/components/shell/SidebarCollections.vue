<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Plus } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollections } from '@/composables/useCollections'
import { useWebsites } from '@/composables/useWebsites'
import { collectionIcon } from '@/utils/collections'

/**
 * The active website's collections as a sidebar group of their own: each collection is a top-level item with its icon.
 * The heading opens the overview of all collections.
 */
defineProps<{ collapsed: boolean }>()
defineEmits<{ navigate: [] }>()

const route = useRoute()
const { getWebsites } = useWebsites()
const { websites } = getWebsites()
const { activeWebsiteId } = useActiveWebsite()
// Same fallback as getActiveWebsite(), without assuming the websites have loaded.
const websiteId = computed(() => websites.value.find((w) => w.id === activeWebsiteId.value)?.id ?? websites.value[0]?.id ?? '')
const { collections } = useCollections().getCollections(websiteId)

const isCurrent = (id: string) => route.path === `/collections/${id}` || route.path.startsWith(`/collections/${id}/`)
const isOverview = computed(() => route.path === '/collections' || route.path === '/collections/new')

const item = (collapsed: boolean, current: boolean) =>
  cn(
    'flex h-11 items-center gap-3.5 rounded-xl text-[0.9375rem] transition-colors',
    collapsed ? 'justify-center' : 'px-4',
    current ? 'bg-charcoal/[0.06] font-medium text-charcoal' : 'text-charcoal/75 hover:bg-charcoal/[0.04] hover:text-charcoal',
  )
</script>

<template>
  <section :class="collapsed ? 'mt-3 border-t border-charcoal/[0.06] pt-3' : 'mt-5'">
    <h2 v-if="!collapsed" class="mb-1.5 flex items-center justify-between px-4">
      <RouterLink
        to="/collections"
        :aria-current="isOverview ? 'page' : undefined"
        :class="cn('text-[0.6875rem] font-medium uppercase tracking-[0.1em] transition-colors hover:text-charcoal', isOverview ? 'text-charcoal' : 'text-subtle')"
        title="All collections"
        @click="$emit('navigate')"
      >
        Collections
      </RouterLink>
    </h2>
    <h2 v-else class="sr-only">Collections</h2>

    <ul class="flex flex-col gap-1">
      <li v-for="c in collections" :key="c.id">
        <RouterLink
          :to="{ name: 'collection', params: { collectionId: c.id } }"
          :aria-current="isCurrent(c.id) ? 'page' : undefined"
          :title="collapsed ? c.name : undefined"
          :class="item(collapsed, isCurrent(c.id))"
          @click="$emit('navigate')"
        >
          <component :is="collectionIcon(c.icon)" class="size-5 shrink-0" :stroke-width="1.6" aria-hidden="true" />
          <span v-if="!collapsed" class="truncate">{{ c.name }}</span>
        </RouterLink>
      </li>
      <li>
        <RouterLink
          :to="{ name: 'collection-new' }"
          :title="collapsed ? 'New collection' : undefined"
          :aria-label="collapsed ? 'New collection' : undefined"
          :class="cn(item(collapsed, false), 'text-muted')"
          @click="$emit('navigate')"
        >
          <Plus class="size-5 shrink-0" :stroke-width="1.6" aria-hidden="true" />
          <span v-if="!collapsed">New collection</span>
        </RouterLink>
      </li>
    </ul>
  </section>
</template>
