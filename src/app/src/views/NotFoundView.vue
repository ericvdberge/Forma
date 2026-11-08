<script setup lang="ts">
import { FCard } from '@forma/design-system'
import LinkButton from '@/components/LinkButton.vue'
</script>

<template>
  <div class="mx-auto max-w-[1240px] pt-2">
    <FCard padding="lg" class="flex min-h-[420px] flex-col items-center justify-center gap-4 text-center">
      <p class="font-display text-7xl font-bold tracking-[-0.05em]">404<span class="text-accent">.</span></p>
      <p class="text-muted">This page doesn’t exist.</p>
      <LinkButton to="/" variant="secondary" size="sm">Back to dashboard</LinkButton>
    </FCard>
  </div>
</template>
