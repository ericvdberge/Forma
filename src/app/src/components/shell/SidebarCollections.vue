<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Plus } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollections } from '@/composables/useCollections'
import { useWebsites } from '@/composables/useWebsites'

/** The active website's collections, nested under Collections in the sidebar. */
defineEmits<{ navigate: [] }>()

const route = useRoute()
const { getWebsites } = useWebsites()
const { websites } = getWebsites()
const { activeWebsiteId } = useActiveWebsite()
// Same fallback as getActiveWebsite(), without assuming the websites have loaded.
const websiteId = computed(() => websites.value.find((w) => w.id === activeWebsiteId.value)?.id ?? websites.value[0]?.id ?? '')
const { collections } = useCollections().getCollections(websiteId)

const isCurrent = (id: string) => route.path === `/collections/${id}` || route.path.startsWith(`/collections/${id}/`)

const link = 'flex h-9 items-center gap-2.5 rounded-lg pl-[3.25rem] pr-3 text-sm transition-colors'
</script>

<template>
  <ul class="mt-0.5 flex flex-col gap-0.5" aria-label="Collections of this website">
    <li v-for="c in collections" :key="c.id">
      <RouterLink
        :to="{ name: 'collection', params: { collectionId: c.id } }"
        :aria-current="isCurrent(c.id) ? 'page' : undefined"
        :class="cn(link, isCurrent(c.id) ? 'font-medium text-charcoal' : 'text-charcoal/65 hover:bg-charcoal/[0.04] hover:text-charcoal')"
        @click="$emit('navigate')"
      >
        <span :class="cn('size-1.5 shrink-0 rounded-full', isCurrent(c.id) ? 'bg-accent' : 'bg-charcoal/20')" aria-hidden="true" />
        <span class="truncate">{{ c.name }}</span>
      </RouterLink>
    </li>
    <li>
      <RouterLink :to="{ name: 'collection-new' }" :class="cn(link, 'text-muted hover:bg-charcoal/[0.04] hover:text-charcoal')" @click="$emit('navigate')">
        <Plus class="size-3.5 shrink-0" aria-hidden="true" />
        New collection
      </RouterLink>
    </li>
  </ul>
</template>
