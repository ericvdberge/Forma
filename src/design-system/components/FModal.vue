<script lang="ts">
/** Each modal that opens stacks above the ones already open, whatever order they were mounted in. */
let stack = 50
</script>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { cn } from '../utils/cn'

/**
 * Dialog on a blurred backdrop. Traps focus, closes on Escape or a backdrop click, locks page
 * scroll and gives focus back to whatever opened it. Put an element with `data-autofocus` inside
 * to choose where focus starts.
 */
const open = defineModel<boolean>('open', { required: true })
const props = withDefaults(
  defineProps<{
    /** Accessible name, when there is no visible heading to point `labelledby` at */
    label?: string
    labelledby?: string
    size?: 'sm' | 'md' | 'lg' | 'xl'
    /** Fills small screens edge to edge */
    fullscreenOnMobile?: boolean
    panelClass?: string
  }>(),
  { size: 'md', fullscreenOnMobile: false },
)
const emit = defineEmits<{ close: [] }>()

const sizes = { sm: 'max-w-md', md: 'max-w-2xl', lg: 'max-w-4xl', xl: 'max-w-[1300px]' }
const panel = ref<HTMLElement | null>(null)
const zIndex = ref(stack)
let opener: HTMLElement | null = null
let overflow = ''

function close() {
  open.value = false
  emit('close')
}

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select, textarea, [tabindex]:not([tabindex="-1"])'
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    e.stopPropagation()
    close()
    return
  }
  if (e.key !== 'Tab' || !panel.value) return
  const items = Array.from(panel.value.querySelectorAll<HTMLElement>(FOCUSABLE)).filter((el) => el.offsetParent !== null)
  const first = items[0]
  const last = items[items.length - 1]
  if (!first || !last) return e.preventDefault()
  if (e.shiftKey && document.activeElement === first) {
    e.preventDefault()
    last.focus()
  } else if (!e.shiftKey && document.activeElement === last) {
    e.preventDefault()
    first.focus()
  }
}

watch(
  open,
  (isOpen) => {
    if (isOpen) {
      zIndex.value = ++stack
      opener = document.activeElement as HTMLElement | null
      overflow = document.body.style.overflow
      document.body.style.overflow = 'hidden'
      nextTick(() => (panel.value?.querySelector<HTMLElement>('[data-autofocus]') ?? panel.value)?.focus())
    } else {
      document.body.style.overflow = overflow
      opener?.focus?.()
      opener = null
    }
  },
  { immediate: true },
)
onBeforeUnmount(() => {
  if (open.value) {
    document.body.style.overflow = overflow
    opener?.focus?.()
  }
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-from-class="opacity-0"
      leave-to-class="opacity-0"
      enter-active-class="transition duration-200 ease-out"
      leave-active-class="transition duration-150 ease-in"
    >
      <div
        v-if="open"
        :style="{ zIndex }"
        :class="cn('fixed inset-0 flex items-center justify-center bg-charcoal/45 backdrop-blur-md', fullscreenOnMobile ? 'sm:p-6' : 'p-4 sm:p-6')"
        @mousedown.self="close"
      >
        <div
          ref="panel"
          role="dialog"
          aria-modal="true"
          :aria-label="props.labelledby ? undefined : props.label"
          :aria-labelledby="props.labelledby"
          tabindex="-1"
          :class="
            cn(
              'relative flex max-h-full w-full flex-col overflow-hidden bg-white shadow-float outline-none ring-1 ring-white/60 animate-pop-in',
              fullscreenOnMobile ? 'h-full sm:h-auto sm:rounded-panel' : 'rounded-panel',
              sizes[size],
              panelClass,
            )
          "
          @keydown="onKeydown"
        >
          <slot :close="close" />
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
