<script setup lang="ts">
import { computed, ref, watch, type Component } from 'vue'
import { useRoute } from 'vue-router'
import { Check, TriangleAlert } from 'lucide-vue-next'
import { FButton } from '@forma/design-system'
import AppSidebar from '@/components/shell/AppSidebar.vue'
import AppTopbar from '@/components/shell/AppTopbar.vue'
import DashboardSkeleton from '@/components/skeletons/DashboardSkeleton.vue'
import PagesSkeleton from '@/components/skeletons/PagesSkeleton.vue'
import StudioSkeleton from '@/components/skeletons/StudioSkeleton.vue'
import WebsitesSkeleton from '@/components/skeletons/WebsitesSkeleton.vue'
import { useWebsites } from '@/composables/useWebsites'
import { useToast } from '@/composables/useToast'

const { getWebsites } = useWebsites()
const { websites, isPending, isError, isFetching, refetch } = getWebsites()
const { toast } = useToast()
const route = useRoute()

/** Views that need websites, with the skeleton to show while they load. Others render right away. */
const skeletons: Record<string, Component> = {
  dashboard: DashboardSkeleton,
  pages: PagesSkeleton,
  websites: WebsitesSkeleton,
  website: WebsitesSkeleton,
  studio: StudioSkeleton,
}
const skeleton = computed(() => skeletons[String(route.name)])

const COLLAPSED_KEY = 'forma.sidebarCollapsed'
const collapsed = ref(false)
try {
  collapsed.value = localStorage.getItem(COLLAPSED_KEY) === '1'
} catch {
  /* storage unavailable */
}
watch(collapsed, (v) => {
  try {
    localStorage.setItem(COLLAPSED_KEY, v ? '1' : '0')
  } catch {
    /* storage unavailable */
  }
})

const drawerOpen = ref(false)
watch(() => route.fullPath, () => (drawerOpen.value = false))
</script>

<template>
  <div class="flex h-dvh overflow-hidden bg-[#f7f7f5]">
    <!-- Desktop sidebar -->
    <AppSidebar class="hidden lg:flex" :collapsed="collapsed" @toggle="collapsed = !collapsed" />

    <!-- Mobile drawer -->
    <Transition enter-from-class="opacity-0" leave-to-class="opacity-0" enter-active-class="transition" leave-active-class="transition">
      <div v-if="drawerOpen" class="fixed inset-0 z-40 bg-charcoal/30 lg:hidden" @click="drawerOpen = false" />
    </Transition>
    <Transition enter-from-class="-translate-x-full" leave-to-class="-translate-x-full" enter-active-class="transition" leave-active-class="transition">
      <AppSidebar
        v-if="drawerOpen"
        class="fixed inset-y-0 left-0 z-50 shadow-float lg:hidden"
        :collapsed="false"
        @toggle="drawerOpen = false"
        @navigate="drawerOpen = false"
      />
    </Transition>

    <div class="relative flex min-w-0 flex-1 flex-col overflow-y-auto overflow-x-hidden">
      <AppTopbar @menu="drawerOpen = true" />
      <main class="flex-1 px-4 pb-10 sm:px-8">
        <!-- Views assume an active website, so hold them back until websites have loaded. -->
        <RouterView v-if="!skeleton" />
        <div v-else-if="isPending" role="status" aria-busy="true">
          <span class="sr-only">Loading your websites…</span>
          <component :is="skeleton" />
        </div>
        <div v-else-if="isError" role="alert" class="flex min-h-80 flex-col items-center justify-center gap-4 text-center">
          <TriangleAlert class="size-6 text-danger" aria-hidden="true" />
          <div>
            <p class="font-medium">We couldn't load your websites</p>
            <p class="mt-1 text-sm text-muted">Check that the Forma API is running, then try again.</p>
          </div>
          <FButton size="sm" variant="secondary" :disabled="isFetching" @click="refetch()">Try again</FButton>
        </div>
        <div v-else-if="!websites.length" class="flex min-h-80 items-center justify-center text-sm text-muted">
          You don't have any websites yet.
        </div>
        <RouterView v-else />
      </main>
    </div>

    <Transition
      enter-from-class="translate-y-4 opacity-0"
      leave-to-class="translate-y-4 opacity-0"
      enter-active-class="transition duration-300"
      leave-active-class="transition duration-200"
    >
      <div
        v-if="toast"
        :key="toast.id"
        role="status"
        class="fixed bottom-6 left-1/2 z-50 flex -translate-x-1/2 items-center gap-2.5 rounded-full bg-charcoal px-5 py-3 text-sm font-medium text-warm-white shadow-float"
      >
        <Check class="size-4 text-sage" aria-hidden="true" />
        {{ toast.message }}
      </div>
    </Transition>
  </div>
</template>
