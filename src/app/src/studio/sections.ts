import type { Component } from 'vue'
import { Columns2, LayoutGrid, Megaphone, PanelBottom, PanelLeft, PanelTop, RectangleHorizontal, Sparkles } from 'lucide-vue-next'
import { createBlock } from './blocks'
import { clone, defaultBlockStyle, merge, newId, responsivePadding, sectionSettings, type SectionPatch } from './defaults'
import type { Block, BlockType, PageDocument, Section, SectionType } from './types'

export const sectionTypeLabels: Record<SectionType, string> = {
  hero: 'Hero',
  content: 'Content',
  features: 'Features',
  split: 'Split',
  cta: 'Call to action',
  header: 'Header',
  footer: 'Footer',
  sidebar: 'Sidebar',
}

export function createSection(settings: SectionPatch = {}, blocks: Block[] = []): Section {
  return { id: newId('s'), settings: sectionSettings(settings), blocks }
}

export interface SectionTemplate {
  id: string
  label: string
  description: string
  icon: Component
  create: () => Section
}

const IMG = (name: string) => `/media/${name}.svg`

/** Ready-made sections: the library's Templates tab. */
export const sectionTemplates: SectionTemplate[] = [
  {
    id: 'hero',
    label: 'Hero',
    description: 'Headline, intro and two buttons over an image.',
    icon: Sparkles,
    create: () =>
      createSection(
        {
          type: 'hero',
          minHeight: 520,
          verticalAlign: 'center',
          padding: responsivePadding(96, 96),
          background: { kind: 'image', image: IMG('hero'), position: 'left center', overlayColor: '#ffffff', overlayOpacity: 0 },
        },
        [
          createBlock('badge', { text: 'Design with ease' }),
          createBlock('heading', { html: '<p>Build beautiful <span data-muted="">experiences.</span></p>', level: 1, size: 'xl' }, { maxWidth: 'md' }),
          createBlock('text', { html: '<p>A modern, flexible design system for building stunning websites and apps.</p>', size: 'lg' }, { maxWidth: 'sm' }),
          createBlock('button', { label: 'Start creating' }, { marginTop: 8 }),
          createBlock('button', { label: 'Explore templates', variant: 'secondary', arrow: false }, { marginTop: 8 }),
        ],
      ),
  },
  {
    id: 'features',
    label: 'Features',
    description: 'A row of features with icons.',
    icon: LayoutGrid,
    create: () =>
      createSection({ type: 'features', padding: responsivePadding(56, 56) }, [
        createBlock('heading', { html: '<p>Everything you need</p>', size: 'md' }),
        createBlock('features', {
          columns: 4,
          items: [
            { icon: 'wand', title: 'Visual Freedom', text: 'Drag, drop and customize with ease.' },
            { icon: 'layers', title: 'Modern Components', text: 'A rich library of blocks.' },
            { icon: 'zap', title: 'Blazing Fast', text: 'Optimized for performance.' },
            { icon: 'scale', title: 'Built for Scale', text: 'Grow without limits.' },
          ],
        }),
      ]),
  },
  {
    id: 'split',
    label: 'Image & text',
    description: 'An image beside a headline and copy.',
    icon: Columns2,
    create: () =>
      createSection({ type: 'split', layout: 'split', splitAt: 1, splitRatio: 42, gap: 16 }, [
        createBlock('image', { src: IMG('facade'), aspect: '4/3' }),
        createBlock('badge', { text: 'Our platform' }),
        createBlock('heading', { html: '<p>Create without <span data-muted="">limits.</span></p>', size: 'lg' }),
        createBlock('text', { html: '<p>From landing pages to complex web apps, Forma gives you the tools to bring your ideas to life.</p>', size: 'lg' }),
      ]),
  },
  {
    id: 'cta',
    label: 'Call to action',
    description: 'A dark banner with one strong button.',
    icon: Megaphone,
    create: () =>
      createSection(
        {
          type: 'cta',
          align: 'center',
          theme: 'dark',
          contentWidth: 'normal',
          background: { kind: 'gradient', gradientFrom: '#0b0b0f', gradientTo: '#2a2c33', gradientAngle: 135 },
        },
        [
          createBlock('heading', { html: '<p>Ready to build something beautiful?</p>', size: 'lg' }),
          createBlock('text', { html: '<p>Start for free. No credit card required.</p>' }),
          createBlock('button', { label: 'Get started', variant: 'accent' }, { marginTop: 8 }),
        ],
      ),
  },
  {
    id: 'content',
    label: 'Blank section',
    description: 'An empty section to fill with blocks.',
    icon: RectangleHorizontal,
    create: () => createSection(),
  },
]

