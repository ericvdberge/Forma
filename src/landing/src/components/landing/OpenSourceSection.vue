<script setup lang="ts">
import { Code, GitFork, Server, Users } from 'lucide-vue-next'
import { FBrowserWindow, FButton, FContainer, FEyebrow, FFeature, FHeading, FText } from '@forma/design-system'
import { links } from '@/config'

const points = [
  { icon: Code, title: 'Free, forever', description: 'Every feature, no paywalls or seat limits.' },
  { icon: Server, title: 'Self-host anywhere', description: 'Your server, your data, your rules.' },
  { icon: GitFork, title: 'Built in the open', description: 'Read, fork and extend every line.' },
  { icon: Users, title: 'Community-driven', description: 'Shaped by the people who use it.' },
]
</script>

<template>
  <section id="open-source" class="scroll-mt-24 bg-canvas py-20 md:py-28">
    <FContainer class="grid items-center gap-14 lg:grid-cols-[1fr_1.1fr] lg:gap-20">
      <div>
        <FEyebrow>Open source</FEyebrow>
        <FHeading size="xl" class="mt-5">Free to use. Open to everyone.</FHeading>
        <FText class="mt-6 max-w-md">
          Forma is fully open source. No pricing tiers, no locked features — clone it, run it, and make it your own.
        </FText>
        <div class="mt-9 flex flex-wrap gap-3">
          <FButton :href="links.github" arrow>View on GitHub</FButton>
          <FButton variant="secondary" :href="`${links.github}#readme`">Read the docs</FButton>
        </div>
      </div>

      <div class="flex flex-col gap-10">
        <FBrowserWindow tone="dark" url="terminal">
          <pre
            class="overflow-x-auto p-6 font-mono text-[0.8125rem] leading-7 text-warm-white/80"
          ><code><span class="text-white/35">$</span> git clone {{ links.github }}.git
<span class="text-white/35">$</span> cd Forma/src/landing
<span class="text-white/35">$</span> npm install && npm run dev
<span class="text-accent-300">✓</span> <span class="text-warm-white">Forma is running</span> <span class="text-white/35">→ http://localhost:5173</span></code></pre>
        </FBrowserWindow>
        <div class="grid grid-cols-1 gap-8 sm:grid-cols-2">
          <FFeature v-for="p in points" :key="p.title" v-bind="p" align="start" />
        </div>
      </div>
    </FContainer>
  </section>
</template>
