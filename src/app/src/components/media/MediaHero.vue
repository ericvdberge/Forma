<script setup lang="ts">
import { CloudUpload } from 'lucide-vue-next'
import { FBadge, FButton, FPageHero, FUploadProgress } from '@forma/design-system'
import type { MediaItem } from '@/data/types'
import type { PendingUpload } from '@/composables/useMedia'
import MediaCollage from './MediaCollage.vue'

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
    <FPageHero class="relative z-10" title="Your creative" accent="media hub.">
      <template #badge><FBadge dot-tone="positive">Media Library</FBadge></template>
      Upload, organize and manage the images of your website in one sleek workspace.
      <template #actions>
        <FButton size="lg" arrow :disabled="!canUpload" @click="$emit('upload')">
          <CloudUpload class="size-[1.125rem]" aria-hidden="true" /> Upload media
        </FButton>
      </template>
    </FPageHero>

    <MediaCollage class="hidden h-[300px] md:block lg:h-[340px]" :items="latest" @open="$emit('open', $event)" />

    <!-- Floating upload card -->
    <Transition enter-from-class="opacity-0 -translate-y-2" leave-to-class="opacity-0" enter-active-class="transition duration-300" leave-active-class="transition duration-200">
      <div v-if="upload" class="z-20 w-full md:absolute md:right-0 md:top-2 md:w-64" role="status">
        <FUploadProgress
          variant="floating"
          :name="moreUploading ? `${upload.fileName} (+${moreUploading} more)` : upload.fileName"
          :src="upload.previewUrl"
          :status="upload.status === 'failed' ? 'uploading' : upload.status"
        />
      </div>
    </Transition>
  </section>
</template>
