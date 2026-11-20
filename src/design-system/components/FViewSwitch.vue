<script setup lang="ts">
import { LayoutGrid, List } from 'lucide-vue-next'
import FSegmented from './FSegmented.vue'

/**
 * Switches a collection between cards and a list. Pair it with `FCollection`, which draws the items either way.
 * Remembering the choice (e.g. in localStorage) is up to the page.
 */
export type FCollectionView = 'grid' | 'list'

const view = defineModel<FCollectionView>({ required: true })
const { label = 'Layout' } = defineProps<{ label?: string }>()

const options = [
  { value: 'grid' as const, label: 'Grid view', icon: LayoutGrid },
  { value: 'list' as const, label: 'List view', icon: List },
]
</script>

<template>
  <FSegmented v-model="view" :options="options" :label="label" />
</template>
