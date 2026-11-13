export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  fullName: string
  email?: string
  locale?: string
  userId: string
  corporationId: string
  /** true: hesap parolasını değiştirmeden başka sayfa/uç kullanamaz (V50). */
  mustChangePassword?: boolean
}

export interface AuthUser {
  userId: string
  corporationId: string
  fullName: string
  email?: string
  locale: string
  permissions: string[]
}
