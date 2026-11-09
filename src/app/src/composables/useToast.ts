import { readonly, ref } from 'vue'

export type ToastTone = 'success' | 'error'

const toast = ref<null | { id: number; message: string; tone: ToastTone }>(null)
let timer: ReturnType<typeof setTimeout> | undefined

/** Short confirmation shown at the bottom of the screen. */
export function useToast() {
  function notify(message: string, tone: ToastTone = 'success') {
    toast.value = { id: Date.now(), message, tone }
    clearTimeout(timer)
    timer = setTimeout(() => (toast.value = null), 3200)
  }

  return {
    toast: readonly(toast),
    notify,
  }
}
