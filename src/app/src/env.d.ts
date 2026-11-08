/// <reference types="vite/client" />
interface ImportMetaEnv {
  /** Base URL of the Forma API. Defaults to `/api` (proxied by the dev server). */
  readonly VITE_API_URL?: string
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<object, object, unknown>
  export default component
}
