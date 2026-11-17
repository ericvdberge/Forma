<script setup lang="ts">
import { TriangleAlert } from 'lucide-vue-next'
import { FButton, FSkeleton } from '@forma/design-system'
import { useWebsites } from '@/composables/useWebsites'
import LinkButton from '@/components/LinkButton.vue'
import StudioEditor from '@/components/studio/StudioEditor.vue'

/** Full-screen page editor (outside the app shell), so it loads the websites itself before editing. */
const { getWebsites } = useWebsites()
const { websites, isPending, isError, isFetching, refetch } = getWebsites()
</script>

<template>
  <div v-if="isPending" class="flex h-dvh flex-col bg-[#ecebe8]" role="status" aria-busy="true">
    <span class="sr-only">Loading the editor…</span>
    <div class="h-16 border-b border-charcoal/[0.06] bg-white" />
    <div class="flex-1 p-4"><FSkeleton class="mx-auto h-full max-w-[1200px]" /></div>
  </div>
  <div v-else-if="isError || !websites.length" role="alert" class="flex h-dvh flex-col items-center justify-center gap-4 text-center">
    <TriangleAlert class="size-6 text-danger" aria-hidden="true" />
    <p class="font-medium">{{ isError ? 'We couldn’t load your websites' : 'You don’t have any websites yet' }}</p>
    <FButton v-if="isError" size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
    <LinkButton v-else size="sm" to="/websites">Go to websites</LinkButton>
  </div>
  <StudioEditor v-else />
</template>
