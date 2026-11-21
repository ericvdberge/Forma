<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { CalendarClock, ChevronDown, Layers, Send, Undo2 } from 'lucide-vue-next'
import { FButton, cn } from '@forma/design-system'
import type { EntityRefDto, PublishIssueDto } from '@/api/generated'
import { useDismiss } from '@/composables/useDismiss'
import { useDrafts } from '@/composables/useDrafts'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { formatWhen, usePublishing } from '@/composables/usePublishing'
import { useScheduledPublishes } from '@/composables/useScheduledPublishes'
import { timeAgo } from '@/utils/time'

/**
 * The top-right corner every editor shares (docs/design/drafts-and-publish.md § 4.1): the status chip, **Save**
 * (also Ctrl+S) and the **Publish** split button with Publish now, Schedule…, Publish with other changes… and Discard changes.
 */
const props = defineProps<{
  websiteId: string
  /** What is being edited; null until it is saved for the first time. */
  entity: EntityRefDto | null
  /** Whether there are edits that aren't saved yet. */
  dirty: boolean
  /** Whether it was ever published. */
  isLive: boolean
  /** Whether the server has a draft: saved changes that aren't live. */
  hasDraft: boolean
  draftSavedAt: number | null
  /** Saves the draft and gives back what was saved, or null when saving failed (the editor says why). */
  save: () => Promise<EntityRefDto | null>
  /** Goes back to the live version. True when it did. */
  discard?: () => Promise<boolean>
  /** What it is called in sentences, e.g. "Blog posts". */
  name: string
}>()
const emit = defineEmits<{ blocked: [issues: PublishIssueDto[]]; published: [warnings: PublishIssueDto[]] }>()

const { draftOf } = useDrafts(() => props.websiteId)
const { pendingFor, failedFor } = useScheduledPublishes(() => props.websiteId)
const publishing = usePublishing()
const { schedule, openDialog } = usePublishDialog()

const busy = ref<null | 'save' | 'publish'>(null)
const menuOpen = ref(false)
const confirmingDiscard = ref(false)
const root = ref<HTMLElement | null>(null)
useDismiss(root, () => {
  menuOpen.value = false
  confirmingDiscard.value = false
})

const scheduled = computed(() => (props.entity ? pendingFor(props.entity.kind, props.entity.id) : undefined))
const scheduledAt = computed(() => scheduled.value?.publishAt ?? (props.entity ? draftOf(props.entity.kind, props.entity.id)?.scheduledAt : undefined))
const failed = computed(() => (props.entity ? failedFor(props.entity.kind, props.entity.id) : undefined))

/** One of: Live · Unsaved changes · Draft · saved 2 min ago · Scheduled · Mon 24 Nov, 09:00 · Not published: needs attention. */
const status = computed(() => {
  if (props.dirty) return { label: 'Unsaved changes', tone: 'accent' }
  if (failed.value) return { label: 'Not published: needs attention', tone: 'danger' }
  if (scheduledAt.value) return { label: `Scheduled · ${formatWhen(scheduledAt.value)}`, tone: 'accent' }
  if (props.hasDraft) return { label: props.draftSavedAt ? `Draft · saved ${timeAgo(props.draftSavedAt)}` : 'Draft', tone: 'muted' }
  if (props.isLive) return { label: 'Live', tone: 'positive' }
  return { label: 'Not published yet', tone: 'muted' }
})
const tones: Record<string, string> = {
  accent: 'bg-accent-50 text-accent-700',
  danger: 'bg-danger/10 text-danger',
  muted: 'bg-canvas text-charcoal/70',
  positive: 'bg-positive/10 text-positive',
}

/** Something to publish: saved or unsaved changes, or something that was never published. */
const canPublish = computed(() => props.dirty || props.hasDraft || !props.isLive)
const canDiscard = computed(() => !!props.discard && props.isLive && (props.dirty || props.hasDraft))

/** Saves when there is anything unsaved, and gives back what to publish. */
async function saved(): Promise<EntityRefDto | null> {
  if (!props.dirty && props.entity) return props.entity
  return props.save()
}

async function onSave() {
  if (busy.value) return
  busy.value = 'save'
  try {
    await props.save()
  } finally {
    busy.value = null
  }
}

async function onPublish() {
  menuOpen.value = false
  if (busy.value) return
  busy.value = 'publish'
  try {
    const entity = await saved()
    if (!entity) return
    const outcome = await publishing.publishOne(props.websiteId, entity)
    if (outcome.kind === 'blocked') emit('blocked', outcome.issues)
    if (outcome.kind === 'published') emit('published', outcome.warnings)
  } finally {
    busy.value = null
  }
}

