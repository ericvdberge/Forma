<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, useId, type Component } from 'vue'
import { Ellipsis } from 'lucide-vue-next'
import FIconButton from './FIconButton.vue'
import { cn } from '../utils/cn'

export interface FMenuItem {
  label: string
  icon?: Component
  /** Destructive actions are set apart and coloured */
  danger?: boolean
  disabled?: boolean
  /** Renders a link; `download` names the saved file */
  href?: string
  download?: string
  target?: string
  onSelect?: () => void
}

/**
 * "More" menu. The popover is fixed to the viewport, so scroll containers and cards that clip
 * their overflow don't cut it off.
 */
withDefaults(
  defineProps<{
    items: FMenuItem[]
    /** Accessible name of the trigger */
    label: string
    icon?: Component
    variant?: 'ghost' | 'surface' | 'glass' | 'outline'
    size?: 'sm' | 'md' | 'lg'
    shape?: 'rounded' | 'circle'
  }>(),
  { icon: () => Ellipsis, variant: 'ghost', size: 'sm', shape: 'rounded' },
)

const open = ref(false)
const pos = ref({ top: 0, left: 0, up: false })
const trigger = ref<HTMLElement | null>(null)
const menu = ref<HTMLElement | null>(null)
const menuId = useId()
const WIDTH = 208

function place() {
  const el = trigger.value?.firstElementChild as HTMLElement | null
  if (!el) return
  const r = el.getBoundingClientRect()
  const up = r.bottom + 220 > window.innerHeight && r.top > 220
  pos.value = {
    top: up ? r.top - 6 : r.bottom + 6,
    left: Math.max(8, Math.min(r.right - WIDTH, window.innerWidth - WIDTH - 8)),
    up,
  }
}
function focusables() {
  return Array.from(menu.value?.querySelectorAll<HTMLElement>('[role=menuitem]:not([aria-disabled=true])') ?? [])
}
function show() {
  place()
  open.value = true
  document.addEventListener('mousedown', onOutside, true)
  document.addEventListener('scroll', hide, true)
  window.addEventListener('resize', hide)
  nextTick(() => focusables()[0]?.focus())
}
function hide() {
  if (!open.value) return
  open.value = false
  document.removeEventListener('mousedown', onOutside, true)
  document.removeEventListener('scroll', hide, true)
  window.removeEventListener('resize', hide)
}
function toggle() {
  if (open.value) hide()
  else show()
}
function onOutside(e: MouseEvent) {
  const t = e.target as Node
  if (!menu.value?.contains(t) && !trigger.value?.contains(t)) hide()
}
function close(refocus = true) {
  hide()
  if (refocus) (trigger.value?.firstElementChild as HTMLElement | null)?.focus()
}
function select(item: FMenuItem) {
  if (item.disabled) return
  close(false)
  item.onSelect?.()
}
function onKeydown(e: KeyboardEvent) {
  const list = focusables()
  const i = list.indexOf(document.activeElement as HTMLElement)
  if (e.key === 'Escape') {
    e.stopPropagation()
    close()
  } else if (e.key === 'ArrowDown') {
    e.preventDefault()
    list[(i + 1) % list.length]?.focus()
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    list[(i - 1 + list.length) % list.length]?.focus()
  } else if (e.key === 'Tab') {
    close(false)
  }
}
onBeforeUnmount(hide)

const itemClass = (item: FMenuItem) =>
  cn(
    'flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-left text-sm outline-none transition-colors hover:bg-canvas focus-visible:bg-canvas',
    item.danger ? 'text-danger' : 'text-charcoal',
    item.disabled && 'pointer-events-none opacity-40',
  )
</script>

<template>
  <span ref="trigger" class="inline-flex" @click.stop>
    <FIconButton
      :icon="icon"
      :label="label"
      :variant="variant"
      :size="size"
      :shape="shape"
      aria-haspopup="menu"
      :aria-expanded="open"
      :aria-controls="open ? menuId : undefined"
      @click="toggle"
    />
  </span>
  <Teleport to="body">
    <Transition enter-from-class="opacity-0 scale-95" enter-active-class="transition duration-100 ease-out">
      <div
        v-if="open"
        :id="menuId"
        ref="menu"
        role="menu"
        :aria-label="label"
        :style="{ top: `${pos.top}px`, left: `${pos.left}px`, width: `${WIDTH}px` }"
        :class="cn('fixed z-[1000] rounded-2xl bg-white p-1.5 ring-1 ring-charcoal/[0.06] shadow-float', pos.up ? '-translate-y-full origin-bottom-right' : 'origin-top-right')"
        @keydown="onKeydown"
        @click.stop
      >
        <template v-for="(item, i) in items" :key="item.label">
          <div v-if="item.danger && i > 0 && !items[i - 1]?.danger" class="my-1 h-px bg-charcoal/[0.06]" role="separator" />
          <a
            v-if="item.href"
            role="menuitem"
            tabindex="-1"
            :href="item.href"
            :download="item.download"
            :target="item.target"
            :rel="item.target === '_blank' ? 'noopener' : undefined"
            :aria-disabled="item.disabled || undefined"
            :class="itemClass(item)"
            @click="select(item)"
          >
            <component :is="item.icon" v-if="item.icon" class="size-4" :stroke-width="1.7" aria-hidden="true" />
            {{ item.label }}
          </a>
          <button
            v-else
            type="button"
            role="menuitem"
            tabindex="-1"
            :aria-disabled="item.disabled || undefined"
            :class="itemClass(item)"
            @click="select(item)"
          >
            <component :is="item.icon" v-if="item.icon" class="size-4" :stroke-width="1.7" aria-hidden="true" />
            {{ item.label }}
          </button>
        </template>
      </div>
    </Transition>
  </Teleport>
</template>
