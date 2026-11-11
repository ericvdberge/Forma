import {
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

export const nav = [
  { to: '/', label: 'Dashboard', icon: House },
  { to: '/websites', label: 'Websites', icon: Globe },
  { to: '/pages', label: 'Pages', icon: FileText },
  { to: '/templates', label: 'Templates', icon: LayoutTemplate },
  { to: '/blocks', label: 'Blocks', icon: LayoutGrid },
  { to: '/media', label: 'Media', icon: Image },
  { to: '/styles', label: 'Global Styles', icon: Wrench },
  { to: '/settings', label: 'Settings', icon: Settings },
  { to: '/plugins', label: 'Plugins', icon: Puzzle },
  { to: '/users', label: 'Users', icon: User },
]
