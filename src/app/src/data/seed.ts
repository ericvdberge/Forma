import type { Activity, Stat, Website } from './types'

/**
 * Seed content for the CMS until `src/api` exists.
 * Timestamps are relative to load time so "2 minutes ago" stays meaningful.
 */
const now = Date.now()
const min = 60_000
const hour = 60 * min
const day = 24 * hour

export const currentUser = { name: 'Alex Morgan', firstName: 'Alex', email: 'alex@forma.dev' }

export const websites: Website[] = [
  {
    id: 'studio',
    name: 'Forma Studio',
    domain: 'studio.forma.dev',
    description: 'A modern website built with Forma.',
    status: 'live',
    tone: 'light',
    collaborators: ['Alex Morgan', 'Sam Lee', 'Jo Park', 'Rin Ota', 'Max Berg'],
    updatedAt: now - 2 * min,
  },
  {
    id: 'agency',
    name: 'Agency Pro',
    domain: 'agency.forma.dev',
    description: 'Case studies and services for a small design agency.',
    status: 'draft',
    tone: 'dark',
    collaborators: ['Alex Morgan', 'Sam Lee'],
    updatedAt: now - 3 * hour,
  },
  {
    id: 'portfolio',
    name: 'Portfolio',
    domain: 'portfolio.forma.dev',
    description: 'Selected work, 2019 – today.',
    status: 'draft',
    tone: 'light',
    collaborators: ['Alex Morgan'],
    updatedAt: now - 2 * day,
  },
]

export const activity: Activity[] = [
  { id: 'a1', message: 'You published a new page', at: now - 2 * min, kind: 'page' },
  { id: 'a2', message: 'Template ‘Minimal’ was added', at: now - hour, kind: 'template' },
  { id: 'a3', message: 'You updated global styles', at: now - 2 * hour, kind: 'styles' },
]

export const stats: Stat[] = [
  { label: 'Total Views', value: '48.2K', change: 12, trend: [5, 6, 5.5, 7, 6.2, 6.8, 6, 7.5, 7, 8.4, 7.8, 8.2] },
  { label: 'Total Pages', value: '24', change: 2, trend: [6, 5.8, 6.1, 5.6, 6.2, 6, 6.4, 6.1, 6.6, 6.2, 6.8] },
  { label: 'Active Templates', value: '12', change: 1, trend: [4, 4.2, 4.6, 4.3, 4.8, 5.2, 4.9, 5.1, 5.6, 5.2, 5.4] },
  { label: 'Total Visitors', value: '12.4K', change: 18, trend: [3, 3.6, 3.2, 4.4, 4, 4.8, 4.4, 5.6, 5, 6.2, 5.8, 6.6] },
]
