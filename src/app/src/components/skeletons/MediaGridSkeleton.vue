<script setup lang="ts">
import { FSkeleton } from '@forma/design-system'

withDefaults(defineProps<{ count?: number }>(), { count: 10 })
</script>

<template>
  <div class="grid grid-cols-2 gap-4 p-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
    <div v-for="i in count" :key="i">
      <FSkeleton class="aspect-square w-full" rounded="xl" />
      <FSkeleton class="mt-3 h-4 w-3/4" />
      <FSkeleton class="mt-2 h-3 w-1/2" />
    </div>
  </div>
</template>
