<script setup lang="ts">
import { computed, ref, shallowRef, watch } from 'vue'
import { CalendarClock, Check, CircleAlert, LoaderCircle, TriangleAlert, X } from 'lucide-vue-next'
import { FButton, FModal, cn } from '@forma/design-system'
import type { DraftDto, DraftKind, EntityRefDto, PublishCheckDto, PublishIssueDto, ScheduledPublishDto } from '@/api/generated'
import { useDrafts } from '@/composables/useDrafts'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { formatWhen, usePublishing } from '@/composables/usePublishing'
import { selectionOf, useScheduledPublishes } from '@/composables/useScheduledPublishes'
import { usePublishingQuerier } from '@/queriers/usePublishingQuerier'
import { editorRoute } from '@/utils/publishing'
import { timeAgo } from '@/utils/time'

/**
 * The publish dialog (docs/design/drafts-and-publish.md § 4.2): every draft of the website with a checkbox, the live
 * result of the server's check, Publish now and Schedule…; and a Scheduled tab with what is coming up or didn't go live.
 */
const { dialog, schedule } = usePublishDialog()
const publishing = usePublishing()
const querier = usePublishingQuerier()

const websiteId = computed(() => dialog.value?.websiteId ?? '')
const { drafts, query: draftsQuery } = useDrafts(websiteId)
const { pending, failed } = useScheduledPublishes(websiteId)

const open = computed({
  get: () => !!dialog.value,
  set: (v) => {
    if (!v) dialog.value = null
  },
})

const tab = ref<'changes' | 'scheduled'>('changes')

/* ---------- What is ticked ---------- */
const key = (e: { kind: DraftKind; id: string }) => `${e.kind}:${e.id}`

const ticked = ref<Record<string, EntityRefDto>>({})
/** Needed drafts the person unticked: they stay unticked, and the check says why that blocks. */
const declined = ref(new Set<string>())
/** Drafts ticked because others need them, with what needs them. */
const neededBy = ref<Record<string, string[]>>({})

const check = shallowRef<PublishCheckDto | null>(null)
const checking = ref(false)
const busy = ref(false)

watch(
  dialog,
  (d) => {
    if (!d) return
    tab.value = d.tab
    ticked.value = Object.fromEntries(d.ticked.map((e) => [key(e), e]))
    declined.value = new Set()
    neededBy.value = {}
    check.value = null
    void draftsQuery.refetch()
  },
  { immediate: true },
)

const isScheduled = (d: DraftDto) => !!d.scheduledPublishId
const tickedList = computed(() => Object.values(ticked.value))

function toggle(d: DraftDto) {
  const k = key(d.entity)
  const next = { ...ticked.value }
  if (next[k]) {
    delete next[k]
    if (neededBy.value[k]) declined.value.add(k)
  } else {
    next[k] = d.entity
    declined.value.delete(k)
  }
  ticked.value = next
}

/* ---------- The server's check, live ---------- */
let timer: ReturnType<typeof setTimeout> | undefined
let run = 0

watch(
  [tickedList, open],
  () => {
    clearTimeout(timer)
    if (!open.value || !tickedList.value.length) {
      check.value = null
      return
    }
    checking.value = true
    timer = setTimeout(runCheck, 250)
  },
  { deep: true },
)

async function runCheck() {
  const mine = ++run
  const result = await querier.check({ websiteId: websiteId.value, ...selectionOf(tickedList.value) })
  if (mine !== run) return
  checking.value = false
  check.value = result
  if (result) tickNeeded(result)
}

/** Ticking something that needs other drafts ticks them too, unless the person unticked them. */
function tickNeeded(result: PublishCheckDto) {
  const add: Record<string, EntityRefDto> = {}
  const by = { ...neededBy.value }

  for (const need of result.needs) {
    const k = key(need.entity)
    by[k] = [...new Set([...(by[k] ?? []), ...need.neededBy.map((n) => n.name)])]
    const draft = drafts.value.find((d) => key(d.entity) === k)
    if (!ticked.value[k] && !declined.value.has(k) && draft && !isScheduled(draft)) add[k] = need.entity
  }

  neededBy.value = by
  if (Object.keys(add).length) ticked.value = { ...ticked.value, ...add }
}

