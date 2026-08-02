import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type { PermissionListItemDto } from '@/types/permission.types'
import type {
  RoleDto,
  RoleListItemDto,
  RoleQuery,
  CreateRoleRequest,
  UpdateRoleRequest,
  AssignRolePermissionRequest,
} from '@/types/role.types'

const BASE = '/roles'

export const roleService = {
  list(params: RoleQuery) {
    return apiService.get<PaginatedResult<RoleListItemDto>>(BASE, { params })
  },

  get(id: string) {
    return apiService.get<RoleDto>(`${BASE}/${id}`)
  },

  getPermissions(id: string) {
    return apiService.get<PermissionListItemDto[]>(`${BASE}/${id}/permissions`)
  },

  create(request: CreateRoleRequest) {
    return apiService.post<RoleListItemDto>(BASE, request)
  },

  update(id: string, request: UpdateRoleRequest) {
    return apiService.put<RoleListItemDto>(`${BASE}/${id}`, request)
  },

  remove(id: string) {
    return apiService.delete(`${BASE}/${id}`)
  },

  assignPermission(id: string, request: AssignRolePermissionRequest) {
    return apiService.post<PermissionListItemDto>(`${BASE}/${id}/permissions`, request)
  },

  removePermission(id: string, permissionId: string) {
    return apiService.delete(`${BASE}/${id}/permissions/${permissionId}`)
  },
}
