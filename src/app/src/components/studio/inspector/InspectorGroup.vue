<script setup lang="ts">
import { ref, useId } from 'vue'
import { ChevronDown } from 'lucide-vue-next'
import { cn } from '@forma/design-system'

const props = withDefaults(defineProps<{ title: string; open?: boolean }>(), { open: true })
const expanded = ref(props.open)
const id = useId()
</script>

<template>
  <section class="border-b border-charcoal/[0.06] px-5 py-4 last:border-0">
    <h3>
      <button
        type="button"
        class="flex w-full items-center justify-between text-sm font-bold"
        :aria-expanded="expanded"
        :aria-controls="id"
        @click="expanded = !expanded"
      >
        {{ title }}
        <ChevronDown :class="cn('size-4 text-muted transition-transform', expanded && 'rotate-180')" aria-hidden="true" />
      </button>
    </h3>
    <div v-show="expanded" :id="id" class="mt-4 flex flex-col gap-4">
      <slot />
    </div>
  </section>
</template>
