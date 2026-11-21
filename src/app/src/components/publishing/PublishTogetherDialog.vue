<script setup lang="ts">
import { computed, ref } from 'vue'
import { Layers } from 'lucide-vue-next'
import { FButton, FModal } from '@forma/design-system'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { usePublishing } from '@/composables/usePublishing'

/**
 * "Publish together?": Publish from an editor found that it needs other drafts. Rather than failing, it says which
 * and offers to publish them in one go.
 */
const { together, openDialog } = usePublishDialog()
const publishing = usePublishing()
const busy = ref(false)

const open = computed({
  get: () => !!together.value,
  set: (v) => {
    if (!v) together.value = null
  },
})

const prompt = computed(() => together.value)
const all = computed(() => (prompt.value ? [prompt.value.entity, ...prompt.value.needs.map((n) => n.entity)] : []))
const names = computed(() => all.value.map((e) => e.name))
/** "Blog posts", "Blog posts and Home", "Blog posts, Home and About". */
const others = computed(() => {
  const rest = names.value.slice(1)
  return rest.length > 1 ? `${rest.slice(0, -1).join(', ')} and ${rest.at(-1)}` : (rest[0] ?? '')
})
const primaryLabel = computed(() => (all.value.length === 2 ? 'Publish both' : `Publish all ${all.value.length}`))

async function publishAll() {
  const p = prompt.value
  if (!p) return
  const entities = all.value
  busy.value = true
  try {
    const outcome = await publishing.publishNow(p.websiteId, entities)
    together.value = null
    if (outcome.kind === 'blocked') openDialog(p.websiteId, entities)
  } finally {
    busy.value = false
  }
}

function choose() {
  const p = prompt.value
  if (!p) return
  const entities = all.value
  together.value = null
  openDialog(p.websiteId, entities)
}
</script>

<template>
  <FModal v-model:open="open" size="sm" labelledby="together-title">
    <div v-if="prompt" class="flex flex-col gap-5 p-6 sm:p-7">
      <span class="grid size-11 place-items-center rounded-2xl bg-accent-50 text-accent-700">
        <Layers class="size-5" aria-hidden="true" />
      </span>
      <div>
        <h2 id="together-title" class="font-display text-xl font-bold tracking-[-0.02em]">Publish together?</h2>
        <ul class="mt-2 flex flex-col gap-1.5 text-sm text-charcoal/80">
          <li v-for="(issue, i) in prompt.issues.slice(0, 4)" :key="i">
            <template v-if="issue.entity && !issue.message.startsWith(issue.entity.name)"><strong>{{ issue.entity.name }}</strong>: </template>{{ issue.message }}
          </li>
        </ul>
        <p class="mt-3 text-sm text-muted">
          {{ prompt.entity.name }} can’t go live on its own. Publishing
          {{ others }} with it puts everything live at the same moment.
        </p>
      </div>
      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <FButton variant="secondary" size="sm" @click="open = false">Cancel</FButton>
        <FButton variant="secondary" size="sm" @click="choose">Choose what to publish…</FButton>
        <FButton size="sm" data-autofocus :disabled="busy" @click="publishAll">{{ busy ? 'Publishing…' : primaryLabel }}</FButton>
      </div>
    </div>
  </FModal>
</template>
