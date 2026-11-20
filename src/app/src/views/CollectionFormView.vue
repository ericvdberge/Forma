<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowDown, ArrowLeft, ArrowUp, ChevronDown, Plus, Trash2, TriangleAlert } from 'lucide-vue-next'
import { FButton, FCard, FInput, FSkeleton, FSwitch, cn } from '@forma/design-system'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollections, type CollectionDraft } from '@/composables/useCollections'
import type { CollectionField, CollectionFieldType } from '@/data/types'
import { collectionPresets, fieldTypeOrder, fieldTypes } from '@/utils/collections'
import { slugify } from '@/utils/slugify'

/**
 * Creates a collection (`/collections/new`) or changes one (`/collections/:id/settings`): its names, address and fields.
 * Creating one also creates the detail page its items are shown with.
 */
const route = useRoute()
const router = useRouter()
const activeWebsite = useActiveWebsite().getActiveWebsite()
const collectionsApi = useCollections()

const collectionId = computed(() => (typeof route.params.collectionId === 'string' ? route.params.collectionId : ''))
const editing = computed(() => !!collectionId.value)
const { collection, isPending, isError } = collectionsApi.getCollection(() => activeWebsite.value.id, collectionId)

/* ---------- The form ---------- */
/** A field row; `key` keeps rows apart before a new field has an id. */
type FieldRow = CollectionField & { key: string }

const name = ref('')
const singularName = ref('')
const slug = ref('')
const fields = ref<FieldRow[]>([])
const slugTouched = ref(false)
const singularTouched = ref(false)
const errors = ref<Record<string, string[]>>({})
const saving = ref(false)

const row = (f: Omit<CollectionField, 'id'> & { id?: string }): FieldRow => ({ id: f.id ?? '', label: f.label, type: f.type, required: f.required, key: crypto.randomUUID() })

function startFrom(presetId: string) {
  const preset = collectionPresets.find((p) => p.id === presetId)
  if (!preset) return
  name.value = preset.name
  singularName.value = preset.singularName
  slug.value = preset.slug
  fields.value = preset.fields.map(row)
  slugTouched.value = singularTouched.value = false
  errors.value = {}
}

// A new collection starts as a blog: the most common case, and easy to change.
if (!editing.value) startFrom('blog')

watch(
  collection,
  (c) => {
    if (!c || !editing.value) return
    name.value = c.name
    singularName.value = c.singularName
    slug.value = c.slug
    fields.value = c.fields.map(row)
    slugTouched.value = singularTouched.value = true
  },
  { immediate: true },
)

/** Typing a name: until the person types their own, the address and the singular name follow it. */
function onName(n: string) {
  name.value = n
  if (!slugTouched.value) slug.value = slugify(n)
  if (!singularTouched.value) singularName.value = singular(n)
}

/** A first guess at the singular of an English plural: Case studies → Case study, Boxes → Box, Posts → Post. */
function singular(n: string) {
  if (/ies$/i.test(n)) return n.replace(/ies$/i, 'y')
  if (/(s|x|z|ch|sh)es$/i.test(n)) return n.replace(/es$/i, '')
  if (/[^s]s$/i.test(n)) return n.replace(/s$/i, '')
  return n
}

/* ---------- Fields ---------- */
const addingOpen = ref(false)

function addField(type: CollectionFieldType) {
  fields.value.push(row({ label: fieldTypes[type].label, type, required: false }))
  addingOpen.value = false
}

function moveField(i: number, dir: -1 | 1) {
  const [f] = fields.value.splice(i, 1)
  fields.value.splice(i + dir, 0, f!)
}

function removeField(i: number) {
  fields.value.splice(i, 1)
}

/** Fields that items have values for: removing one drops those values. */
const removedFields = computed(() => (collection.value?.fields ?? []).filter((f) => !fields.value.some((r) => r.id === f.id)))
const changedTypes = computed(() =>
  (collection.value?.fields ?? []).filter((f) => fields.value.some((r) => r.id === f.id && r.type !== f.type)),
)

/* ---------- Saving ---------- */
const errorFor = (key: string) => errors.value[key]?.[0]
const fieldError = (i: number) => errorFor(`fields[${i}].label`) ?? errorFor(`Fields[${i}].Label`)
const formError = computed(() => errorFor('fields') ?? errorFor('Fields'))

function draft(): CollectionDraft {
  return {
    name: name.value.trim(),
    singularName: singularName.value.trim(),
    slug: slug.value.trim(),
    fields: fields.value.map(({ key: _, ...f }) => ({ ...f, label: f.label.trim() })),
  }
}

async function save() {
  saving.value = true
  errors.value = {}
  try {
    const outcome = editing.value && collection.value
      ? await collectionsApi.updateCollection(collection.value, draft())
      : await collectionsApi.createCollection(activeWebsite.value.id, draft())
    if (outcome.kind === 'invalid') errors.value = normalize(outcome.errors)
    if (outcome.kind === 'saved') router.push({ name: 'collection', params: { collectionId: outcome.collection.id } })
  } finally {
    saving.value = false
  }
}