/** Sections that frame a page. */
export const globalElements: SectionTemplate[] = [
  {
    id: 'header',
    label: 'Header',
    description: 'Logo, links and a button.',
    icon: PanelTop,
    create: () =>
      createSection({ type: 'header', padding: { desktop: { top: 20, bottom: 20, left: 32, right: 32 }, mobile: { left: 16, right: 16 } } }, [
        createBlock('nav'),
      ]),
  },
  {
    id: 'footer',
    label: 'Footer',
    description: 'Links and the small print.',
    icon: PanelBottom,
    create: () =>
      createSection(
        {
          type: 'footer',
          theme: 'dark',
          gap: 24,
          padding: responsivePadding(56, 40),
          background: { kind: 'color', color: '#0b0b0f' },
        },
        [
          createBlock('nav', {
            links: [
              { label: 'Product', href: '#' },
              { label: 'Templates', href: '#' },
              { label: 'Docs', href: '#' },
              { label: 'Contact', href: '#' },
            ],
            ctaLabel: '',
          }),
          createBlock('divider'),
          createBlock('text', { html: '<p>© 2026 Forma. Built with Forma.</p>', size: 'sm' }),
        ],
      ),
  },
  {
    id: 'sidebar',
    label: 'Sidebar',
    description: 'A side menu beside your content.',
    icon: PanelLeft,
    create: () =>
      createSection({ type: 'sidebar', layout: 'split', splitAt: 1, splitRatio: 26, verticalAlign: 'start', gap: 16 }, [
        createBlock('nav', {
          orientation: 'vertical',
          ctaLabel: '',
          links: [
            { label: 'Overview', href: '#' },
            { label: 'Getting started', href: '#' },
            { label: 'Guides', href: '#' },
            { label: 'API reference', href: '#' },
          ],
        }),
        createBlock('heading', { html: '<p>Documentation</p>', size: 'md' }),
        createBlock('text', { html: '<p>Everything you need to know to get the most out of your website.</p>' }),
      ]),
  },
]

/* ---- Loading documents from JSON ---- */

type BlockJson = { id?: string; type: BlockType; props?: Record<string, unknown>; style?: Record<string, unknown> }
type SectionJson = { id?: string; settings?: Record<string, unknown>; blocks?: BlockJson[] }
export type PageDocumentJson = { sections: SectionJson[] }

/**
 * Fills a (possibly partial) document from JSON with defaults, so mock files and future API payloads
 * only need to spell out what differs from the defaults.
 */
export function normalizeDocument(json: PageDocumentJson): PageDocument {
  return {
    version: 1,
    sections: json.sections.map((s) => ({
      id: s.id ?? newId('s'),
      settings: sectionSettings(s.settings),
      blocks: (s.blocks ?? []).map((b) => {
        const block = createBlock(b.type, (b.props ?? {}) as never, merge(defaultBlockStyle(), b.style ?? {}))
        return b.id ? { ...block, id: b.id } : block
      }),
    })),
  }
}

/** A deep copy with fresh ids, e.g. for duplicating or inserting a saved item. */
export function withFreshIds<T extends Section | Block>(item: T): T {
  const copy = clone(item) as T
  if ('blocks' in copy) {
    copy.id = newId('s')
    copy.blocks.forEach((b) => (b.id = newId('b')))
  } else {
    copy.id = newId('b')
  }
  return copy
}
