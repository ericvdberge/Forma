<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { ArrowLeft, ExternalLink, Trash2, TriangleAlert } from 'lucide-vue-next'
import { FButton, FCard, FSkeleton, FStatus, cn } from '@forma/design-system'
import ItemFieldInput from '@/components/collections/ItemFieldInput.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollectionItems, type ItemDraft } from '@/composables/useCollectionItems'
import { useCollections } from '@/composables/useCollections'
import type { CollectionValue, PageStatus } from '@/data/types'
import { titleField } from '@/utils/collections'
import { slugify } from '@/utils/slugify'
import { timeAgo } from '@/utils/time'

/** Writes one item of a collection (`…/items/new` or `…/items/:itemId`): a form built from the collection's fields. */
const route = useRoute()
const router = useRouter()
const activeWebsite = useActiveWebsite().getActiveWebsite()
const collectionId = computed(() => String(route.params.collectionId ?? ''))
const itemId = computed(() => (typeof route.params.itemId === 'string' ? route.params.itemId : ''))
const isNew = computed(() => !itemId.value)

const { collection, isPending: collectionPending } = useCollections().getCollection(() => activeWebsite.value.id, collectionId)
const itemsApi = useCollectionItems()
const { items, isPending: itemsPending } = itemsApi.getItems(collectionId)
const item = computed(() => items.value.find((i) => i.id === itemId.value))
const loading = computed(() => collectionPending.value || (!isNew.value && itemsPending.value))

/* ---------- The form ---------- */
const values = ref<Record<string, CollectionValue | null>>({})
const slug = ref('')
const slugTouched = ref(false)
const status = ref<PageStatus>('draft')
const errors = ref<Record<string, string[]>>({})
const saving = ref<null | 'draft' | 'live'>(null)
/** The values as last loaded or saved, to tell whether there is anything unsaved. */
const saved = ref('')

const snapshot = () => JSON.stringify({ values: values.value, slug: slug.value })
const dirty = computed(() => snapshot() !== saved.value)

watch(
  [item, isNew],
  () => {
    if (isNew.value) {
      values.value = {}
      slug.value = ''
      slugTouched.value = false
      status.value = 'draft'
    } else if (item.value) {
      values.value = { ...item.value.values }
      slug.value = item.value.slug
      slugTouched.value = true
      status.value = item.value.status
    }
    saved.value = snapshot()
    errors.value = {}
  },
  { immediate: true },
)

// A new item's address follows its title until the person types their own.
const nameField = computed(() => (collection.value ? titleField(collection.value) : undefined))
watch(
  () => (nameField.value ? values.value[nameField.value.id] : undefined),
  (title) => {
    if (!slugTouched.value && typeof title === 'string') slug.value = slugify(title)
  },
)

const errorFor = (key: string) => errors.value[key]?.[0]
const heading = computed(() => {
  const title = nameField.value ? values.value[nameField.value.id] : undefined
  if (typeof title === 'string' && title.trim()) return title.trim()
  return isNew.value ? `New ${collection.value?.singularName.toLowerCase() ?? 'item'}` : (item.value?.slug ?? '')
})

/* ---------- Saving ---------- */
async function save(next: PageStatus) {
  if (!collection.value) return
  saving.value = next
  errors.value = {}
  const draft: ItemDraft = { slug: slug.value.trim() || slugify(heading.value), values: values.value, status: next }
  try {
    const outcome = isNew.value
      ? await itemsApi.createItem(collection.value, draft)
      : await itemsApi.saveItem(collection.value, itemId.value, draft)
    if (outcome.kind === 'invalid') errors.value = outcome.errors
    if (outcome.kind !== 'saved') return
    saved.value = snapshot()
    status.value = next
    if (isNew.value)
      router.replace({ name: 'collection-item', params: { collectionId: collection.value.id, itemId: outcome.item.id } })
  } finally {
    saving.value = null
  }
}

const hasErrors = computed(() => Object.keys(errors.value).length > 0)

/* ---------- Deleting ---------- */
const confirmingDelete = ref(false)
const deleting = ref(false)
async function remove() {
  if (!collection.value || !item.value) return
  deleting.value = true
  const deleted = await itemsApi.deleteItem(collection.value, item.value, heading.value)
  deleting.value = false
  confirmingDelete.value = false
  if (deleted) {
    saved.value = snapshot()
    router.push({ name: 'collection', params: { collectionId: collection.value.id } })
  }
}

onBeforeRouteLeave(() => !dirty.value || window.confirm('Leave without saving your changes?'))
</script>

