import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  UserDto,
  UserListItemDto,
  UserRoleDto,
  UserQuery,
  CreateUserRequest,
  UpdateUserRequest,
  AssignUserRoleRequest,
} from '@/types/user.types'

const BASE = '/users'

export const userService = {
  list(params: UserQuery) {
    return apiService.get<PaginatedResult<UserListItemDto>>(BASE, { params })
  },

  get(id: string) {
    return apiService.get<UserDto>(`${BASE}/${id}`)
  },

  getRoles(id: string) {
    return apiService.get<UserRoleDto[]>(`${BASE}/${id}/roles`)
  },

  create(request: CreateUserRequest) {
    return apiService.post<UserDto>(BASE, request)
  },

  update(id: string, request: UpdateUserRequest) {
    return apiService.put<UserDto>(`${BASE}/${id}`, request)
  },

  remove(id: string, rowVersion: number) {
    return apiService.delete(`${BASE}/${id}`, { params: { rowVersion } })
  },

  activate(id: string) {
    return apiService.post(`${BASE}/${id}/activate`)
  },

  suspend(id: string) {
    return apiService.post(`${BASE}/${id}/suspend`)
  },

  assignRole(id: string, request: AssignUserRoleRequest) {
    return apiService.post<UserRoleDto>(`${BASE}/${id}/roles`, request)
  },

  removeRole(id: string, userRoleId: string) {
    return apiService.delete(`${BASE}/${id}/roles/${userRoleId}`)
  },
}
