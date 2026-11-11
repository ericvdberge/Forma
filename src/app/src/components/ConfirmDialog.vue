<script setup lang="ts">
import { TriangleAlert } from 'lucide-vue-next'
import { FButton, FModal } from '@forma/design-system'

/** Asks before something that can't be undone. */
const open = defineModel<boolean>('open', { required: true })
withDefaults(
  defineProps<{
    title: string
    message?: string
    confirmLabel?: string
    busy?: boolean
  }>(),
  { confirmLabel: 'Delete', busy: false },
)
const emit = defineEmits<{ confirm: [] }>()
</script>

<template>
  <FModal v-model:open="open" size="sm" labelledby="confirm-title">
    <div class="flex flex-col gap-5 p-6 sm:p-7">
      <span class="grid size-11 place-items-center rounded-2xl bg-danger/10 text-danger">
        <TriangleAlert class="size-5" aria-hidden="true" />
      </span>
      <div>
        <h2 id="confirm-title" class="font-display text-xl font-bold tracking-[-0.02em]">{{ title }}</h2>
        <p v-if="message" class="mt-2 text-sm text-muted">{{ message }}</p>
      </div>
      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <FButton variant="secondary" size="sm" data-autofocus @click="open = false">Cancel</FButton>
        <button
          type="button"
          class="inline-flex h-9 items-center justify-center rounded-full bg-danger px-4 text-sm font-medium text-white transition-colors hover:bg-danger/90 disabled:opacity-50"
          :disabled="busy"
          @click="emit('confirm')"
        >
          {{ busy ? 'Deleting…' : confirmLabel }}
        </button>
      </div>
    </div>
  </FModal>
</template>
