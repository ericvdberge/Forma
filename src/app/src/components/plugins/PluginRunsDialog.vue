<script setup lang="ts">
import { computed } from 'vue'
import { CircleAlert, CircleCheck, Info, RefreshCw, TriangleAlert } from 'lucide-vue-next'
import { FButton, FModal, FSkeleton } from '@forma/design-system'
import type { PluginDto, PluginLogLevel } from '@/api/generated'
import { HOOK_EVENTS, eventLabel, usePlugins } from '@/composables/usePlugins'
import { shortAgo } from '@/utils/time'

/** A plugin's details and its last 25 runs, with what each run logged. Refreshes every few seconds while open. */
const props = defineProps<{ plugin: PluginDto | null }>()
const emit = defineEmits<{ close: [] }>()

const open = computed({
  get: () => !!props.plugin,
  set: (v) => {
    if (!v) emit('close')
  },
})

const { getPluginRuns } = usePlugins()
const { data: runs, isPending, isError, isFetching, refetch } = getPluginRuns(() => props.plugin?.id)

const levelIcon: Record<PluginLogLevel, typeof Info> = { debug: Info, info: Info, warning: TriangleAlert, error: CircleAlert }
const levelTone: Record<PluginLogLevel, string> = {
  debug: 'text-subtle',
  info: 'text-muted',
  warning: 'text-accent-600',
  error: 'text-danger',
}
const fuel = (n: number) => (n >= 1e6 ? `${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `${(n / 1e3).toFixed(1)}k` : `${n}`)
const ms = (n: number) => (n < 1 ? `${n.toFixed(2)} ms` : `${n.toFixed(1)} ms`)
</script>

<template>
  <FModal v-model:open="open" size="lg" labelledby="plugin-runs-title">
    <div v-if="plugin" class="flex max-h-[85dvh] flex-col gap-5 p-6 sm:p-7">
      <header>
        <p class="font-mono text-xs text-subtle">{{ plugin.id }}<template v-if="plugin.version"> · v{{ plugin.version }}</template></p>
        <h2 id="plugin-runs-title" class="mt-1 font-display text-2xl font-bold tracking-[-0.02em]">{{ plugin.name }}</h2>
        <p v-if="plugin.description" class="mt-1.5 text-sm text-muted">{{ plugin.description }}</p>
        <p v-if="!plugin.loaded" class="mt-3 rounded-xl bg-danger/[0.06] px-3 py-2.5 text-sm text-danger">
          Couldn’t load: {{ plugin.error }}
        </p>
        <dl class="mt-4 grid gap-3 text-sm sm:grid-cols-2">
          <div v-for="e in plugin.events" :key="e" class="rounded-xl bg-canvas px-3 py-2.5">
            <dt class="font-medium">{{ eventLabel(e) }}</dt>
            <dd class="mt-0.5 text-xs text-muted">
              <code class="font-mono">{{ e }}</code> ·
              {{
                HOOK_EVENTS.has(e)
                  ? 'Its warnings and errors appear when you publish a page in the studio.'
                  : 'Runs in the background; what it logs shows up below.'
              }}
            </dd>
          </div>
        </dl>
      </header>

      <section class="flex min-h-0 flex-col" aria-labelledby="plugin-runs-heading">
        <div class="mb-2 flex items-center justify-between gap-3">
          <h3 id="plugin-runs-heading" class="text-xs font-semibold uppercase tracking-wide text-muted">
            Recent runs <span class="font-normal normal-case tracking-normal text-subtle">· last 25 since the API started</span>
          </h3>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-xs text-muted hover:bg-canvas hover:text-charcoal"
            @click="refetch()"
          >
            <RefreshCw :class="['size-3.5', isFetching && 'animate-spin']" aria-hidden="true" /> Refresh
          </button>
        </div>

        <div class="-mx-2 min-h-0 overflow-y-auto px-2">
          <div v-if="isPending" class="flex flex-col gap-2">
            <FSkeleton v-for="i in 3" :key="i" class="h-16 rounded-xl" />
          </div>
          <p v-else-if="isError" class="rounded-xl bg-canvas px-4 py-6 text-center text-sm text-muted">
            Couldn’t load the runs. <button type="button" class="underline" @click="refetch()">Try again</button>
          </p>
          <p v-else-if="!runs?.length" class="rounded-xl bg-canvas px-4 py-8 text-center text-sm text-muted">
            It hasn’t run yet. Open a page in the studio and choose <strong class="text-charcoal">Publish</strong> to run its checks.
          </p>
          <ol v-else class="flex flex-col gap-2">
            <li v-for="run in runs" :key="run.id" class="rounded-xl ring-1 ring-charcoal/[0.06]">
              <div class="flex flex-wrap items-center gap-x-3 gap-y-1 px-3 py-2.5">
                <CircleCheck v-if="run.succeeded" class="size-4 shrink-0 text-positive" aria-label="Succeeded" />
                <CircleAlert v-else class="size-4 shrink-0 text-danger" aria-label="Failed" />
                <span class="text-sm font-medium">{{ eventLabel(run.event) }}</span>
                <code class="font-mono text-xs text-subtle">{{ run.event }}</code>
                <span class="ml-auto flex gap-3 text-xs tabular-nums text-subtle">
                  <span :title="`${run.fuelConsumed.toLocaleString('en')} instructions`">{{ fuel(run.fuelConsumed) }} fuel</span>
                  <span>{{ ms(run.durationMs) }}</span>
                  <time :datetime="run.startedAt" :title="new Date(run.startedAt).toLocaleString()">
                    {{ shortAgo(Date.parse(run.startedAt)) }}
                  </time>
                </span>
              </div>
              <p v-if="run.error" class="border-t border-line px-3 py-2 text-xs text-danger">{{ run.error }}</p>
              <ul v-if="run.logs.length" class="border-t border-line bg-canvas/60 px-3 py-2 font-mono text-xs">
                <li v-for="(line, i) in run.logs" :key="i" :class="['flex items-start gap-2 py-0.5', levelTone[line.level]]">
                  <component :is="levelIcon[line.level]" class="mt-0.5 size-3 shrink-0" aria-hidden="true" />
                  <span class="sr-only">{{ line.level }}:</span>
                  <span class="whitespace-pre-wrap break-words">{{ line.message }}</span>
                </li>
                <li v-if="run.droppedLogLines" class="py-0.5 text-subtle">… {{ run.droppedLogLines }} more lines dropped</li>
              </ul>
            </li>
          </ol>
        </div>
      </section>

      <div class="flex justify-end">
        <FButton variant="secondary" size="sm" data-autofocus @click="open = false">Close</FButton>
      </div>
    </div>
  </FModal>
</template>
