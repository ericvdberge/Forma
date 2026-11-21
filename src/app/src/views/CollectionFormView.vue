<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowDown, ArrowLeft, ArrowUp, ChevronDown, Plus, Trash2, TriangleAlert } from 'lucide-vue-next'
import { FButton, FCard, FInput, FModal, FSkeleton, FSwitch, cn } from '@forma/design-system'
import type { EntityRefDto, PublishIssueDto } from '@/api/generated'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import PageHeader from '@/components/PageHeader.vue'
import PublishControls from '@/components/publishing/PublishControls.vue'
import ScheduledBanner from '@/components/publishing/ScheduledBanner.vue'
import { useActiveWebsite } from '@/composables/useActiveWebsite'
import { useCollectionItems } from '@/composables/useCollectionItems'
import { newFieldId, useCollections, type CollectionDraft } from '@/composables/useCollections'
import { usePublishDialog } from '@/composables/usePublishDialog'
import type { Collection, CollectionField, CollectionFieldType } from '@/data/types'
import { collectionIcons, collectionPresets, fieldTypeOrder, fieldTypes } from '@/utils/collections'
import { editorRoute } from '@/utils/publishing'
import { slugify } from '@/utils/slugify'

/**
 * Creates a collection (`/collections/new`) or changes one (`/collections/:id/settings`). Its names and icon are saved
 * directly; its address and fields are a draft until they are published, with where the values of removed fields go.
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
const icon = ref('database')
const fields = ref<FieldRow[]>([])
const slugTouched = ref(false)
const singularTouched = ref(false)
/** Where the values of removed live fields go when the draft is published: a field id, or null to remove them. */
const valueMoves = ref<Record<string, string | null>>({})
const errors = ref<Record<string, string[]>>({})
const saving = ref(false)
/** What was last loaded or saved, to tell whether there is anything unsaved. */
const savedSnapshot = ref('')

// New fields get their id right away, so values can move to them before they are saved.
const row = (f: Omit<CollectionField, 'id'> & { id?: string }): FieldRow => ({ id: f.id || newFieldId(), label: f.label, type: f.type, required: f.required, key: crypto.randomUUID() })

function startFrom(presetId: string) {
  const preset = collectionPresets.find((p) => p.id === presetId)
  if (!preset) return
  name.value = preset.name
  singularName.value = preset.singularName
  slug.value = preset.slug
  icon.value = preset.icon
  fields.value = preset.fields.map(row)
  valueMoves.value = {}
  slugTouched.value = singularTouched.value = false
  errors.value = {}
}

// A new collection starts as a blog: the most common case, and easy to change. Both routes share this view, so the
// form is filled again whenever it switches between "new" and a collection (or the collection loads).
const loadedId = ref<string | null>(null)
watch(
  [collection, editing],
  ([c, isEditing]) => {
    // A refetch while editing keeps the edits; another collection (or a save) loads what the server has.
    if (savedSnapshot.value && snapshot() !== savedSnapshot.value && loadedId.value === collectionId.value) return
    if (!isEditing) startFrom('blog')
    else if (c) loadFrom(c)
    loadedId.value = collectionId.value
    savedSnapshot.value = isEditing ? snapshot() : ''
  },
  { immediate: true },
)

/** The form shows the draft: its address and fields, or the live ones without a draft. */
function loadFrom(c: Collection) {
  name.value = c.name
  singularName.value = c.singularName
  slug.value = c.draft?.slug ?? c.slug
  icon.value = c.icon
  fields.value = (c.draft?.fields ?? c.fields).map(row)
  valueMoves.value = { ...(c.draft?.valueMoves ?? {}) }
  slugTouched.value = singularTouched.value = true
  errors.value = {}
}

function snapshot() {
  return JSON.stringify({ name: name.value, singularName: singularName.value, slug: slug.value, icon: icon.value, fields: fields.value.map(({ key: _, ...f }) => f), valueMoves: valueMoves.value })
}
const dirty = computed(() => snapshot() !== savedSnapshot.value)

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

