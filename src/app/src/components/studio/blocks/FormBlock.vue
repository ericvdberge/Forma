<script setup lang="ts">
import { ref, useId } from 'vue'
import { CircleCheck } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'form'>; ctx: RenderContext }>()
const id = useId()
const sent = ref(false)

function submit() {
  if (!props.ctx.editing) sent.value = true
}
</script>

<template>
  <div
    v-if="sent"
    :class="cn('flex items-center gap-3 rounded-2xl p-5', ctx.theme === 'dark' ? 'bg-white/10' : 'bg-white ring-1 ring-charcoal/[0.06]')"
    role="status"
  >
    <CircleCheck class="size-5 text-positive" aria-hidden="true" />
    {{ block.props.successMessage }}
  </div>
  <form v-else class="flex flex-col gap-3" @submit.prevent="submit">
    <label v-for="(f, i) in block.props.fields" :key="i" :for="`${id}-${i}`" class="flex flex-col gap-1.5 text-left text-sm font-medium">
      <span>{{ f.label }}<span v-if="f.required" class="text-danger"> *</span></span>
      <component
        :is="f.type === 'textarea' ? 'textarea' : 'input'"
        :id="`${id}-${i}`"
        :type="f.type === 'textarea' ? undefined : f.type"
        :required="f.required"
        :readonly="ctx.editing"
        :rows="f.type === 'textarea' ? 4 : undefined"
        :class="
          cn(
            'w-full rounded-xl px-4 py-2.5 text-[0.9375rem] font-normal outline-none ring-1 focus:ring-2 focus:ring-accent',
            ctx.theme === 'dark' ? 'bg-white/5 ring-white/15 text-warm-white' : 'bg-white ring-charcoal/10 text-charcoal',
            ctx.editing && 'pointer-events-none',
          )
        "
      />
    </label>
    <button
      :type="ctx.editing ? 'button' : 'submit'"
      :class="
        cn(
          'mt-1 inline-flex h-11 items-center justify-center self-start rounded-full px-5 text-[0.9375rem] font-medium',
          ctx.theme === 'dark' ? 'bg-warm-white text-charcoal' : 'bg-charcoal text-warm-white',
        )
      "
    >
      {{ block.props.submitLabel }}
    </button>
  </form>
</template>
