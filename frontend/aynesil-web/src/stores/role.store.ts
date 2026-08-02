import { defineStore } from 'pinia'
import { ref } from 'vue'
import { roleService } from '@/services/role.service'
import type { PaginatedResult } from '@/types/api.types'
import type { PermissionListItemDto } from '@/types/permission.types'
import type {
  RoleListItemDto,
  RoleDto,
  RoleQuery,
  CreateRoleRequest,
  UpdateRoleRequest,
  AssignRolePermissionRequest,
} from '@/types/role.types'

export const useRoleStore = defineStore('role', () => {
  const list = ref<PaginatedResult<RoleListItemDto>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  })
  const current = ref<RoleDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(params: RoleQuery = {}) {
    loading.value = true
    error.value = null
    try {
      const res = await roleService.list({ page: 1, pageSize: 50, ...params })
      if (res.success && res.data) list.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await roleService.get(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function create(request: CreateRoleRequest) {
    saving.value = true
    try {
      const res = await roleService.create(request)
      if (!res.success) throw new Error(res.message)
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, request: UpdateRoleRequest) {
    saving.value = true
    try {
      const res = await roleService.update(id, request)
      if (!res.success) throw new Error(res.message)
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    await roleService.remove(id)
  }

  async function assignPermission(id: string, request: AssignRolePermissionRequest): Promise<PermissionListItemDto> {
    const res = await roleService.assignPermission(id, request)
    if (!res.success) throw new Error(res.message)
    if (res.data && current.value?.id === id) {
      current.value.permissions.push(res.data)
    }
    return res.data!
  }

  async function removePermission(id: string, permissionId: string) {
    await roleService.removePermission(id, permissionId)
    if (current.value?.id === id) {
      current.value.permissions = current.value.permissions.filter((p) => p.id !== permissionId)
    }
  }

  function clear() {
    current.value = null
    error.value = null
  }

  return {
    list,
    current,
    loading,
    saving,
    error,
    fetchList,
    fetchOne,
    create,
    update,
    remove,
    assignPermission,
    removePermission,
    clear,
  }
})
