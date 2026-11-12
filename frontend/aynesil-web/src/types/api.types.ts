// Mirrors Aynesil.Shared.ApiResponse<T> from the backend

/** Alan hatası: makine okunur kod + istek dilinde mesaj. */
export interface ErrorDetail {
  code: string
  message: string
}

export interface ApiResponse<T = unknown> {
  success: boolean
  data?: T
  message?: string
  /** Hata kodu (ör. 'Error.NotFound'); başarısız yanıtlarda dolar. */
  code?: string
  /** alan → mesajlar (geriye uyumlu biçim). */
  errors?: Record<string, string[]>
  /** alan → { code, message } (kodlu biçim). */
  errorDetails?: Record<string, ErrorDetail[]>
  traceId?: string
}

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface PagedQuery {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}
