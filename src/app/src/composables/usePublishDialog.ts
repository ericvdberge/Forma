import { ref, shallowRef } from 'vue'
import type { EntityRefDto, PublishIssueDto, PublishNeedDto } from '@/api/generated'

/** What the publish dialog opens with. */
export interface PublishDialogRequest {
  websiteId: string
  /** Drafts to tick when it opens. */
  ticked: EntityRefDto[]
  tab: 'changes' | 'scheduled'
}

/** "Publish together?": what Publish needs besides the thing being published. */
export interface PublishTogetherPrompt {
  websiteId: string
  entity: EntityRefDto
  issues: PublishIssueDto[]
  needs: PublishNeedDto[]
}

/** The date and time picker, for a new scheduled publish or another time for one. */
export interface ScheduleRequest {
  websiteId: string
  entries: EntityRefDto[]
  /** Changing the time of this scheduled publish instead of scheduling a new one. */
  publishId?: string
  /** The time it starts at (ms). */
  initial?: number
}

const dialog = shallowRef<PublishDialogRequest | null>(null)
const together = shallowRef<PublishTogetherPrompt | null>(null)
const schedule = shallowRef<ScheduleRequest | null>(null)
/** Bumped after every publish, schedule or discard, so editors reload what changed. */
const published = ref(0)

/**
 * The publishing dialogs, shared by every editor and the top bar. They are mounted once, in `App.vue`; this opens them.
 */
export function usePublishDialog() {
  function openDialog(websiteId: string, ticked: EntityRefDto[] = [], tab: PublishDialogRequest['tab'] = 'changes') {
    dialog.value = { websiteId, ticked, tab }
  }

  return {
    dialog,
    together,
    schedule,
    published,
    openDialog,
  }
}
