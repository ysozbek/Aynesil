/** Login account linked to an educator or guardian record (backend LinkedUserAccountDto). */
export interface LinkedUserAccount {
  id: string
  username: string
  fullName: string
  email?: string | null
  status: string
  lastLoginAt?: string | null
}

/** Response of "create a user and link it": the temporary password is shown to the admin once. */
export interface CreatedLinkedUserAccount {
  user: LinkedUserAccount
  temporaryPassword: string
}

export interface NewLinkedUserAccountPayload {
  username: string
  fullName: string
  email?: string | null
  phone?: string | null
  roleId: string
  campusId?: string | null
}

/** Records that can be linked to a user account (`/api/{resource}/{id}/user-account`). */
export type UserLinkResource = 'educators' | 'guardians'

export interface LinkedEducator {
  id: string
  fullName: string
  isActive: boolean
}

export interface LinkedGuardian {
  id: string
  fullName: string
}
