import { computed, reactive, readonly, watch } from 'vue'
import * as seed from '@/data/seed'
import type { Page, PublishStatus, Website } from '@/data/types'

/**
 * In-memory CMS store. Seeded from `data/seed.ts`; swap the actions for API calls once `src/api` lands.
 * The active website is remembered per browser.
 */
const ACTIVE_KEY = 'forma.activeWebsite'

function readActive(): string {
  try {
    const id = localStorage.getItem(ACTIVE_KEY)
    if (id && seed.websites.some((w) => w.id === id)) return id
  } catch {
    /* storage unavailable */
  }
  return seed.websites[0]!.id
}

const state = reactive({
  websites: structuredClone(seed.websites),
  pages: structuredClone(seed.pages),
  activity: structuredClone(seed.activity),
  activeWebsiteId: readActive(),
  toast: null as null | { id: number; message: string },
})

watch(
  () => state.activeWebsiteId,
  (id) => {
    try {
      localStorage.setItem(ACTIVE_KEY, id)
    } catch {
      /* storage unavailable */
    }
  },
)

const slugify = (s: string) =>
  s
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')

function log(message: string, kind: 'page' | 'website') {
  state.activity.unshift({ id: crypto.randomUUID(), message, at: Date.now(), kind })
}

let toastTimer: ReturnType<typeof setTimeout> | undefined
function notify(message: string) {
  state.toast = { id: Date.now(), message }
  clearTimeout(toastTimer)
  toastTimer = setTimeout(() => (state.toast = null), 3200)
}

function touchWebsite(id: string) {
  const site = state.websites.find((w) => w.id === id)
  if (site) site.updatedAt = Date.now()
}

export function useCms() {
  const activeWebsite = computed(() => state.websites.find((w) => w.id === state.activeWebsiteId) ?? state.websites[0]!)

  return {
    state: readonly(state),
    currentUser: seed.currentUser,
    stats: seed.stats,
    activeWebsite,

    pagesFor: (websiteId: string) => state.pages.filter((p) => p.websiteId === websiteId),

    setActiveWebsite(id: string) {
      state.activeWebsiteId = id
      const site = state.websites.find((w) => w.id === id)
      if (site) notify(`Switched to ${site.name}`)
    },

    updateWebsite(id: string, patch: Partial<Pick<Website, 'name' | 'domain' | 'description' | 'status'>>) {
      const site = state.websites.find((w) => w.id === id)
      if (!site) return
      const wentLive = patch.status === 'live' && site.status !== 'live'
      Object.assign(site, patch, { updatedAt: Date.now() })
      log(wentLive ? `You published ${site.name}` : `You updated ${site.name}`, 'website')
      notify('Website saved')
    },

    createWebsite(name: string) {
      const id = `${slugify(name) || 'site'}-${Math.random().toString(36).slice(2, 6)}`
      const site: Website = {
        id,
        name,
        domain: `${slugify(name) || 'site'}.forma.dev`,
        description: '',
        status: 'draft',
        tone: state.websites.length % 2 ? 'light' : 'dark',
        collaborators: [seed.currentUser.name],
        updatedAt: Date.now(),
      }
      state.websites.push(site)
      state.pages.push({ id: crypto.randomUUID(), websiteId: id, title: 'Home', path: '/', status: 'draft', author: seed.currentUser.name, updatedAt: Date.now() })
      state.activeWebsiteId = id
      log(`You created ${name}`, 'website')
      notify(`${name} created`)
      return site
    },

    createPage(websiteId: string, title: string, path?: string) {
      const page: Page = {
        id: crypto.randomUUID(),
        websiteId,
        title,
        path: path?.trim() || `/${slugify(title)}`,
        status: 'draft',
        author: seed.currentUser.name,
        updatedAt: Date.now(),
      }
      state.pages.unshift(page)
      touchWebsite(websiteId)
      log(`You created the page ‘${title}’`, 'page')
      notify(`Page ‘${title}’ created`)
      return page
    },

    setPageStatus(id: string, status: PublishStatus) {
      const page = state.pages.find((p) => p.id === id)
      if (!page) return
      page.status = status
      page.updatedAt = Date.now()
      touchWebsite(page.websiteId)
      const verb = { live: 'published', draft: 'unpublished', scheduled: 'scheduled', archived: 'archived' }[status]
      log(`You ${verb} ‘${page.title}’`, 'page')
      notify(`‘${page.title}’ ${verb}`)
    },

    deletePage(id: string) {
      const i = state.pages.findIndex((p) => p.id === id)
      if (i === -1) return
      const [page] = state.pages.splice(i, 1)
      notify(`‘${page!.title}’ deleted`)
    },

    notify,
  }
}
