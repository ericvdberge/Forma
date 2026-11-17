import type { Background, BlockStyle, Responsive, SectionSettings, Spacing, Visibility } from './types'

/**
 * A deep copy of studio data (plain JSON). Unlike `structuredClone`, it also copies the reactive
 * proxies Vue Query hands out, at any depth.
 */
export const clone = <T>(value: T): T => JSON.parse(JSON.stringify(value)) as T

export const newId = (prefix: string) => `${prefix}_${crypto.randomUUID().slice(0, 8)}`

export const visibleEverywhere = (): Visibility => ({ desktop: true, tablet: true, mobile: true })

export const spacing = (top: number, bottom = top, left = 0, right = left): Spacing => ({ top, bottom, left, right })

/** Desktop padding with proportionally smaller tablet and mobile overrides. */
export function responsivePadding(top: number, bottom = top, x = 48): Responsive<Spacing> {
  const scale = (n: number, f: number) => Math.round((n * f) / 4) * 4
  return {
    desktop: spacing(top, bottom, x),
    tablet: spacing(scale(top, 0.8), scale(bottom, 0.8), Math.min(x, 32)),
    mobile: spacing(scale(top, 0.6), scale(bottom, 0.6), Math.min(x, 20)),
  }
}

export function defaultBackground(): Background {
  return {
    kind: 'none',
    color: '#ffffff',
    image: '',
    position: 'center',
    gradientFrom: '#f5f4f0',
    gradientTo: '#ffffff',
    gradientAngle: 180,
    overlayColor: '#000000',
    overlayOpacity: 0,
  }
}

export function defaultSectionSettings(): SectionSettings {
  return {
    type: 'content',
    name: '',
    layout: 'stack',
    splitRatio: 45,
    splitAt: 1,
    verticalAlign: 'center',
    padding: responsivePadding(80, 80, 48),
    background: defaultBackground(),
    contentWidth: 'wide',
    align: 'left',
    theme: 'light',
    minHeight: 0,
    gap: 20,
    radius: 0,
    animation: { entrance: 'none', delay: 0 },
    anchor: '',
    className: '',
    visibility: visibleEverywhere(),
  }
}

export function defaultBlockStyle(): BlockStyle {
  return {
    align: 'inherit',
    marginTop: 0,
    marginBottom: 0,
    maxWidth: 'none',
    anchor: '',
    className: '',
    visibility: visibleEverywhere(),
  }
}

/** Section settings where nested objects (background, animation…) only need the keys that differ from the defaults. */
export type SectionPatch = {
  [K in keyof SectionSettings]?: SectionSettings[K] extends object ? Partial<SectionSettings[K]> : SectionSettings[K]
}

/** Section settings from defaults plus `patch`. A patch's padding replaces the default one rather than merging into it. */
export function sectionSettings(patch: SectionPatch | Record<string, unknown> = {}): SectionSettings {
  const settings = merge(defaultSectionSettings(), patch)
  const padding = (patch as SectionPatch).padding as SectionSettings['padding'] | undefined
  if (padding) settings.padding = { ...padding, desktop: merge(spacing(0), padding.desktop) }
  return settings
}

const isPlainObject = (v: unknown): v is Record<string, unknown> => typeof v === 'object' && v !== null && !Array.isArray(v)

/** Deep-merges `patch` over `base`. Arrays and primitives in `patch` replace those in `base`. */
export function merge<T>(base: T, patch: unknown): T {
  if (!isPlainObject(base) || !isPlainObject(patch)) return (patch === undefined ? base : patch) as T
  const out: Record<string, unknown> = { ...base }
  for (const [k, v] of Object.entries(patch)) {
    if (v === undefined) continue
    out[k] = isPlainObject(out[k]) && isPlainObject(v) ? merge(out[k], v) : v
  }
  return out as T
}