/* ---------- Removing a field that has values ---------- */
const itemsQuery = useCollectionItems().getItems(collectionId)
/** How many items have a value for a field, live or in their draft. */
function valueCount(fieldId: string) {
  const has = (v: unknown) => v !== undefined && v !== null && v !== ''
  return itemsQuery.items.value.filter((i) => has(i.values[fieldId]) || has(i.draft?.values[fieldId])).length
}

/** The field being removed while "Move values to…" asks where its values go. */
const removing = ref<{ field: CollectionField; count: number; index: number | null; target: string } | null>(null)

function removeField(i: number) {
  const field = fields.value[i]!
  const isLive = collection.value?.fields.some((f) => f.id === field.id)
  const count = isLive ? valueCount(field.id) : 0
  if (count) {
    removing.value = { field, count, index: i, target: moveTargets(field)[0]?.id ?? '' }
    return
  }
  fields.value.splice(i, 1)
}

/** Fields the values can move to: the other fields of the draft, of the same type first. */
function moveTargets(field: CollectionField) {
  const others = fields.value.filter((f) => f.id !== field.id)
  return [...others.filter((f) => f.type === field.type), ...others.filter((f) => f.type !== field.type)]
}

function confirmRemove(move: boolean) {
  const r = removing.value
  if (!r) return
  valueMoves.value = { ...valueMoves.value, [r.field.id]: move && r.target ? r.target : null }
  if (r.index !== null) fields.value.splice(r.index, 1)
  removing.value = null
}

/** Asks again where a removed field's values go. */
function changeMove(field: CollectionField) {
  removing.value = { field, count: valueCount(field.id), index: null, target: valueMoves.value[field.id] ?? moveTargets(field)[0]?.id ?? '' }
}

const moveLabel = (field: CollectionField) => {
  const to = valueMoves.value[field.id]
  const target = to ? fields.value.find((f) => f.id === to) : undefined
  return target ? `Values move to ${target.label}` : 'Values are removed'
}

/* ---------- Used by… ---------- */
const usagesQuery = collectionsApi.getUsages(collectionId)
function usedBy(fieldId: string) {
  return [...new Set((usagesQuery.data.value ?? []).filter((u) => u.fieldId === fieldId).map((u) => u.pageTitle))]
}

/** Live fields the draft removes. */
const removedFields = computed(() => (collection.value?.fields ?? []).filter((f) => !fields.value.some((r) => r.id === f.id)))
const changedTypes = computed(() =>
  (collection.value?.fields ?? []).filter((f) => fields.value.some((r) => r.id === f.id && r.type !== f.type)),
)

/* ---------- Saving ---------- */
const errorFor = (key: string) => errors.value[key]?.[0]
const fieldError = (i: number) => errorFor(`fields[${i}].label`)
const formError = computed(() => errorFor('fields'))

function draft(): CollectionDraft {
  const kept = new Set(fields.value.map((f) => f.id))
  const removed = new Set(removedFields.value.map((f) => f.id))
  return {
    name: name.value.trim(),
    singularName: singularName.value.trim(),
    slug: slug.value.trim(),
    icon: icon.value,
    fields: fields.value.map(({ key: _, ...f }) => ({ ...f, label: f.label.trim() })),
    // Only moves away from fields that are removed, to fields that stay.
    valueMoves: Object.fromEntries(Object.entries(valueMoves.value).filter(([from, to]) => removed.has(from) && (to === null || kept.has(to)))),
  }
}

const entity = computed<EntityRefDto | null>(() => (collection.value ? { kind: 'collection', id: collection.value.id, name: collection.value.name, collectionId: null } : null))

