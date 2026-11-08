import { readonly, ref } from 'vue'

const toast = ref<null | { id: number; message: string }>(null)
let timer: ReturnType<typeof setTimeout> | undefined

/** Short confirmation shown at the bottom of the screen. */
export function useToast() {
  function notify(message: string) {
    toast.value = { id: Date.now(), message }
    clearTimeout(timer)
    timer = setTimeout(() => (toast.value = null), 3200)
  }

  return {
    toast: readonly(toast),
    notify,
  }
}
