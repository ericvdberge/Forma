<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { Menu, X } from 'lucide-vue-next'
import { FButton, FContainer, FDropdown, FLogo, cn } from '@forma/design-system'
import { links as siteLinks } from '@/config'

const links = [
  { label: 'Features', href: '#features' },
  { label: 'Templates', href: '#templates' },
  { label: 'Open source', href: '#open-source' },
]
const resources = [
  { label: 'Design system', href: '#/design-system', description: 'Tokens, components and brand rules' },
  { label: 'Guides', href: '#workflow', description: 'From idea to published site' },
  { label: 'Releases', href: `${siteLinks.github}/releases`, description: 'What shipped recently' },
]

const scrolled = ref(false)
const mobileOpen = ref(false)
const onScroll = () => (scrolled.value = window.scrollY > 8)
onMounted(() => {
  onScroll()
  window.addEventListener('scroll', onScroll, { passive: true })
})
onBeforeUnmount(() => window.removeEventListener('scroll', onScroll))
</script>

<template>
  <header
    :class="
      cn(
        'sticky top-0 z-40 transition-all duration-300',
        scrolled || mobileOpen ? 'bg-warm-white/85 shadow-[0_1px_0_rgb(11_11_15/0.06)] backdrop-blur-xl' : 'bg-transparent',
      )
    "
  >
    <FContainer class="flex h-20 items-center justify-between gap-6">
      <a href="#top" class="shrink-0" aria-label="Forma home"><FLogo size="md" /></a>

      <nav class="hidden items-center gap-9 md:flex" aria-label="Main">
        <a
          v-for="link in links"
          :key="link.label"
          :href="link.href"
          class="text-sm font-medium text-charcoal/75 transition-colors hover:text-charcoal"
        >
          {{ link.label }}
        </a>
        <FDropdown label="Resources" :items="resources" />
      </nav>

      <div class="hidden items-center gap-3 md:flex">
        <FButton variant="ghost" size="sm" :href="siteLinks.github">GitHub</FButton>
        <FButton size="md" href="#get-started" arrow>Get started</FButton>
      </div>

      <button
        type="button"
        class="grid size-10 place-items-center rounded-full ring-1 ring-charcoal/10 md:hidden"
        :aria-expanded="mobileOpen"
        aria-controls="mobile-nav"
        :aria-label="mobileOpen ? 'Close menu' : 'Open menu'"
        @click="mobileOpen = !mobileOpen"
      >
        <X v-if="mobileOpen" class="size-5" />
        <Menu v-else class="size-5" />
      </button>
    </FContainer>

    <div v-show="mobileOpen" id="mobile-nav" class="border-t border-charcoal/[0.06] md:hidden">
      <FContainer as="nav" class="flex flex-col gap-1 py-4" aria-label="Mobile">
        <a
          v-for="link in [...links, ...resources]"
          :key="link.label"
          :href="link.href"
          class="rounded-xl px-3 py-3 text-base font-medium hover:bg-canvas"
          @click="mobileOpen = false"
        >
          {{ link.label }}
        </a>
        <div class="mt-3 flex gap-3">
          <FButton variant="secondary" :href="siteLinks.github" class="flex-1">GitHub</FButton>
          <FButton href="#get-started" arrow class="flex-1">Get started</FButton>
        </div>
      </FContainer>
    </div>
  </header>
</template>