/** The API names properties in PascalCase (`Name`) or camelCase (`slug`); the form looks them up in camelCase. */
function normalize(e: Record<string, string[]>) {
  return Object.fromEntries(Object.entries(e).map(([k, v]) => [k.replace(/(^|\.)([A-Z])/g, (_, dot: string, c: string) => dot + c.toLowerCase()), v]))
}

const canSave = computed(() => name.value.trim() && singularName.value.trim() && slug.value.trim() && !saving.value)

/* ---------- Deleting ---------- */
const confirmingDelete = ref(false)
const deleting = ref(false)
async function remove() {
  if (!collection.value) return
  deleting.value = true
  const deleted = await collectionsApi.deleteCollection(collection.value)
  deleting.value = false
  confirmingDelete.value = false
  if (deleted) router.push({ name: 'collections' })
}

const label = 'text-sm font-medium text-charcoal'
const iconButton = 'grid size-8 place-items-center rounded-lg text-muted transition-colors hover:bg-canvas hover:text-charcoal disabled:pointer-events-none disabled:opacity-30'
</script>

<template>
  <div class="mx-auto max-w-[920px]">
    <RouterLink
      :to="editing ? { name: 'collection', params: { collectionId } } : { name: 'collections' }"
      class="mb-4 inline-flex items-center gap-2 text-sm text-muted hover:text-charcoal"
    >
      <ArrowLeft class="size-4" aria-hidden="true" /> {{ editing ? (collection?.name ?? 'Collection') : 'Collections' }}
    </RouterLink>

    <div v-if="editing && isPending" class="flex flex-col gap-4" aria-busy="true"><FSkeleton class="h-24" /><FSkeleton class="h-80" /></div>

    <FCard v-else-if="editing && (isError || !collection)" class="flex flex-col items-center gap-3 py-16 text-center">
      <TriangleAlert class="size-5" aria-hidden="true" />
      <p class="font-medium">{{ isError ? 'Couldn’t load the collection' : 'This collection doesn’t exist' }}</p>
      <RouterLink :to="{ name: 'collections' }" class="text-sm underline">Back to collections</RouterLink>
    </FCard>

    <form v-else class="flex flex-col gap-6" @submit.prevent="save">
      <PageHeader :title="editing ? 'Collection settings' : 'New collection'">
        <template #meta>
          <p class="mt-3 max-w-xl text-[0.9375rem] text-muted">
            {{
              editing
                ? 'Rename the collection, change its address or its fields. Items keep their values for fields that stay.'
                : 'Name your collection and choose the fields every item fills in. You can change them later.'
            }}
          </p>
        </template>
      </PageHeader>

      <!-- Presets -->
      <FCard v-if="!editing" padding="none" class="p-5">
        <p :class="label">Start from</p>
        <div class="mt-3 flex flex-wrap gap-2" role="group" aria-label="Start from a ready-made collection">
          <button
            v-for="p in collectionPresets"
            :key="p.id"
            type="button"
            :class="cn('h-9 rounded-full px-4 text-sm transition-colors', name === p.name ? 'bg-charcoal text-warm-white' : 'bg-canvas hover:bg-charcoal/[0.08]')"
            @click="startFrom(p.id)"
          >
            {{ p.name }}
          </button>
          <button
            type="button"
            class="h-9 rounded-full bg-canvas px-4 text-sm hover:bg-charcoal/[0.08]"
            @click="name = ''; slugTouched = singularTouched = false; fields = [row({ label: 'Name', type: 'text', required: true })]"
          >
            Blank
          </button>
        </div>
      </FCard>

      <!-- Names -->
      <FCard padding="none" class="grid gap-5 p-5 sm:grid-cols-2">
        <div class="flex flex-col gap-1">
          <FInput :model-value="name" label="Name" placeholder="e.g. Blog posts" @update:model-value="onName" />
          <p v-if="errorFor('name')" class="px-1 text-xs text-danger">{{ errorFor('name') }}</p>
        </div>
        <div class="flex flex-col gap-1">
          <FInput :model-value="singularName" label="One item is called" placeholder="e.g. Blog post" @update:model-value="singularName = $event; singularTouched = true" />
          <p v-if="errorFor('singularName')" class="px-1 text-xs text-danger">{{ errorFor('singularName') }}</p>
        </div>
        <div class="flex flex-col gap-1 sm:col-span-2">
          <FInput :model-value="slug" label="Address" placeholder="blog" @update:model-value="slug = slugify($event); slugTouched = true" />
          <p :class="cn('px-1 text-xs', errorFor('slug') ? 'text-danger' : 'text-muted')">
            {{ errorFor('slug') ?? `Items are shown at ${activeWebsite.domain}/${slug || 'address'}/item-name` }}
          </p>
        </div>
      </FCard>

      <!-- Fields -->
      <FCard padding="none">
        <div class="flex items-center justify-between gap-3 border-b border-charcoal/[0.06] px-5 py-4">
          <div>
            <h2 class="font-bold">Fields</h2>
            <p class="text-sm text-muted">What every {{ singularName.toLowerCase() || 'item' }} has. The first text field is its name.</p>
          </div>
          <span class="text-xs text-subtle">{{ fields.length }}</span>
        </div>

        <ol class="flex flex-col">
          <li v-for="(f, i) in fields" :key="f.key" class="flex flex-col gap-2 border-b border-charcoal/[0.04] px-5 py-3 sm:flex-row sm:items-center sm:gap-3">
            <span class="hidden size-9 shrink-0 place-items-center rounded-lg bg-canvas text-charcoal/70 sm:grid">
              <component :is="fieldTypes[f.type].icon" class="size-4" aria-hidden="true" />
            </span>
            <div class="min-w-0 flex-1">
              <label :for="`field-${f.key}`" class="sr-only">Name of field {{ i + 1 }}</label>
              <input
                :id="`field-${f.key}`"
                v-model="f.label"
                class="h-10 w-full rounded-lg bg-white px-3 text-sm ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent"
                placeholder="Field name"
              />
              <p v-if="fieldError(i)" class="mt-1 text-xs text-danger">{{ fieldError(i) }}</p>
            </div>
            <div class="relative sm:w-40">
              <label :for="`type-${f.key}`" class="sr-only">Type of {{ f.label || `field ${i + 1}` }}</label>
              <select
                :id="`type-${f.key}`"
                v-model="f.type"
                class="h-10 w-full cursor-pointer appearance-none rounded-lg bg-white pl-3 pr-8 text-sm ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent"
              >
                <option v-for="t in fieldTypeOrder" :key="t" :value="t">{{ fieldTypes[t].label }}</option>
              </select>
              <ChevronDown class="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted" aria-hidden="true" />
            </div>
            <div class="flex items-center justify-between gap-1 sm:justify-end">
              <FSwitch v-model="f.required" label="Required" class="mr-2 text-sm text-charcoal/80" />
              <button type="button" :class="iconButton" :disabled="i === 0" :aria-label="`Move ${f.label} up`" @click="moveField(i, -1)">
                <ArrowUp class="size-4" aria-hidden="true" />
              </button>
              <button type="button" :class="iconButton" :disabled="i === fields.length - 1" :aria-label="`Move ${f.label} down`" @click="moveField(i, 1)">
                <ArrowDown class="size-4" aria-hidden="true" />
              </button>
              <button type="button" :class="cn(iconButton, 'hover:text-danger')" :aria-label="`Remove ${f.label}`" @click="removeField(i)">
                <Trash2 class="size-4" aria-hidden="true" />
              </button>
            </div>
          </li>
        </ol>

        <div class="p-4">
          <button
            v-if="!addingOpen"
            type="button"
            class="flex h-11 w-full items-center justify-center gap-2 rounded-xl border border-dashed border-charcoal/15 text-sm text-muted hover:border-charcoal/40 hover:text-charcoal"
            :aria-expanded="addingOpen"
            @click="addingOpen = true"
          >
            <Plus class="size-4" aria-hidden="true" /> Add a field
          </button>
          <div v-else class="rounded-xl bg-canvas p-2">
            <p class="px-2 pb-2 pt-1 text-xs font-medium uppercase tracking-[0.08em] text-subtle">Choose a type</p>
            <div class="grid gap-1 sm:grid-cols-2">
              <button
                v-for="t in fieldTypeOrder"
                :key="t"
                type="button"
                class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-left hover:bg-white"
                @click="addField(t)"
              >
                <component :is="fieldTypes[t].icon" class="size-4 shrink-0 text-charcoal/70" aria-hidden="true" />
                <span>
                  <span class="block text-sm font-medium">{{ fieldTypes[t].label }}</span>
                  <span class="block text-xs text-muted">{{ fieldTypes[t].help }}</span>
                </span>
              </button>
            </div>
          </div>
        </div>
      </FCard>

      <p v-if="formError" class="text-sm text-danger" role="alert">{{ formError }}</p>
      <p v-if="removedFields.length || changedTypes.length" class="rounded-xl bg-accent-50 p-4 text-sm text-accent-700" role="status">
        <template v-if="removedFields.length">Removing {{ removedFields.map((f) => `‘${f.label}’`).join(', ') }} drops its values from every item. </template>
        <template v-if="changedTypes.length">Items whose {{ changedTypes.map((f) => `‘${f.label}’`).join(', ') }} no longer fits the new type need it filled in again. </template>
        Pages that show these fields will point it out when you next publish them.
      </p>

      <div class="flex flex-wrap items-center justify-between gap-3">
        <FButton v-if="editing" type="button" variant="secondary" class="text-danger" @click="confirmingDelete = true">
          <Trash2 class="size-4" aria-hidden="true" /> Delete collection
        </FButton>
        <span v-else />
        <FButton type="submit" :disabled="!canSave">{{ saving ? 'Saving…' : editing ? 'Save changes' : 'Create collection' }}</FButton>
      </div>
    </form>

    <ConfirmDialog
      v-model:open="confirmingDelete"
      :title="`Delete ${collection?.name ?? 'this collection'}?`"
      message="Its items and its detail page are deleted too. Lists of it on other pages will show nothing. This can’t be undone."
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
