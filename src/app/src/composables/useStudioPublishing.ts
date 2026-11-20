import { computed, ref, shallowRef, type Ref } from 'vue'
import type { ContentIssueDto } from '@/api/generated'
import { usePages } from '@/composables/usePages'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'
import type { PublishOutcome } from '@/queriers/usePageContentQuerier'
import { clone } from '@/studio/defaults'
import type { PageDocument, Selection, StudioPage } from '@/studio/types'

interface PublishingContext {
  page: Ref<StudioPage | undefined>
  writePage: (update: (p: StudioPage) => StudioPage, id?: string) => void
  publishContent: (pageId: string, document: PageDocument) => Promise<PublishOutcome>
  change: (mutate: (d: PageDocument) => void) => void
  keepSelectionValid: (d: PageDocument) => void
  select: (sel: Selection) => void
}

/**
 * Publishing the page (PUT /pages/{id}/content) and discarding unpublished changes. The server checks the page:
 * errors stop publishing and warnings come back afterwards; the publish dialog shows them while `issues` is set.
 */
export function useStudioPublishing({ page, writePage, publishContent, change, keepSelectionValid, select }: PublishingContext) {
  const { setPageStatus } = usePages()
  const { logActivity } = useActivity()
  const { notify } = useToast()

  const publishing = ref(false)
  /** What to fix (errors) or look at (warnings after publishing); the publish dialog shows it while set. */
  const issues = shallowRef<ContentIssueDto[] | null>(null)

  const isPublished = computed(() => !!page.value?.published)
  const hasChanges = computed(() => !!page.value && (!page.value.published || JSON.stringify(page.value.draft) !== JSON.stringify(page.value.published)))

  async function publish() {
    const p = page.value
    if (!p || publishing.value) return

    publishing.value = true
    // What is sent is what goes live. Kept as is: the server's JSON spells some defaults differently (e.g. null overrides).
    const sent = clone(p.draft)
    try {
      const outcome = await publishContent(p.pageId, sent)
      switch (outcome.kind) {
        case 'published': {
          writePage((old) => ({ ...old, published: sent, publishedAt: Date.parse(outcome.content.updatedAt) }), p.pageId)
          setPageStatus(p.pageId, 'live')
          issues.value = outcome.warnings.length ? outcome.warnings : null
          break
        }
        case 'invalid':
          if (outcome.issues.length) {
            issues.value = outcome.issues
            break
          }
          // The studio produced something the API refuses: a bug, not something the person can fix.
          console.error('The API refused the page:', outcome.errors)
          notify('Couldn’t publish: the page has something the server doesn’t accept', 'error')
          break
        case 'not-found':
          notify('This page doesn’t exist on the server, so it can’t be published', 'error')
          break
        default:
          notify('Couldn’t publish the page. Try again.', 'error')
      }
    } finally {
      publishing.value = false
    }
  }

  /** Selects what an issue is about, so its settings open. */
  function showIssue(issue: ContentIssueDto) {
    issues.value = null
    if (issue.blockId && issue.sectionId) select({ kind: 'block', sectionId: issue.sectionId, blockId: issue.blockId })
    else if (issue.sectionId) select({ kind: 'section', sectionId: issue.sectionId })
  }

  /** Throws away the edits since the last publish (as one undo step). */
  function discard() {
    const published = page.value?.published
    if (!published || !hasChanges.value) return

    change((d) => Object.assign(d, clone(published)))
    keepSelectionValid(published)
    logActivity('You discarded unpublished changes', 'page')
    notify('Changes discarded')
  }

  return {
    publishing,
    issues,
    isPublished,
    hasChanges,
    publish,
    showIssue,
    discard,
  }
}
