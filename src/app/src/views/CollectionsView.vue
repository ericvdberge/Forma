<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight, Database, LayoutTemplate, Plus, TriangleAlert } from 'lucide-vue-next'
import { FButton, FCard, FSelect, FSkeleton } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollections } from '@/composables/useCollections'
import { useWebsites } from '@/composables/useWebsites'
import { fieldTypes } from '@/utils/collections'
import { timeAgo } from '@/utils/time'

/** The active website's collections: open one to manage its items, or design the page its items are shown with. */
const { getWebsites } = useWebsites()
const { websites } = getWebsites()
const { getActiveWebsite, setActiveWebsite } = useActiveWebsite()
const activeWebsite = getActiveWebsite()
const { collections, isPending, isError, isFetching, refetch } = useCollections().getCollections(() => activeWebsite.value.id)

const websiteId = computed({
  get: () => activeWebsite.value.id,
  set: (id: string) => setActiveWebsite(id),
})
const websiteOptions = computed(() => websites.value.map((w) => ({ label: w.name, value: w.id })))
</script>

<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Collections">
      <template #meta>
        <p class="mt-3 max-w-xl text-[0.9375rem] text-muted">
          Content you write once and show in many places, like blog posts, team members or projects, on
          <span class="font-medium text-charcoal">{{ activeWebsite.name }}</span>.
        </p>
      </template>
      <FSelect v-model="websiteId" label="Website" hide-label size="sm" :options="websiteOptions" class="w-48" />
      <LinkButton :to="{ name: 'collection-new' }"><Plus class="size-4" aria-hidden="true" /> New collection</LinkButton>
    </PageHeader>

    <div v-if="isPending" class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3" aria-busy="true" aria-label="Loading collections">
      <FSkeleton v-for="i in 3" :key="i" class="h-56" />
    </div>

    <FCard v-else-if="isError" class="flex flex-col items-center gap-3 py-16 text-center">
      <span class="grid size-12 place-items-center rounded-2xl bg-canvas"><TriangleAlert class="size-5" aria-hidden="true" /></span>
      <p class="font-medium">Couldn’t load the collections</p>
      <p class="max-w-xs text-sm text-muted">Check that the Forma API is running, then try again.</p>
      <FButton size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
    </FCard>

    <FCard v-else-if="!collections.length" class="flex flex-col items-center gap-3 py-16 text-center">
      <span class="grid size-12 place-items-center rounded-2xl bg-accent-50 text-accent"><Database class="size-5" aria-hidden="true" /></span>
      <p class="font-medium">No collections yet</p>
      <p class="max-w-sm text-sm text-muted">
        Start a blog, a team page or a portfolio: define the fields once, add as many items as you like and design one page that
        shows each of them.
      </p>
      <LinkButton size="sm" :to="{ name: 'collection-new' }">Create a collection</LinkButton>
    </FCard>

    <ul v-else class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
      <li v-for="c in collections" :key="c.id">
        <FCard padding="none" class="flex h-full flex-col">
          <RouterLink :to="{ name: 'collection', params: { collectionId: c.id } }" class="group flex flex-1 flex-col gap-4 p-6">
            <div class="flex items-start justify-between gap-3">
              <span class="grid size-11 shrink-0 place-items-center rounded-2xl bg-canvas text-charcoal/80">
                <Database class="size-5" :stroke-width="1.6" aria-hidden="true" />
              </span>
              <ArrowRight class="size-4 text-subtle transition-transform group-hover:translate-x-0.5 group-hover:text-charcoal" aria-hidden="true" />
            </div>
            <div class="min-w-0">
              <h2 class="truncate text-lg font-bold group-hover:underline">{{ c.name }}</h2>
              <p class="truncate text-sm text-muted">{{ activeWebsite.domain }}/{{ c.slug }}/…</p>
            </div>
            <ul class="flex flex-wrap gap-1.5" :aria-label="`Fields of ${c.name}`">
              <li
                v-for="f in c.fields.slice(0, 5)"
                :key="f.id"
                class="inline-flex items-center gap-1.5 rounded-full bg-canvas px-2.5 py-1 text-xs text-charcoal/75"
              >
                <component :is="fieldTypes[f.type].icon" class="size-3" aria-hidden="true" /> {{ f.label }}
              </li>
              <li v-if="c.fields.length > 5" class="rounded-full px-2 py-1 text-xs text-subtle">+{{ c.fields.length - 5 }} more</li>
              <li v-if="!c.fields.length" class="text-xs text-subtle">No fields yet</li>
            </ul>
          </RouterLink>
          <div class="flex items-center justify-between gap-3 border-t border-charcoal/[0.06] px-6 py-3.5 text-sm">
            <span class="text-xs text-subtle">Updated {{ timeAgo(c.updatedAt) }}</span>
            <RouterLink
              v-if="c.detailPageId"
              :to="{ name: 'studio', params: { pageId: c.detailPageId } }"
              class="inline-flex items-center gap-1.5 font-medium text-charcoal/80 hover:text-charcoal"
            >
              <LayoutTemplate class="size-4" aria-hidden="true" /> Design detail page
            </RouterLink>
          </div>
        </FCard>
      </li>
    </ul>
  </div>
</template>
