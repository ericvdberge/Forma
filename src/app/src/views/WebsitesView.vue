<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Check, ExternalLink, FileText, Plus, X } from 'lucide-vue-next'
import { FArchitectureArt, FAvatar, FButton, FCard, FIconButton, FInput, FStatus, FSwitch, cn } from '@forma/design-system'
import PageHeader from '@/components/PageHeader.vue'
import LinkButton from '@/components/LinkButton.vue'
import { useCms } from '@/stores/cms'
import { shortAgo } from '@/utils/time'

const cms = useCms()
const { state, activeWebsite } = cms
const route = useRoute()
const router = useRouter()

const selected = computed(() => {
  const id = typeof route.params.id === 'string' ? route.params.id : activeWebsite.value.id
  return state.websites.find((w) => w.id === id) ?? activeWebsite.value
})
const pageCount = (id: string) => cms.pagesFor(id).length

/* Edit form, reset whenever the selection changes */
const form = reactive({ name: '', domain: '', description: '', published: false })
function reset() {
  form.name = selected.value.name
  form.domain = selected.value.domain
  form.description = selected.value.description
  form.published = selected.value.status === 'live'
}
watch(() => selected.value.id, reset, { immediate: true })

const dirty = computed(
  () =>
    form.name !== selected.value.name ||
    form.domain !== selected.value.domain ||
    form.description !== selected.value.description ||
    form.published !== (selected.value.status === 'live'),
)
const domainValid = computed(() => /^[a-z0-9-]+(\.[a-z0-9-]+)+$/i.test(form.domain.trim()))
const canSave = computed(() => dirty.value && form.name.trim() !== '' && domainValid.value)

function save() {
  if (!canSave.value) return
  cms.updateWebsite(selected.value.id, {
    name: form.name.trim(),
    domain: form.domain.trim().toLowerCase(),
    description: form.description.trim(),
    status: form.published ? 'live' : 'draft',
  })
}

/* New website */
const creating = ref(false)
const newName = ref('')
function create() {
  if (!newName.value.trim()) return
  const site = cms.createWebsite(newName.value.trim())
  creating.value = false
  newName.value = ''
  router.push(`/websites/${site.id}`)
}
</script>

