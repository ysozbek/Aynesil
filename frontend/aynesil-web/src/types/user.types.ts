import type { PagedQuery } from './api.types'

export interface UserListItemDto {
  id: string
  username: string
  email?: string
  fullName: string
  status: string
  preferredLocale?: string
  primaryCampusId?: string
  lastLoginAt?: string
  createdAt: string
}

export interface UserDto extends UserListItemDto {
  corporationId: string
  phone?: string
  mfaEnabled: boolean
  updatedAt: string
  rowVersion: number
}

export interface UserRoleDto {
  id: string
  roleId: string
  roleCode: string
  roleName: string
  campusId?: string
  validFrom?: string
  validTo?: string
  createdAt: string
}

export interface CreateUserRequest {
  username: string
  fullName: string
  email?: string
  phone?: string
  password?: string
  preferredLocale?: string
  primaryCampusId?: string
}

export interface UpdateUserRequest {
  fullName: string
  phone?: string
  email?: string
  preferredLocale?: string
  primaryCampusId?: string
  rowVersion: number
}

export interface AssignUserRoleRequest {
  roleId: string
  campusId?: string
  validFrom?: string
  validTo?: string
}

export interface UserQuery extends PagedQuery {
  status?: string
  roleId?: string
  campusId?: string
}