async function onSchedule() {
  menuOpen.value = false
  const entity = await saved()
  if (entity) schedule.value = { websiteId: props.websiteId, entries: [entity] }
}

async function onPublishWithOthers() {
  menuOpen.value = false
  const entity = await saved()
  if (entity) openDialog(props.websiteId, [entity])
}

async function onDiscard() {
  confirmingDiscard.value = false
  if (!props.discard) return
  await props.discard()
}

const scheduledDay = computed(() =>
  scheduledAt.value ? new Date(scheduledAt.value).toLocaleDateString(undefined, { weekday: 'long' }) : '',
)

/* Ctrl+S (⌘S) saves, wherever the cursor is. */
function onKey(e: KeyboardEvent) {
  if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 's') {
    e.preventDefault()
    void onSave()
  }
}
onMounted(() => window.addEventListener('keydown', onKey))
onBeforeUnmount(() => window.removeEventListener('keydown', onKey))

const item = 'flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm hover:bg-canvas disabled:pointer-events-none disabled:opacity-40'
</script>

<template>
  <div ref="root" class="relative flex flex-wrap items-center gap-2">
    <span :class="cn('inline-flex h-8 items-center rounded-full px-3 text-xs font-medium', tones[status.tone])" role="status">
      {{ status.label }}
    </span>

    <FButton size="sm" variant="secondary" :disabled="!!busy || (!dirty && !!entity)" title="Save (Ctrl+S)" @click="onSave">
      {{ busy === 'save' ? 'Saving…' : 'Save' }}
    </FButton>

    <div class="flex">
      <button
        type="button"
        class="inline-flex h-9 items-center gap-2 rounded-l-full bg-charcoal pl-4 pr-3 text-sm font-medium text-warm-white transition-colors hover:bg-ink disabled:opacity-40"
        :disabled="!!busy || !canPublish"
        @click="onPublish"
      >
        <Send class="size-4" aria-hidden="true" /> {{ busy === 'publish' ? 'Publishing…' : 'Publish' }}
      </button>
      <button
        type="button"
        class="inline-flex h-9 items-center rounded-r-full border-l border-warm-white/20 bg-charcoal px-2.5 text-warm-white transition-colors hover:bg-ink disabled:opacity-40"
        :disabled="!!busy"
        :aria-expanded="menuOpen"
        aria-label="More ways to publish"
        @click="menuOpen = !menuOpen; confirmingDiscard = false"
      >
        <ChevronDown class="size-4" aria-hidden="true" />
      </button>
    </div>

    <div v-if="menuOpen" class="absolute right-0 top-full z-50 mt-2 w-72 rounded-2xl bg-white p-2 shadow-float ring-1 ring-charcoal/[0.06]" role="menu">
      <button type="button" role="menuitem" :class="item" :disabled="!canPublish" @click="onPublish">
        <Send class="size-4 text-charcoal/70" aria-hidden="true" /> Publish now
      </button>
      <button type="button" role="menuitem" :class="item" :disabled="!canPublish" @click="onSchedule">
        <CalendarClock class="size-4 text-charcoal/70" aria-hidden="true" /> Schedule…
      </button>
      <button type="button" role="menuitem" :class="item" @click="onPublishWithOthers">
        <Layers class="size-4 text-charcoal/70" aria-hidden="true" /> Publish with other changes…
      </button>
      <button v-if="canDiscard" type="button" role="menuitem" :class="cn(item, 'text-danger')" @click="menuOpen = false; confirmingDiscard = true">
        <Undo2 class="size-4" aria-hidden="true" /> Discard changes
      </button>
    </div>

    <div
      v-if="confirmingDiscard"
      class="absolute right-0 top-full z-50 mt-2 w-80 rounded-2xl bg-white p-4 shadow-float ring-1 ring-charcoal/[0.06]"
      role="alertdialog"
      aria-labelledby="discard-title"
    >
      <p id="discard-title" class="text-sm font-medium">Discard your changes to {{ name }}?</p>
      <p class="mt-1 text-sm text-muted">
        The live version stays as it is.<template v-if="scheduledAt"> It’s also taken out of {{ scheduledDay }}’s publish.</template>
      </p>
      <div class="mt-3 flex justify-end gap-2">
        <FButton size="sm" variant="secondary" @click="confirmingDiscard = false">Keep editing</FButton>
        <button type="button" class="inline-flex h-9 items-center rounded-full bg-danger px-4 text-sm font-medium text-white hover:bg-danger/90" @click="onDiscard">
          Discard changes
        </button>
      </div>
    </div>
  </div>
</template>