const errors = computed(() => check.value?.issues.filter((i) => i.severity === 'error') ?? [])
const warnings = computed(() => check.value?.issues.filter((i) => i.severity === 'warning') ?? [])
const canPublish = computed(() => !!tickedList.value.length && !checking.value && !!check.value?.canPublish)

/* ---------- Lists ---------- */
const groups = computed(() =>
  (
    [
      { kind: 'collection', label: 'Collections' },
      { kind: 'page', label: 'Pages' },
      { kind: 'item', label: 'Items' },
    ] as const
  )
    .map((g) => ({ ...g, drafts: drafts.value.filter((d) => d.entity.kind === g.kind) }))
    .filter((g) => g.drafts.length),
)

const edited = (d: DraftDto) => (d.isNew ? 'New' : d.updatedAt ? `Edited ${timeAgo(Date.parse(d.updatedAt))}` : 'Edited')
const changes = (n: number) => (n === 1 ? '1 change' : `${n} changes`)

/* ---------- Actions ---------- */
async function publishNow() {
  if (!canPublish.value) return
  busy.value = true
  try {
    const outcome = await publishing.publishNow(websiteId.value, tickedList.value)
    if (outcome.kind === 'published') dialog.value = null
    else if (outcome.kind === 'blocked' && check.value) check.value = { ...check.value, canPublish: false, issues: outcome.issues, needs: outcome.needs }
  } finally {
    busy.value = false
  }
}

function scheduleTicked() {
  if (!canPublish.value) return
  schedule.value = { websiteId: websiteId.value, entries: tickedList.value }
  dialog.value = null
}

/* ---------- Scheduled tab ---------- */
const working = ref<string | null>(null)

async function publishScheduledNow(p: ScheduledPublishDto) {
  working.value = p.id
  try {
    const outcome = await publishing.publishNow(websiteId.value, p.entries)
    // Publishing them takes them out of a pending publish; a failed one is cancelled here.
    if (outcome.kind === 'published' && p.status === 'failed') await publishing.cancel(p.id, { quiet: true })
    if (outcome.kind === 'blocked') failedIssues.value = { ...failedIssues.value, [p.id]: outcome.issues }
  } finally {
    working.value = null
  }
}

async function cancelScheduled(p: ScheduledPublishDto) {
  working.value = p.id
  try {
    await publishing.cancel(p.id)
  } finally {
    working.value = null
  }
}

function changeTime(p: ScheduledPublishDto) {
  schedule.value = { websiteId: websiteId.value, entries: p.entries, publishId: p.id, initial: Date.parse(p.publishAt) }
  dialog.value = null
}

/** Issues of a Try again that was blocked, shown in place of the ones it failed with. */
const failedIssues = ref<Record<string, PublishIssueDto[]>>({})
const issuesOf = (p: ScheduledPublishDto) => (failedIssues.value[p.id] ?? p.issues).filter((i) => i.severity === 'error')

const close = () => (dialog.value = null)
const scheduledCount = computed(() => pending.value.length + failed.value.length)
</script>

