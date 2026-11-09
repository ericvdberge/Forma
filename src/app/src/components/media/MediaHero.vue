<script setup lang="ts">
import { CloudUpload } from 'lucide-vue-next'
import { FArchitectureArt, FBadge, FButton, FUploadProgress } from '@forma/design-system'
import type { MediaItem } from '@/data/types'
import type { PendingUpload } from '@/composables/useMedia'

/** Headline, upload call to action and a collage of the latest images. */
defineProps<{
  /** Newest first; the first three make the collage. */
  latest: MediaItem[]
  /** The upload to show on the floating card, if any. */
  upload?: PendingUpload
  /** How many more are uploading after `upload`. */
  moreUploading?: number
  canUpload: boolean
}>()
defineEmits<{ upload: []; open: [id: string] }>()
</script>

<template>
  <section class="relative grid items-center gap-8 pb-8 pt-2 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] md:pb-10">
    <div class="relative z-10">
      <FBadge dot-tone="positive">Media Library</FBadge>
      <h1 class="mt-6 font-display text-[clamp(2.5rem,5vw,4rem)] font-bold leading-[0.98] tracking-[-0.05em]">
        Your creative<br />
        media <span class="text-charcoal/45">hub.</span>
      </h1>
      <p class="mt-5 max-w-md text-[1.0625rem] leading-relaxed text-muted">
        Upload, organize and manage the images of your website in one sleek workspace.
      </p>
      <div class="mt-7 flex flex-wrap items-center gap-3">
        <FButton size="lg" arrow :disabled="!canUpload" @click="$emit('upload')">
          <CloudUpload class="size-[1.125rem]" aria-hidden="true" /> Upload media
        </FButton>
      </div>
    </div>

    <!-- Collage -->
    <div class="relative hidden h-[300px] md:block lg:h-[340px]" aria-hidden="true">
      <div
        class="absolute left-[6%] top-[28%] h-[52%] w-[34%] -rotate-[9deg] overflow-hidden rounded-[1.5rem] bg-white p-1.5 shadow-float ring-1 ring-white"
      >
        <img v-if="latest[1]" :src="latest[1].thumbnailUrl" alt="" class="size-full rounded-[1.125rem] object-cover" />
        <div v-else class="size-full overflow-hidden rounded-[1.125rem]"><FArchitectureArt tone="light" /></div>
      </div>
      <div
        class="absolute right-[2%] top-[18%] h-[66%] w-[48%] rotate-[6deg] overflow-hidden rounded-[1.5rem] bg-white/70 p-1.5 shadow-card ring-1 ring-white"
      >
        <img v-if="latest[2]" :src="latest[2].thumbnailUrl" alt="" class="size-full rounded-[1.125rem] object-cover opacity-80" />
        <div v-else class="size-full overflow-hidden rounded-[1.125rem] opacity-70"><FArchitectureArt tone="light" /></div>
      </div>
      <button
        type="button"
        tabindex="-1"
        class="absolute left-[24%] top-[6%] h-[80%] w-[56%] rotate-[3deg] overflow-hidden rounded-[1.75rem] bg-white p-2 shadow-float ring-1 ring-white transition-transform duration-500 hover:rotate-[1deg] hover:scale-[1.01] disabled:pointer-events-none"
        :disabled="!latest[0]"
        @click="latest[0] && $emit('open', latest[0].id)"
      >
        <img v-if="latest[0]" :src="latest[0].thumbnailUrl" alt="" class="size-full rounded-[1.25rem] object-cover" />
        <div v-else class="size-full overflow-hidden rounded-[1.25rem]"><FArchitectureArt /></div>
      </button>
    </div>

    <!-- Floating upload card -->
    <Transition enter-from-class="opacity-0 -translate-y-2" leave-to-class="opacity-0" enter-active-class="transition duration-300" leave-active-class="transition duration-200">
      <div v-if="upload" class="z-20 w-full md:absolute md:right-0 md:top-2 md:w-64" role="status">
        <FUploadProgress
          variant="floating"
          :name="moreUploading ? `${upload.fileName} (+${moreUploading} more)` : upload.fileName"
          :src="upload.previewUrl"
          :status="upload.status === 'failed' ? 'uploading' : upload.status"
          :progress="upload.progress"
        />
      </div>
    </Transition>
  </section>
</template>
