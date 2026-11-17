import { client } from './generated/client.gen'

/** Turns a route the API returns (e.g. `/media/{id}/image`) into a URL the browser can load. */
export function apiUrl(path: string): string {
  if (/^https?:\/\//.test(path)) return path
  const base = (client.getConfig().baseUrl ?? '').replace(/\/$/, '')
  return `${base}${path.startsWith('/') ? '' : '/'}${path}`
}
