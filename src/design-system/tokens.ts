/** Brand tokens mirrored in TypeScript. The Tailwind source of truth lives in styles/index.css. */
export const palette = [
  { name: 'Charcoal', hex: '#0B0B0F', token: 'charcoal' },
  { name: 'Accent Blue', hex: '#3B82F6', token: 'accent' },
  { name: 'Soft Gray', hex: '#E5E7EB', token: 'soft-gray' },
  { name: 'Warm White', hex: '#FAFAF8', token: 'warm-white' },
] as const

export const supportingPalette = [
  { name: 'Ink', hex: '#171717', token: 'ink' },
  { name: 'Canvas', hex: '#F5F4F0', token: 'canvas' },
  { name: 'Stone', hex: '#D8D2C8', token: 'stone' },
  { name: 'Sage', hex: '#A8B5A0', token: 'sage' },
] as const

export const typography = [
  { role: 'Headlines', family: 'Satoshi', weight: 'Bold' },
  { role: 'Subheadings', family: 'Satoshi', weight: 'Medium' },
  { role: 'Body Text', family: 'Satoshi', weight: 'Regular' },
] as const

export const brand = {
  name: 'forma.',
  tagline: 'Build. Design. Grow.',
  promise: 'Give your ideas form.',
  values: ['Modern.', 'Minimal.', 'Powerful.'],
} as const
