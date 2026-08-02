/**
 * Branch store — wraps Campus API (Branch = Campus in backend).
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { campusService } from '@/services/campus.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CampusListItemDto,
  CampusDto,
  CampusQuery,
  CreateCampusRequest,
  UpdateCampusRequest,
} from '@/types/campus.types'

export const useBranchStore = defineStore('branch', () => {
  const list = ref<PaginatedResult<CampusListItemDto>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  })
  const current = ref<CampusDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(params: CampusQuery = {}) {
    loading.value = true
    error.value = null
    try {
      const res = await campusService.list({ page: 1, pageSize: 20, ...params })
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
      const res = await campusService.get(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function create(request: CreateCampusRequest) {
    saving.value = true
    try {
      const res = await campusService.create(request)
      if (!res.success) throw new Error(res.message)
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, request: UpdateCampusRequest) {
    saving.value = true
    try {
      const res = await campusService.update(id, request)
      if (!res.success) throw new Error(res.message)
      if (res.data && current.value?.id === id) current.value = res.data
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    await campusService.remove(id)
  }

  async function activate(id: string) {
    await campusService.activate(id)
    const item = list.value.items.find((c) => c.id === id)
    if (item) item.isActive = true
    if (current.value?.id === id) current.value.isActive = true
  }

  async function deactivate(id: string) {
    await campusService.deactivate(id)
    const item = list.value.items.find((c) => c.id === id)
    if (item) item.isActive = false
    if (current.value?.id === id) current.value.isActive = false
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
    activate,
    deactivate,
    clear,
  }
})
