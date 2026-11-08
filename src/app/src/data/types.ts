export type PublishStatus = 'live' | 'draft' | 'scheduled' | 'archived'

/** The statuses the pages API knows: `published` maps to `live`. */
export type PageStatus = Extract<PublishStatus, 'live' | 'draft'>

export interface Website {
  id: string
  name: string
  domain: string
  description: string
  status: 'live' | 'draft'
  tone: 'light' | 'dark'
  collaborators: string[]
  updatedAt: number
}

export interface Page {
  id: string
  websiteId: string
  title: string
  slug: string
  /** `/` + slug, for display. */
  path: string
  status: PageStatus
  updatedAt: number
}

export interface Activity {
  id: string
  message: string
  at: number
  kind: 'page' | 'template' | 'styles' | 'website'
}

export interface Stat {
  label: string
  value: string
  change: number
  trend: number[]
}
