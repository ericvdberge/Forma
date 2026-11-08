<script setup lang="ts">
import { ref } from 'vue'
import { ArrowLeft, Layers, Zap } from 'lucide-vue-next'
import {
  FArrowLink,
  FBadge,
  FBrandShape,
  FButton,
  FCard,
  FContainer,
  FDivider,
  FEyebrow,
  FFeature,
  FHeading,
  FInput,
  FLogo,
  FPromoBanner,
  FSwatch,
  FText,
  brand,
  palette,
  supportingPalette,
  typography,
} from '@forma/design-system'

const demoEmail = ref('')
</script>

<template>
  <div class="min-h-screen bg-canvas pb-24">
    <FContainer class="flex h-20 items-center justify-between">
      <a href="#/" class="inline-flex items-center gap-2 text-sm font-medium text-charcoal/75 hover:text-charcoal">
        <ArrowLeft class="size-4" /> Back to site
      </a>
      <FBadge tone="accent" :dot="false">Brand Guide v1.0</FBadge>
    </FContainer>

    <FContainer as="main" class="flex flex-col gap-20">
      <!-- Intro -->
      <section class="grid gap-10 border-b border-charcoal/[0.08] pb-16 md:grid-cols-[1.2fr_1fr] md:items-end">
        <div>
          <FLogo size="xl" />
          <p class="mt-4 text-2xl text-charcoal/80">{{ brand.tagline }}</p>
        </div>
        <div class="border-charcoal/[0.08] md:border-l md:pl-10">
          <FHeading as="h1" size="md">Brand Guide</FHeading>
          <FText class="mt-3">
            Forma is a modern website creation platform for everyone who wants to build something beautiful on the web.
          </FText>
        </div>
      </section>

      <!-- Logo -->
      <section aria-labelledby="ds-logo">
        <FHeading id="ds-logo" as="h2" size="sm">Logo variations</FHeading>
        <div class="mt-6 grid gap-4 sm:grid-cols-3">
          <FCard class="grid h-36 place-items-center"><FLogo size="lg" /></FCard>
          <FCard variant="dark" class="grid h-36 place-items-center"><FLogo size="lg" tone="light" /></FCard>
          <FCard class="grid h-36 place-items-center"><FLogo size="lg" variant="mark" accent-dot /></FCard>
        </div>
      </section>

      <!-- Colour -->
      <section aria-labelledby="ds-colour">
        <FHeading id="ds-colour" as="h2" size="sm">Colour palette</FHeading>
        <div class="mt-8 grid grid-cols-2 gap-8 sm:grid-cols-4">
          <FSwatch v-for="c in palette" :key="c.hex" :name="c.name" :hex="c.hex" />
        </div>
        <p class="mt-10 text-sm font-medium text-muted">Supporting neutrals</p>
        <div class="mt-6 grid grid-cols-2 gap-8 sm:grid-cols-4">
          <FSwatch v-for="c in supportingPalette" :key="c.hex" :name="c.name" :hex="c.hex" />
        </div>
      </section>

      <!-- Type -->
      <section aria-labelledby="ds-type" class="grid gap-10 md:grid-cols-[auto_1fr]">
        <div>
          <FHeading id="ds-type" as="h2" size="sm">Typography</FHeading>
          <p class="mt-4 font-display text-[7rem] font-bold leading-none tracking-[-0.05em]">Aa</p>
          <p class="mt-2 text-lg font-medium">Satoshi</p>
        </div>
        <div class="flex flex-col gap-6 md:border-l md:border-charcoal/[0.08] md:pl-10">
          <div v-for="t in typography" :key="t.role">
            <p class="text-sm font-bold">{{ t.role }}</p>
            <p class="text-sm text-muted">{{ t.family }} {{ t.weight }}</p>
          </div>
          <FDivider />
          <FHeading as="p" size="display">Display</FHeading>
          <FHeading as="p" size="xl">Heading XL</FHeading>
          <FHeading as="p" size="lg">Heading LG</FHeading>
          <FText size="lead" tone="default">Lead — A powerful, intuitive platform for creating stunning websites.</FText>
          <FText>Body — From minimal portfolios to complex business sites.</FText>
        </div>
      </section>

      <!-- Elements -->
      <section aria-labelledby="ds-elements">
        <FHeading id="ds-elements" as="h2" size="sm">Brand elements</FHeading>
        <div class="mt-6 flex flex-wrap items-center gap-8">
          <FBrandShape shape="leaf" />
          <FBrandShape shape="circle" />
          <FBrandShape shape="wedge" />
          <FBrandShape shape="gradient" />
          <span class="hidden h-16 w-px bg-charcoal/10 sm:block" />
          <p class="font-display text-xl font-medium leading-tight">
            <span v-for="v in brand.values" :key="v" class="block">{{ v }}</span>
          </p>
        </div>
      </section>

      <!-- Components -->
      <section aria-labelledby="ds-components" class="flex flex-col gap-10">
        <FHeading id="ds-components" as="h2" size="sm">Components</FHeading>

        <FCard padding="lg" class="flex flex-col gap-8">
          <div class="flex flex-wrap items-center gap-3">
            <FButton arrow>Primary</FButton>
            <FButton variant="secondary">Secondary</FButton>
            <FButton variant="accent" arrow>Accent</FButton>
            <FButton variant="ghost">Ghost</FButton>
            <FButton size="sm">Small</FButton>
            <FButton size="lg" arrow>Large</FButton>
            <FButton disabled>Disabled</FButton>
          </div>
          <FDivider />
          <div class="flex flex-wrap items-center gap-3">
            <FBadge>The modern website builder</FBadge>
            <FBadge tone="accent" :dot="false">New</FBadge>
            <FBadge tone="dark">Live</FBadge>
          </div>
          <FDivider />
          <div class="flex flex-wrap items-center gap-10">
            <FEyebrow>Design freedom</FEyebrow>
            <FArrowLink href="#/design-system">Discover more</FArrowLink>
          </div>
          <FDivider />
          <div class="grid gap-8 sm:grid-cols-2">
            <FFeature :icon="Zap" title="Drag & drop editor" description="Design freely, no code." />
            <FFeature :icon="Layers" title="Flexible components" description="Build without limits." align="start" />
          </div>
          <FDivider />
          <div class="max-w-sm">
            <FInput v-model="demoEmail" label="Email address" placeholder="you@studio.com" type="email" />
          </div>
        </FCard>

        <FPromoBanner href="#/">Turn your ideas<br />into a stunning website.</FPromoBanner>
      </section>
    </FContainer>
  </div>
</template>
