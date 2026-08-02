import { defineStore } from 'pinia'
import { ref } from 'vue'
import { userService } from '@/services/user.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  UserListItemDto,
  UserDto,
  UserRoleDto,
  UserQuery,
  CreateUserRequest,
  UpdateUserRequest,
  AssignUserRoleRequest,
} from '@/types/user.types'

export const useUserStore = defineStore('user', () => {
  const list = ref<PaginatedResult<UserListItemDto>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  })
  const current = ref<UserDto | null>(null)
  const currentRoles = ref<UserRoleDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(params: UserQuery = {}) {
    loading.value = true
    error.value = null
    try {
      const res = await userService.list({ page: 1, pageSize: 20, ...params })
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
      const res = await userService.get(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchRoles(id: string) {
    const res = await userService.getRoles(id)
    if (res.success && res.data) currentRoles.value = res.data
    return currentRoles.value
  }

  async function create(request: CreateUserRequest) {
    saving.value = true
    try {
      const res = await userService.create(request)
      if (!res.success) throw new Error(res.message)
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, request: UpdateUserRequest) {
    saving.value = true
    try {
      const res = await userService.update(id, request)
      if (!res.success) throw new Error(res.message)
      if (res.data && current.value?.id === id) current.value = res.data
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string, rowVersion: number) {
    await userService.remove(id, rowVersion)
  }

  async function activate(id: string) {
    await userService.activate(id)
    const item = list.value.items.find((u) => u.id === id)
    if (item) item.status = 'Active'
    if (current.value?.id === id) current.value.status = 'Active'
  }

  async function suspend(id: string) {
    await userService.suspend(id)
    const item = list.value.items.find((u) => u.id === id)
    if (item) item.status = 'Suspended'
    if (current.value?.id === id) current.value.status = 'Suspended'
  }

  async function assignRole(id: string, request: AssignUserRoleRequest) {
    const res = await userService.assignRole(id, request)
    if (res.success && res.data) currentRoles.value.push(res.data)
    return res.data!
  }

  async function removeRole(id: string, userRoleId: string) {
    await userService.removeRole(id, userRoleId)
    currentRoles.value = currentRoles.value.filter((r) => r.id !== userRoleId)
  }

  function clear() {
    current.value = null
    currentRoles.value = []
    error.value = null
  }

  return {
    list,
    current,
    currentRoles,
    loading,
    saving,
    error,
    fetchList,
    fetchOne,
    fetchRoles,
    create,
    update,
    remove,
    activate,
    suspend,
    assignRole,
    removeRole,
    clear,
  }
})
