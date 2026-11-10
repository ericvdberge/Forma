/**
 * The Design Studio's page document: a page is a list of sections, each holding a list of blocks.
 * Plain JSON, so it can live in the query cache, be cloned for undo/redo and later be sent to the API as is.
 */

export type Breakpoint = 'desktop' | 'tablet' | 'mobile'
export const breakpoints: Breakpoint[] = ['desktop', 'tablet', 'mobile']

/** A value set for desktop, optionally overridden for smaller screens. Smaller screens inherit from larger ones. */
export interface Responsive<T> {
  desktop: T
  tablet?: Partial<T>
  mobile?: Partial<T>
}

export interface Spacing {
  top: number
  bottom: number
  left: number
  right: number
}

export interface Background {
  kind: 'none' | 'color' | 'image' | 'gradient'
  color: string
  image: string
  /** Where the image is anchored, e.g. `right center`. */
  position: string
  gradientFrom: string
  gradientTo: string
  gradientAngle: number
  overlayColor: string
  /** 0–100 */
  overlayOpacity: number
}

export type SectionType = 'hero' | 'content' | 'features' | 'split' | 'cta' | 'header' | 'footer' | 'sidebar'
export type SectionLayout = 'stack' | 'split'
export type ContentWidth = 'narrow' | 'normal' | 'wide' | 'full'
export type EntranceAnimation = 'none' | 'fade' | 'fade-up' | 'zoom' | 'slide-left'

export interface Visibility {
  desktop: boolean
  tablet: boolean
  mobile: boolean
}

export interface SectionSettings {
  type: SectionType
  /** Shown in the layers list; falls back to the section type. */
  name: string
  layout: SectionLayout
  /** Split layout: share of the width the first column takes, in %. */
  splitRatio: number
  /** Split layout: index of the first block that goes into the second column. */
  splitAt: number
  verticalAlign: 'start' | 'center' | 'end'
  padding: Responsive<Spacing>
  background: Background
  contentWidth: ContentWidth
  align: 'left' | 'center'
  theme: 'light' | 'dark'
  /** px, 0 = auto */
  minHeight: number
  /** px between blocks */
  gap: number
  /** px; rounds the section's corners (sections sit inside the page frame). */
  radius: number
  animation: { entrance: EntranceAnimation; delay: number }
  anchor: string
  className: string
  visibility: Visibility
}

export interface BlockStyle {
  align: 'inherit' | 'left' | 'center' | 'right'
  marginTop: number
  marginBottom: number
  maxWidth: 'none' | 'sm' | 'md' | 'lg'
  anchor: string
  className: string
  visibility: Visibility
}

export interface LinkItem {
  label: string
  href: string
  /** Shows a dropdown chevron in nav menus. */
  menu?: boolean
}

export interface BlockPropsMap {
  heading: { html: string; level: 1 | 2 | 3 | 4; size: 'xl' | 'lg' | 'md' | 'sm' }
  text: { html: string; size: 'lg' | 'md' | 'sm'; tone: 'default' | 'muted' }
  button: { label: string; href: string; variant: 'primary' | 'secondary' | 'accent'; arrow: boolean; size: 'sm' | 'md' | 'lg' }
  badge: { text: string }
  image: { src: string; alt: string; aspect: 'auto' | '16/9' | '4/3' | '1/1' | '3/4'; radius: number; caption: string }
  gallery: { images: Array<{ src: string; alt: string }>; columns: number; gap: number; radius: number }
  video: { url: string; aspect: '16/9' | '4/3' | '1/1'; radius: number }
  divider: { style: 'solid' | 'dashed'; thickness: number }
  spacer: { height: number }
  form: { fields: Array<{ label: string; type: 'text' | 'email' | 'textarea'; required: boolean }>; submitLabel: string; successMessage: string }
  map: { address: string; zoom: number; height: number; radius: number }
  carousel: { slides: Array<{ src: string; caption: string }>; autoplay: boolean; aspect: '16/9' | '4/3' | '21/9'; radius: number }
  faq: { items: Array<{ question: string; answer: string }> }
  testimonials: { items: Array<{ quote: string; name: string; role: string }>; columns: number }
  features: { items: Array<{ icon: string; title: string; text: string }>; columns: number; card: boolean }
  nav: { logo: string; links: LinkItem[]; ctaLabel: string; ctaHref: string; orientation: 'horizontal' | 'vertical'; card: boolean }
}

export type BlockType = keyof BlockPropsMap

export type Block<T extends BlockType = BlockType> = {
  [K in T]: { id: string; type: K; props: BlockPropsMap[K]; style: BlockStyle }
}[T]

export interface Section {
  id: string
  settings: SectionSettings
  blocks: Block[]
}

export interface PageDocument {
  version: 1
  sections: Section[]
}

/** What the studio keeps per page: the working draft and the last published version. */
export interface StudioPage {
  pageId: string
  draft: PageDocument
  published: PageDocument
  updatedAt: number
  publishedAt: number | null
}

export interface MediaAsset {
  id: string
  name: string
  src: string
  /** Set for files added from the user's computer in this session. */
  uploaded?: boolean
}

/** A section or block the user saved to reuse later (the library's Saved tab). */
export type SavedItem =
  | { id: string; name: string; kind: 'section'; section: Section; savedAt: number }
  | { id: string; name: string; kind: 'block'; block: Block; savedAt: number }

export type Selection = { kind: 'section'; sectionId: string } | { kind: 'block'; sectionId: string; blockId: string } | null
