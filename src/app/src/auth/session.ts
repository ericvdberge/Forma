/**
 * The signed-in session: a short-lived bearer access token (memory only) and a refresh token
 * (localStorage, so a reload or new tab stays signed in).
 *
 * `authFetch` is the API client's fetch (see api/client-config.ts). It attaches the access token,
 * refreshes it when it has expired or the API answers 401, and retries the request once.
 *
 * This file deliberately doesn't import the generated client, which imports client-config, which
 * imports this file.
 */

export const API_BASE_URL = import.meta.env.VITE_API_URL ?? '/api'

const REFRESH_TOKEN_KEY = 'forma.refreshToken'
/** Refresh a little before the server-side expiry so requests don't race it. */
const EXPIRY_SKEW_MS = 30_000

export interface AccessTokenResponse {
  accessToken: string
  expiresIn: number
  refreshToken: string
}

let accessToken: string | null = null
let accessTokenExpiresAt = 0
let refreshInFlight: Promise<boolean> | null = null
const expiredHandlers = new Set<() => void>()

function readRefreshToken(): string | null {
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  } catch {
    return null
  }
}

function writeRefreshToken(token: string | null) {
  try {
    if (token) localStorage.setItem(REFRESH_TOKEN_KEY, token)
    else localStorage.removeItem(REFRESH_TOKEN_KEY)
  } catch {
    /* storage unavailable: the session lasts until the tab closes */
  }
}

/** Fallback when storage is unavailable, so a session still works for this tab. */
let memoryRefreshToken: string | null = null
const getRefreshToken = () => readRefreshToken() ?? memoryRefreshToken

export function setSession(tokens: AccessTokenResponse) {
  accessToken = tokens.accessToken
  accessTokenExpiresAt = Date.now() + tokens.expiresIn * 1000 - EXPIRY_SKEW_MS
  memoryRefreshToken = tokens.refreshToken
  writeRefreshToken(tokens.refreshToken)
}

export function clearSession() {
  accessToken = null
  accessTokenExpiresAt = 0
  memoryRefreshToken = null
  writeRefreshToken(null)
}

export function hasSession() {
  return getRefreshToken() !== null
}

/** Called when the session can't be refreshed any more, e.g. to send the user to the login page. */
export function onSessionExpired(handler: () => void) {
  expiredHandlers.add(handler)
  return () => expiredHandlers.delete(handler)
}

/** Exchanges the refresh token for new tokens. Concurrent callers share one request. */
function refresh(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    const refreshToken = getRefreshToken()
    if (!refreshToken) return false
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!response.ok) {
        // 401 = expired or revoked. Other failures (API down, 429) keep the token for a later try.
        if (response.status === 401) clearSession()
        return false
      }
      setSession((await response.json()) as AccessTokenResponse)
      return true
    } catch {
      return false
    }
  })().finally(() => (refreshInFlight = null))
  return refreshInFlight
}

/** Makes sure there is a usable access token, refreshing it if needed. */
export async function ensureSession(): Promise<boolean> {
  if (accessToken && Date.now() < accessTokenExpiresAt) return true
  return refresh()
}

function isAnonymousAuthCall(url: string) {
  return /\/auth\/(login|refresh)$/.test(new URL(url, location.origin).pathname)
}

function withToken(request: Request) {
  if (!accessToken) return request
  const headers = new Headers(request.headers)
  headers.set('Authorization', `Bearer ${accessToken}`)
  return new Request(request, { headers })
}

export const authFetch: typeof fetch = async (input, init) => {
  const request = new Request(input, init)
  if (isAnonymousAuthCall(request.url)) return fetch(request)

  if (hasSession() && !(accessToken && Date.now() < accessTokenExpiresAt)) await refresh()

  // Keep an unsent copy so the request (and its body) can be retried after a refresh.
  const retry = request.clone()
  const response = await fetch(withToken(request))
  if (response.status !== 401 || !hasSession()) {
    if (response.status === 401) expiredHandlers.forEach((h) => h())
    return response
  }

  if (await refresh()) return fetch(withToken(retry))

  if (!hasSession()) expiredHandlers.forEach((h) => h())
  return response
}
