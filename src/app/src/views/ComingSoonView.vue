<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { FBrandShape, FCard } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'

const route = useRoute()
const title = computed(() => route.meta.title ?? 'This section')
</script>

<template>
  <div class="mx-auto max-w-[1240px] pt-2">
    <FCard padding="lg" class="flex min-h-[420px] flex-col items-center justify-center gap-5 text-center">
      <div class="flex items-center gap-3" aria-hidden="true">
        <FBrandShape shape="leaf" :size="28" />
        <FBrandShape shape="circle" :size="28" />
        <FBrandShape shape="wedge" :size="28" />
        <FBrandShape shape="gradient" :size="28" />
      </div>
      <h1 class="font-display text-3xl font-bold tracking-[-0.04em]">{{ title }}</h1>
      <p class="max-w-sm text-muted">We’re still shaping this part of Forma. It’s coming soon.</p>
      <LinkButton to="/" variant="secondary" size="sm">Back to dashboard</LinkButton>
    </FCard>
  </div>
</template>
