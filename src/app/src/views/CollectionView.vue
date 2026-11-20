<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ArrowLeft, ImageOff, LayoutTemplate, Pencil, Plus, Search, Settings2, Trash2, TriangleAlert } from 'lucide-vue-next'
import { FButton, FCard, FIconButton, FSkeleton, FStatus, cn } from '@forma/design-system'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import LinkButton from '@/components/LinkButton.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollectionItems } from '@/composables/useCollectionItems'
import { useCollections } from '@/composables/useCollections'
import type { CollectionItem, PageStatus } from '@/data/types'
import { imageUrl } from '@/studio/media'
import { itemTitle } from '@/utils/collections'
import { timeAgo } from '@/utils/time'

/** One collection: its items, and links to its fields and to the page its items are shown with. */
const route = useRoute()
const activeWebsite = useActiveWebsite().getActiveWebsite()
const collectionId = computed(() => String(route.params.collectionId ?? ''))
const { collection, isPending: collectionPending, isError: collectionError } = useCollections().getCollection(() => activeWebsite.value.id, collectionId)
const itemsApi = useCollectionItems()
const { items, isPending: itemsPending, isError: itemsError, isFetching, refetch } = itemsApi.getItems(collectionId)

const coverField = computed(() => collection.value?.fields.find((f) => f.type === 'image'))
const title = (item: CollectionItem) => (collection.value ? itemTitle(collection.value, item) : item.slug)
const cover = (item: CollectionItem) => {
  const value = coverField.value ? item.values[coverField.value.id] : undefined
  return typeof value === 'string' && value ? imageUrl(value) : ''
}

/* Filters */
type Filter = 'all' | PageStatus
const filter = ref<Filter>('all')
const search = ref('')
const searched = computed(() => {
  const q = search.value.trim().toLowerCase()
  return q ? items.value.filter((i) => title(i).toLowerCase().includes(q) || i.slug.includes(q)) : items.value
})
const tabs = computed(() =>
  (['all', 'live', 'draft'] as const).map((value) => ({
    value,
    label: { all: 'All', live: 'Published', draft: 'Draft' }[value],
    count: value === 'all' ? searched.value.length : searched.value.filter((i) => i.status === value).length,
  })),
)
const visible = computed(() => (filter.value === 'all' ? searched.value : searched.value.filter((i) => i.status === filter.value)))

/* Deleting */
const deleting = ref<CollectionItem | null>(null)
const busy = ref(false)
const confirmOpen = computed({ get: () => !!deleting.value, set: (v) => !v && (deleting.value = null) })
async function remove() {
  if (!collection.value || !deleting.value) return
  busy.value = true
  await itemsApi.deleteItem(collection.value, deleting.value, title(deleting.value))
  busy.value = false
  deleting.value = null
}
</script>

