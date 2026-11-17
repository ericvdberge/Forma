import type { Breakpoint } from './types'

/** What a block renderer needs to know about where it is drawn. */
export interface RenderContext {
  bp: Breakpoint
  theme: 'light' | 'dark'
  /** In the editor (not preview): links don't navigate, embeds don't capture clicks. */
  editing: boolean
  /** Selected in the editor; rich text becomes editable. */
  selected: boolean
}

const scale: Record<Breakpoint, number> = { desktop: 1, tablet: 0.82, mobile: 0.62 }

/** Font size in px for the device, never below `min`. */
export const fluid = (px: number, bp: Breakpoint, min = 14) => `${Math.max(min, Math.round(px * scale[bp]))}px`

/** Responsive column count: tablets get at most 2, phones 1 (or 2 for small tiles). */
export function columnsFor(columns: number, bp: Breakpoint, mobileMax = 1) {
  if (bp === 'mobile') return Math.min(columns, mobileMax)
  if (bp === 'tablet') return Math.min(columns, 2)
  return columns
}

export const tone = (theme: 'light' | 'dark') =>
  theme === 'dark'
    ? { text: '#fafaf8', muted: 'rgb(250 250 248 / 0.64)', line: 'rgb(250 250 248 / 0.16)', surface: 'rgb(255 255 255 / 0.06)' }
    : { text: '#0b0b0f', muted: '#6b6a66', line: 'rgb(11 11 15 / 0.08)', surface: '#ffffff' }

/** Turns a YouTube or Vimeo link into an embeddable URL. */
export function embedUrl(url: string) {
  try {
    const u = new URL(url)
    if (u.hostname.includes('youtu.be')) return `https://www.youtube-nocookie.com/embed/${u.pathname.slice(1)}`
    if (u.hostname.includes('youtube.com')) {
      const id = u.searchParams.get('v') ?? u.pathname.split('/').pop()
      return id ? `https://www.youtube-nocookie.com/embed/${id}` : null
    }
    if (u.hostname.includes('vimeo.com')) {
      const id = u.pathname.split('/').filter(Boolean).pop()
      return id ? `https://player.vimeo.com/video/${id}` : null
    }
  } catch {
    /* not a URL yet */
  }
  return null
}