/** Saves the names and icon, and the draft of the address and fields. Gives back what to publish, or null. */
async function save(): Promise<EntityRefDto | null> {
  saving.value = true
  errors.value = {}
  try {
    const d = draft()
    if (!editing.value || !collection.value) {
      const created = await collectionsApi.createCollection(activeWebsite.value.id, d)
      if (created.kind === 'invalid') errors.value = created.errors
      if (created.kind !== 'saved') return null
      savedSnapshot.value = snapshot()
      await router.replace({ name: 'collection-settings', params: { collectionId: created.collection.id } })
      return { kind: 'collection', id: created.collection.id, name: created.collection.name, collectionId: null }
    }

    const c = collection.value
    const before = JSON.stringify({ slug: c.draft?.slug ?? c.slug, fields: c.draft?.fields ?? c.fields, valueMoves: c.draft?.valueMoves ?? {} })
    const after = JSON.stringify({ slug: d.slug, fields: d.fields, valueMoves: d.valueMoves })
    const outcome = await collectionsApi.saveCollection(c, d, {
      namesChanged: d.name !== c.name || d.singularName !== c.singularName || d.icon !== c.icon,
      draftChanged: before !== after,
    })
    if (outcome?.kind === 'invalid') errors.value = outcome.errors
    if (outcome && outcome.kind !== 'saved') return null
    savedSnapshot.value = snapshot()
    return { kind: 'collection', id: c.id, name: d.name, collectionId: null }
  } finally {
    saving.value = false
  }
}

async function discard() {
  if (!collection.value) return false
  const outcome = await collectionsApi.discardDraft(collection.value)
  if (outcome.kind !== 'saved') return false
  savedSnapshot.value = ''
  loadFrom({ ...collection.value, draft: null })
  savedSnapshot.value = snapshot()
  return true
}

/** Publish was blocked: what is wrong with this collection shows here; what needs fixing elsewhere opens the publish dialog. */
const { openDialog } = usePublishDialog()
const blocked = ref<PublishIssueDto[]>([])
function onBlocked(issues: PublishIssueDto[]) {
  blocked.value = issues.filter((i) => i.severity === 'error')
  if (blocked.value.some((i) => i.entity?.id !== collectionId.value) && entity.value) openDialog(activeWebsite.value.id, [entity.value])
}

