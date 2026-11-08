<script setup lang="ts">
import { ref } from 'vue'
import { Bell, ChevronDown, Menu, Moon } from 'lucide-vue-next'
import { FAvatar, FIconButton, cn } from '@forma/design-system'
import { currentUser } from '@/data/seed'
import { useActivity } from '@/composables/useActivity'
import { useToast } from '@/composables/useToast'
import { useDismiss } from '@/composables/useDismiss'
import { timeAgo } from '@/utils/time'
import GlobalSearch from './GlobalSearch.vue'

defineEmits<{ menu: [] }>()

const { getActivity } = useActivity()
const { activity } = getActivity()
const { notify } = useToast()

const openMenu = ref<null | 'notifications' | 'account'>(null)
const seenAt = ref(0)
const root = ref<HTMLElement | null>(null)
useDismiss(root, () => (openMenu.value = null))

function toggle(menu: 'notifications' | 'account') {
  openMenu.value = openMenu.value === menu ? null : menu
  if (menu === 'notifications') seenAt.value = Date.now()
}
const hasUnread = () => activity.value.some((a) => a.at > seenAt.value)
</script>

<template>
  <header class="relative z-30 flex h-24 items-center gap-2 px-4 sm:gap-4 sm:px-8">
    <FIconButton :icon="Menu" label="Open menu" class="lg:hidden" @click="$emit('menu')" />
    <GlobalSearch />

    <div ref="root" class="ml-auto flex shrink-0 items-center gap-1 sm:gap-4">
      <FIconButton :icon="Moon" label="Dark mode (coming soon)" class="hidden sm:grid" @click="notify('Dark mode is coming soon')" />

      <div class="relative">
        <FIconButton
          :icon="Bell"
          label="Notifications"
          :dot="hasUnread()"
          :aria-expanded="openMenu === 'notifications'"
          @click="toggle('notifications')"
        />
        <div
          v-show="openMenu === 'notifications'"
          class="absolute right-0 top-full mt-2 w-80 rounded-2xl bg-white p-2 ring-1 ring-charcoal/[0.06] shadow-float"
        >
          <p class="px-3 pb-1 pt-2 text-sm font-bold">Notifications</p>
          <ul>
            <li v-for="a in activity.slice(0, 5)" :key="a.id" class="rounded-xl px-3 py-2.5 hover:bg-canvas">
              <p class="text-sm">{{ a.message }}</p>
              <p class="text-xs text-subtle">{{ timeAgo(a.at) }}</p>
            </li>
          </ul>
        </div>
      </div>

      <div class="relative">
        <button
          type="button"
          class="flex items-center gap-1.5 rounded-full p-0.5 pr-1 transition-colors hover:bg-charcoal/5"
          :aria-expanded="openMenu === 'account'"
          aria-label="Account menu"
          @click="toggle('account')"
        >
          <FAvatar :name="currentUser.name" size="lg" class="!size-10 sm:!size-12" />
          <ChevronDown :class="cn('hidden size-4 transition-transform sm:block', openMenu === 'account' && 'rotate-180')" aria-hidden="true" />
        </button>
        <div
          v-show="openMenu === 'account'"
          class="absolute right-0 top-full mt-2 w-60 rounded-2xl bg-white p-2 ring-1 ring-charcoal/[0.06] shadow-float"
        >
          <div class="px-3 py-2">
            <p class="text-sm font-bold">{{ currentUser.name }}</p>
            <p class="text-xs text-muted">{{ currentUser.email }}</p>
          </div>
          <RouterLink
            to="/settings"
            class="block rounded-xl px-3 py-2.5 text-sm hover:bg-canvas"
            @click="openMenu = null"
            >Settings</RouterLink
          >
          <RouterLink to="/websites" class="block rounded-xl px-3 py-2.5 text-sm hover:bg-canvas" @click="openMenu = null"
            >Your websites</RouterLink
          >
        </div>
      </div>
    </div>
  </header>
</template>
