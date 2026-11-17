import type { Breakpoint, Responsive } from './types'

/** The value in effect at a breakpoint: smaller screens inherit from larger ones. */
export function resolve<T extends object>(value: Responsive<T>, bp: Breakpoint): T {
  if (bp === 'desktop') return value.desktop
  const tablet = { ...value.desktop, ...value.tablet }
  return bp === 'tablet' ? tablet : { ...tablet, ...value.mobile }
}

/** Where the value at `bp` for `key` comes from, for "inherited from desktop" hints. */
export function sourceOf<T extends object>(value: Responsive<T>, bp: Breakpoint, key: keyof T): Breakpoint {
  if (bp === 'mobile' && value.mobile?.[key] !== undefined) return 'mobile'
  if (bp !== 'desktop' && value.tablet?.[key] !== undefined) return 'tablet'
  return 'desktop'
}

/** Logical canvas width per device; the frame is zoomed to fit the available space. */
export const frameWidth: Record<Breakpoint, number> = { desktop: 1200, tablet: 768, mobile: 390 }
