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
  CampListQuery, CampEnrollmentQuery,
  CreateCampPayload, UpdateCampPayload,
  CreatePeriodPayload, EnrollStudentPayload,
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
  const attendanceSummary = ref<CampAttendanceSummaryDto | null>(null)
  const performance = ref<CampPerformanceDto | null>(null)
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

  async function fetchCamp(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await campService.get(id)
      if (res.success && res.data) currentCamp.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
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

  async function updateCamp(id: string, payload: UpdateCampPayload): Promise<CampDto> {
    saving.value = true
    try {
      const res = await campService.update(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kamp güncellenemedi.')
      currentCamp.value = res.data
      return res.data
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
      await fetchEnrollments(periodId)
    } finally { saving.value = false }
  }

  async function withdraw(enrollmentId: string, periodId: string) {
    saving.value = true
    try {
      await campService.withdraw(enrollmentId)
      await fetchEnrollments(periodId)
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
      const res = await campService.getEnrollmentSummary(periodId)
      if (res.success && res.data) enrollmentSummary.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchPerformance(campId: string) {
    loading.value = true
    try {
      const res = await campService.getPerformance(campId)
      if (res.success && res.data) performance.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchActivities(periodId: string) {
    loading.value = true
    try {
      const res = await campService.listActivities(periodId)
      if (res.success && res.data) activities.value = res.data
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
    currentEnrollment.value = null; performance.value = null
  }

  return {
    campList, currentCamp, currentPeriod, enrollments, currentEnrollment,
    enrollmentSummary, attendanceSummary, performance, activities, educators,
    loading, saving, error,
    fetchCamps, fetchCamp, createCamp, updateCamp,
    createPeriod,
    fetchEnrollments, enrollStudent, promoteFromWaitlist, withdraw,
    recordAttendance, fetchEnrollmentSummary, fetchPerformance,
    fetchActivities, fetchEducators, clearCurrent,
  }
})
