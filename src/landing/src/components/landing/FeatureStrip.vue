<script setup lang="ts">
import { ChartNoAxesCombined, Layers, LayoutGrid, Zap } from 'lucide-vue-next'
import { FContainer, FFeature } from '@forma/design-system'

const features = [
  { icon: Zap, title: 'Drag & drop editor', description: 'Design freely, no code.' },
  { icon: LayoutGrid, title: 'Stunning templates', description: 'Get started faster.' },
  { icon: Layers, title: 'Flexible components', description: 'Build without limits.' },
  { icon: ChartNoAxesCombined, title: 'Built to grow', description: 'Scale with confidence.' },
]
</script>

<template>
  <section id="features" aria-label="Key features" class="py-10 md:py-14">
    <FContainer>
      <div class="grid grid-cols-2 gap-y-10 lg:grid-cols-4">
        <FFeature
          v-for="(feature, i) in features"
          :key="feature.title"
          v-bind="feature"
          :class="[
            'px-4',
            i % 2 === 1 && 'border-l border-charcoal/[0.08]',
            i > 0 && 'lg:border-l lg:border-charcoal/[0.08]',
          ]"
        />
      </div>
    </FContainer>
  </section>
</template>
