import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { getCollectionsQueryKey, getPagesQueryKey } from '@/api/generated/@tanstack/vue-query.gen'
import type { CollectionDto, PageDto } from '@/api/generated'
import type { Collection, CollectionField } from '@/data/types'
import { useCollectionsQuerier, type CollectionWriteOutcome } from '@/queriers/useCollectionsQuerier'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'

export function toCollection(dto: CollectionDto): Collection {
  return {
    id: dto.id,
    websiteId: dto.websiteId,
    name: dto.name,
    singularName: dto.singularName,
    slug: dto.slug,
    icon: dto.icon,
    fields: dto.fields.map((f) => ({ ...f })),
    detailPageId: dto.detailPageId ?? null,
    draft: dto.draft
      ? {
          slug: dto.draft.slug,
          fields: dto.draft.fields.map((f) => ({ ...f })),
          valueMoves: { ...dto.draft.valueMoves },
          updatedAt: dto.draft.updatedAt ? Date.parse(dto.draft.updatedAt) : null,
        }
      : null,
    publishedAt: dto.publishedAt ? Date.parse(dto.publishedAt) : null,
    updatedAt: Date.parse(dto.updatedAt),
  }
}

/**
 * What the collection editor sends: names and icon (saved directly), slug and fields (saved as the draft), and where
 * the values of removed fields go when the draft is published (`null`: they are removed).
 */
export interface CollectionDraft {
  name: string
  singularName: string
  slug: string
  icon: string
  fields: CollectionField[]
  valueMoves: Record<string, string | null>
}

/** A new field's id, chosen here so value moves can point at it before it is saved. */
export const newFieldId = () => `fld_${crypto.randomUUID().replaceAll('-', '').slice(0, 8)}`

const toRequestFields = (fields: CollectionField[]) =>
  fields.map((f) => ({ id: f.id || null, label: f.label, type: f.type, required: f.required }))

/** The detail page as GET /pages describes it, so the studio can open it without reloading the pages. */
function detailPageDto(dto: CollectionDto, old?: PageDto): PageDto | undefined {
  if (!dto.detailPageId) return undefined
  const now = new Date().toISOString()
  return {
    id: dto.detailPageId,
    websiteId: dto.websiteId,
    title: `${dto.singularName} page`,
    slug: `${dto.slug}/{item}`,
    status: old?.status ?? 'draft',
    kind: 'collectionTemplate',
    collectionId: dto.id,
    createdAt: old?.createdAt ?? now,
    updatedAt: now,
  }
}

/**
 * A website's collections (Blog posts, Team members…), read from GET /collections and written through the
 * collections API. Nothing is fetched until a view calls `getCollections()`.
 */
export function useCollections() {
  const queryClient = useQueryClient()
  const querier = useCollectionsQuerier()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  /** The website's collections, by name. */
  function getCollections(websiteId: MaybeRefOrGetter<string>) {
    const query = querier.getCollections(websiteId)
    const collections = computed(() => (query.data.value ?? []).map(toCollection))
    return { ...query, collections }
  }

  /** One collection of the website, from the same list. */
  function getCollection(websiteId: MaybeRefOrGetter<string>, collectionId: MaybeRefOrGetter<string>) {
    const query = getCollections(websiteId)
    const collection = computed(() => query.collections.value.find((c) => c.id === toValue(collectionId)))
    return { ...query, collection }
  }

  function writeCached(dto: CollectionDto, { remove = false } = {}) {
    queryClient.setQueryData<CollectionDto[]>(getCollectionsQueryKey({ query: { websiteId: dto.websiteId } }), (old) => {
      const others = (old ?? []).filter((d) => d.id !== dto.id)
      return remove ? others : [...others, dto].sort((a, b) => a.name.localeCompare(b.name))
    })

    queryClient.setQueryData<PageDto[]>(getPagesQueryKey({ query: { websiteId: dto.websiteId } }), (old) => {
      if (!old) return old
      const existing = old.find((p) => p.id === dto.detailPageId)
      const others = old.filter((p) => p.id !== dto.detailPageId)
      const page = remove ? undefined : detailPageDto(dto, existing)
      return page ? [page, ...others] : others
    })
  }

  function afterWrite(outcome: CollectionWriteOutcome, message: string) {
    if (outcome.kind !== 'saved') {
      if (outcome.kind !== 'invalid') notify('Saving the collection failed. Try again.', 'error')
      return outcome
    }
    writeCached(outcome.collection)
    logActivity(message, 'page')
    notify(message.replace(/^You /, '').replace(/^./, (c) => c.toUpperCase()))
    return outcome
  }

  function afterDraft(outcome: CollectionWriteOutcome) {
    if (outcome.kind === 'saved') {
      writeCached(outcome.collection)
      void queryClient.invalidateQueries({ queryKey: ['drafts'] })
    } else if (outcome.kind !== 'invalid') {
      notify('Saving the collection failed. Try again.', 'error')
    }
    return outcome
  }

  /** Creates the collection as a draft: visitors don't see it until it is published. */
  async function createCollection(websiteId: string, draft: CollectionDraft) {
    const outcome = await querier.create({
      websiteId,
      name: draft.name,
      singularName: draft.singularName,
      slug: draft.slug,
      icon: draft.icon,
      fields: toRequestFields(draft.fields),
    })
    if (outcome.kind === 'saved') void queryClient.invalidateQueries({ queryKey: ['drafts'] })
    return afterWrite(outcome, `You created the collection ‘${draft.name}’`)
  }

  /**
   * Saves the collection: its names and icon directly, its slug and fields as the draft (only what changed). The
   * outcome is the last write's (null when nothing changed); the first one that fails stops it.
   */
  async function saveCollection(collection: Collection, draft: CollectionDraft, { namesChanged, draftChanged }: { namesChanged: boolean; draftChanged: boolean }) {
    let outcome: CollectionWriteOutcome | undefined

    if (namesChanged) {
      outcome = await querier.update(collection.id, { name: draft.name, singularName: draft.singularName, icon: draft.icon })
      afterDraft(outcome)
      if (outcome.kind !== 'saved') return outcome
    }

    if (draftChanged) {
      outcome = await querier.saveDraft(collection.id, { slug: draft.slug, fields: toRequestFields(draft.fields), valueMoves: draft.valueMoves as Record<string, string> })
      afterDraft(outcome)
    }

    if (outcome?.kind === 'saved') notify(draftChanged ? 'Saved as draft' : 'Saved')
    return outcome ?? null
  }

  /** Throws the draft away: the slug and fields go back to the live version. */
  async function discardDraft(collection: Collection) {
    const outcome = await querier.discardDraft(collection.id)
    afterDraft(outcome)
    if (outcome.kind === 'saved') notify(`Changes to ${collection.name} discarded`)
    return outcome
  }

  async function deleteCollection(collection: Collection) {
    const deleted = await querier.remove(collection.id)
    if (!deleted) {
      notify('Deleting the collection failed. Try again.', 'error')
      return false
    }
    const dto = queryClient
      .getQueryData<CollectionDto[]>(getCollectionsQueryKey({ query: { websiteId: collection.websiteId } }))
      ?.find((d) => d.id === collection.id)
    if (dto) writeCached(dto, { remove: true })
    notify(`‘${collection.name}’ deleted`)
    return true
  }

  return {
    getCollections,
    getCollection,
    getUsages: querier.getUsages,
    createCollection,
    saveCollection,
    discardDraft,
    deleteCollection,
  }
}
