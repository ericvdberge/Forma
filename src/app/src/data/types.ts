export type PublishStatus = 'live' | 'draft' | 'scheduled' | 'archived'

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
  path: string
  status: PublishStatus
  author: string
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
