import type { Component } from 'vue'
import {
  Database,
  FileText,
  Globe,
  House,
  Image,
  LayoutGrid,
  LayoutTemplate,
  Puzzle,
  Settings,
  User,
  Wrench,
} from 'lucide-vue-next'

export interface NavItem {
  to: string
  label: string
  icon: Component
  /** Lists the active website's collections underneath. */
  collections?: boolean
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

/** The sidebar, in groups: what you work on, how it looks, and how the workspace is set up. */
export const navGroups: NavGroup[] = [
  {
    label: 'Overview',
    items: [
      { to: '/', label: 'Dashboard', icon: House },
      { to: '/websites', label: 'Websites', icon: Globe },
    ],
  },
  {
    label: 'Content',
    items: [
      { to: '/pages', label: 'Pages', icon: FileText },
      { to: '/collections', label: 'Collections', icon: Database, collections: true },
      { to: '/media', label: 'Media', icon: Image },
    ],
  },
  {
    label: 'Design',
    items: [
      { to: '/templates', label: 'Templates', icon: LayoutTemplate },
      { to: '/blocks', label: 'Blocks', icon: LayoutGrid },
      { to: '/styles', label: 'Global Styles', icon: Wrench },
    ],
  },
  {
    label: 'Workspace',
    items: [
      { to: '/settings', label: 'Settings', icon: Settings },
      { to: '/plugins', label: 'Plugins', icon: Puzzle },
      { to: '/users', label: 'Users', icon: User },
    ],
  },
]
