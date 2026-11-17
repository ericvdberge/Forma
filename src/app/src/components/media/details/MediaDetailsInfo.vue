<script setup lang="ts">
import { computed } from 'vue'
import { ArrowRight, Calendar, Copy, Database, Download, Globe, ImageIcon, Maximize, Share2, Trash2, X } from 'lucide-vue-next'
import { FActionButton, FDivider, FIconButton, FThumbnailRow } from '@forma/design-system'
import type { MediaItem, Website } from '@/data/types'
import { useMediaLink } from '@/composables/useMediaLink'
import { formatBytes } from '@/utils/format'

/** Side panel of the details dialog: name, facts, actions and the website it belongs to. */
const { item } = defineProps<{
  item: MediaItem
  website: Website
  /** Id for the heading, so the dialog can be labelled by it. */
  titleId: string
}>()
const emit = defineEmits<{ delete: []; website: []; close: [] }>()
const { copyLink, shareLink } = useMediaLink()

const uploadedAt = computed(() => new Intl.DateTimeFormat('en', { dateStyle: 'medium' }).format(item.createdAt))
const facts = computed(() => [
  { icon: ImageIcon, text: `Image · ${item.extension}` },
  { icon: Database, text: formatBytes(item.sizeInBytes) },
  { icon: Calendar, text: `Uploaded ${uploadedAt.value}` },
  { icon: Maximize, text: `${item.width} × ${item.height} px`, numeric: true },
])
</script>

<template>
  <aside class="relative flex w-full shrink-0 flex-col gap-6 overflow-y-auto px-6 pb-6 pt-7 lg:w-[340px] lg:px-7 lg:pt-16">
    <FIconButton :icon="X" label="Close" variant="outline" shape="circle" class="absolute right-5 top-4 hidden lg:grid" data-autofocus @click="emit('close')" />

    <div>
      <h2 :id="titleId" class="break-words font-display text-[1.625rem] font-bold leading-tight tracking-[-0.03em]">{{ item.fileName }}</h2>
      <p class="mt-2 text-[0.9375rem] text-muted">Part of the media library of {{ website.name }}.</p>
    </div>

    <FDivider />

    <ul class="flex flex-col gap-3.5 text-[0.9375rem] text-charcoal/85">
      <li v-for="fact in facts" :key="fact.text" class="flex items-center gap-3">
        <component :is="fact.icon" class="size-[1.125rem] text-charcoal/60" :stroke-width="1.6" aria-hidden="true" />
        <span :class="{ 'tabular-nums': fact.numeric }">{{ fact.text }}</span>
      </li>
    </ul>

    <FDivider />

    <section aria-labelledby="media-actions-title">
      <h3 id="media-actions-title" class="mb-3 text-[0.9375rem] font-medium">Actions</h3>
      <div class="flex flex-col gap-2">
        <FActionButton :icon="Download" :href="item.imageUrl" :download="item.fileName" size="sm">Download</FActionButton>
        <FActionButton :icon="Share2" size="sm" @click="shareLink(item)">Share link</FActionButton>
        <FActionButton :icon="Copy" size="sm" @click="copyLink(item)">Copy link</FActionButton>
        <FActionButton :icon="Trash2" size="sm" tone="danger" @click="emit('delete')">Delete</FActionButton>
      </div>
    </section>

    <FThumbnailRow
      class="mt-auto"
      bordered
      interactive
      eyebrow="In website"
      :title="website.name"
      :subtitle="website.domain"
      :icon="Globe"
      @click="emit('website')"
    >
      <template #trailing><ArrowRight class="mr-1 size-4 text-charcoal/60" aria-hidden="true" /></template>
    </FThumbnailRow>
  </aside>
</template>
