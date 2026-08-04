/**
 * Institution store.
 * Delegates to useConsultancyStore; provides institution-focused view
 * for institution management screens.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { consultancyService } from '@/services/consultancy.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  InstitutionListItemDto, InstitutionDto,
  InstitutionListQuery, CreateInstitutionPayload, UpdateInstitutionPayload,
} from '@/types/consultancy.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useInstitutionStore = defineStore('institution', () => {
  const institutions = ref<PaginatedResult<InstitutionListItemDto>>(emptyPage<InstitutionListItemDto>())
  const currentInstitution = ref<InstitutionDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchInstitutions(query: InstitutionListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.listInstitutions(query)
      if (res.success && res.data) institutions.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchInstitution(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getInstitution(id)
      if (res.success && res.data) currentInstitution.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createInstitution(payload: CreateInstitutionPayload): Promise<InstitutionDto> {
    saving.value = true
    try {
      const res = await consultancyService.createInstitution(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kurum oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateInstitution(id: string, payload: UpdateInstitutionPayload): Promise<InstitutionDto> {
    saving.value = true
    try {
      const res = await consultancyService.updateInstitution(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kurum güncellenemedi.')
      currentInstitution.value = res.data
      return res.data
    } finally { saving.value = false }
  }

  function clearCurrent() { currentInstitution.value = null }

  return { institutions, currentInstitution, loading, saving, error, fetchInstitutions, fetchInstitution, createInstitution, updateInstitution, clearCurrent }
})
