<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { CalendarClock } from 'lucide-vue-next'
import { FButton, FModal } from '@forma/design-system'
import type { PublishIssueDto } from '@/api/generated'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { usePublishing } from '@/composables/usePublishing'
import { timeZone } from '@/utils/publishing'

/** Asks when to publish: a date and a time in the browser's time zone, which it names. It must be in the future. */
const { schedule } = usePublishDialog()
const publishing = usePublishing()

const open = computed({
  get: () => !!schedule.value,
  set: (v) => {
    if (!v) schedule.value = null
  },
})

const date = ref('')
const time = ref('')
const error = ref('')
const issues = ref<PublishIssueDto[]>([])
const busy = ref(false)

const pad = (n: number) => String(n).padStart(2, '0')
const localDate = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
const localTime = (d: Date) => `${pad(d.getHours())}:${pad(d.getMinutes())}`

// Start at the time it had, or the next full hour.
watch(schedule, (s) => {
  if (!s) return
  const start = s.initial ? new Date(s.initial) : new Date(Math.ceil((Date.now() + 60_000) / 3_600_000) * 3_600_000)
  date.value = localDate(start)
  time.value = localTime(start)
  error.value = ''
  issues.value = []
})

const when = computed(() => (date.value && time.value ? new Date(`${date.value}T${time.value}`) : null))
const inPast = computed(() => !!when.value && when.value.getTime() <= Date.now())
const what = computed(() => {
  const entries = schedule.value?.entries ?? []
  return entries.length === 1 ? entries[0]!.name : `${entries.length} changes`
})

async function confirm() {
  const s = schedule.value
  if (!s || !when.value || inPast.value) return
  busy.value = true
  error.value = ''
  issues.value = []
  try {
    const outcome = s.publishId ? await publishing.reschedule(s.publishId, when.value) : await publishing.schedule(s.websiteId, s.entries, when.value)
    if (outcome.kind === 'scheduled') {
      schedule.value = null
      return
    }
    if (outcome.kind === 'invalid') {
      issues.value = outcome.issues.filter((i) => i.severity === 'error')
      error.value = outcome.errors.publishAt?.[0] ?? outcome.errors.entries?.[0] ?? (issues.value.length ? 'These need attention before it can be scheduled.' : '')
    }
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <FModal v-model:open="open" size="sm" labelledby="schedule-title">
    <form v-if="schedule" class="flex flex-col gap-5 p-6 sm:p-7" @submit.prevent="confirm">
      <span class="grid size-11 place-items-center rounded-2xl bg-accent-50 text-accent-700">
        <CalendarClock class="size-5" aria-hidden="true" />
      </span>
      <div>
        <h2 id="schedule-title" class="font-display text-xl font-bold tracking-[-0.02em]">
          {{ schedule.publishId ? 'Change the time' : `Schedule ${what}` }}
        </h2>
        <p class="mt-2 text-sm text-muted">It goes live as it is at that moment. You can keep editing until then.</p>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <label class="flex flex-col gap-1.5 text-sm font-medium">
          Date
          <input v-model="date" type="date" required class="h-10 rounded-xl bg-white px-3 text-sm font-normal ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent" />
        </label>
        <label class="flex flex-col gap-1.5 text-sm font-medium">
          Time
          <input v-model="time" type="time" required class="h-10 rounded-xl bg-white px-3 text-sm font-normal ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent" />
        </label>
        <p class="col-span-2 text-xs text-muted">Time zone: {{ timeZone() }}</p>
      </div>

      <p v-if="inPast" class="text-sm text-danger" role="alert">Choose a time in the future.</p>
      <div v-else-if="error" class="text-sm text-danger" role="alert">
        <p>{{ error }}</p>
        <ul v-if="issues.length" class="mt-1 list-disc pl-5">
          <li v-for="(issue, i) in issues" :key="i"><template v-if="issue.entity">{{ issue.entity.name }}: </template>{{ issue.message }}</li>
        </ul>
      </div>

      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <FButton variant="secondary" size="sm" type="button" @click="open = false">Cancel</FButton>
        <FButton size="sm" type="submit" :disabled="busy || !when || inPast">{{ busy ? 'Scheduling…' : schedule.publishId ? 'Change time' : 'Schedule' }}</FButton>
      </div>
    </form>
  </FModal>
</template>
