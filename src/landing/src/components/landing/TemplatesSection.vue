<script setup lang="ts">
import { FArchitectureArt, FArrowLink, FBrowserWindow, FCard, FContainer, FEyebrow, FHeading, FText } from '@forma/design-system'

const templates = [
  { name: 'Studio', category: 'Portfolio', headline: 'Quiet work, loud ideas.', tone: 'light' as const },
  { name: 'Atelier', category: 'Architecture', headline: 'Dream bigger.', tone: 'dark' as const },
  { name: 'Common', category: 'Business', headline: 'Grow with intent.', tone: 'accent' as const },
]
</script>

<template>
  <section id="templates" class="scroll-mt-24 bg-canvas py-20 md:py-28">
    <FContainer>
      <div class="flex flex-col justify-between gap-6 md:flex-row md:items-end">
        <div>
          <FEyebrow>Templates</FEyebrow>
          <FHeading size="xl" class="mt-5 max-w-xl">Start from something beautiful.</FHeading>
        </div>
        <FText class="max-w-sm">
          Thoughtful starting points for every kind of site. Every template is fully responsive and endlessly
          editable.
        </FText>
      </div>

      <div class="mt-14 grid gap-6 md:grid-cols-3">
        <FCard v-for="t in templates" :key="t.name" as="article" padding="sm" interactive class="group">
          <FBrowserWindow dots="mono" class="!shadow-none">
            <div
              :class="[
                'relative flex aspect-[4/3] flex-col justify-end overflow-hidden p-5',
                t.tone === 'light' && 'bg-warm-white',
                t.tone === 'accent' && 'bg-gradient-to-br from-accent-100 to-accent-300',
              ]"
            >
              <FArchitectureArt v-if="t.tone === 'dark'" :id="`tpl-${t.name}`" class="absolute inset-0" />
              <template v-if="t.tone === 'light'">
                <span class="absolute right-5 top-5 size-16 rounded-full bg-charcoal" aria-hidden="true" />
                <span
                  class="absolute right-20 top-14 size-10 rounded-[60%_15%_60%_15%] bg-sage"
                  aria-hidden="true"
                />
              </template>
              <p
                :class="[
                  'relative max-w-[75%] font-display text-2xl font-bold leading-[0.95] tracking-[-0.04em] transition-transform duration-500 group-hover:-translate-y-1',
                  t.tone === 'dark' ? 'text-warm-white' : 'text-charcoal',
                ]"
              >
                {{ t.headline }}
              </p>
            </div>
          </FBrowserWindow>
          <div class="flex items-center justify-between px-2 pb-1 pt-5">
            <div>
              <h3 class="font-display text-lg font-medium tracking-[-0.02em]">{{ t.name }}</h3>
              <p class="text-sm text-muted">{{ t.category }}</p>
            </div>
            <FArrowLink :href="`#template-${t.name.toLowerCase()}`" aria-label="Use template">Use</FArrowLink>
          </div>
        </FCard>
      </div>
    </FContainer>
  </section>
</template>
