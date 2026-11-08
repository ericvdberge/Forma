<script setup lang="ts">
import SiteHeader from '@/components/landing/SiteHeader.vue'
import HeroSection from '@/components/landing/HeroSection.vue'
import FeatureStrip from '@/components/landing/FeatureStrip.vue'
import DesignFreedomSection from '@/components/landing/DesignFreedomSection.vue'
import TemplatesSection from '@/components/landing/TemplatesSection.vue'
import WorkflowSection from '@/components/landing/WorkflowSection.vue'
import OpenSourceSection from '@/components/landing/OpenSourceSection.vue'
import CtaSection from '@/components/landing/CtaSection.vue'
import SiteFooter from '@/components/landing/SiteFooter.vue'
</script>

<template>
  <div class="relative min-h-screen bg-gradient-to-b from-canvas via-warm-white to-warm-white">
    <SiteHeader />
    <main>
      <HeroSection />
      <FeatureStrip />
      <DesignFreedomSection />
      <TemplatesSection />
      <WorkflowSection />
      <OpenSourceSection />
      <CtaSection />
    </main>
    <SiteFooter />
  </div>
</template>
