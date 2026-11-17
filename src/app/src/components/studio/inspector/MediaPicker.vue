<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { ImageOff, Link, LoaderCircle, Upload, X } from 'lucide-vue-next'
import { FButton, FSkeleton } from '@forma/design-system'
import { useStudio } from '@/composables/useStudio'
import { ACCEPT_ATTRIBUTE } from '@/composables/useMedia'

/** The media library: the website's images from the media API. Uploads go to the same library. */
const emit = defineEmits<{ close: []; select: [src: string] }>()
const studio = useStudio()
const { isPending, isError, refetch } = studio.mediaQuery
const fileInput = ref<HTMLInputElement | null>(null)
const url = ref('')
const uploading = ref(false)

async function upload(e: Event) {
  const input = e.target as HTMLInputElement
  const files = input.files ? [...input.files] : []
  input.value = ''
  if (!files.length) return
  uploading.value = true
  try {
    const [first] = await studio.uploadMedia(files)
    if (first) emit('select', first.src)
  } finally {
    uploading.value = false
  }
}

const onKey = (e: KeyboardEvent) => e.key === 'Escape' && emit('close')
onMounted(() => document.addEventListener('keydown', onKey))
onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[60] grid place-items-center bg-charcoal/30 p-4" @click.self="emit('close')">
      <div role="dialog" aria-modal="true" aria-labelledby="media-title" class="flex max-h-[85dvh] w-full max-w-2xl flex-col rounded-panel bg-white shadow-float">
        <div class="flex items-center justify-between gap-4 border-b border-charcoal/[0.06] px-6 py-4">
          <h2 id="media-title" class="text-lg font-bold">Media library</h2>
          <div class="flex items-center gap-2">
            <FButton size="sm" variant="secondary" :disabled="uploading" @click="fileInput?.click()">
              <LoaderCircle v-if="uploading" class="size-4 animate-spin" aria-hidden="true" />
              <Upload v-else class="size-4" aria-hidden="true" />
              {{ uploading ? 'Uploading…' : 'Upload' }}
            </FButton>
            <input ref="fileInput" type="file" :accept="ACCEPT_ATTRIBUTE" class="hidden" @change="upload" />
            <button type="button" class="grid size-9 place-items-center rounded-full hover:bg-canvas" aria-label="Close" @click="emit('close')">
              <X class="size-5" aria-hidden="true" />
            </button>
          </div>
        </div>

        <div class="overflow-y-auto p-6">
          <div v-if="isPending" class="grid grid-cols-3 gap-3">
            <FSkeleton v-for="i in 6" :key="i" class="aspect-[4/3]" />
          </div>
          <div v-else-if="isError" class="flex flex-col items-center gap-3 rounded-xl bg-canvas px-6 py-10 text-center text-sm text-muted">
            Couldn’t load the media library.
            <FButton size="sm" variant="secondary" @click="refetch()">Try again</FButton>
          </div>
          <div v-else-if="studio.media.value.length" class="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <button
              v-for="m in studio.media.value"
              :key="m.id"
              type="button"
              class="group overflow-hidden rounded-xl text-left ring-1 ring-charcoal/[0.08] hover:ring-2 hover:ring-accent"
              @click="emit('select', m.src)"
            >
              <img :src="m.thumbnailUrl" :alt="m.name" loading="lazy" class="aspect-[4/3] w-full bg-canvas object-cover" />
              <span class="block truncate px-3 py-2 text-xs text-muted group-hover:text-charcoal">{{ m.name }}</span>
            </button>
          </div>
          <div v-else class="flex flex-col items-center gap-2 rounded-xl bg-canvas px-6 py-10 text-center text-sm text-muted">
            <ImageOff class="size-6 text-subtle" aria-hidden="true" />
            No images in this website’s media library yet. Upload one to use it here.
          </div>

          <form class="mt-6 flex items-end gap-2" @submit.prevent="url.trim() && emit('select', url.trim())">
            <label class="flex flex-1 flex-col gap-1.5 text-[0.8125rem] text-charcoal/80">
              Or use an image link
              <span class="relative">
                <Link class="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-subtle" aria-hidden="true" />
                <input
                  v-model="url"
                  type="url"
                  placeholder="https://…"
                  class="h-10 w-full rounded-lg bg-white pl-9 pr-3 text-sm ring-1 ring-charcoal/10 outline-none focus:ring-2 focus:ring-accent"
                />
              </span>
            </label>
            <FButton size="sm" type="submit" :disabled="!url.trim()">Use link</FButton>
          </form>
        </div>
      </div>
    </div>
  </Teleport>
</template>