<template>
  <div class="mx-auto max-w-[1240px]">
    <PageHeader title="Websites" description="Switch between your websites and manage their details.">
      <FButton @click="creating = !creating"><Plus class="size-4" aria-hidden="true" /> New website</FButton>
    </PageHeader>

    <FCard v-if="creating" padding="none" class="mb-6">
      <form class="flex flex-col gap-4 p-5 sm:flex-row sm:items-end" @submit.prevent="create">
        <FInput v-model="newName" label="Website name" placeholder="e.g. Northlight Studio" />
        <div class="flex shrink-0 gap-2">
          <FButton type="submit" :disabled="!newName.trim()">Create website</FButton>
          <FIconButton :icon="X" label="Cancel" @click="creating = false" />
        </div>
      </form>
    </FCard>

    <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <!-- List -->
      <section aria-label="Your websites" class="flex flex-col gap-3">
        <RouterLink
          v-for="site in state.websites"
          :key="site.id"
          :to="`/websites/${site.id}`"
          :aria-current="site.id === selected.id ? 'true' : undefined"
          :class="
            cn(
              'group flex items-center gap-4 rounded-card bg-white p-3 ring-1 transition-all',
              site.id === selected.id ? 'ring-2 ring-charcoal shadow-card' : 'ring-charcoal/[0.06] shadow-soft hover:ring-charcoal/20',
            )
          "
        >
          <span class="h-[4.5rem] w-28 shrink-0 overflow-hidden rounded-xl ring-1 ring-charcoal/[0.06]">
            <FArchitectureArt :tone="site.tone" />
          </span>
          <span class="min-w-0 flex-1">
            <span class="flex items-center gap-2">
              <span class="truncate font-medium">{{ site.name }}</span>
              <span
                v-if="site.id === activeWebsite.id"
                class="rounded-full bg-accent-50 px-2 py-0.5 text-[0.6875rem] font-medium text-accent-700"
                >Active</span
              >
            </span>
            <span class="block truncate text-sm text-muted">{{ site.domain }}</span>
            <span class="mt-1 block text-xs text-subtle">{{ pageCount(site.id) }} {{ pageCount(site.id) === 1 ? 'page' : 'pages' }} · updated {{ shortAgo(site.updatedAt) }}</span>
          </span>
          <FStatus :status="site.status" class="shrink-0 pr-2" />
        </RouterLink>
      </section>

      <!-- Detail -->
      <FCard as="section" padding="none" :aria-label="`${selected.name} settings`" class="self-start">
        <div class="relative h-40">
          <FArchitectureArt :tone="selected.tone" />
          <div class="absolute inset-x-0 bottom-0 flex items-end justify-between gap-3 bg-gradient-to-t from-charcoal/50 to-transparent p-5">
            <div class="min-w-0 text-warm-white">
              <p class="truncate font-display text-2xl font-bold tracking-[-0.03em]">{{ selected.name }}</p>
              <p class="inline-flex items-center gap-1.5 text-sm text-warm-white/80">
                {{ selected.domain }} <ExternalLink class="size-3.5" aria-hidden="true" />
              </p>
            </div>
            <div class="flex shrink-0 -space-x-1.5">
              <FAvatar v-for="c in selected.collaborators.slice(0, 4)" :key="c" :name="c" size="sm" ring />
            </div>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-3 border-b border-charcoal/[0.06] px-5 py-4">
          <FButton v-if="selected.id !== activeWebsite.id" size="sm" @click="cms.setActiveWebsite(selected.id)">
            Switch to this website
          </FButton>
          <span v-else class="inline-flex h-9 items-center gap-1.5 text-sm font-medium text-accent-700">
            <Check class="size-4" aria-hidden="true" /> Active website
          </span>
          <LinkButton
            v-if="selected.id === activeWebsite.id"
            to="/pages"
            variant="secondary"
            size="sm"
            class="ml-auto"
          >
            <FileText class="size-4" aria-hidden="true" /> Manage pages
          </LinkButton>
        </div>

        <form class="flex flex-col gap-5 p-5" @submit.prevent="save">
          <FInput v-model="form.name" label="Website name" />
          <div>
            <FInput v-model="form.domain" label="Domain" placeholder="studio.forma.dev" />
            <p v-if="form.domain && !domainValid" class="mt-1.5 px-1 text-xs text-danger">Enter a domain like studio.forma.dev</p>
          </div>
          <div class="flex flex-col gap-1.5">
            <label for="site-description" class="text-sm font-medium">Description</label>
            <textarea
              id="site-description"
              v-model="form.description"
              rows="3"
              placeholder="What is this website about?"
              class="w-full resize-none rounded-2xl bg-white px-5 py-3 text-[0.9375rem] ring-1 ring-charcoal/10 outline-none transition-shadow placeholder:text-subtle focus:ring-2 focus:ring-accent"
            />
          </div>
          <div class="flex items-center justify-between rounded-2xl bg-canvas px-4 py-3">
            <div>
              <p class="text-sm font-medium">Published</p>
              <p class="text-xs text-muted">{{ form.published ? 'Visible to everyone.' : 'Only collaborators can see it.' }}</p>
            </div>
            <FSwitch v-model="form.published" label="Published" hide-label />
          </div>
          <div class="flex justify-end gap-2 pt-1">
            <FButton variant="ghost" :disabled="!dirty" @click="reset">Discard</FButton>
            <FButton type="submit" :disabled="!canSave">Save changes</FButton>
          </div>
        </form>
      </FCard>
    </div>
  </div>
</template>
