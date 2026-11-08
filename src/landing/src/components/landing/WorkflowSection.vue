<script setup lang="ts">
import { FContainer, FEyebrow, FHeading, FText } from '@forma/design-system'

const steps = [
  { n: '01', title: 'Design', body: 'Create beautiful layouts with flexible visual controls and thoughtful defaults.' },
  { n: '02', title: 'Build', body: 'Combine reusable blocks, sections, components and templates.' },
  { n: '03', title: 'Publish', body: 'Deliver responsive, polished websites ready for the real world.' },
]
</script>

<template>
  <section id="workflow" class="scroll-mt-24 py-20 md:py-28">
    <FContainer>
      <div class="grid gap-12 lg:grid-cols-[1fr_1.4fr] lg:gap-20">
        <div>
          <FEyebrow>How it works</FEyebrow>
          <FHeading size="xl" class="mt-5">Design. Build. Publish.</FHeading>
          <FText class="mt-6 max-w-sm">
            A simple path from idea to online. Forma handles the hard parts so you can focus on the work.
          </FText>
        </div>
        <ol class="grid gap-8 sm:grid-cols-3 sm:gap-6">
          <li v-for="step in steps" :key="step.n" class="border-t border-charcoal pt-6">
            <span class="text-xs font-bold tracking-[0.14em] text-subtle">{{ step.n }}</span>
            <h3 class="mt-6 font-display text-2xl font-bold tracking-[-0.03em]">{{ step.title }}</h3>
            <p class="mt-3 text-sm leading-relaxed text-muted">{{ step.body }}</p>
          </li>
        </ol>
      </div>
    </FContainer>
  </section>
</template>