const canSave = computed(() => !!(name.value.trim() && singularName.value.trim() && slug.value.trim()) && !saving.value)

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
                ? 'Names and the icon are saved right away. A new address and changed fields are a draft until you publish them, together with the pages that show them.'
                : 'Name your collection and choose the fields every item fills in. You can change them later.'
            }}
          </p>
        </template>
        <template #default>
          <PublishControls
            :website-id="activeWebsite.id"
            :entity="entity"
            :name="name || 'this collection'"
            :dirty="dirty"
            :is-live="!!collection?.publishedAt"
            :has-draft="!!collection?.draft"
            :draft-saved-at="collection?.draft?.updatedAt ?? null"
            :save="save"
            :discard="discard"
            @blocked="onBlocked"
          />
        </template>
      </PageHeader>
      <ScheduledBanner :website-id="activeWebsite.id" :entity="entity" />

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
            @click="name = ''; icon = 'database'; slugTouched = singularTouched = false; fields = [row({ label: 'Name', type: 'text', required: true })]"
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
        <div class="flex flex-col gap-2 sm:col-span-2">
          <p id="icon-label" class="text-sm font-medium text-charcoal">Icon in the sidebar</p>
          <div role="radiogroup" aria-labelledby="icon-label" class="flex flex-wrap gap-1.5">
            <button
              v-for="(meta, key) in collectionIcons"
              :key="key"
              type="button"
              role="radio"
              :aria-checked="icon === key"
              :aria-label="meta.label"
              :title="meta.label"
              :class="
                cn(
                  'grid size-10 place-items-center rounded-xl transition-colors',
                  icon === key ? 'bg-charcoal text-warm-white' : 'bg-canvas text-charcoal/70 hover:bg-charcoal/[0.08] hover:text-charcoal',
                )
              "
              @click="icon = String(key)"
            >
              <component :is="meta.icon" class="size-[18px]" :stroke-width="1.7" aria-hidden="true" />
            </button>
          </div>
          <p v-if="errorFor('icon')" class="px-1 text-xs text-danger">{{ errorFor('icon') }}</p>
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
              <p v-else-if="usedBy(f.id).length" class="mt-1 text-xs text-muted">Used by {{ usedBy(f.id).join(', ') }}</p>
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

      <FCard v-if="removedFields.length" padding="none" class="p-5">
        <h2 class="font-bold">Removed fields</h2>
        <p class="text-sm text-muted">These go when you publish. Pages that show them have to be published with it.</p>
        <ul class="mt-3 flex flex-col gap-2">
          <li v-for="f in removedFields" :key="f.id" class="flex flex-wrap items-center gap-3 rounded-xl bg-canvas px-3 py-2 text-sm">
            <span class="font-medium line-through decoration-charcoal/40">{{ f.label }}</span>
            <span class="text-muted">{{ valueCount(f.id) ? moveLabel(f) : 'No values' }}</span>
            <span v-if="usedBy(f.id).length" class="text-muted">· Used by {{ usedBy(f.id).join(', ') }}</span>
            <button v-if="valueCount(f.id)" type="button" class="ml-auto text-sm font-medium underline" @click="changeMove(f)">Change</button>
          </li>
        </ul>
      </FCard>

      <p v-if="changedTypes.length" class="rounded-xl bg-accent-50 p-4 text-sm text-accent-700" role="status">
        Items whose {{ changedTypes.map((f) => `‘${f.label}’`).join(', ') }} no longer fits the new type need it filled in again before they can go live.
      </p>

      <div v-if="blocked.length" class="rounded-xl bg-danger/5 p-4 text-sm" role="alert">
        <p class="font-medium text-danger">This can’t go live yet</p>
        <ul class="mt-2 flex flex-col gap-1.5">
          <li v-for="(issue, i) in blocked" :key="i" class="flex items-start gap-2">
            <span class="min-w-0 flex-1"><template v-if="issue.entity"><strong>{{ issue.entity.name }}</strong>: </template>{{ issue.message }}</span>
            <RouterLink v-if="issue.entity && issue.entity.id !== collectionId" :to="editorRoute(issue.entity)" class="shrink-0 font-medium underline">Open</RouterLink>
          </li>
        </ul>
      </div>

      <div class="flex flex-wrap items-center justify-between gap-3">
        <FButton v-if="editing" type="button" variant="secondary" class="text-danger" @click="confirmingDelete = true">
          <Trash2 class="size-4" aria-hidden="true" /> Delete collection
        </FButton>
        <span v-else />
        <FButton v-if="!editing" type="submit" :disabled="!canSave">{{ saving ? 'Creating…' : 'Create collection' }}</FButton>
      </div>
    </form>

    <FModal :open="!!removing" size="sm" labelledby="remove-field-title" @update:open="(v: boolean) => { if (!v) removing = null }">
      <div v-if="removing" class="flex flex-col gap-5 p-6 sm:p-7">
        <div>
          <h2 id="remove-field-title" class="font-display text-xl font-bold tracking-[-0.02em]">Remove {{ removing.field.label }}?</h2>
          <p class="mt-2 text-sm text-muted">
            {{ removing.count === 1 ? '1 item has' : `${removing.count} items have` }} a value for it. Move the values to another field, or remove them.
            Nothing changes on the site until you publish.
          </p>
        </div>
        <label v-if="moveTargets(removing.field).length" class="flex flex-col gap-1.5 text-sm font-medium">
          Move values to
          <select v-model="removing.target" class="h-10 rounded-xl bg-white px-3 text-sm font-normal ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent">
            <option v-for="t in moveTargets(removing.field)" :key="t.id" :value="t.id">{{ t.label }} ({{ fieldTypes[t.type].label }})</option>
          </select>
        </label>
        <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <FButton variant="secondary" size="sm" @click="removing = null">Cancel</FButton>
          <FButton variant="secondary" size="sm" @click="confirmRemove(false)">Remove values</FButton>
          <FButton v-if="moveTargets(removing.field).length" size="sm" data-autofocus @click="confirmRemove(true)">Move values</FButton>
        </div>
      </div>
    </FModal>

    <ConfirmDialog
      v-model:open="confirmingDelete"
      :title="`Delete ${collection?.name ?? 'this collection'}?`"
      message="Its items and its detail page are deleted too. Lists of it on other pages will show nothing. This can’t be undone."
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
