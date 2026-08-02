import type { PagedQuery } from './api.types'
import type { PermissionListItemDto } from './permission.types'

export interface RoleListItemDto {
  id: string
  corporationId?: string
  code: string
  name: string
  description?: string
  isSystem: boolean
  permissionCount: number
  createdAt: string
}

export interface RoleDto extends Omit<RoleListItemDto, 'permissionCount'> {
  permissions: PermissionListItemDto[]
  updatedAt: string
  rowVersion: number
}

export interface CreateRoleRequest {
  code: string
  name: string
  description?: string
}

export interface UpdateRoleRequest {
  name: string
  description?: string
  rowVersion: number
}

export interface AssignRolePermissionRequest {
  permissionId: string
}

export interface RoleQuery extends PagedQuery {
  includeSystem?: boolean
}
