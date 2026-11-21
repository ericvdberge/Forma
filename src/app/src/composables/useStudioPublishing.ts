import { computed, ref, shallowRef, watch, type Ref } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import type { EntityRefDto, PublishIssueDto } from '@/api/generated'
import { useActivity } from '@/composables/useActivity'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { usePublishing } from '@/composables/usePublishing'
import { useToast } from '@/composables/useToast'
import type { SaveDraftOutcome } from '@/queriers/usePageContentQuerier'
import { clone } from '@/studio/defaults'
import type { PageDocument, Selection, StudioPage } from '@/studio/types'

interface PublishingContext {
  page: Ref<StudioPage | undefined>
  /** The page's title and website, for the publish calls and their toasts. */
  pageTitle: Ref<string>
  websiteId: Ref<string>
  writePage: (update: (p: StudioPage) => StudioPage, id?: string) => void
  saveDraft: (pageId: string, document: PageDocument) => Promise<SaveDraftOutcome>
  discardDraft: (pageId: string) => Promise<boolean>
  refetch: () => Promise<unknown>
  change: (mutate: (d: PageDocument) => void) => void
  keepSelectionValid: (d: PageDocument) => void
  select: (sel: Selection) => void
}

const same = (a: PageDocument | null | undefined, b: PageDocument | null | undefined) => JSON.stringify(a) === JSON.stringify(b)

/**
 * Saving the page's draft (PUT /pages/{id}/content/draft), publishing it (POST /publish, through usePublishing) and
 * discarding it (DELETE …/draft). Publish's errors on this page, and its warnings afterwards, are shown in the studio's
 * issues dialog while `issues` is set; errors elsewhere open the publish dialog.
 */
export function useStudioPublishing(ctx: PublishingContext) {
  const { page, pageTitle, websiteId, writePage, saveDraft, discardDraft, refetch, change, keepSelectionValid, select } = ctx
  const queryClient = useQueryClient()
  const { logActivity } = useActivity()
  const { notify } = useToast()
  const { published, openDialog } = usePublishDialog()
  const publishing = usePublishing()

  const saving = ref(false)
  /** What to fix (errors) or look at (warnings after publishing); the issues dialog shows it while set. */
  const issues = shallowRef<PublishIssueDto[] | null>(null)

  const entity = computed<EntityRefDto | null>(() => (page.value ? { kind: 'page', id: page.value.pageId, name: pageTitle.value, collectionId: null } : null))
  const isPublished = computed(() => !!page.value?.published)
  /** Edits that aren't saved to the server yet. */
  const dirty = computed(() => !!page.value && !same(page.value.draft, page.value.saved))
  const hasDraft = computed(() => !!page.value?.hasDraft)
  /** Anything that isn't live: unsaved edits or a saved draft. */
  const hasChanges = computed(() => dirty.value || hasDraft.value)

  /** Saves the draft. Gives back the page to publish, or null when saving failed. */
  async function save(): Promise<EntityRefDto | null> {
    const p = page.value
    if (!p || saving.value) return null

    saving.value = true
    // What is sent is what is saved. Kept as is: the server's JSON spells some defaults differently (e.g. null overrides).
    const sent = clone(p.draft)
    try {
      const outcome = await saveDraft(p.pageId, sent)
      switch (outcome.kind) {
        case 'saved':
          writePage(
            (old) => ({
              ...old,
              saved: sent,
              hasDraft: outcome.content.hasDraft,
              draftSavedAt: outcome.content.draftUpdatedAt ? Date.parse(outcome.content.draftUpdatedAt) : Date.now(),
            }),
            p.pageId,
          )
          void queryClient.invalidateQueries({ queryKey: ['drafts'] })
          notify('Saved as draft')
          return entity.value
        case 'invalid':
          // The studio produced something the API refuses: a bug, not something the person can fix.
          console.error('The API refused the page:', outcome.errors)
          notify('Couldn’t save: the page has something the server doesn’t accept', 'error')
          return null
        case 'not-found':
          notify('This page doesn’t exist on the server, so it can’t be saved', 'error')
          return null
        default:
          notify('Couldn’t save the page. Try again.', 'error')
          return null
      }
    } finally {
      saving.value = false
    }
  }

  /** Publish was blocked: this page's issues show in the studio; anything elsewhere opens the publish dialog. */
  function onBlocked(found: PublishIssueDto[]) {
    const elsewhere = found.some((i) => i.severity === 'error' && i.entity && i.entity.id !== page.value?.pageId)
    if (elsewhere && entity.value) openDialog(websiteId.value, [entity.value])
    else issues.value = found
  }

  /** After publishing: the page's warnings, worth a look. */
  function onPublished(warnings: PublishIssueDto[]) {
    logActivity(`You published ‘${pageTitle.value}’`, 'page')
    const mine = warnings.filter((w) => w.entity?.id === page.value?.pageId)
    issues.value = mine.length ? mine : null
  }

  /** Publish from the studio: save, then publish this page (usePublishing asks "Publish together?" when needed). */
  async function publish() {
    const saved = dirty.value || !page.value?.hasDraft ? await save() : entity.value
    if (!saved) return
    const outcome = await publishing.publishOne(websiteId.value, saved)
    if (outcome.kind === 'blocked') onBlocked(outcome.issues)
    if (outcome.kind === 'published') onPublished(outcome.warnings)
  }

  /** Something was published (here or from the publish dialog): reload what the server has, unless there are unsaved edits. */
  watch(published, () => {
    if (!dirty.value) void refetch()
  })

  /** Selects what an issue is about, so its settings open. */
  function showIssue(issue: PublishIssueDto) {
    issues.value = null
    if (issue.blockId && issue.sectionId) select({ kind: 'block', sectionId: issue.sectionId, blockId: issue.blockId })
    else if (issue.sectionId) select({ kind: 'section', sectionId: issue.sectionId })
  }

  /** Goes back to the live version: the draft on the server is discarded, the editor shows the live page (as one undo step). */
  async function discard() {
    const p = page.value
    const live = p?.published
    if (!p || !live) return false

    if (p.hasDraft && !(await discardDraft(p.pageId))) {
      notify('Couldn’t discard the changes. Try again.', 'error')
      return false
    }

    change((d) => Object.assign(d, clone(live)))
    keepSelectionValid(live)
    writePage((old) => ({ ...old, saved: clone(live), hasDraft: false, draftSavedAt: null }), p.pageId)
    await publishing.refresh()
    logActivity('You discarded unpublished changes', 'page')
    notify(`Changes to ${pageTitle.value} discarded`)
    return true
  }

  return {
    saving,
    issues,
    entity,
    isPublished,
    dirty,
    hasDraft,
    hasChanges,
    save,
    publish,
    onBlocked,
    onPublished,
    showIssue,
    discard,
  }
}
