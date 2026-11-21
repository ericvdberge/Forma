import { inject, provide, ref, shallowRef, type InjectionKey, type Ref, type ShallowRef } from 'vue'
import type { Adding, DropTarget } from '@/composables/useStudioEditing'
import type { Selection } from './types'

/**
 * What the section and block renderers (`CanvasSection`, `CanvasBlock`) need from around them. The Design Studio
 * provides itself; the website preview provides a read-only canvas, so the same renderers draw the published site.
 */
export interface CanvasContext {
  selection: Ref<Selection>
  dropTarget: Ref<DropTarget | null>
  /** How much the frame is scaled down to fit; floating UI inside it counter-scales. */
  zoom: Ref<number>
  adding: Ref<Adding>
  textPointer: ShallowRef<{ x: number; y: number } | null>
  select: (selection: Selection) => void
  setBlockValue: (blockId: string, path: string, value: unknown) => void
}

const CanvasKey: InjectionKey<CanvasContext> = Symbol('canvas')

export function provideCanvas(canvas: CanvasContext) {
  provide(CanvasKey, canvas)
}

export function useCanvas() {
  const canvas = inject(CanvasKey)
  if (!canvas) throw new Error('useCanvas() must be used inside the Design Studio or the website preview')
  return canvas
}

/** A canvas nothing can be selected, dropped or typed on: for showing pages as visitors see them. */
export function readOnlyCanvas(zoom: Ref<number> = ref(1)): CanvasContext {
  return {
    selection: ref(null),
    dropTarget: ref(null),
    zoom,
    adding: ref(null),
    textPointer: shallowRef(null),
    select: () => {},
    setBlockValue: () => {},
  }
}
