<script setup lang="ts">
import { Image, Plus, Type } from 'lucide-vue-next'
import { FArrowLink, FContainer, FEyebrow, FHeading, FText } from '@forma/design-system'
import ArchitectureArt from './ArchitectureArt.vue'
</script>

<template>
  <section class="py-16 md:py-24">
    <FContainer class="grid items-center gap-12 lg:grid-cols-[1.15fr_1fr] lg:gap-20">
      <!-- laptop scene -->
      <div
        class="relative isolate flex aspect-[4/3] items-end justify-center overflow-hidden rounded-panel bg-gradient-to-br from-[#efebe4] via-[#e6e0d6] to-stone px-6 pb-[12%] sm:px-12"
      >
        <span aria-hidden="true" class="absolute -left-10 top-6 -z-10 h-[70%] w-1/3 rounded-full bg-white/50 blur-3xl" />
        <!-- vase -->
        <span aria-hidden="true" class="absolute bottom-[12%] left-[7%] hidden h-24 w-12 rounded-t-[40%] rounded-b-xl bg-white/50 ring-1 ring-white/70 backdrop-blur sm:block" />
        <span aria-hidden="true" class="absolute bottom-[12%] left-0 right-0 h-px bg-charcoal/10" />

        <div class="relative w-full max-w-[460px] [transform:perspective(1400px)_rotateY(-8deg)]" role="img" aria-label="Forma editor on a laptop">
          <div class="rounded-t-2xl bg-charcoal p-2 shadow-float">
            <div class="flex aspect-[16/10] overflow-hidden rounded-lg bg-warm-white">
              <div class="flex w-9 flex-col items-center gap-3 border-r border-charcoal/[0.06] py-3 text-charcoal/50">
                <Plus class="size-3" /><Type class="size-3" /><Image class="size-3" />
              </div>
              <div class="flex flex-1 items-center gap-3 p-4">
                <div class="flex-1">
                  <p class="font-display text-[clamp(0.9rem,2.4vw,1.35rem)] font-bold leading-[0.95] tracking-[-0.04em]">
                    Ideas.<br />Into impact.
                  </p>
                  <span class="mt-3 inline-block h-3 w-12 rounded-full bg-charcoal" />
                </div>
                <div class="h-[80%] w-[48%] overflow-hidden rounded-md">
                  <ArchitectureArt id="laptop-arch" />
                </div>
              </div>
            </div>
          </div>
          <div class="mx-[-6%] h-3 rounded-b-2xl bg-gradient-to-b from-[#c9c6c0] to-[#8f8c86]" />
          <div class="mx-auto h-1 w-1/4 rounded-b-md bg-[#7b7872]" />
        </div>
      </div>

      <div>
        <FEyebrow>Design freedom</FEyebrow>
        <FHeading size="xl" class="mt-5">Create without boundaries.</FHeading>
        <FText class="mt-6 max-w-md">
          From minimal portfolios to complex business sites — Forma gives you the freedom to design exactly what you
          envision.
        </FText>
        <FArrowLink href="#templates" class="mt-8">Discover more</FArrowLink>
      </div>
    </FContainer>
  </section>
</template>
