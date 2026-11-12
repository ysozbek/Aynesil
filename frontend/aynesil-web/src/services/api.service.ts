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
import { useLocaleStore } from '@/stores/locale.store'
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
  // Arayüz dili (locale store) her istekte gönderilir; backend: Accept-Language → JWT locale → tr.
  config.headers['Accept-Language'] = useLocaleStore().current

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

// ── Response Interceptor — normalize non-2xx errors ───────────────────────
// Registered after the refresh interceptor so it also sees errors it re-rejects.
// Turns the backend envelope into `error.message`, so existing
// `(e as Error).message` call sites show the real reason.
/** Error carrying backend field errors (`{ success:false, message, errors }`). */
export interface ApiError extends Error {
  fieldErrors?: Record<string, string[]>
  status?: number
}

const STATUS_FALLBACKS: Record<number, string> = {
  401: 'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.',
  403: 'Bu işlem için yetkiniz yok.',
  404: 'İstenen kayıt bulunamadı.',
  500: 'Beklenmeyen bir sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.',
}

function flattenErrors(errors?: Record<string, string[]>): string[] {
  if (!errors) return []
  return Object.values(errors).flatMap((v) => (Array.isArray(v) ? v : [String(v)]))
}

function resolveErrorMessage(status: number | undefined, data: unknown): { message?: string; errors?: Record<string, string[]> } {
  const body = (data && typeof data === 'object' ? data : {}) as {
    message?: string
    title?: string
    errors?: Record<string, string[]>
  }
  const errors = body.errors && typeof body.errors === 'object' ? body.errors : undefined
  const details = flattenErrors(errors)

  // Field errors present → show only them (the generic "validation failed" text would just repeat).
  if (details.length) return { message: details.join(' '), errors }

  // 1) ApiResponse envelope  2) ValidationProblemDetails title
  const base = body.message || body.title
  if (base) return { message: base, errors }

  // 3) 403 / 401 / 404 / 500 generic texts
  if (status && STATUS_FALLBACKS[status]) return { message: STATUS_FALLBACKS[status] }
  if (status && status >= 500) return { message: STATUS_FALLBACKS[500] }
  return {}
}

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (!axios.isAxiosError(error)) return Promise.reject(error)

    if (!error.response) {
      error.message = error.code === 'ECONNABORTED'
        ? 'İstek zaman aşımına uğradı. Lütfen tekrar deneyin.'
        : 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.'
      return Promise.reject(error)
    }

    const { status, data } = error.response
    const { message, errors } = resolveErrorMessage(status, data)
    if (message) error.message = message
    const apiError = error as unknown as ApiError
    apiError.status = status
    if (errors) apiError.fieldErrors = errors
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
