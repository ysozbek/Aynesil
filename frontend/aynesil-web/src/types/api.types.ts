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

/** Pagination only. Base for lists whose backend query supports neither `search` nor `sortBy`. */
export interface PageQuery {
  page?: number
  pageSize?: number
}

/** Pagination + sorting, no free-text search (backend ignores `search`). */
export interface SortedPageQuery extends PageQuery {
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

/** Pagination + free-text search, no sorting (backend ignores `sortBy`). */
export interface SearchPageQuery extends PageQuery {
  search?: string
}

/** Pagination + search + sorting. Use only for lists whose handler honours both `search` and `sortBy`. */
export interface PagedQuery extends SortedPageQuery {
  search?: string
}
