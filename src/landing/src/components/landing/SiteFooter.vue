<script setup lang="ts">
import { FContainer, FDivider, FLogo, brand } from '@forma/design-system'
import { links } from '@/config'

const gh = links.github
const columns = [
  {
    title: 'Product',
    links: [
      { label: 'Features', href: '#features' },
      { label: 'Templates', href: '#templates' },
      { label: 'Open source', href: '#open-source' },
    ],
  },
  {
    title: 'Resources',
    links: [
      { label: 'Docs', href: `${gh}#readme` },
      { label: 'Design system', href: '#/design-system' },
      { label: 'Releases', href: `${gh}/releases` },
    ],
  },
  {
    title: 'Community',
    links: [
      { label: 'GitHub', href: gh },
      { label: 'Issues', href: `${gh}/issues` },
      { label: 'Discussions', href: `${gh}/discussions` },
    ],
  },
]
</script>

<template>
  <footer class="pb-10 pt-6">
    <FContainer>
      <div class="grid gap-12 py-12 md:grid-cols-[1.5fr_repeat(3,1fr)]">
        <div>
          <FLogo size="lg" />
          <p class="mt-4 text-lg text-muted">{{ brand.tagline }}</p>
        </div>
        <nav v-for="col in columns" :key="col.title" :aria-label="col.title">
          <h2 class="text-xs font-bold uppercase tracking-[0.14em] text-subtle">{{ col.title }}</h2>
          <ul class="mt-5 flex flex-col gap-3">
            <li v-for="link in col.links" :key="link.label">
              <a :href="link.href" class="text-sm text-charcoal/75 transition-colors hover:text-charcoal">{{ link.label }}</a>
            </li>
          </ul>
        </nav>
      </div>
      <FDivider />
      <div class="flex flex-col justify-between gap-4 pt-8 text-sm text-muted sm:flex-row">
        <p>Forma is open source. {{ brand.values.join(' ') }}</p>
        <a :href="gh" class="hover:text-charcoal">Contribute on GitHub →</a>
      </div>
    </FContainer>
  </footer>
</template>
