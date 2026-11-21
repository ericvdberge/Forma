<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { ArrowLeft, ExternalLink, Trash2, TriangleAlert } from 'lucide-vue-next'
import { FCard, FSkeleton, cn } from '@forma/design-system'
import type { EntityRefDto, PublishIssueDto } from '@/api/generated'
import ItemFieldInput from '@/components/collections/ItemFieldInput.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import PublishControls from '@/components/publishing/PublishControls.vue'
import ScheduledBanner from '@/components/publishing/ScheduledBanner.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { editorFields, useCollectionItems, type ItemDraft } from '@/composables/useCollectionItems'
import { useCollections } from '@/composables/useCollections'
import { usePublishDialog } from '@/composables/usePublishDialog'
import type { CollectionValue } from '@/data/types'
import { titleField } from '@/utils/collections'
import { slugify } from '@/utils/slugify'

/**
 * Writes one item of a collection (`…/items/new` or `…/items/:itemId`): a form built from the collection's fields
 * (its draft fields while it has a draft). Save stores the item's draft; Publish puts it live.
 */
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
const errors = ref<Record<string, string[]>>({})
/** The values as last loaded or saved, to tell whether there is anything unsaved. */
const saved = ref('')

const snapshot = () => JSON.stringify({ values: values.value, slug: slug.value })
const dirty = computed(() => snapshot() !== saved.value)

/** The item as the editor shows it: its draft, or the live version without one. */
const current = computed(() => (item.value ? (item.value.draft ?? { slug: item.value.slug, values: item.value.values }) : undefined))
const loadedId = ref<string | null>(null)

watch(
  [current, isNew, () => collection.value?.id],
  () => {
    // A refetch while typing keeps the edits; another item (or a save) loads what the server has.
    if (dirty.value && loadedId.value === itemId.value) return
    if (isNew.value) {
      values.value = withDefaults({})
      slug.value = ''
      slugTouched.value = false
    } else if (current.value) {
      values.value = withDefaults(current.value.values)
      slug.value = current.value.slug
      slugTouched.value = true
    }
    loadedId.value = itemId.value
    saved.value = snapshot()
    errors.value = {}
  },
  { immediate: true },
)

/** Yes/no fields read as "no" until switched on, so they start as `false` rather than without a value. */
function withDefaults(current: Record<string, CollectionValue | null>) {
  const booleans = (collection.value ? editorFields(collection.value) : []).filter((f) => f.type === 'boolean' && typeof current[f.id] !== 'boolean')

  return { ...current, ...Object.fromEntries(booleans.map((f) => [f.id, false])) }
}

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

/* ---------- Saving and publishing ---------- */
const fields = computed(() => (collection.value ? editorFields(collection.value) : []))
const entity = computed<EntityRefDto | null>(() =>
  item.value ? { kind: 'item', id: item.value.id, name: heading.value, collectionId: collectionId.value } : null,
)

/** Saves the draft (creating the item the first time). Gives back what to publish, or null when it needs a look. */
async function save(): Promise<EntityRefDto | null> {
  if (!collection.value) return null
  errors.value = {}
  const draft: ItemDraft = { slug: slug.value.trim() || slugify(heading.value), values: values.value }
  const outcome = isNew.value ? await itemsApi.createItem(collection.value, draft) : await itemsApi.saveItem(collection.value, itemId.value, draft)

  if (outcome.kind === 'invalid') errors.value = outcome.errors
  if (outcome.kind !== 'saved') return null

  saved.value = snapshot()
  const savedItem: EntityRefDto = { kind: 'item', id: outcome.item.id, name: heading.value, collectionId: collection.value.id }
  if (isNew.value) {
    loadedId.value = outcome.item.id
    await router.replace({ name: 'collection-item', params: { collectionId: collection.value.id, itemId: outcome.item.id } })
  }
  return savedItem
}

async function discard() {
  if (!collection.value || !item.value) return false
  const outcome = await itemsApi.discardItem(collection.value, item.value.id, heading.value)
  if (outcome.kind !== 'saved') return false
  saved.value = ''
  loadedId.value = null
  values.value = withDefaults(outcome.item.values as Record<string, CollectionValue>)
  slug.value = outcome.item.slug
  saved.value = snapshot()
  return true
}

/** Publish was blocked: this item's problems show on its fields; anything else opens the publish dialog. */
const { openDialog } = usePublishDialog()
const blockedMessage = ref('')
function onBlocked(issues: PublishIssueDto[]) {
  const mine = issues.filter((i) => i.severity === 'error' && i.entity?.id === itemId.value)
  errors.value = Object.fromEntries(mine.filter((i) => i.field).map((i) => [i.field!, [i.message]]))
  blockedMessage.value = mine.length ? 'Some fields need a look before this can go live.' : ''
  if (mine.length < issues.filter((i) => i.severity === 'error').length && entity.value) openDialog(activeWebsite.value.id, [entity.value])
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

    <form v-else class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_300px]" @submit.prevent="save()">
      <div class="flex min-w-0 flex-col gap-6">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <h1 class="min-w-0 truncate font-display text-[clamp(1.75rem,3.5vw,2.5rem)] font-bold leading-tight tracking-[-0.04em]">{{ heading }}</h1>
          <PublishControls
            :website-id="activeWebsite.id"
            :entity="entity"
            :name="heading"
            :dirty="dirty || isNew"
            :is-live="item?.status === 'live'"
            :has-draft="!!item?.draft"
            :draft-saved-at="item?.draft?.updatedAt ?? null"
            :save="save"
            :discard="discard"
            @blocked="onBlocked"
          />
        </div>
        <ScheduledBanner :website-id="activeWebsite.id" :entity="entity" />

        <FCard padding="none" class="flex flex-col gap-6 p-5 sm:p-6">
          <ItemFieldInput
            v-for="f in fields"
            :key="f.id"
            :field="f"
            :model-value="values[f.id]"
            :error="errorFor(`values.${f.id}`)"
            @update:model-value="values = { ...values, [f.id]: $event }"
          />
          <p v-if="!fields.length" class="text-sm text-muted">
            This collection has no fields yet.
            <RouterLink :to="{ name: 'collection-settings', params: { collectionId } }" class="underline">Add some</RouterLink> first.
          </p>
        </FCard>
      </div>

      <aside class="flex flex-col gap-4 lg:sticky lg:top-24 lg:self-start">
        <FCard padding="none" class="flex flex-col gap-4 p-5">
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

          <p v-if="hasErrors" class="rounded-xl bg-danger/5 p-3 text-sm text-danger" role="alert">
            {{ blockedMessage || 'Some fields need a look before this can be saved.' }}
          </p>

          <p class="text-xs leading-relaxed text-muted">
            Save keeps your changes as a draft; visitors see them once you publish. Live {{ collection.name.toLowerCase() }} show in
            lists on your site and get their own page, designed once for every {{ collection.singularName.toLowerCase() }}.
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
