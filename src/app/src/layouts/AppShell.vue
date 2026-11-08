<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { Check } from 'lucide-vue-next'
import AppSidebar from '@/components/shell/AppSidebar.vue'
import AppTopbar from '@/components/shell/AppTopbar.vue'
import { useCms } from '@/stores/cms'

const { state } = useCms()
const route = useRoute()

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
        <RouterView />
      </main>
    </div>

    <Transition
      enter-from-class="translate-y-4 opacity-0"
      leave-to-class="translate-y-4 opacity-0"
      enter-active-class="transition duration-300"
      leave-active-class="transition duration-200"
    >
      <div
        v-if="state.toast"
        :key="state.toast.id"
        role="status"
        class="fixed bottom-6 left-1/2 z-50 flex -translate-x-1/2 items-center gap-2.5 rounded-full bg-charcoal px-5 py-3 text-sm font-medium text-warm-white shadow-float"
      >
        <Check class="size-4 text-sage" aria-hidden="true" />
        {{ state.toast.message }}
      </div>
    </Transition>
  </div>
</template>
