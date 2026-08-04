/**
 * Attendance store — session attendance, student history, summary.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { schedulingService } from '@/services/scheduling.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  AttendanceDto,
  AttendanceSummaryDto,
  RecordAttendancePayload,
  BulkAttendancePayload,
  StudentAttendanceQuery,
} from '@/types/scheduling.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useAttendanceStore = defineStore('attendance', () => {
  const sessionAttendance = ref<AttendanceDto[]>([])
  const studentAttendance = ref<PaginatedResult<AttendanceDto>>(emptyPage<AttendanceDto>())
  const summary = ref<AttendanceSummaryDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchSessionAttendance(sessionId: string) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getSessionAttendance(sessionId)
      if (res.success && res.data) sessionAttendance.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function recordAttendance(sessionId: string, payload: RecordAttendancePayload): Promise<AttendanceDto> {
    saving.value = true
    try {
      const res = await schedulingService.recordAttendance(sessionId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Devam kaydedilemedi.')
      const idx = sessionAttendance.value.findIndex(a => a.studentId === payload.studentId)
      if (idx >= 0) sessionAttendance.value[idx] = res.data
      else sessionAttendance.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function bulkRecordAttendance(sessionId: string, payload: BulkAttendancePayload): Promise<AttendanceDto[]> {
    saving.value = true
    try {
      const res = await schedulingService.bulkRecordAttendance(sessionId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplu devam kaydedilemedi.')
      sessionAttendance.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function fetchStudentAttendance(studentId: string, query?: StudentAttendanceQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getStudentAttendance(studentId, query)
      if (res.success && res.data) studentAttendance.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchAttendanceSummary(studentId: string) {
    loading.value = true
    try {
      const res = await schedulingService.getAttendanceSummary(studentId)
      if (res.success && res.data) summary.value = res.data
    } finally {
      loading.value = false
    }
  }

  function clearSession() {
    sessionAttendance.value = []
  }

  return {
    sessionAttendance, studentAttendance, summary,
    loading, saving, error,
    fetchSessionAttendance, recordAttendance, bulkRecordAttendance,
    fetchStudentAttendance, fetchAttendanceSummary,
    clearSession,
  }
})
