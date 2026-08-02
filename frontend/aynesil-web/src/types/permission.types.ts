import type { PagedQuery } from './api.types'

export interface PermissionListItemDto {
  id: string
  code: string
  resource: string
  action: string
  description?: string
}

export type PermissionDto = PermissionListItemDto

export interface PermissionQuery extends PagedQuery {
  resource?: string
}
