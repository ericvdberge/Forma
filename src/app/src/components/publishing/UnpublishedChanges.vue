<script setup lang="ts">
import { computed } from 'vue'
import { CircleAlert, Send } from 'lucide-vue-next'
import { useDrafts } from '@/composables/useDrafts'
import { usePublishDialog } from '@/composables/usePublishDialog'
import { useScheduledPublishes } from '@/composables/useScheduledPublishes'

/** In the top bar: "4 unpublished changes", or that a scheduled publish didn't go live. Opens the publish dialog. */
const props = defineProps<{ websiteId: string }>()

const { count } = useDrafts(() => props.websiteId)
const { failed } = useScheduledPublishes(() => props.websiteId)
const { openDialog } = usePublishDialog()

const label = computed(() => (count.value === 1 ? '1 unpublished change' : `${count.value} unpublished changes`))
</script>

<template>
  <button
    v-if="failed.length"
    type="button"
    class="inline-flex h-10 items-center gap-2 rounded-full bg-danger/10 px-4 text-sm font-medium text-danger transition-colors hover:bg-danger/15"
    @click="openDialog(websiteId, [], 'scheduled')"
  >
    <CircleAlert class="size-4" aria-hidden="true" />
    <span class="hidden sm:inline">A scheduled publish didn’t go live</span>
    <span class="sm:hidden">Not published</span>
  </button>
  <button
    v-else-if="count"
    type="button"
    class="inline-flex h-10 items-center gap-2 rounded-full bg-accent-50 px-4 text-sm font-medium text-accent-700 transition-colors hover:bg-accent-100"
    @click="openDialog(websiteId)"
  >
    <Send class="size-4" aria-hidden="true" />
    <span class="hidden sm:inline">{{ label }}</span>
    <span class="sm:hidden">{{ count }}</span>
  </button>
</template>
