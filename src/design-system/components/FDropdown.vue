<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, useId } from 'vue'
import { ChevronDown } from 'lucide-vue-next'
import { cn } from '../utils/cn'

defineProps<{ label: string; items: Array<{ label: string; href: string; description?: string }> }>()

const open = ref(false)
const root = ref<HTMLElement | null>(null)
const menuId = useId()

function onDocClick(e: MouseEvent) {
  if (root.value && !root.value.contains(e.target as Node)) open.value = false
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') open.value = false
}
onMounted(() => {
  document.addEventListener('click', onDocClick)
  document.addEventListener('keydown', onKey)
})
onBeforeUnmount(() => {
  document.removeEventListener('click', onDocClick)
  document.removeEventListener('keydown', onKey)
})
</script>

<template>
  <div ref="root" class="relative">
    <button
      type="button"
      class="inline-flex items-center gap-1 text-sm font-medium text-charcoal/75 transition-colors hover:text-charcoal"
      :aria-expanded="open"
      :aria-controls="menuId"
      @click="open = !open"
    >
      {{ label }}
      <ChevronDown :class="cn('size-3.5 transition-transform', open && 'rotate-180')" aria-hidden="true" />
    </button>
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0 -translate-y-1"
      leave-active-class="transition duration-100 ease-in"
      leave-to-class="opacity-0 -translate-y-1"
    >
      <div
        v-show="open"
        :id="menuId"
        class="absolute left-1/2 top-full z-50 mt-3 w-64 -translate-x-1/2 rounded-2xl bg-white p-2 ring-1 ring-charcoal/[0.06] shadow-float"
      >
        <a
          v-for="item in items"
          :key="item.label"
          :href="item.href"
          class="block rounded-xl px-3 py-2.5 transition-colors hover:bg-canvas"
          @click="open = false"
        >
          <span class="block text-sm font-medium text-charcoal">{{ item.label }}</span>
          <span v-if="item.description" class="block text-xs text-muted">{{ item.description }}</span>
        </a>
      </div>
    </Transition>
  </div>
</template>
