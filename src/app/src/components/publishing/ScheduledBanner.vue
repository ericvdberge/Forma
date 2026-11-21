<script setup lang="ts">
import { computed, ref } from 'vue'
import { CalendarClock, CircleAlert } from 'lucide-vue-next'
import { FButton } from '@forma/design-system'
import type { EntityRefDto } from '@/api/generated'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { formatWhenLong, usePublishing } from '@/composables/usePublishing'
import { useScheduledPublishes } from '@/composables/useScheduledPublishes'

/**
 * In an editor whose draft is scheduled: when it goes live, and that changes saved now go live then too. When the
 * scheduled publish didn't go live: why, with Try again.
 */
const props = defineProps<{ websiteId: string; entity: EntityRefDto | null }>()

const { pendingFor, failedFor } = useScheduledPublishes(() => props.websiteId)
const publishing = usePublishing()
const { openDialog } = usePublishDialog()
const retrying = ref(false)

const pending = computed(() => (props.entity ? pendingFor(props.entity.kind, props.entity.id) : undefined))
const failed = computed(() => (props.entity ? failedFor(props.entity.kind, props.entity.id) : undefined))

/** Try again: publish what was in it now; it leaves the failed publish once it is live. */
async function retry() {
  const p = failed.value
  if (!p) return
  retrying.value = true
  try {
    const outcome = await publishing.publishNow(props.websiteId, p.entries)
    if (outcome.kind === 'published') await publishing.cancel(p.id, { quiet: true })
    else if (outcome.kind === 'blocked') openDialog(props.websiteId, [], 'scheduled')
  } finally {
    retrying.value = false
  }
}
</script>

<template>
  <div v-if="failed" class="flex flex-wrap items-start gap-3 rounded-2xl bg-danger/[0.06] p-4 text-sm" role="alert">
    <CircleAlert class="mt-0.5 size-4 shrink-0 text-danger" aria-hidden="true" />
    <div class="min-w-0 flex-1">
      <p class="font-medium text-danger">The scheduled publish for {{ formatWhenLong(failed.publishAt) }} didn’t go live.</p>
      <ul class="mt-1 list-disc pl-5 text-charcoal/80">
        <li v-for="(issue, i) in failed.issues.slice(0, 3)" :key="i">
          <template v-if="issue.entity">{{ issue.entity.name }}: </template>{{ issue.message }}
        </li>
      </ul>
    </div>
    <div class="flex gap-2">
      <FButton size="sm" variant="secondary" @click="openDialog(websiteId, [], 'scheduled')">Details</FButton>
      <FButton size="sm" :disabled="retrying" @click="retry">{{ retrying ? 'Publishing…' : 'Try again' }}</FButton>
    </div>
  </div>
  <div v-else-if="pending" class="flex items-start gap-3 rounded-2xl bg-accent-50 p-4 text-sm text-accent-700" role="status">
    <CalendarClock class="mt-0.5 size-4 shrink-0" aria-hidden="true" />
    <p>Scheduled to go live on {{ formatWhenLong(pending.publishAt) }}. Changes you save now go live then too.</p>
  </div>
</template>
