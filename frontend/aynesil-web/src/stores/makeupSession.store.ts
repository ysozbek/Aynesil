/**
 * Makeup session store — request lifecycle management.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { schedulingService } from '@/services/scheduling.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  MakeupRequestDto,
  MakeupRequestListItemDto,
  MakeupRequestListQuery,
  CreateMakeupRequestPayload,
  ApproveMakeupRequestPayload,
  RejectMakeupRequestPayload,
  AssignMakeupSessionPayload,
} from '@/types/scheduling.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useMakeupSessionStore = defineStore('makeupSession', () => {
  const requestList = ref<PaginatedResult<MakeupRequestListItemDto>>(emptyPage<MakeupRequestListItemDto>())
  const currentRequest = ref<MakeupRequestDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchMakeupRequests(query: MakeupRequestListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.listMakeupRequests(query)
      if (res.success && res.data) requestList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchMakeupRequest(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getMakeupRequest(id)
      if (res.success && res.data) currentRequest.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createMakeupRequest(payload: CreateMakeupRequestPayload): Promise<MakeupRequestDto> {
    saving.value = true
    try {
      const res = await schedulingService.createMakeupRequest(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Telafi talebi oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function approveRequest(id: string, payload: ApproveMakeupRequestPayload): Promise<MakeupRequestDto> {
    saving.value = true
    try {
      const res = await schedulingService.approveMakeupRequest(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Talep onaylanamadı.')
      currentRequest.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function rejectRequest(id: string, payload: RejectMakeupRequestPayload): Promise<MakeupRequestDto> {
    saving.value = true
    try {
      const res = await schedulingService.rejectMakeupRequest(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Talep reddedilemedi.')
      currentRequest.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function assignMakeupSession(id: string, payload: AssignMakeupSessionPayload): Promise<MakeupRequestDto> {
    saving.value = true
    try {
      const res = await schedulingService.assignMakeupSession(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Telafi seansı atanamadı.')
      currentRequest.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function completeMakeupRequest(id: string, rowVersion: string): Promise<MakeupRequestDto> {
    saving.value = true
    try {
      const res = await schedulingService.completeMakeupRequest(id, { rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'Talep tamamlanamadı.')
      currentRequest.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentRequest.value = null
  }

  return {
    requestList, currentRequest, loading, saving, error,
    fetchMakeupRequests, fetchMakeupRequest, createMakeupRequest,
    approveRequest, rejectRequest, assignMakeupSession, completeMakeupRequest,
    clearCurrent,
  }
})
