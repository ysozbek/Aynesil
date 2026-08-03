/**
 * Educator store — list, detail, CRUD, specialties, campus assignments,
 * certifications, hierarchy (supervisors/subordinates), availability, utilization.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { educatorService } from '@/services/educator.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  EducatorDto,
  EducatorListItemDto,
  EducatorAvailabilityDto,
  EducatorUtilizationDto,
  EducatorListQuery,
  UtilizationQuery,
  CreateEducatorPayload,
  UpdateEducatorPayload,
  AssignSpecialtyPayload,
  AssignCampusPayload,
  EndCampusAssignmentPayload,
  AddCertificationPayload,
  UpdateCertificationPayload,
  LinkHierarchyPayload,
  EndHierarchyPayload,
} from '@/types/educator.types'

const emptyPage = (): PaginatedResult<EducatorListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useEducatorStore = defineStore('educator', () => {
  const list = ref<PaginatedResult<EducatorListItemDto>>(emptyPage())
  const current = ref<EducatorDto | null>(null)
  const availability = ref<EducatorAvailabilityDto | null>(null)
  const utilization = ref<EducatorUtilizationDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(query: EducatorListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await educatorService.listEducators(query)
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
      const res = await educatorService.getEducator(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchAvailability(id: string) {
    loading.value = true
    try {
      const res = await educatorService.getAvailability(id)
      if (res.success && res.data) availability.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchUtilization(query: UtilizationQuery) {
    loading.value = true
    try {
      const res = await educatorService.getUtilization(query)
      if (res.success && res.data) utilization.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateEducatorPayload): Promise<EducatorDto> {
    saving.value = true
    try {
      const res = await educatorService.createEducator(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Eğitimci oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateEducatorPayload): Promise<EducatorDto> {
    saving.value = true
    try {
      const res = await educatorService.updateEducator(id, payload)
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
      await educatorService.deleteEducator(id)
      if (current.value?.id === id) current.value = null
    } finally {
      saving.value = false
    }
  }

  async function activate(id: string): Promise<EducatorDto> {
    saving.value = true
    try {
      const res = await educatorService.activateEducator(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Aktifleştirme başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deactivate(id: string): Promise<EducatorDto> {
    saving.value = true
    try {
      const res = await educatorService.deactivateEducator(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Devre dışı bırakma başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function assignSpecialty(id: string, payload: AssignSpecialtyPayload) {
    saving.value = true
    try {
      const res = await educatorService.assignSpecialty(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Uzmanlık eklenemedi.')
      if (current.value) current.value.specialties.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function removeSpecialty(id: string, assignmentId: string) {
    saving.value = true
    try {
      await educatorService.removeSpecialty(id, assignmentId)
      if (current.value) {
        current.value = {
          ...current.value,
          specialties: current.value.specialties.filter(s => s.id !== assignmentId),
        }
      }
    } finally {
      saving.value = false
    }
  }

  async function assignCampus(id: string, payload: AssignCampusPayload) {
    saving.value = true
    try {
      const res = await educatorService.assignCampus(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kampüs ataması başarısız.')
      if (current.value) current.value.campuses.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function endCampusAssignment(id: string, assignmentId: string, payload: EndCampusAssignmentPayload) {
    saving.value = true
    try {
      const res = await educatorService.endCampusAssignment(id, assignmentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kampüs ataması sonlandırılamadı.')
      if (current.value) {
        const idx = current.value.campuses.findIndex(c => c.id === assignmentId)
        if (idx >= 0) current.value.campuses[idx] = res.data
      }
    } finally {
      saving.value = false
    }
  }

  async function addCertification(id: string, payload: AddCertificationPayload) {
    saving.value = true
    try {
      const res = await educatorService.addCertification(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Sertifika eklenemedi.')
      if (current.value) current.value.certifications.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateCertification(id: string, certId: string, payload: UpdateCertificationPayload) {
    saving.value = true
    try {
      const res = await educatorService.updateCertification(id, certId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Sertifika güncellenemedi.')
      if (current.value) {
        const idx = current.value.certifications.findIndex(c => c.id === certId)
        if (idx >= 0) current.value.certifications[idx] = res.data
      }
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteCertification(id: string, certId: string) {
    saving.value = true
    try {
      await educatorService.deleteCertification(id, certId)
      if (current.value) {
        current.value = {
          ...current.value,
          certifications: current.value.certifications.filter(c => c.id !== certId),
        }
      }
    } finally {
      saving.value = false
    }
  }

  async function linkHierarchy(id: string, payload: LinkHierarchyPayload) {
    saving.value = true
    try {
      const res = await educatorService.linkHierarchy(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hiyerarşi bağlantısı kurulamadı.')
      if (current.value) current.value.supervisors.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function unlinkHierarchy(id: string, edgeId: string) {
    saving.value = true
    try {
      await educatorService.unlinkHierarchy(id, edgeId)
      if (current.value) {
        current.value = {
          ...current.value,
          supervisors: current.value.supervisors.filter(s => s.id !== edgeId),
          subordinates: current.value.subordinates.filter(s => s.id !== edgeId),
        }
      }
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    current.value = null
    availability.value = null
  }

  return {
    list, current, availability, utilization, loading, saving, error,
    fetchList, fetchOne, fetchAvailability, fetchUtilization,
    create, update, remove, activate, deactivate,
    assignSpecialty, removeSpecialty, assignCampus, endCampusAssignment,
    addCertification, updateCertification, deleteCertification,
    linkHierarchy, unlinkHierarchy,
    clearCurrent,
  }
})
