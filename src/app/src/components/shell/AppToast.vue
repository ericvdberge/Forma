<script setup lang="ts">
import { Check } from 'lucide-vue-next'
import { useToast } from '@/composables/useToast'

/** The confirmation toast from `useToast()`, shown by the app shell and by full-screen views. */
const { toast } = useToast()
</script>

<template>
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
</template>
