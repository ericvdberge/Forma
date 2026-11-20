<script setup lang="ts">
import { computed, inject } from 'vue'
import { ArrowRight, Database, ImageOff } from 'lucide-vue-next'
import type { CollectionItem } from '@/data/types'
import { collectionDataKey } from '@/studio/inject'
import { imageUrl } from '@/studio/media'
import { columnsFor, tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'
import { valueAsText } from '@/utils/collections'

/** A grid of cards, one per published item of a collection, each linking to the item's page. */
const props = defineProps<{ block: Block<'collectionCards'>; ctx: RenderContext }>()
const data = inject(collectionDataKey, null)

const p = computed(() => props.block.props)
const collection = computed(() => data?.collections.value.find((c) => c.id === p.value.collectionId))
const loading = computed(() => !!p.value.collectionId && !!data?.isLoading(p.value.collectionId))

/** Published items in the chosen order; in the editor drafts show too (faded), so a new collection isn't blank. */
const items = computed(() => {
  if (!collection.value || !data) return []
  const all = data.itemsOf(collection.value.id).filter((i) => i.status === 'live' || props.ctx.editing)
  const sorted = p.value.sort === 'oldest' ? [...all].reverse() : all
  return sorted.slice(0, p.value.limit)
})

const field = (id: string) => collection.value?.fields.find((f) => f.id === id)
const image = (item: CollectionItem) => {
  const value = item.values[p.value.imageField]
  return typeof value === 'string' && value ? imageUrl(value) : ''
}
const title = (item: CollectionItem) => {
  const f = field(p.value.titleField)
  return (f && valueAsText(f, item.values[f.id])) || item.slug
}
const text = (item: CollectionItem) => {
  const f = field(p.value.textField)
  return f ? valueAsText(f, item.values[f.id]) : ''
}
const href = (item: CollectionItem) => `/${collection.value!.slug}/${item.slug}`

const cols = computed(() => columnsFor(p.value.columns, props.ctx.bp))
const t = computed(() => tone(props.ctx.theme))
</script>

<template>
  <!-- Nothing chosen yet: a hint in the editor, nothing for visitors -->
  <div
    v-if="!collection"
    class="flex flex-col items-center gap-2 rounded-2xl border border-dashed px-6 py-10 text-center text-sm"
    :style="{ borderColor: t.line, color: t.muted }"
  >
    <Database class="size-5" aria-hidden="true" />
    <template v-if="ctx.editing">
      <p class="font-medium" :style="{ color: t.text }">{{ p.collectionId ? 'This collection was deleted' : 'Collection cards' }}</p>
      <p>Choose which collection to show in the settings on the right.</p>
    </template>
  </div>

  <div v-else-if="loading" class="grid" :style="{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`, gap: `${p.gap}px` }" aria-busy="true">
    <div v-for="i in Math.min(p.limit, cols)" :key="i" class="aspect-[4/5] animate-pulse" :style="{ background: t.surface, borderRadius: `${p.radius}px` }" />
  </div>

  <p v-else-if="!items.length" class="py-8 text-center text-sm" :style="{ color: t.muted }">{{ p.emptyText || 'Nothing here yet.' }}</p>

  <ul v-else class="grid text-left" :style="{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`, gap: `${p.gap}px` }">
    <li
      v-for="item in items"
      :key="item.id"
      :class="['flex flex-col overflow-hidden', item.status !== 'live' && 'opacity-50']"
      :style="{
        borderRadius: `${p.radius}px`,
        background: p.card ? t.surface : undefined,
        boxShadow: p.card ? `inset 0 0 0 1px ${t.line}` : undefined,
      }"
    >
      <template v-if="p.imageField">
        <img v-if="image(item)" :src="image(item)" alt="" class="aspect-[4/3] w-full object-cover" :style="!p.card ? { borderRadius: `${p.radius}px` } : undefined" draggable="false" />
        <div v-else class="grid aspect-[4/3] w-full place-items-center" :style="{ background: t.line, borderRadius: !p.card ? `${p.radius}px` : undefined }">
          <ImageOff class="size-5 opacity-40" aria-hidden="true" />
        </div>
      </template>
      <div :class="['flex flex-1 flex-col gap-2', p.card ? 'p-5' : 'pt-4']">
        <span v-if="item.status !== 'live'" class="w-fit rounded-full px-2 py-0.5 text-[11px] font-medium" :style="{ background: t.line }">Draft</span>
        <h3 class="text-lg font-bold leading-snug" :style="{ color: t.text }">{{ title(item) }}</h3>
        <p v-if="text(item)" class="line-clamp-3 text-sm leading-relaxed" :style="{ color: t.muted }">{{ text(item) }}</p>
        <a
          v-if="p.buttonLabel"
          :href="ctx.editing ? undefined : href(item)"
          class="mt-auto inline-flex items-center gap-1.5 pt-2 text-sm font-medium"
          :style="{ color: t.text }"
          @click="ctx.editing && $event.preventDefault()"
        >
          {{ p.buttonLabel }} <ArrowRight class="size-4" aria-hidden="true" />
        </a>
      </div>
    </li>
  </ul>
</template>
