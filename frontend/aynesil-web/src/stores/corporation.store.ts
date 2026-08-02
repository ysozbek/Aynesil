import { defineStore } from 'pinia'
import { ref } from 'vue'
import { corporationService } from '@/services/corporation.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CorporationListItemDto,
  CorporationDto,
  CorporationSettingsDto,
  CorporationQuery,
  CreateCorporationRequest,
  UpdateCorporationRequest,
  UpdateCorporationSettingsRequest,
} from '@/types/corporation.types'

export const useCorporationStore = defineStore('corporation', () => {
  const list = ref<PaginatedResult<CorporationListItemDto>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  })
  const current = ref<CorporationDto | null>(null)
  const settings = ref<CorporationSettingsDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(params: CorporationQuery = {}) {
    loading.value = true
    error.value = null
    try {
      const res = await corporationService.list({ page: 1, pageSize: 20, ...params })
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
      const res = await corporationService.get(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchSettings(id: string) {
    const res = await corporationService.getSettings(id)
    if (res.success && res.data) settings.value = res.data
    return settings.value
  }

  async function create(request: CreateCorporationRequest) {
    saving.value = true
    try {
      const res = await corporationService.create(request)
      if (!res.success) throw new Error(res.message)
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, request: UpdateCorporationRequest) {
    saving.value = true
    try {
      const res = await corporationService.update(id, request)
      if (!res.success) throw new Error(res.message)
      if (res.data && current.value?.id === id) current.value = res.data
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function updateSettings(id: string, request: UpdateCorporationSettingsRequest) {
    saving.value = true
    try {
      const res = await corporationService.updateSettings(id, request)
      if (!res.success) throw new Error(res.message)
      return res.data!
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    await corporationService.remove(id)
  }

  async function activate(id: string) {
    await corporationService.activate(id)
    const item = list.value.items.find((c) => c.id === id)
    if (item) item.status = 'Active'
    if (current.value?.id === id) current.value.status = 'Active'
  }

  async function deactivate(id: string) {
    await corporationService.deactivate(id)
    const item = list.value.items.find((c) => c.id === id)
    if (item) item.status = 'Inactive'
    if (current.value?.id === id) current.value.status = 'Inactive'
  }

  function clear() {
    current.value = null
    settings.value = null
    error.value = null
  }

  return {
    list,
    current,
    settings,
    loading,
    saving,
    error,
    fetchList,
    fetchOne,
    fetchSettings,
    create,
    update,
    updateSettings,
    remove,
    activate,
    deactivate,
    clear,
  }
})
