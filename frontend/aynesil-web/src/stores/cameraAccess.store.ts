/**
 * Camera Access store.
 * Delegates to useCameraStore; provides access-request and consent-status
 * focused view for the parent-facing camera access workflow.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { cameraService } from '@/services/camera.service'
import type { PaginatedResult } from '@/types/api.types'
import type { ViewingAuthorizationDto, ViewingAuthorizationQuery, CreateAuthorizationPayload } from '@/types/camera.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useCameraAccessStore = defineStore('cameraAccess', () => {
  const authorizations = ref<PaginatedResult<ViewingAuthorizationDto>>(emptyPage<ViewingAuthorizationDto>())
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchAuthorizations(query: ViewingAuthorizationQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await cameraService.listAuthorizations(query)
      if (res.success && res.data) authorizations.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createAuthorization(payload: CreateAuthorizationPayload): Promise<ViewingAuthorizationDto> {
    saving.value = true
    try {
      const res = await cameraService.createAuthorization(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Yetki oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function revokeAuthorization(id: string) {
    saving.value = true
    try {
      const res = await cameraService.revokeAuthorization(id)
      if (!res.success) throw new Error(res.message ?? 'Yetki iptal edilemedi.')
    } finally {
      saving.value = false
    }
  }

  return { authorizations, loading, saving, error, fetchAuthorizations, createAuthorization, revokeAuthorization }
})
