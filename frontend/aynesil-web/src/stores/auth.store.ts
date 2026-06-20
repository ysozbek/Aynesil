/**
 * Pinia Auth Store
 * Persists access + refresh tokens in localStorage.
 * Access token is decoded client-side to extract user info (no separate /me call needed).
 * Refresh logic is handled by the Axios interceptor in api.service.ts.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { apiService } from '@/services/api.service'
import type { AuthUser, LoginRequest, LoginResult } from '@/types/auth.types'

function parseJwt(token: string): Record<string, unknown> {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    // atob() binary string döndürür — Türkçe/Unicode için UTF-8 decode gerekir
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    )
    return JSON.parse(jsonPayload)
  } catch {
    return {}
  }
}

export const useAuthStore = defineStore('auth', () => {
  // ── State ─────────────────────────────────────────────────────────────────
  const accessToken = ref<string | null>(localStorage.getItem('access_token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refresh_token'))
  const user = ref<AuthUser | null>(null)

  // ── Computed ──────────────────────────────────────────────────────────────
  const isAuthenticated = computed(() => !!accessToken.value)

  const permissions = computed<string[]>(() => {
    if (!accessToken.value) return []
    const payload = parseJwt(accessToken.value)
    const perms = payload['perm']
    if (Array.isArray(perms)) return perms as string[]
    if (typeof perms === 'string') return [perms]
    return []
  })

  // ── Actions ───────────────────────────────────────────────────────────────
  function hydrateUserFromToken(token: string): AuthUser | null {
    const payload = parseJwt(token)
    if (!payload['sub']) return null

    const perms = payload['perm']
    return {
      userId: payload['sub'] as string,
      corporationId: payload['corporation_id'] as string,
      fullName: payload['full_name'] as string ?? '',
      email: payload['email'] as string | undefined,
      locale: payload['locale'] as string ?? 'tr',
      permissions: Array.isArray(perms) ? perms as string[] : typeof perms === 'string' ? [perms] : [],
    }
  }

  async function login(credentials: LoginRequest): Promise<void> {
    const response = await apiService.post<LoginResult>('/auth/login', credentials)
    if (!response.success || !response.data) throw new Error(response.message)

    setTokens(response.data.accessToken, response.data.refreshToken)
  }

  async function refresh(): Promise<LoginResult> {
    if (!refreshToken.value) throw new Error('No refresh token available.')

    const response = await apiService.post<LoginResult>('/auth/refresh', {
      refreshToken: refreshToken.value,
    })
    if (!response.success || !response.data) throw new Error(response.message)

    setTokens(response.data.accessToken, response.data.refreshToken)
    return response.data
  }

  async function logout(): Promise<void> {
    if (refreshToken.value) {
      try {
        await apiService.post('/auth/logout', { refreshToken: refreshToken.value })
      } catch {
        // Always clear local state even if server logout fails
      }
    }
    clearTokens()
  }

  function setTokens(access: string, refresh: string) {
    accessToken.value = access
    refreshToken.value = refresh
    localStorage.setItem('access_token', access)
    localStorage.setItem('refresh_token', refresh)
    user.value = hydrateUserFromToken(access)
  }

  function clearTokens() {
    accessToken.value = null
    refreshToken.value = null
    user.value = null
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
  }

  function hasPermission(code: string): boolean {
    return permissions.value.includes(code)
  }

  // Hydrate user from stored token on store init
  if (accessToken.value) {
    user.value = hydrateUserFromToken(accessToken.value)
  }

  return {
    accessToken,
    refreshToken,
    user,
    isAuthenticated,
    permissions,
    login,
    refresh,
    logout,
    hasPermission,
  }
})