<template>
  <div class="mx-auto max-w-[1240px]">
    <RouterLink :to="{ name: 'collections' }" class="mb-4 inline-flex items-center gap-2 text-sm text-muted hover:text-charcoal">
      <ArrowLeft class="size-4" aria-hidden="true" /> Collections
    </RouterLink>

    <div v-if="collectionPending" class="flex flex-col gap-4" aria-busy="true"><FSkeleton class="h-20" /><FSkeleton class="h-80" /></div>

    <FCard v-else-if="collectionError || !collection" class="flex flex-col items-center gap-3 py-16 text-center">
      <TriangleAlert class="size-5" aria-hidden="true" />
      <p class="font-medium">{{ collectionError ? 'Couldn’t load the collection' : 'This collection isn’t in this website' }}</p>
      <RouterLink :to="{ name: 'collections' }" class="text-sm underline">Back to collections</RouterLink>
    </FCard>

    <template v-else>
      <PageHeader :title="collection.name">
        <template #meta>
          <p class="mt-3 text-[0.9375rem] text-muted">
            {{ items.length }} {{ items.length === 1 ? collection.singularName.toLowerCase() : 'items' }} ·
            <span class="font-medium text-charcoal">{{ activeWebsite.domain }}/{{ collection.slug }}/…</span>
          </p>
        </template>
        <LinkButton variant="secondary" :to="{ name: 'collection-settings', params: { collectionId: collection.id } }">
          <Settings2 class="size-4" aria-hidden="true" /> Fields
        </LinkButton>
        <LinkButton v-if="collection.detailPageId" variant="secondary" :to="{ name: 'studio', params: { pageId: collection.detailPageId } }">
          <LayoutTemplate class="size-4" aria-hidden="true" /> Design detail page
        </LinkButton>
        <LinkButton :to="{ name: 'collection-item-new', params: { collectionId: collection.id } }">
          <Plus class="size-4" aria-hidden="true" /> New {{ collection.singularName.toLowerCase() }}
        </LinkButton>
      </PageHeader>

      <FCard padding="none">
        <div class="flex flex-col gap-4 border-b border-charcoal/[0.06] p-4 md:flex-row md:items-center md:justify-between">
          <div role="tablist" aria-label="Filter by status" class="flex gap-1 overflow-x-auto">
            <button
              v-for="t in tabs"
              :key="t.value"
              type="button"
              role="tab"
              :aria-selected="filter === t.value"
              :class="
                cn(
                  'inline-flex h-9 shrink-0 items-center gap-2 rounded-full px-4 text-sm transition-colors',
                  filter === t.value ? 'bg-charcoal text-warm-white' : 'text-charcoal/70 hover:bg-charcoal/5 hover:text-charcoal',
                )
              "
              @click="filter = t.value"
            >
              {{ t.label }}
              <span :class="cn('text-xs', filter === t.value ? 'text-warm-white/60' : 'text-subtle')">{{ t.count }}</span>
            </button>
          </div>
          <div class="relative md:w-72">
            <Search class="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-subtle" aria-hidden="true" />
            <label for="item-search" class="sr-only">Search {{ collection.name.toLowerCase() }}</label>
            <input
              id="item-search"
              v-model="search"
              type="search"
              :placeholder="`Search ${collection.name.toLowerCase()}`"
              class="h-9 w-full rounded-full bg-canvas pl-10 pr-4 text-sm outline-none ring-1 ring-transparent transition-shadow placeholder:text-subtle focus:bg-white focus:ring-2 focus:ring-accent"
            />
          </div>
        </div>

        <div v-if="itemsPending" aria-busy="true" aria-label="Loading items">
          <div v-for="i in 3" :key="i" class="flex items-center gap-4 border-b border-charcoal/[0.04] px-5 py-3.5 last:border-0">
            <FSkeleton class="h-10 w-14 shrink-0" />
            <FSkeleton class="h-4 flex-1" />
            <FSkeleton class="h-4 w-20" />
          </div>
        </div>

        <div v-else-if="itemsError" class="flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="font-medium">Couldn’t load the items</p>
          <FButton size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
        </div>

        <template v-else>
          <div class="overflow-x-auto">
            <table class="w-full min-w-[560px] text-left text-sm">
              <thead class="text-xs text-muted">
                <tr class="border-b border-charcoal/[0.06]">
                  <th scope="col" class="px-5 py-3 font-medium">{{ collection.singularName }}</th>
                  <th scope="col" class="px-5 py-3 font-medium">Status</th>
                  <th scope="col" class="px-5 py-3 font-medium">Updated</th>
                  <th scope="col" class="w-14 px-5 py-3"><span class="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in visible" :key="item.id" class="border-b border-charcoal/[0.04] transition-colors last:border-0 hover:bg-warm-white">
                  <td class="px-5 py-3">
                    <div class="flex items-center gap-3">
                      <span class="grid h-10 w-14 shrink-0 place-items-center overflow-hidden rounded-lg bg-canvas text-subtle">
                        <img v-if="cover(item)" :src="cover(item)" alt="" class="size-full object-cover" />
                        <ImageOff v-else-if="coverField" class="size-4" aria-hidden="true" />
                      </span>
                      <div class="min-w-0">
                        <RouterLink
                          :to="{ name: 'collection-item', params: { collectionId: collection.id, itemId: item.id } }"
                          class="block truncate font-medium hover:underline"
                        >
                          {{ title(item) }}
                        </RouterLink>
                        <p class="truncate text-xs text-muted">/{{ collection.slug }}/{{ item.slug }}</p>
                      </div>
                    </div>
                  </td>
                  <td class="px-5 py-3"><FStatus :status="item.status" :label="item.status === 'live' ? 'Published' : undefined" /></td>
                  <td class="whitespace-nowrap px-5 py-3 text-muted">{{ timeAgo(item.updatedAt) }}</td>
                  <td class="px-5 py-3">
                    <div class="flex items-center justify-end gap-1">
                      <LinkButton
                        size="sm"
                        variant="secondary"
                        :to="{ name: 'collection-item', params: { collectionId: collection.id, itemId: item.id } }"
                        :aria-label="`Edit ${title(item)}`"
                      >
                        <Pencil class="size-3.5" aria-hidden="true" /> Edit
                      </LinkButton>
                      <FIconButton :icon="Trash2" size="sm" :label="`Delete ${title(item)}`" @click="deleting = item" />
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div v-if="!visible.length" class="flex flex-col items-center gap-3 px-6 py-16 text-center">
            <p class="font-medium">{{ search ? 'Nothing matches' : `No ${collection.name.toLowerCase()} yet` }}</p>
            <p class="max-w-xs text-sm text-muted">
              {{ search ? `Nothing is called “${search.trim()}”.` : `Add the first ${collection.singularName.toLowerCase()} to show it on your site.` }}
            </p>
            <LinkButton v-if="!search" size="sm" variant="secondary" :to="{ name: 'collection-item-new', params: { collectionId: collection.id } }">
              New {{ collection.singularName.toLowerCase() }}
            </LinkButton>
          </div>
        </template>
      </FCard>
    </template>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="`Delete ‘${deleting ? title(deleting) : ''}’?`"
      message="It disappears from every list and its page goes offline. This can’t be undone."
      :busy="busy"
      @confirm="remove"
    />
  </div>
</template>
