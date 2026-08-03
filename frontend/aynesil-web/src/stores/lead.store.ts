/**
 * Lead store — list, detail, CRUD, status/pipeline changes, conversion.
 */
import { defineStore } from 'pinia'
import { ref, reactive } from 'vue'
import { leadService } from '@/services/lead.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  LeadDto,
  LeadListItemDto,
  CreateLeadPayload,
  UpdateLeadPayload,
  ChangeLeadStatusPayload,
  AssignLeadPayload,
  ConvertLeadPayload,
  LeadListQuery,
} from '@/types/crm.types'

const emptyPage: PaginatedResult<LeadListItemDto> = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
}

export const useLeadStore = defineStore('lead', () => {
  const list = ref<PaginatedResult<LeadListItemDto>>({ ...emptyPage })
  const current = ref<LeadDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(query: LeadListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await leadService.list(query)
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
      const res = await leadService.getById(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateLeadPayload): Promise<LeadDto> {
    saving.value = true
    try {
      const res = await leadService.create(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kayıt oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateLeadPayload): Promise<LeadDto> {
    saving.value = true
    try {
      const res = await leadService.update(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Güncelleme başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    saving.value = true
    try {
      await leadService.delete(id)
      if (current.value?.id === id) current.value = null
    } finally {
      saving.value = false
    }
  }

  async function changeStatus(id: string, payload: ChangeLeadStatusPayload): Promise<LeadDto> {
    saving.value = true
    try {
      const res = await leadService.changeStatus(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Durum değiştirilemedi.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function assign(id: string, payload: AssignLeadPayload): Promise<LeadDto> {
    saving.value = true
    try {
      const res = await leadService.assign(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Atama başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function convert(id: string, payload: ConvertLeadPayload): Promise<LeadDto> {
    saving.value = true
    try {
      const res = await leadService.convert(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Dönüşüm başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    current.value = null
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
    changeStatus,
    assign,
    convert,
    clearCurrent,
  }
})
