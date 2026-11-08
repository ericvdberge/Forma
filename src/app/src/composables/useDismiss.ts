import { onBeforeUnmount, onMounted, type Ref } from 'vue'

/** Calls `close` on outside click or Escape. */
export function useDismiss(root: Ref<HTMLElement | null>, close: () => void) {
  const onClick = (e: MouseEvent) => {
    if (root.value && !root.value.contains(e.target as Node)) close()
  }
  const onKey = (e: KeyboardEvent) => {
    if (e.key === 'Escape') close()
  }
  onMounted(() => {
    document.addEventListener('mousedown', onClick)
    document.addEventListener('keydown', onKey)
  })
  onBeforeUnmount(() => {
    document.removeEventListener('mousedown', onClick)
    document.removeEventListener('keydown', onKey)
  })
}
