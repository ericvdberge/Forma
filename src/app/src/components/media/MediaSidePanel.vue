<script setup lang="ts">
import { computed, ref } from 'vue'
import { ArrowRight, ClipboardPaste, CloudUpload, Globe } from 'lucide-vue-next'
import { FActionButton, FDropzone, FThumbnailRow, FUploadProgress } from '@forma/design-system'
import type { MediaItem } from '@/data/types'
import { ACCEPT_ATTRIBUTE, ACCEPTED_TYPES, type PendingUpload } from '@/composables/useMedia'
import { formatBytes } from '@/utils/format'

/** Upload box, upload queue, the library broken down by format and quick actions. */
const props = defineProps<{
  media: MediaItem[]
  uploads: PendingUpload[]
  canUpload: boolean
}>()
const emit = defineEmits<{
  files: [files: File[]]
  dismiss: [id: string]
  dismissFailed: []
  format: [format: string]
  paste: []
  website: []
}>()

const dropzone = ref<InstanceType<typeof FDropzone> | null>(null)
defineExpose({ browse: () => dropzone.value?.browse() })

const failedCount = computed(() => props.uploads.filter((u) => u.status === 'failed').length)
const formats = computed(() =>
  Object.values(ACCEPTED_TYPES)
    .map((format) => {
      const items = props.media.filter((m) => m.format === format)
      return { format, items, size: items.reduce((s, m) => s + m.sizeInBytes, 0) }
    })
    .filter((f) => f.items.length),
)
const canPaste = typeof navigator !== 'undefined' && !!navigator.clipboard?.read
</script>

<template>
  <aside class="flex flex-col gap-8 rounded-panel bg-white/85 p-5 ring-1 ring-charcoal/[0.06] shadow-soft backdrop-blur sm:p-6">
    <section aria-labelledby="upload-title">
      <h2 id="upload-title" class="mb-4 font-display text-xl font-bold tracking-[-0.02em]">Upload media</h2>
      <FDropzone
        ref="dropzone"
        :accept="ACCEPT_ATTRIBUTE"
        :disabled="!canUpload"
        hint="Supports images up to 10 MB"
        :formats="['JPG', 'PNG', 'GIF', 'WEBP']"
        @files="emit('files', $event)"
      />

      <div v-if="uploads.length" class="mt-5 flex flex-col gap-2">
        <div v-if="failedCount > 1" class="flex items-center justify-between text-xs">
          <span class="text-danger">{{ failedCount }} files couldn’t be uploaded</span>
          <button type="button" class="font-medium hover:underline" @click="emit('dismissFailed')">Dismiss all</button>
        </div>
        <ul class="flex max-h-72 flex-col gap-2 overflow-y-auto" aria-label="Uploads">
          <li v-for="u in uploads" :key="u.id">
            <FUploadProgress
              :name="u.fileName"
              :src="u.previewUrl"
              :status="u.status"
              :progress="u.progress"
              :error="u.error"
              :dismiss-label="u.status === 'failed' ? `Dismiss ${u.fileName}` : undefined"
              @dismiss="emit('dismiss', u.id)"
            />
          </li>
        </ul>
      </div>
    </section>

    <section v-if="formats.length" aria-labelledby="formats-title">
      <div class="mb-2 flex items-center justify-between">
        <h2 id="formats-title" class="font-display text-lg font-bold tracking-[-0.02em]">By format</h2>
        <button type="button" class="inline-flex items-center gap-1 text-xs font-medium text-charcoal/75 hover:text-charcoal" @click="emit('format', 'all')">
          View all <ArrowRight class="size-3.5" aria-hidden="true" />
        </button>
      </div>
      <ul class="-mx-2 flex flex-col">
        <li v-for="f in formats" :key="f.format">
          <FThumbnailRow
            interactive
            :title="f.format"
            :subtitle="`${f.items.length} ${f.items.length === 1 ? 'file' : 'files'} · ${formatBytes(f.size)}`"
            :src="f.items[0]?.thumbnailUrl"
            @click="emit('format', f.format)"
          >
            <template #trailing><ArrowRight class="mr-2 size-4 text-charcoal/40" aria-hidden="true" /></template>
          </FThumbnailRow>
        </li>
      </ul>
    </section>

    <section aria-labelledby="quick-title">
      <h2 id="quick-title" class="mb-3 font-display text-lg font-bold tracking-[-0.02em]">Quick actions</h2>
      <div class="flex flex-col gap-2">
        <FActionButton :icon="CloudUpload" size="sm" :disabled="!canUpload" @click="dropzone?.browse()">Bulk upload</FActionButton>
        <FActionButton v-if="canPaste" :icon="ClipboardPaste" size="sm" :disabled="!canUpload" @click="emit('paste')">Paste from clipboard</FActionButton>
        <FActionButton :icon="Globe" size="sm" @click="emit('website')">Manage website</FActionButton>
      </div>
    </section>
  </aside>
</template>
