/**
 * Camp Management store.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { campService } from '@/services/camp.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CampListItemDto, CampDto,
  CampPeriodDto,
  CampEnrollmentListItemDto, CampEnrollmentDto,
  CampAttendanceSummaryDto,
  CampEnrollmentSummaryDto,
  CampPerformanceDto,
  CampActivityListItemDto,
  CampEducatorDto,
  CampListQuery, CampEnrollmentQuery, CampPerformanceQuery,
  CreateCampPayload, UpdateCampPayload,
  CreatePeriodPayload, UpdatePeriodPayload, EnrollStudentPayload,
  RecordAttendancePayload,
} from '@/types/camp.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useCampStore = defineStore('camp', () => {
  const campList = ref<PaginatedResult<CampListItemDto>>(emptyPage<CampListItemDto>())
  const currentCamp = ref<CampDto | null>(null)
  const currentPeriod = ref<CampPeriodDto | null>(null)
  const enrollments = ref<PaginatedResult<CampEnrollmentListItemDto>>(emptyPage<CampEnrollmentListItemDto>())
  const currentEnrollment = ref<CampEnrollmentDto | null>(null)
  const enrollmentSummary = ref<CampEnrollmentSummaryDto | null>(null)
  const attendanceSummary = ref<CampAttendanceSummaryDto[]>([])
  const performance = ref<CampPerformanceDto[]>([])
  const activities = ref<CampActivityListItemDto[]>([])
  const educators = ref<CampEducatorDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchCamps(query: CampListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await campService.list(query)
      if (res.success && res.data) campList.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchCamp(id: string, silent = false) {
    loading.value = true; error.value = null
    try {
      const res = await campService.get(id)
      if (res.success && res.data) currentCamp.value = res.data
    } catch (e: unknown) {
      // silent: güncelleme sonrası yeniden çekme hatası kullanıcıya yansıtılmaz.
      if (silent) console.warn(`fetchCamp yeniden çekilemedi`, e)
      else error.value = (e as Error).message
    }
    finally { loading.value = false }
  }

  async function createCamp(payload: CreateCampPayload): Promise<CampDto> {
    saving.value = true
    try {
      const res = await campService.create(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kamp oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateCamp(id: string, payload: UpdateCampPayload): Promise<CampDto | null> {
    saving.value = true
    try {
      // Backend gövdesiz yanıt döner; güncel kaydı yeniden çek.
      const res = await campService.update(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Kamp güncellenemedi.')
      await fetchCamp(id, true)
      return currentCamp.value
    } finally { saving.value = false }
  }

  async function createPeriod(campId: string, payload: CreatePeriodPayload) {
    saving.value = true
    try {
      const res = await campService.createPeriod(campId, payload)
      if (!res.success) throw new Error(res.message ?? 'Dönem oluşturulamadı.')
      await fetchCamp(campId)
    } finally { saving.value = false }
  }

  async function activateCamp(id: string) {
    saving.value = true
    try { await campService.activate(id); await fetchCamp(id, true) } finally { saving.value = false }
  }

  async function deactivateCamp(id: string) {
    saving.value = true
    try { await campService.deactivate(id); await fetchCamp(id, true) } finally { saving.value = false }
  }

  async function deleteCamp(id: string) {
    saving.value = true
    try { await campService.remove(id) } finally { saving.value = false }
  }

  async function updatePeriod(campId: string, periodId: string, payload: UpdatePeriodPayload) {
    saving.value = true
    try { await campService.updatePeriod(periodId, payload); await fetchCamp(campId, true) } finally { saving.value = false }
  }

  async function deletePeriod(campId: string, periodId: string) {
    saving.value = true
    try { await campService.deletePeriod(periodId); await fetchCamp(campId, true) } finally { saving.value = false }
  }

  async function assignEducator(campId: string, educatorId: string, role: string) {
    saving.value = true
    try { await campService.assignEducator(campId, { educatorId, role }); await fetchEducators(campId) } finally { saving.value = false }
  }

  async function changeEducatorRole(campId: string, assignmentId: string, role: string) {
    saving.value = true
    try { await campService.updateEducatorRole(assignmentId, role); await fetchEducators(campId) } finally { saving.value = false }
  }

  async function removeEducator(campId: string, assignmentId: string) {
    saving.value = true
    try { await campService.removeEducator(assignmentId); await fetchEducators(campId) } finally { saving.value = false }
  }

  async function fetchEnrollments(periodId: string, query?: CampEnrollmentQuery) {
    loading.value = true
    try {
      const res = await campService.listEnrollments(periodId, query)
      if (res.success && res.data) enrollments.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function enrollStudent(periodId: string, payload: EnrollStudentPayload) {
    saving.value = true
    try {
      const res = await campService.enroll(periodId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kayıt yapılamadı.')
      await fetchEnrollments(periodId)
    } finally { saving.value = false }
  }

  async function promoteFromWaitlist(enrollmentId: string, periodId: string) {
    saving.value = true
    try {
      await campService.promoteFromWaitlist(enrollmentId)
      await Promise.all([fetchEnrollments(periodId), fetchEnrollmentSummary(periodId)])
    } finally { saving.value = false }
  }

  async function moveToWaitlist(enrollmentId: string, periodId: string) {
    saving.value = true
    try {
      await campService.moveToWaitlist(enrollmentId)
      await Promise.all([fetchEnrollments(periodId), fetchEnrollmentSummary(periodId)])
    } finally { saving.value = false }
  }

  async function completeEnrollment(enrollmentId: string, periodId: string) {
    saving.value = true
    try {
      await campService.completeEnrollment(enrollmentId)
      await Promise.all([fetchEnrollments(periodId), fetchEnrollmentSummary(periodId)])
    } finally { saving.value = false }
  }

  /** Dönem sonunda tüm kayıtlı öğrencileri tamamlar; tamamlanan sayıyı döner. */
  async function bulkComplete(periodId: string): Promise<number> {
    saving.value = true
    try {
      const res = await campService.bulkComplete(periodId)
      await Promise.all([fetchEnrollments(periodId), fetchEnrollmentSummary(periodId)])
      return res.data ?? 0
    } finally { saving.value = false }
  }

  async function withdraw(enrollmentId: string, periodId: string) {
    saving.value = true
    try {
      await campService.withdraw(enrollmentId)
      await Promise.all([fetchEnrollments(periodId), fetchEnrollmentSummary(periodId)])
    } finally { saving.value = false }
  }

  async function recordAttendance(enrollmentId: string, payload: RecordAttendancePayload) {
    saving.value = true
    try {
      const res = await campService.recordAttendance(enrollmentId, payload)
      if (!res.success) throw new Error(res.message ?? 'Devamsızlık kaydedilemedi.')
    } finally { saving.value = false }
  }

  async function fetchEnrollmentSummary(periodId: string) {
    loading.value = true
    try {
      // Summary endpoint is camp-based; resolve the camp from the period, then pick the period's row.
      const period = await campService.getPeriod(periodId)
      if (!period.success || !period.data) return
      const res = await campService.getEnrollmentSummary(period.data.campId)
      if (res.success && res.data)
        enrollmentSummary.value = res.data.find(s => s.campPeriodId === periodId) ?? null
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchAttendanceSummary(periodId: string) {
    loading.value = true
    try {
      const res = await campService.getAttendanceSummary(periodId)
      if (res.success && res.data) attendanceSummary.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchPerformance(query: CampPerformanceQuery = {}) {
    loading.value = true
    try {
      const res = await campService.getPerformance(query)
      if (res.success && res.data) performance.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchActivities(periodId: string) {
    loading.value = true
    try {
      const res = await campService.listActivities(periodId)
      if (res.success && res.data) activities.value = res.data.items
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchEducators(campId: string) {
    loading.value = true
    try {
      const res = await campService.listEducators(campId)
      if (res.success && res.data) educators.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  function clearCurrent() {
    currentCamp.value = null; currentPeriod.value = null
    currentEnrollment.value = null; performance.value = []
  }

  return {
    campList, currentCamp, currentPeriod, enrollments, currentEnrollment,
    enrollmentSummary, attendanceSummary, performance, activities, educators,
    loading, saving, error,
    fetchCamps, fetchCamp, createCamp, updateCamp,
    createPeriod, updatePeriod, deletePeriod,
    activateCamp, deactivateCamp, deleteCamp,
    assignEducator, changeEducatorRole, removeEducator,
    fetchEnrollments, enrollStudent, promoteFromWaitlist, withdraw,
    moveToWaitlist, completeEnrollment, bulkComplete,
    recordAttendance, fetchEnrollmentSummary, fetchAttendanceSummary, fetchPerformance,
    fetchActivities, fetchEducators, clearCurrent,
  }
})
