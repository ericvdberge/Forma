<script setup lang="ts">
import { FButton, FContainer, FHeading, FText } from '@forma/design-system'
import { links } from '@/config'
</script>

<template>
  <section id="get-started" class="scroll-mt-24 py-20 md:py-28">
    <FContainer>
      <div class="relative isolate overflow-hidden rounded-panel bg-charcoal px-6 py-16 sm:px-12 md:py-20 lg:px-20">
        <div
          aria-hidden="true"
          class="absolute -right-24 -top-40 -z-10 size-[560px] rounded-full bg-[radial-gradient(circle_at_30%_30%,rgb(255_255_255/0.2),rgb(255_255_255/0.04)_55%,transparent_70%)]"
        />
        <div
          aria-hidden="true"
          class="absolute -bottom-40 -left-20 -z-10 size-[420px] rounded-full bg-accent/25 blur-[100px]"
        />
        <div class="grid items-end gap-10 lg:grid-cols-[1.3fr_1fr]">
          <div>
            <FHeading size="xl" tone="light">Turn your ideas into a stunning website.</FHeading>
            <FText tone="light-muted" class="mt-6 max-w-md">
              Free and open source. Clone the repo, run it locally, and start building in minutes.
            </FText>
          </div>
          <div class="flex flex-wrap gap-3 lg:justify-end">
            <FButton variant="inverse" size="lg" :href="links.github" arrow>Get the code</FButton>
            <FButton variant="ghost" size="lg" :href="`${links.github}#readme`" class="!text-warm-white ring-1 ring-inset ring-white/20 hover:!bg-white/10">
              Read the docs
            </FButton>
          </div>
        </div>
      </div>
    </FContainer>
  </section>
</template>
