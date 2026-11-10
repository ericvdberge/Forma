import type { Component } from 'vue'
import {
  AlignLeft,
  ClipboardList,
  GalleryHorizontal,
  Globe,
  Heading,
  Heart,
  Image,
  Images,
  Layers,
  LayoutGrid,
  Map,
  Menu,
  MessageCircleQuestion,
  MessageSquareQuote,
  Minus,
  MousePointerClick,
  Palette,
  Rocket,
  Scaling,
  ShieldCheck,
  SquarePlay,
  Tag,
  UnfoldVertical,
  Users,
  WandSparkles,
  Zap,
} from 'lucide-vue-next'
import { defaultBlockStyle, merge, newId } from './defaults'
import type { Block, BlockPropsMap, BlockStyle, BlockType } from './types'

/** Icons a Features item can show, by the name stored in the document. */
export const featureIcons: Record<string, Component> = {
  wand: WandSparkles,
  layers: Layers,
  zap: Zap,
  scale: Scaling,
  shield: ShieldCheck,
  globe: Globe,
  palette: Palette,
  heart: Heart,
  rocket: Rocket,
  users: Users,
}
const iconOptions = Object.keys(featureIcons).map((value) => ({ value, label: value[0]!.toUpperCase() + value.slice(1) }))

/* Inspector fields: the content form for each block type is generated from these. */
type Option = { value: string | number; label: string }
export type Field =
  | { key: string; label: string; kind: 'text' | 'textarea' | 'url'; placeholder?: string }
  | { key: string; label: string; kind: 'number' | 'slider'; min: number; max: number; step?: number; unit?: string }
  | { key: string; label: string; kind: 'select' | 'segmented'; options: Option[] }
  | { key: string; label: string; kind: 'toggle' }
  | { key: string; label: string; kind: 'image' | 'color' }
  | {
      key: string
      label: string
      kind: 'list'
      /** Field of an item used as its title in the list. */
      titleKey: string
      itemFields: Field[]
      newItem: () => Record<string, unknown>
    }

export interface BlockDefinition<T extends BlockType = BlockType> {
  type: T
  label: string
  icon: Component
  /** Sits next to other inline blocks (e.g. two buttons in a row) instead of taking the full width. */
  inline?: boolean
  /** Its text is edited on the canvas with the rich-text editor. */
  richText?: boolean
  props: () => BlockPropsMap[T]
  fields: Field[]
}

const radius: Field = { key: 'radius', label: 'Corner radius', kind: 'slider', min: 0, max: 40, unit: 'px' }
const columns = (max = 4): Field => ({
  key: 'columns',
  label: 'Columns',
  kind: 'segmented',
  options: Array.from({ length: max }, (_, i) => ({ value: i + 1, label: String(i + 1) })),
})

const IMG = (name: string) => `/media/${name}.svg`

