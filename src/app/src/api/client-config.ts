import type { CreateClientConfig } from './generated/client.gen'

/** Requests go to `/api`, which the Vite dev server proxies to the Forma API (see vite.config.ts). */
export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  baseUrl: import.meta.env.VITE_API_URL ?? '/api',
})
