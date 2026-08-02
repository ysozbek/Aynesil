import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type { PermissionDto, PermissionListItemDto, PermissionQuery } from '@/types/permission.types'

const BASE = '/permissions'

export const permissionService = {
  list(params: PermissionQuery) {
    return apiService.get<PaginatedResult<PermissionListItemDto>>(BASE, { params })
  },

  get(id: string) {
    return apiService.get<PermissionDto>(`${BASE}/${id}`)
  },

  listAll() {
    return apiService.get<PaginatedResult<PermissionListItemDto>>(BASE, {
      params: { page: 1, pageSize: 500 },
    })
  },
}
