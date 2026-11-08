import { computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { login as loginRequest, logout as logoutRequest } from '@/api/generated'
import { getCurrentUserOptions } from '@/api/generated/@tanstack/vue-query.gen'
import { clearSession, setSession, type AccessTokenResponse } from '@/auth/session'

/** Signing in and out, and the signed-in user. */
export function useAuth() {
  const queryClient = useQueryClient()

  /** Throws only when the API can't be reached or fails unexpectedly. */
  async function login(userName: string, password: string): Promise<'ok' | 'invalid' | 'rate-limited'> {
    const { data, response } = await loginRequest({ body: { userName, password } })
    if (response?.status === 401) return 'invalid'
    if (response?.status === 429) return 'rate-limited'
    if (!response?.ok || !data) throw new Error(`Login failed (${response?.status ?? 'network error'})`)

    setSession(data as AccessTokenResponse)
    return 'ok'
  }

  /** Revokes the refresh tokens server-side (best effort), then forgets everything locally. */
  async function logout() {
    try {
      await logoutRequest()
    } finally {
      clearSession()
      queryClient.clear()
    }
  }

  function getCurrentUser() {
    const query = useQuery({ ...getCurrentUserOptions(), staleTime: Infinity })
    const user = computed(() => query.data.value)
    const firstName = computed(() => user.value?.displayName.split(' ')[0] ?? '')
    return { ...query, user, firstName }
  }

  return { login, logout, getCurrentUser }
}
