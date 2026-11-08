<script setup lang="ts">
import { Columns2, Image, Plus, RectangleHorizontal, Type } from 'lucide-vue-next'
import { FArchitectureArt, FLogo, FToolbar } from '@forma/design-system'

const tools = [
  { icon: Plus, label: 'Add block' },
  { icon: Type, label: 'Text', active: true },
  { icon: RectangleHorizontal, label: 'Button' },
  { icon: Image, label: 'Image' },
  { icon: Columns2, label: 'Layout' },
]
</script>

<template>
  <div class="relative h-[380px] w-full sm:h-[460px] lg:h-[520px]" aria-label="Forma editor preview" role="img">
    <!-- accent orb -->
    <div
      aria-hidden="true"
      class="absolute -right-10 -top-16 size-[360px] rounded-full bg-[radial-gradient(circle_at_40%_40%,#7aa7fb_0%,#b9d0ff_40%,rgb(220_231_251/0.6)_62%,transparent_72%)] blur-[2px] sm:size-[440px]"
    />

    <!-- background editor canvas -->
    <div
      aria-hidden="true"
      class="absolute left-[6%] right-[-12%] top-[10%] h-[78%] rounded-2xl bg-white/70 ring-1 ring-charcoal/[0.05] backdrop-blur [transform:perspective(1800px)_rotateY(-16deg)_rotateX(8deg)_rotateZ(-3deg)]"
    >
      <div class="flex h-9 items-center gap-6 border-b border-charcoal/[0.05] px-6 text-[0.625rem] text-subtle">
        <span>Pages</span><span>Design</span><span class="text-charcoal/60">Components</span><span>Publish</span>
      </div>
      <div class="grid grid-cols-3 gap-3 p-5 opacity-60">
        <span class="h-2 rounded-full bg-charcoal/10" /><span class="h-2 rounded-full bg-charcoal/[0.06]" /><span
          class="h-2 rounded-full bg-charcoal/[0.06]"
        />
      </div>
    </div>

    <!-- floating toolbar -->
    <div class="absolute left-0 top-[18%] z-20 animate-float sm:left-[2%]">
      <FToolbar :items="tools" />
    </div>

    <!-- hero card -->
    <div
      class="absolute left-[16%] right-[-4%] top-[22%] z-10 aspect-[16/11] overflow-hidden rounded-[1.5rem] shadow-float ring-1 ring-white/10 [transform:perspective(1600px)_rotateY(-14deg)_rotateX(6deg)_rotateZ(3deg)] sm:left-[18%] lg:right-[-10%]"
    >
      <FArchitectureArt id="hero-arch" />
      <div class="absolute inset-0 flex flex-col justify-between p-6 sm:p-8">
        <FLogo tone="light" size="sm" />
        <div>
          <p class="max-w-[60%] font-display text-[clamp(2rem,4.5vw,3.5rem)] font-bold leading-[0.92] tracking-[-0.05em] text-warm-white">
            Dream bigger.
          </p>
          <span
            class="mt-5 inline-flex items-center gap-2 rounded-full bg-white/15 px-4 py-1.5 text-[0.6875rem] font-medium text-white ring-1 ring-white/25 backdrop-blur"
          >
            Explore →
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
