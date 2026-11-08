<script setup lang="ts">
import { FBadge, FButton, FContainer, FText } from '@forma/design-system'
import HeroEditorMockup from './HeroEditorMockup.vue'
</script>

<template>
  <section id="top" class="relative overflow-x-clip pb-12 pt-10 md:pt-16 lg:pb-20">
    <FContainer class="grid items-center gap-12 lg:grid-cols-[1.05fr_1fr] lg:gap-6">
      <div class="relative z-10 animate-fade-up">
        <FBadge>The modern website builder</FBadge>
        <h1
          class="mt-7 font-display text-[clamp(2.75rem,6.6vw,5.5rem)] font-bold leading-[0.95] tracking-[-0.05em] text-charcoal"
        >
          Build beautiful<br class="hidden sm:block" />
          websites with<br />
          <span class="bg-gradient-to-r from-accent-600 via-accent to-accent-300 bg-clip-text text-transparent">Forma.</span>
        </h1>
        <FText size="lead" tone="default" class="mt-7 max-w-[30rem] text-charcoal/80">
          A powerful, intuitive platform for creating stunning websites — no code required.
        </FText>
        <div class="mt-9 flex flex-wrap gap-3">
          <FButton size="lg" href="#get-started" arrow>Start for free</FButton>
          <FButton size="lg" variant="secondary" href="#templates">Explore templates</FButton>
        </div>
      </div>

      <HeroEditorMockup />
    </FContainer>
  </section>
</template>
