<script setup lang="ts">
import { computed } from 'vue'
import { ImageIcon, TriangleAlert, X } from 'lucide-vue-next'
import FProgress from './FProgress.vue'
import { cn } from '../utils/cn'

/** One file being uploaded: thumbnail, status, progress and a dismiss/cancel button. */
const props = withDefaults(
  defineProps<{
    name: string
    /** Local preview (object URL) */
    src?: string
    status?: 'queued' | 'uploading' | 'processing' | 'failed'
    /** 0–100, while uploading */
    progress?: number | null
    error?: string
    /** `floating` is the frosted card laid over artwork */
    variant?: 'plain' | 'floating'
    dismissLabel?: string
  }>(),
  { status: 'uploading', progress: null, variant: 'plain' },
)
defineEmits<{ dismiss: [] }>()

const statusText = computed(
  () =>
    ({
      queued: 'Waiting…',
      uploading: 'Uploading…',
      processing: 'Processing…',
      failed: props.error ?? 'Upload failed',
    })[props.status],
)
const percent = computed(() => (props.status === 'uploading' && props.progress != null ? Math.round(props.progress) : null))
</script>

<template>
  <div
    :class="
      cn(
        'flex items-center gap-3 rounded-2xl p-2.5',
        variant === 'floating' ? 'bg-white/85 shadow-float ring-1 ring-white/70 backdrop-blur-xl' : 'bg-white ring-1 ring-charcoal/[0.06]',
      )
    "
  >
    <span class="grid size-11 shrink-0 place-items-center overflow-hidden rounded-xl bg-canvas text-subtle">
      <img v-if="src" :src="src" alt="" class="size-full object-cover" />
      <ImageIcon v-else class="size-4" aria-hidden="true" />
    </span>
    <div class="min-w-0 flex-1">
      <div class="flex items-baseline justify-between gap-2 text-xs">
        <p class="truncate font-medium text-charcoal" :title="name">{{ status === 'failed' ? name : statusText }}</p>
        <span v-if="percent != null" class="tabular-nums text-muted">{{ percent }}%</span>
      </div>
      <p v-if="status === 'failed'" class="mt-0.5 flex items-start gap-1 text-xs text-danger">
        <TriangleAlert class="mt-px size-3 shrink-0" aria-hidden="true" />{{ statusText }}
      </p>
      <template v-else>
        <p class="sr-only">{{ name }}</p>
        <FProgress
          class="mt-2"
          :value="status === 'queued' ? 0 : status === 'processing' ? null : (progress ?? null)"
          :label="`Uploading ${name}`"
        />
      </template>
    </div>
    <button
      v-if="dismissLabel"
      type="button"
      :aria-label="dismissLabel"
      class="grid size-7 shrink-0 place-items-center self-start rounded-lg text-charcoal/60 transition-colors hover:bg-charcoal/5 hover:text-charcoal"
      @click="$emit('dismiss')"
    >
      <X class="size-4" aria-hidden="true" />
    </button>
  </div>
</template>
