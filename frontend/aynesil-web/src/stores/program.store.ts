/**
 * Program store — programs, enrollments, and student-program assignments.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { programService, enrollmentService, studentProgramService } from '@/services/program.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  ProgramDto,
  ProgramListItemDto,
  EnrollmentDto,
  EnrollmentListItemDto,
  StudentProgramDto,
  StudentProgramListItemDto,
  ProgramListQuery,
  EnrollmentListQuery,
  StudentProgramListQuery,
  CreateProgramPayload,
  UpdateProgramPayload,
  SetTranslationPayload,
  AddProgramServicePayload,
  UpdateProgramServicePayload,
  CreateEnrollmentPayload,
  ChangeEnrollmentStatusPayload,
  EndEnrollmentPayload,
  AssignStudentToProgramPayload,
  UpdateStudentProgramPayload,
} from '@/types/program.types'

const emptyProgramPage = (): PaginatedResult<ProgramListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

const emptyEnrollmentPage = (): PaginatedResult<EnrollmentListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

const emptyStudentProgramPage = (): PaginatedResult<StudentProgramListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useProgramStore = defineStore('program', () => {
  const programList = ref<PaginatedResult<ProgramListItemDto>>(emptyProgramPage())
  const currentProgram = ref<ProgramDto | null>(null)
  const enrollmentList = ref<PaginatedResult<EnrollmentListItemDto>>(emptyEnrollmentPage())
  const currentEnrollment = ref<EnrollmentDto | null>(null)
  const studentProgramList = ref<PaginatedResult<StudentProgramListItemDto>>(emptyStudentProgramPage())
  const currentStudentProgram = ref<StudentProgramDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Programs ────────────────────────────────────────────────────────────────

  async function fetchPrograms(query: ProgramListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await programService.listPrograms(query)
      if (res.success && res.data) programList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchProgram(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await programService.getProgram(id)
      if (res.success && res.data) currentProgram.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createProgram(payload: CreateProgramPayload): Promise<ProgramDto> {
    saving.value = true
    try {
      const res = await programService.createProgram(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Program oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateProgram(id: string, payload: UpdateProgramPayload): Promise<ProgramDto> {
    saving.value = true
    try {
      const res = await programService.updateProgram(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Güncelleme başarısız.')
      currentProgram.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteProgram(id: string) {
    saving.value = true
    try {
      await programService.deleteProgram(id)
      if (currentProgram.value?.id === id) currentProgram.value = null
    } finally {
      saving.value = false
    }
  }

  async function setTranslation(id: string, locale: string, payload: SetTranslationPayload) {
    saving.value = true
    try {
      const res = await programService.setTranslation(id, locale, payload)
      if (!res.success) throw new Error(res.message ?? 'Çeviri güncellenemedi.')
      if (currentProgram.value) await fetchProgram(id)
    } finally {
      saving.value = false
    }
  }

  async function addService(id: string, payload: AddProgramServicePayload) {
    saving.value = true
    try {
      const res = await programService.addService(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hizmet eklenemedi.')
      if (currentProgram.value) currentProgram.value.services.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateService(id: string, serviceId: string, payload: UpdateProgramServicePayload) {
    saving.value = true
    try {
      const res = await programService.updateService(id, serviceId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hizmet güncellenemedi.')
      if (currentProgram.value) {
        const idx = currentProgram.value.services.findIndex(s => s.id === serviceId)
        if (idx >= 0) currentProgram.value.services[idx] = res.data
      }
    } finally {
      saving.value = false
    }
  }

  async function deleteService(id: string, serviceId: string) {
    saving.value = true
    try {
      await programService.deleteService(id, serviceId)
      if (currentProgram.value) {
        currentProgram.value = {
          ...currentProgram.value,
          services: currentProgram.value.services.filter(s => s.id !== serviceId),
        }
      }
    } finally {
      saving.value = false
    }
  }

  // ── Enrollments ─────────────────────────────────────────────────────────────

  async function fetchEnrollments(query: EnrollmentListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await enrollmentService.listEnrollments(query)
      if (res.success && res.data) enrollmentList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchEnrollment(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await enrollmentService.getEnrollment(id)
      if (res.success && res.data) currentEnrollment.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createEnrollment(payload: CreateEnrollmentPayload): Promise<EnrollmentDto> {
    saving.value = true
    try {
      const res = await enrollmentService.createEnrollment(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kayıt oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function changeEnrollmentStatus(id: string, payload: ChangeEnrollmentStatusPayload): Promise<EnrollmentDto> {
    saving.value = true
    try {
      const res = await enrollmentService.changeStatus(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Durum değiştirilemedi.')
      currentEnrollment.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function endEnrollment(id: string, payload: EndEnrollmentPayload): Promise<EnrollmentDto> {
    saving.value = true
    try {
      const res = await enrollmentService.endEnrollment(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kayıt sonlandırılamadı.')
      currentEnrollment.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  // ── Student Programs ────────────────────────────────────────────────────────

  async function fetchStudentPrograms(query: StudentProgramListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await studentProgramService.listStudentPrograms(query)
      if (res.success && res.data) studentProgramList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function assignStudentToProgram(payload: AssignStudentToProgramPayload): Promise<StudentProgramDto> {
    saving.value = true
    try {
      const res = await studentProgramService.assignStudentToProgram(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Program atama başarısız.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateStudentProgram(enrollmentId: string, spId: string, payload: UpdateStudentProgramPayload): Promise<StudentProgramDto> {
    saving.value = true
    try {
      const res = await enrollmentService.updateStudentProgram(enrollmentId, spId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Program güncellenemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function removeStudentProgram(enrollmentId: string, spId: string) {
    saving.value = true
    try {
      await enrollmentService.removeStudentProgram(enrollmentId, spId)
      if (currentEnrollment.value) {
        currentEnrollment.value = {
          ...currentEnrollment.value,
          studentPrograms: currentEnrollment.value.studentPrograms.filter(s => s.id !== spId),
        }
      }
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentProgram.value = null
    currentEnrollment.value = null
    currentStudentProgram.value = null
  }

  return {
    programList, currentProgram, enrollmentList, currentEnrollment,
    studentProgramList, currentStudentProgram, loading, saving, error,
    fetchPrograms, fetchProgram, createProgram, updateProgram, deleteProgram,
    setTranslation, addService, updateService, deleteService,
    fetchEnrollments, fetchEnrollment, createEnrollment,
    changeEnrollmentStatus, endEnrollment,
    fetchStudentPrograms, assignStudentToProgram, updateStudentProgram, removeStudentProgram,
    clearCurrent,
  }
})
