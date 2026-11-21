<script setup lang="ts">
import { computed } from 'vue'
import { ChevronRight, CircleAlert, TriangleAlert } from 'lucide-vue-next'
import { FButton, FModal } from '@forma/design-system'
import type { PublishIssueDto } from '@/api/generated'
import { useStudio } from '@/composables/useStudio'
import { blockDefinition } from '@/studio/blocks'
import { sectionTypeLabels } from '@/studio/sections'

/**
 * What the publish check said about this page: errors that stopped it from being published, or warnings worth a look
 * after it was published. Choosing an issue selects the block or section it is about.
 */
const studio = useStudio()

const open = computed({
  get: () => !!studio.issues.value,
  set: (v) => {
    if (!v) studio.issues.value = null
  },
})
const errors = computed(() => studio.issues.value?.filter((i) => i.severity === 'error') ?? [])
const warnings = computed(() => studio.issues.value?.filter((i) => i.severity === 'warning') ?? [])

/** Where on the page an issue is, e.g. "Image in Hero" or "Gallery, image 2". */
function where(issue: PublishIssueDto) {
  const section = studio.doc.value?.sections.find((s) => s.id === issue.sectionId)
  if (!section) return 'Page'
  const sectionLabel = section.settings.name || sectionTypeLabels[section.settings.type]
  const block = section.blocks.find((b) => b.id === issue.blockId)
  if (!block) return `${sectionLabel} section`
  const item = issue.itemIndex != null ? `, item ${issue.itemIndex + 1}` : ''
  return `${blockDefinition(block.type).label}${item} in ${sectionLabel}`
}
</script>

<template>
  <FModal v-model:open="open" size="md" labelledby="publish-issues-title">
    <div class="flex max-h-[80dvh] flex-col gap-5 p-6 sm:p-7">
      <div>
        <h2 id="publish-issues-title" class="font-display text-xl font-bold tracking-[-0.02em]">
          {{ errors.length ? 'Fix these before publishing' : 'Published' }}
        </h2>
        <p class="mt-2 text-sm text-muted">
          {{
            errors.length
              ? 'Visitors would see these as broken. Choose one to open its settings.'
              : 'Your page is live. Nothing is broken, but these are worth a look. Choose one to open its settings.'
          }}
        </p>
      </div>

      <div class="-mx-2 flex min-h-0 flex-col gap-4 overflow-y-auto px-2">
        <section v-if="errors.length" aria-labelledby="issues-errors">
          <h3 id="issues-errors" class="mb-2 text-xs font-semibold uppercase tracking-wide text-danger">
            {{ errors.length }} to fix
          </h3>
          <ul class="flex flex-col gap-1.5">
            <li v-for="(issue, i) in errors" :key="`e${i}`">
              <button
                type="button"
                class="flex w-full items-start gap-3 rounded-xl bg-danger/[0.06] px-3 py-2.5 text-left transition-colors hover:bg-danger/10"
                @click="studio.showIssue(issue)"
              >
                <CircleAlert class="mt-0.5 size-4 shrink-0 text-danger" aria-hidden="true" />
                <span class="min-w-0 flex-1">
                  <span class="block text-sm font-medium">{{ issue.message }}</span>
                  <span class="block text-xs text-muted">{{ where(issue) }}</span>
                </span>
                <ChevronRight class="mt-0.5 size-4 shrink-0 text-muted" aria-hidden="true" />
              </button>
            </li>
          </ul>
        </section>

        <details v-if="warnings.length" :open="!errors.length && warnings.length <= 5" class="group">
          <summary class="mb-2 cursor-pointer text-xs font-semibold uppercase tracking-wide text-muted">
            {{ warnings.length }} worth a look
          </summary>
          <ul class="flex flex-col gap-1.5">
            <li v-for="(issue, i) in warnings" :key="`w${i}`">
              <button
                type="button"
                class="flex w-full items-start gap-3 rounded-xl bg-canvas px-3 py-2.5 text-left transition-colors hover:bg-charcoal/[0.06]"
                @click="studio.showIssue(issue)"
              >
                <TriangleAlert class="mt-0.5 size-4 shrink-0 text-accent" aria-hidden="true" />
                <span class="min-w-0 flex-1">
                  <span class="block text-sm">{{ issue.message }}</span>
                  <span class="block text-xs text-muted">{{ where(issue) }}</span>
                </span>
                <ChevronRight class="mt-0.5 size-4 shrink-0 text-muted" aria-hidden="true" />
              </button>
            </li>
          </ul>
        </details>
      </div>

      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <FButton variant="secondary" size="sm" data-autofocus @click="open = false">Close</FButton>
      </div>
    </div>
  </FModal>
</template>
