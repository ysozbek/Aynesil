/**
 * Guardian store — list, detail, CRUD, portal access management.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { guardianService } from '@/services/student.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  GuardianDto,
  GuardianListItemDto,
  GuardianPortalAccessDto,
  GuardianListQuery,
  CreateGuardianPayload,
  UpdateGuardianPayload,
  GrantPortalAccessPayload,
} from '@/types/student.types'

const emptyPage = (): PaginatedResult<GuardianListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useGuardianStore = defineStore('guardian', () => {
  const list = ref<PaginatedResult<GuardianListItemDto>>(emptyPage())
  const current = ref<GuardianDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(query: GuardianListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await guardianService.listGuardians(query)
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
      const res = await guardianService.getGuardian(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateGuardianPayload): Promise<GuardianDto> {
    saving.value = true
    try {
      const res = await guardianService.createGuardian(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Veli oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateGuardianPayload): Promise<GuardianDto> {
    saving.value = true
    try {
      const res = await guardianService.updateGuardian(id, payload)
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
      await guardianService.deleteGuardian(id)
      if (current.value?.id === id) current.value = null
    } finally {
      saving.value = false
    }
  }

  async function grantPortalAccess(
    guardianId: string, studentId: string, payload: GrantPortalAccessPayload
  ): Promise<GuardianPortalAccessDto> {
    saving.value = true
    try {
      const res = await guardianService.grantPortalAccess(guardianId, studentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Portal erişimi açılamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function revokePortalAccess(guardianId: string, studentId: string) {
    saving.value = true
    try {
      await guardianService.revokePortalAccess(guardianId, studentId)
    } finally {
      saving.value = false
    }
  }

  async function updatePortalPermissions(
    guardianId: string, studentId: string, payload: GrantPortalAccessPayload
  ): Promise<GuardianPortalAccessDto> {
    saving.value = true
    try {
      const res = await guardianService.updatePortalPermissions(guardianId, studentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Portal izinleri güncellenemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    current.value = null
  }

  return {
    list, current, loading, saving, error,
    fetchList, fetchOne, create, update, remove,
    grantPortalAccess, revokePortalAccess, updatePortalPermissions,
    clearCurrent,
  }
})