<template>
  <FModal v-model:open="open" size="lg" labelledby="publish-changes-title" fullscreen-on-mobile>
    <div class="flex max-h-[88dvh] flex-col">
      <div class="flex items-center gap-4 border-b border-charcoal/[0.06] px-6 pb-3 pt-5">
        <h2 id="publish-changes-title" class="font-display text-xl font-bold tracking-[-0.02em]">Publish changes</h2>
        <div role="tablist" class="ml-auto flex gap-1 rounded-xl bg-canvas p-1 text-sm">
          <button
            type="button"
            role="tab"
            :aria-selected="tab === 'changes'"
            :class="cn('h-8 rounded-lg px-3', tab === 'changes' ? 'bg-white font-medium shadow-soft' : 'text-muted hover:text-charcoal')"
            @click="tab = 'changes'"
          >
            Changes <span class="text-subtle">{{ drafts.length }}</span>
          </button>
          <button
            type="button"
            role="tab"
            :aria-selected="tab === 'scheduled'"
            :class="cn('h-8 rounded-lg px-3', tab === 'scheduled' ? 'bg-white font-medium shadow-soft' : 'text-muted hover:text-charcoal')"
            @click="tab = 'scheduled'"
          >
            Scheduled <span :class="failed.length ? 'text-danger' : 'text-subtle'">{{ scheduledCount }}</span>
          </button>
        </div>
        <button type="button" class="grid size-8 place-items-center rounded-full hover:bg-canvas" aria-label="Close" @click="close">
          <X class="size-4" aria-hidden="true" />
        </button>
      </div>

      <!-- Changes -->
      <template v-if="tab === 'changes'">
        <div class="min-h-0 flex-1 overflow-y-auto px-6 py-4">
          <p v-if="!drafts.length" class="py-10 text-center text-sm text-muted">Everything is live. There are no changes to publish.</p>
          <section v-for="g in groups" :key="g.kind" class="mb-4">
            <h3 class="mb-1 text-xs font-semibold uppercase tracking-wide text-subtle">{{ g.label }}</h3>
            <ul class="flex flex-col">
              <li v-for="d in g.drafts" :key="key(d.entity)">
                <label
                  :class="
                    cn(
                      'flex cursor-pointer items-start gap-3 rounded-xl px-2 py-2.5 hover:bg-canvas',
                      isScheduled(d) && 'cursor-not-allowed opacity-70 hover:bg-transparent',
                    )
                  "
                >
                  <input
                    type="checkbox"
                    class="mt-0.5 size-4 accent-charcoal"
                    :checked="!!ticked[key(d.entity)]"
                    :disabled="isScheduled(d)"
                    @change="toggle(d)"
                  />
                  <span class="min-w-0 flex-1">
                    <span class="flex flex-wrap items-baseline gap-x-3">
                      <span class="font-medium">{{ d.entity.name }}</span>
                      <span class="text-sm text-muted">{{ d.summary }}</span>
                    </span>
                    <span v-for="(line, i) in d.details" :key="i" class="block text-sm text-muted">{{ line }}</span>
                  </span>
                  <span class="flex shrink-0 flex-col items-end gap-1 text-xs">
                    <span v-if="isScheduled(d)" class="inline-flex items-center gap-1 rounded-full bg-accent-50 px-2 py-0.5 font-medium text-accent-700">
                      <CalendarClock class="size-3" aria-hidden="true" /> Scheduled · {{ formatWhen(d.scheduledAt!) }}
                    </span>
                    <span v-else class="text-subtle">{{ edited(d) }}</span>
                    <span v-if="neededBy[key(d.entity)]?.length && !isScheduled(d)" :class="cn('rounded-full px-2 py-0.5', declined.has(key(d.entity)) ? 'bg-danger/10 text-danger' : 'bg-canvas text-charcoal/70')">
                      needed by {{ neededBy[key(d.entity)]!.join(', ') }}
                    </span>
                  </span>
                </label>
              </li>
            </ul>
          </section>
        </div>

        <div class="border-t border-charcoal/[0.06] px-6 py-4">
          <div class="mb-3 min-h-6 text-sm" role="status" aria-live="polite">
            <p v-if="!tickedList.length" class="text-muted">Tick the changes you want to put live.</p>
            <p v-else-if="checking" class="flex items-center gap-2 text-muted">
              <LoaderCircle class="size-4 animate-spin" aria-hidden="true" /> Checking…
            </p>
            <p v-else-if="!check" class="text-danger">Couldn’t check these changes. Try again.</p>
            <template v-else-if="check.canPublish">
              <p class="flex items-center gap-2 font-medium text-positive">
                <Check class="size-4" aria-hidden="true" /> Ready to publish {{ changes(tickedList.length) }}
              </p>
              <p v-if="warnings.length" class="mt-1 flex items-center gap-2 text-muted">
                <TriangleAlert class="size-4 text-accent" aria-hidden="true" /> {{ warnings.length }} worth a look after publishing.
              </p>
            </template>
            <ul v-else class="flex max-h-40 flex-col gap-1.5 overflow-y-auto">
              <li v-for="(issue, i) in errors" :key="i" class="flex items-start gap-2">
                <CircleAlert class="mt-0.5 size-4 shrink-0 text-danger" aria-hidden="true" />
                <span class="min-w-0 flex-1">
                  <template v-if="issue.entity"><strong>{{ issue.entity.name }}</strong>: </template>{{ issue.message }}
                </span>
                <RouterLink v-if="issue.entity" :to="editorRoute(issue.entity)" class="shrink-0 font-medium underline" @click="close">Open</RouterLink>
              </li>
            </ul>
          </div>
          <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <FButton variant="secondary" size="sm" :disabled="!canPublish" @click="scheduleTicked">Schedule…</FButton>
            <FButton size="sm" :disabled="!canPublish || busy" @click="publishNow">{{ busy ? 'Publishing…' : 'Publish now' }}</FButton>
          </div>
        </div>
      </template>

      <!-- Scheduled -->
      <div v-else class="min-h-0 flex-1 overflow-y-auto px-6 py-4">
        <p v-if="!scheduledCount" class="py-10 text-center text-sm text-muted">Nothing is scheduled. Use Schedule… to put changes live at a set time.</p>
        <ul class="flex flex-col gap-3">
          <li v-for="p in [...failed, ...pending]" :key="p.id" :class="cn('rounded-2xl p-4 ring-1', p.status === 'failed' ? 'bg-danger/[0.04] ring-danger/20' : 'ring-charcoal/[0.08]')">
            <div class="flex flex-wrap items-start gap-3">
              <div class="min-w-0 flex-1">
                <p v-if="p.status === 'failed'" class="flex items-center gap-2 font-medium text-danger">
                  <CircleAlert class="size-4" aria-hidden="true" /> Didn’t go live on {{ formatWhen(p.publishAt) }}
                </p>
                <p v-else class="flex items-center gap-2 font-medium">
                  <CalendarClock class="size-4 text-accent" aria-hidden="true" /> {{ formatWhen(p.publishAt) }}
                </p>
                <p class="mt-1 text-sm text-muted">{{ p.entries.map((e) => e.name).join(', ') || 'Nothing left in it' }}</p>
              </div>
              <div class="flex flex-wrap gap-2">
                <FButton size="sm" variant="secondary" :disabled="working === p.id" @click="changeTime(p)">Change time</FButton>
                <FButton size="sm" variant="secondary" :disabled="working === p.id" @click="cancelScheduled(p)">Cancel</FButton>
                <FButton size="sm" :disabled="working === p.id" @click="publishScheduledNow(p)">{{ p.status === 'failed' ? 'Try again' : 'Publish now' }}</FButton>
              </div>
            </div>
            <ul v-if="p.status === 'failed' || failedIssues[p.id]" class="mt-3 flex flex-col gap-1.5 text-sm">
              <li v-for="(issue, i) in issuesOf(p)" :key="i" class="flex items-start gap-2">
                <span class="min-w-0 flex-1"><template v-if="issue.entity"><strong>{{ issue.entity.name }}</strong>: </template>{{ issue.message }}</span>
                <RouterLink v-if="issue.entity" :to="editorRoute(issue.entity)" class="shrink-0 font-medium underline" @click="close">Open</RouterLink>
              </li>
            </ul>
          </li>
        </ul>
      </div>
    </div>
  </FModal>
</template>