const definitions: { [K in BlockType]: BlockDefinition<K> } = {
  heading: {
    type: 'heading',
    label: 'Heading',
    icon: Heading,
    richText: true,
    props: () => ({ html: '<p>Your heading</p>', level: 2, size: 'lg' }),
    fields: [
      { key: 'level', label: 'Tag', kind: 'segmented', options: [1, 2, 3, 4].map((n) => ({ value: n, label: `H${n}` })) },
      {
        key: 'size',
        label: 'Size',
        kind: 'select',
        options: [
          { value: 'xl', label: 'Display' },
          { value: 'lg', label: 'Large' },
          { value: 'md', label: 'Medium' },
          { value: 'sm', label: 'Small' },
        ],
      },
    ],
  },
  text: {
    type: 'text',
    label: 'Text',
    icon: AlignLeft,
    richText: true,
    props: () => ({ html: '<p>Tell your visitors what this section is about.</p>', size: 'md', tone: 'muted' }),
    fields: [
      { key: 'size', label: 'Size', kind: 'segmented', options: [{ value: 'sm', label: 'S' }, { value: 'md', label: 'M' }, { value: 'lg', label: 'L' }] },
      { key: 'tone', label: 'Colour', kind: 'segmented', options: [{ value: 'default', label: 'Default' }, { value: 'muted', label: 'Muted' }] },
    ],
  },
  button: {
    type: 'button',
    label: 'Button',
    icon: MousePointerClick,
    inline: true,
    props: () => ({ label: 'Get started', href: '#', variant: 'primary', arrow: true, size: 'md' }),
    fields: [
      { key: 'label', label: 'Label', kind: 'text' },
      { key: 'href', label: 'Link', kind: 'url', placeholder: 'https:// or /page' },
      {
        key: 'variant',
        label: 'Style',
        kind: 'segmented',
        options: [{ value: 'primary', label: 'Solid' }, { value: 'secondary', label: 'Outline' }, { value: 'accent', label: 'Accent' }],
      },
      { key: 'size', label: 'Size', kind: 'segmented', options: [{ value: 'sm', label: 'S' }, { value: 'md', label: 'M' }, { value: 'lg', label: 'L' }] },
      { key: 'arrow', label: 'Show arrow', kind: 'toggle' },
    ],
  },
  badge: {
    type: 'badge',
    label: 'Badge',
    icon: Tag,
    inline: true,
    props: () => ({ text: 'New' }),
    fields: [{ key: 'text', label: 'Text', kind: 'text' }],
  },
  image: {
    type: 'image',
    label: 'Image',
    icon: Image,
    props: () => ({ src: IMG('facade'), alt: '', aspect: '4/3', radius: 16, caption: '' }),
    fields: [
      { key: 'src', label: 'Image', kind: 'image' },
      { key: 'alt', label: 'Alt text', kind: 'text', placeholder: 'Describe the image' },
      {
        key: 'aspect',
        label: 'Aspect ratio',
        kind: 'select',
        options: [
          { value: 'auto', label: 'Original' },
          { value: '16/9', label: '16:9' },
          { value: '4/3', label: '4:3' },
          { value: '1/1', label: 'Square' },
          { value: '3/4', label: 'Portrait' },
        ],
      },
      radius,
      { key: 'caption', label: 'Caption', kind: 'text' },
    ],
  },
  gallery: {
    type: 'gallery',
    label: 'Gallery',
    icon: Images,
    props: () => ({
      images: ['facade', 'arch-light', 'arch-dark', 'stairs', 'courtyard', 'tower'].map((n) => ({ src: IMG(n), alt: '' })),
      columns: 3,
      gap: 12,
      radius: 12,
    }),
    fields: [
      {
        key: 'images',
        label: 'Images',
        kind: 'list',
        titleKey: 'alt',
        itemFields: [
          { key: 'src', label: 'Image', kind: 'image' },
          { key: 'alt', label: 'Alt text', kind: 'text' },
        ],
        newItem: () => ({ src: IMG('arch-light'), alt: '' }),
      },
      columns(4),
      { key: 'gap', label: 'Gap', kind: 'slider', min: 0, max: 40, unit: 'px' },
      radius,
    ],
  },
  video: {
    type: 'video',
    label: 'Video',
    icon: SquarePlay,
    props: () => ({ url: '', aspect: '16/9', radius: 16 }),
    fields: [
      { key: 'url', label: 'YouTube or Vimeo link', kind: 'url', placeholder: 'https://youtube.com/watch?v=…' },
      { key: 'aspect', label: 'Aspect ratio', kind: 'segmented', options: ['16/9', '4/3', '1/1'].map((v) => ({ value: v, label: v.replace('/', ':') })) },
      radius,
    ],
  },
  divider: {
    type: 'divider',
    label: 'Divider',
    icon: Minus,
    props: () => ({ style: 'solid', thickness: 1 }),
    fields: [
      { key: 'style', label: 'Line', kind: 'segmented', options: [{ value: 'solid', label: 'Solid' }, { value: 'dashed', label: 'Dashed' }] },
      { key: 'thickness', label: 'Thickness', kind: 'slider', min: 1, max: 6, unit: 'px' },
    ],
  },
  spacer: {
    type: 'spacer',
    label: 'Spacer',
    icon: UnfoldVertical,
    props: () => ({ height: 48 }),
    fields: [{ key: 'height', label: 'Height', kind: 'slider', min: 8, max: 240, step: 4, unit: 'px' }],
  },
  form: {
    type: 'form',
    label: 'Form',
    icon: ClipboardList,
    props: () => ({
      fields: [
        { label: 'Name', type: 'text', required: true },
        { label: 'Email', type: 'email', required: true },
        { label: 'Message', type: 'textarea', required: false },
      ],
      submitLabel: 'Send message',
      successMessage: 'Thanks! We’ll be in touch soon.',
    }),
    fields: [
      {
        key: 'fields',
        label: 'Fields',
        kind: 'list',
        titleKey: 'label',
        itemFields: [
          { key: 'label', label: 'Label', kind: 'text' },
          {
            key: 'type',
            label: 'Type',
            kind: 'segmented',
            options: [{ value: 'text', label: 'Text' }, { value: 'email', label: 'Email' }, { value: 'textarea', label: 'Long' }],
          },
          { key: 'required', label: 'Required', kind: 'toggle' },
        ],
        newItem: () => ({ label: 'New field', type: 'text', required: false }),
      },
      { key: 'submitLabel', label: 'Button label', kind: 'text' },
      { key: 'successMessage', label: 'Success message', kind: 'textarea' },
    ],
  },
  map: {
    type: 'map',
    label: 'Map',
    icon: Map,
    props: () => ({ address: 'Keizersgracht 123, Amsterdam', zoom: 14, height: 320, radius: 16 }),
    fields: [
      { key: 'address', label: 'Address', kind: 'text' },
      { key: 'zoom', label: 'Zoom', kind: 'slider', min: 3, max: 19 },
      { key: 'height', label: 'Height', kind: 'slider', min: 160, max: 640, step: 8, unit: 'px' },
      radius,
    ],
  },
  carousel: {
    type: 'carousel',
    label: 'Carousel',
    icon: GalleryHorizontal,
    props: () => ({
      slides: [
        { src: IMG('arch-light'), caption: 'Sculpted light' },
        { src: IMG('arch-dark'), caption: 'Evening villa' },
        { src: IMG('courtyard'), caption: 'Quiet courtyard' },
      ],
      autoplay: false,
      aspect: '16/9',
      radius: 16,
    }),
    fields: [
      {
        key: 'slides',
        label: 'Slides',
        kind: 'list',
        titleKey: 'caption',
        itemFields: [
          { key: 'src', label: 'Image', kind: 'image' },
          { key: 'caption', label: 'Caption', kind: 'text' },
        ],
        newItem: () => ({ src: IMG('tower'), caption: '' }),
      },
      { key: 'aspect', label: 'Aspect ratio', kind: 'segmented', options: ['21/9', '16/9', '4/3'].map((v) => ({ value: v, label: v.replace('/', ':') })) },
      { key: 'autoplay', label: 'Autoplay in preview', kind: 'toggle' },
      radius,
    ],
  },
  faq: {
    type: 'faq',
    label: 'FAQ',
    icon: MessageCircleQuestion,
    props: () => ({
      items: [
        { question: 'Do I need to know how to code?', answer: 'No. Everything in Forma is visual — drag, drop and customise.' },
        { question: 'Can I use my own domain?', answer: 'Yes, connect any domain you own from the website settings.' },
        { question: 'Is Forma open source?', answer: 'Yes. Self-host it on your own server, or use Forma Cloud.' },
      ],
    }),
    fields: [
      {
        key: 'items',
        label: 'Questions',
        kind: 'list',
        titleKey: 'question',
        itemFields: [
          { key: 'question', label: 'Question', kind: 'text' },
          { key: 'answer', label: 'Answer', kind: 'textarea' },
        ],
        newItem: () => ({ question: 'New question?', answer: 'The answer.' }),
      },
    ],
  },
  testimonials: {
    type: 'testimonials',
    label: 'Testimonials',
    icon: MessageSquareQuote,
    props: () => ({
      items: [
        { quote: 'Forma let our team ship a new site in a week. It feels like designing, not configuring.', name: 'Maya Chen', role: 'Design lead, Northwind' },
        { quote: 'The calmest website builder we have used. Our clients edit content without calling us.', name: 'Tom Okafor', role: 'Founder, Studio Ten' },
      ],
      columns: 2,
    }),
    fields: [
      {
        key: 'items',
        label: 'Testimonials',
        kind: 'list',
        titleKey: 'name',
        itemFields: [
          { key: 'quote', label: 'Quote', kind: 'textarea' },
          { key: 'name', label: 'Name', kind: 'text' },
          { key: 'role', label: 'Role', kind: 'text' },
        ],
        newItem: () => ({ quote: 'What a great product.', name: 'New customer', role: 'Company' }),
      },
      columns(3),
    ],
  },
  features: {
    type: 'features',
    label: 'Features',
    icon: LayoutGrid,
    props: () => ({
      items: [
        { icon: 'wand', title: 'Visual Freedom', text: 'Drag, drop and customize with ease.' },
        { icon: 'layers', title: 'Modern Components', text: 'A rich library of blocks.' },
        { icon: 'zap', title: 'Blazing Fast', text: 'Optimized for performance.' },
      ],
      columns: 3,
      card: false,
    }),
    fields: [
      {
        key: 'items',
        label: 'Features',
        kind: 'list',
        titleKey: 'title',
        itemFields: [
          { key: 'icon', label: 'Icon', kind: 'select', options: iconOptions },
          { key: 'title', label: 'Title', kind: 'text' },
          { key: 'text', label: 'Description', kind: 'textarea' },
        ],
        newItem: () => ({ icon: 'rocket', title: 'New feature', text: 'Describe it in a sentence.' }),
      },
      columns(4),
      { key: 'card', label: 'Show as cards', kind: 'toggle' },
    ],
  },
  nav: {
    type: 'nav',
    label: 'Navigation',
    icon: Menu,
    props: () => ({
      logo: 'forma.',
      links: [
        { label: 'Solutions', href: '#', menu: true },
        { label: 'Resources', href: '#', menu: true },
        { label: 'Pricing', href: '#' },
      ],
      ctaLabel: 'Get started',
      ctaHref: '#',
      orientation: 'horizontal',
      card: false,
    }),
    fields: [
      { key: 'logo', label: 'Logo text', kind: 'text' },
      {
        key: 'links',
        label: 'Links',
        kind: 'list',
        titleKey: 'label',
        itemFields: [
          { key: 'label', label: 'Label', kind: 'text' },
          { key: 'href', label: 'Link', kind: 'url' },
          { key: 'menu', label: 'Has dropdown', kind: 'toggle' },
        ],
        newItem: () => ({ label: 'Link', href: '#' }),
      },
      { key: 'ctaLabel', label: 'Button label', kind: 'text', placeholder: 'Leave empty to hide' },
      { key: 'ctaHref', label: 'Button link', kind: 'url' },
      { key: 'orientation', label: 'Direction', kind: 'segmented', options: [{ value: 'horizontal', label: 'Row' }, { value: 'vertical', label: 'Column' }] },
      { key: 'card', label: 'Floating bar', kind: 'toggle' },
    ],
  },
}

export function blockDefinition<T extends BlockType>(type: T): BlockDefinition<T> {
  return definitions[type] as BlockDefinition<T>
}

/** Block tiles in the library, in display order. */
export const libraryBlocks: BlockType[] = [
  'heading',
  'text',
  'image',
  'gallery',
  'video',
  'button',
  'divider',
  'spacer',
  'badge',
  'form',
  'map',
  'carousel',
  'faq',
  'testimonials',
  'features',
]

/** A new block with defaults, optionally overridden. */
export function createBlock<T extends BlockType>(
  type: T,
  props: Partial<BlockPropsMap[T]> = {},
  style: Partial<BlockStyle> = {},
): Block<T> {
  const def = blockDefinition(type)
  return { id: newId('b'), type, props: merge(def.props(), props), style: merge(defaultBlockStyle(), style) } as Block<T>
}
