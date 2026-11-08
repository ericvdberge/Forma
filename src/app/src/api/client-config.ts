import type { CreateClientConfig } from './generated/client.gen'
import { API_BASE_URL, authFetch } from '@/auth/session'

/**
 * Requests go to `/api`, which the Vite dev server proxies to the Forma API (see vite.config.ts).
 * `authFetch` adds the bearer token and refreshes it when it expires.
 */
export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  baseUrl: API_BASE_URL,
  fetch: authFetch,
})
