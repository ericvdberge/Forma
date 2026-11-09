<script setup lang="ts">
import { ref } from 'vue'
import { ArrowLeft, Bell, Clock, Copy, Database, Download, Globe, Images, LayoutGrid, Layers, List, Scale, Search, Trash2, Zap } from 'lucide-vue-next'
import {
  FActionButton,
  FArchitectureArt,
  FArrowLink,
  FAvatar,
  FBadge,
  FBrandShape,
  FButton,
  FCard,
  FContainer,
  FDivider,
  FDropzone,
  FEyebrow,
  FFeature,
  FHeading,
  FIconButton,
  FInput,
  FLogo,
  FMediaCard,
  FMediaChip,
  FMenu,
  FModal,
  FPillTabs,
  FProgress,
  FPromoBanner,
  FSegmented,
  FSelect,
  FSparkline,
  FStatGroup,
  FStatTile,
  FStatus,
  FSwatch,
  FSwitch,
  FText,
  FThumbnailRow,
  FUploadProgress,
  brand,
  palette,
  supportingPalette,
  typography,
} from '@forma/design-system'

const demoEmail = ref('')
const demoSwitch = ref(true)
const demoRole = ref<'editor' | 'admin'>('editor')
const demoFilter = ref<'all' | 'images' | 'videos'>('all')
const demoView = ref<'grid' | 'list'>('grid')
const demoModal = ref(false)
const demoMenu = [
  { label: 'Download', icon: Download },
  { label: 'Copy link', icon: Copy },
  { label: 'Delete', icon: Trash2, danger: true },
]
/** Inline artwork so the showcase needs no image files. */
const demoImage = (from: string, to: string) =>
  `data:image/svg+xml,${encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="400"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${from}"/><stop offset="1" stop-color="${to}"/></linearGradient></defs><rect width="640" height="400" fill="url(#g)"/><path d="M0 300 Q 260 140 640 220 L 640 400 L 0 400 Z" fill="#fff" opacity=".55"/></svg>`,
  )}`
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
          <div class="grid max-w-2xl gap-6 sm:grid-cols-2">
            <FInput v-model="demoEmail" label="Email address" placeholder="you@studio.com" type="email" />
            <FSelect
              v-model="demoRole"
              label="Role"
              :options="[
                { label: 'Editor', value: 'editor' },
                { label: 'Admin', value: 'admin' },
              ]"
            />
          </div>
          <FDivider />
          <div class="flex flex-wrap items-center gap-6">
            <FIconButton :icon="Search" label="Search" />
            <FIconButton :icon="Bell" label="Notifications" variant="surface" dot />
            <div class="flex -space-x-2">
              <FAvatar name="Alex Morgan" ring />
              <FAvatar name="Sam Lee" ring />
              <FAvatar name="Jo Park" ring />
            </div>
            <FStatus status="live" />
            <FStatus status="draft" />
            <FStatus status="scheduled" />
            <FSwitch v-model="demoSwitch" label="Published" />
          </div>
          <FDivider />
          <div class="grid gap-6 sm:grid-cols-3">
            <FSparkline :values="[4, 6, 5, 8, 7, 9, 8, 11, 10, 12]" />
            <FSparkline :values="[6, 5, 6, 4, 5, 6, 5, 7]" tone="neutral" />
            <div class="grid grid-cols-2 gap-3">
              <div class="aspect-[4/3] overflow-hidden rounded-xl"><FArchitectureArt /></div>
              <div class="aspect-[4/3] overflow-hidden rounded-xl"><FArchitectureArt tone="light" /></div>
            </div>
          </div>
        </FCard>

        <FHeading id="ds-media" as="h3" size="sm">Media</FHeading>
        <FCard padding="lg" class="flex flex-col gap-8">
          <FStatGroup>
            <FStatTile :icon="Images" value="12.4K" label="Total media" change="12%" change-label="this month" />
            <FStatTile :icon="Database" value="48.2 GB" label="Storage used" change="4%" change-label="this month" />
            <FStatTile :icon="Scale" value="2.1 MB" label="Average size" />
            <FStatTile :icon="Clock" value="2h ago" label="Last upload" />
          </FStatGroup>
          <div class="flex flex-wrap items-center justify-between gap-4">
            <FPillTabs
              v-model="demoFilter"
              label="Filter"
              :options="[
                { label: 'All', value: 'all' },
                { label: 'Images', value: 'images', count: 24 },
                { label: 'Videos', value: 'videos', count: 3 },
              ]"
            />
            <FSegmented
              v-model="demoView"
              label="Layout"
              :options="[
                { value: 'grid', label: 'Grid view', icon: LayoutGrid },
                { value: 'list', label: 'List view', icon: List },
              ]"
            />
          </div>
          <div class="grid gap-5 sm:grid-cols-3">
            <FMediaCard :src="demoImage('#eef1f5', '#93bbfd')" alt="Blue sample" meta="JPG · 2.4 MB" @open="demoModal = true">
              <template #actions><FMenu :items="demoMenu" label="Actions" variant="glass" /></template>
            </FMediaCard>
            <FMediaCard :src="demoImage('#f5f4f0', '#a8b5a0')" alt="Sage sample" meta="PNG · 1.8 MB" @open="demoModal = true" />
            <FMediaCard :src="demoImage('#fafaf8', '#d8d2c8')" alt="Stone sample" meta="WEBP · 640 KB" @open="demoModal = true" />
          </div>
          <FDivider />
          <div class="grid gap-8 md:grid-cols-2">
            <FDropzone hint="Supports images up to 10 MB" :formats="['JPG', 'PNG', 'GIF', 'WEBP']" />
            <div class="flex flex-col gap-3">
              <FUploadProgress name="facade.jpg" :src="demoImage('#eef1f5', '#93bbfd')" :progress="72" />
              <FUploadProgress name="interior.png" status="processing" />
              <FUploadProgress name="notes.txt" status="failed" error="Only images are allowed." dismiss-label="Dismiss" />
              <FProgress :value="40" label="Example progress" />
              <div class="flex flex-wrap items-center gap-3 rounded-2xl bg-charcoal/70 p-3">
                <FMediaChip>JPG · 2.4 MB</FMediaChip>
                <FMediaChip :icon="null" size="md">1 / 24</FMediaChip>
                <FMediaChip tone="light">Light chip</FMediaChip>
              </div>
            </div>
          </div>
          <FDivider />
          <div class="grid gap-8 md:grid-cols-2">
            <div class="flex flex-col gap-2">
              <FActionButton :icon="Download" size="sm">Download</FActionButton>
              <FActionButton :icon="Copy" size="sm">Copy link</FActionButton>
              <FActionButton :icon="Trash2" size="sm" tone="danger">Delete</FActionButton>
            </div>
            <div class="flex flex-col gap-2">
              <FThumbnailRow title="Architecture" subtitle="324 files" :src="demoImage('#eef1f5', '#93bbfd')" interactive />
              <FThumbnailRow eyebrow="In website" title="Atelier Nord" subtitle="ateliernord.com" :icon="Globe" bordered />
              <FButton variant="secondary" size="sm" class="self-start" @click="demoModal = true">Open modal</FButton>
            </div>
          </div>
        </FCard>

        <FModal v-model:open="demoModal" size="sm" labelledby="demo-modal-title">
          <div class="flex flex-col gap-4 p-7">
            <h3 id="demo-modal-title" class="font-display text-xl font-bold tracking-[-0.02em]">A Forma modal</h3>
            <FText>Blurred backdrop, focus kept inside, closes with Escape or a click outside.</FText>
            <FButton size="sm" class="self-end" data-autofocus @click="demoModal = false">Got it</FButton>
          </div>
        </FModal>

        <FPromoBanner href="#/">Turn your ideas<br />into a stunning website.</FPromoBanner>
      </section>
    </FContainer>
  </div>
</template>