<template>
  <div class="mx-auto max-w-[1100px]">
    <RouterLink :to="{ name: 'collection', params: { collectionId } }" class="mb-4 inline-flex items-center gap-2 text-sm text-muted hover:text-charcoal">
      <ArrowLeft class="size-4" aria-hidden="true" /> {{ collection?.name ?? 'Collection' }}
    </RouterLink>

    <div v-if="loading" class="flex flex-col gap-4" aria-busy="true"><FSkeleton class="h-16" /><FSkeleton class="h-96" /></div>

    <FCard v-else-if="!collection || (!isNew && !item)" class="flex flex-col items-center gap-3 py-16 text-center">
      <TriangleAlert class="size-5" aria-hidden="true" />
      <p class="font-medium">{{ collection ? 'This item doesn’t exist' : 'This collection isn’t in this website' }}</p>
      <RouterLink :to="collection ? { name: 'collection', params: { collectionId } } : { name: 'collections' }" class="text-sm underline">Go back</RouterLink>
    </FCard>

    <form v-else class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_300px]" @submit.prevent="save(status)">
      <div class="flex min-w-0 flex-col gap-6">
        <h1 class="truncate font-display text-[clamp(1.75rem,3.5vw,2.5rem)] font-bold leading-tight tracking-[-0.04em]">{{ heading }}</h1>

        <FCard padding="none" class="flex flex-col gap-6 p-5 sm:p-6">
          <ItemFieldInput
            v-for="f in collection.fields"
            :key="f.id"
            :field="f"
            :model-value="values[f.id]"
            :error="errorFor(`values.${f.id}`)"
            @update:model-value="values = { ...values, [f.id]: $event }"
          />
          <p v-if="!collection.fields.length" class="text-sm text-muted">
            This collection has no fields yet.
            <RouterLink :to="{ name: 'collection-settings', params: { collectionId } }" class="underline">Add some</RouterLink> first.
          </p>
        </FCard>
      </div>

      <aside class="flex flex-col gap-4 lg:sticky lg:top-24 lg:self-start">
        <FCard padding="none" class="flex flex-col gap-4 p-5">
          <div class="flex items-center justify-between gap-3">
            <FStatus :status="status" :label="status === 'live' ? 'Published' : 'Draft'" />
            <span v-if="item" class="text-xs text-subtle">Saved {{ timeAgo(item.updatedAt) }}</span>
          </div>

          <div class="flex flex-col gap-1.5">
            <label for="item-slug" class="text-sm font-medium">Address</label>
            <div :class="cn('flex h-10 items-center rounded-xl bg-white text-sm ring-1 ring-charcoal/10 focus-within:ring-2 focus-within:ring-accent', errorFor('slug') && 'ring-danger')">
              <span class="shrink-0 pl-3 text-subtle">/{{ collection.slug }}/</span>
              <input
                id="item-slug"
                :value="slug"
                class="h-full min-w-0 flex-1 bg-transparent pr-3 outline-none"
                :placeholder="slugify(heading) || 'item-name'"
                @input="slug = slugify(($event.target as HTMLInputElement).value); slugTouched = true"
              />
            </div>
            <p v-if="errorFor('slug')" class="text-xs text-danger" role="alert">{{ errorFor('slug') }}</p>
          </div>

          <p v-if="hasErrors" class="rounded-xl bg-danger/5 p-3 text-sm text-danger" role="alert">Some fields need a look before this can be saved.</p>

          <div class="flex flex-col gap-2">
            <FButton type="button" :disabled="!!saving" @click="save('live')">
              {{ saving === 'live' ? 'Publishing…' : status === 'live' ? (dirty ? 'Publish changes' : 'Published') : 'Publish' }}
            </FButton>
            <FButton type="button" variant="secondary" :disabled="!!saving" @click="save('draft')">
              {{ saving === 'draft' ? 'Saving…' : status === 'live' ? 'Unpublish' : 'Save draft' }}
            </FButton>
          </div>
          <p class="text-xs leading-relaxed text-muted">
            Published {{ collection.name.toLowerCase() }} show in lists on your site and get their own page, designed once for every
            {{ collection.singularName.toLowerCase() }}.
          </p>
        </FCard>

        <RouterLink
          v-if="collection.detailPageId"
          :to="{ name: 'studio', params: { pageId: collection.detailPageId } }"
          class="inline-flex items-center gap-2 px-1 text-sm text-charcoal/80 hover:text-charcoal"
        >
          <ExternalLink class="size-4" aria-hidden="true" /> Design the {{ collection.singularName.toLowerCase() }} page
        </RouterLink>

        <button
          v-if="item"
          type="button"
          class="inline-flex items-center gap-2 px-1 text-sm text-danger hover:underline"
          @click="confirmingDelete = true"
        >
          <Trash2 class="size-4" aria-hidden="true" /> Delete this {{ collection.singularName.toLowerCase() }}
        </button>
      </aside>
    </form>

    <ConfirmDialog
      v-model:open="confirmingDelete"
      :title="`Delete ‘${heading}’?`"
      message="It disappears from every list and its page goes offline. This can’t be undone."
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
