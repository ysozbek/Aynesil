/**
 * Axios instance with:
 *  - JWT Bearer token injection
 *  - Automatic refresh token rotation (401 → refresh → retry)
 *  - Hard redirect to /login when session cannot be recovered
 *  - Tenant locale header
 *  - Centralized error handling
 *  - Request/response interceptors
 */
import axios, { type AxiosInstance, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/stores/auth.store'
import type { ApiResponse } from '@/types/api.types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

const api: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
  timeout: 30_000,
})

let isRefreshing = false
let failedQueue: Array<{ resolve: (value: string) => void; reject: (reason?: unknown) => void }> = []

function processQueue(error: unknown, token: string | null = null) {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error)
    else resolve(token!)
  })
  failedQueue = []
}

function isAuthEndpoint(url?: string) {
  if (!url) return false
  return url.includes('/auth/login')
    || url.includes('/auth/refresh')
    || url.includes('/auth/logout')
    || url.includes('/auth/register')
}

function forceLoginRedirect() {
  const auth = useAuthStore()
  auth.clearTokens()
  // Hard navigate — avoids circular import with router and guarantees leaving a dead session shell.
  const redirect = encodeURIComponent(window.location.pathname + window.location.search)
  if (!window.location.pathname.startsWith('/login')) {
    window.location.assign(`/login?redirect=${redirect}`)
  }
}

// ── Request Interceptor — attach Bearer token ──────────────────────────────
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const auth = useAuthStore()
  // Refresh/login must not send a stale access token (stale-tenant middleware would 401 them).
  if (auth.accessToken && !isAuthEndpoint(config.url)) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`
  }
  if (auth.user?.locale) {
    config.headers['Accept-Language'] = auth.user.locale
  }
  return config
})

// ── Response Interceptor — handle 401 with token refresh ──────────────────
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }
    const auth = useAuthStore()

    if (error.response?.status !== 401) {
      return Promise.reject(error)
    }

    // Auth endpoints themselves failed — clear session and go to login.
    if (isAuthEndpoint(originalRequest?.url)) {
      forceLoginRedirect()
      return Promise.reject(error)
    }

    if (!originalRequest._retry && auth.refreshToken) {
      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then((token) => {
          originalRequest.headers = { ...originalRequest.headers, Authorization: `Bearer ${token}` }
          return api(originalRequest)
        }).catch((queueError) => {
          forceLoginRedirect()
          return Promise.reject(queueError)
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const newTokens = await auth.refresh()
        processQueue(null, newTokens.accessToken)
        originalRequest.headers = { ...originalRequest.headers, Authorization: `Bearer ${newTokens.accessToken}` }
        return api(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError)
        forceLoginRedirect()
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    // No refresh token (or already retried) — force login.
    forceLoginRedirect()
    return Promise.reject(error)
  }
)

// ── Typed convenience wrappers ─────────────────────────────────────────────
export const apiService = {
  get: <T>(url: string, config?: AxiosRequestConfig) =>
    api.get<ApiResponse<T>>(url, config).then((r) => r.data),

  post: <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
    api.post<ApiResponse<T>>(url, data, config).then((r) => r.data),

  put: <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
    api.put<ApiResponse<T>>(url, data, config).then((r) => r.data),

  patch: <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
    api.patch<ApiResponse<T>>(url, data, config).then((r) => r.data),

  delete: <T>(url: string, config?: AxiosRequestConfig) =>
    api.delete<ApiResponse<T>>(url, config).then((r) => r.data),

  upload: <T>(url: string, formData: FormData, config?: AxiosRequestConfig) =>
    api.post<ApiResponse<T>>(url, formData, {
      ...config,
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((r) => r.data),
}

export default api
