/**
 * Parent Portal store — guardian-scoped, read-only.
 * Loads only backend-authorized students; never pre-loads all students.
 * Respects ABAC: student access is backend-controlled.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { portalService } from '@/services/portal.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  PortalStudentDto,
  PortalDashboardDto,
  PortalSessionDto,
  PortalAttendanceDto,
  PortalPackageDto,
  PortalDocumentDto,
  PortalEducationPlanDto,
  PortalGoalProgressDto,
  PortalMeetingDto,
  PortalSessionListQuery,
  PortalAttendanceListQuery,
  PortalDocumentListQuery,
} from '@/types/portal.types'
import type { NotificationListItemDto } from '@/types/notification.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useParentPortalStore = defineStore('parentPortal', () => {
  // ── My Children ────────────────────────────────────────────────────────────
  const myStudents = ref<PortalStudentDto[]>([])
  const currentStudent = ref<PortalStudentDto | null>(null)
  const dashboard = ref<PortalDashboardDto | null>(null)

  // ── Sessions ───────────────────────────────────────────────────────────────
  const sessionList = ref<PaginatedResult<PortalSessionDto>>(emptyPage<PortalSessionDto>())

  // ── Attendance ─────────────────────────────────────────────────────────────
  const attendanceList = ref<PaginatedResult<PortalAttendanceDto>>(emptyPage<PortalAttendanceDto>())

  // ── Packages ───────────────────────────────────────────────────────────────
  const packages = ref<PortalPackageDto[]>([])

  // ── Documents ──────────────────────────────────────────────────────────────
  const documentList = ref<PaginatedResult<PortalDocumentDto>>(emptyPage<PortalDocumentDto>())
  const reportList = ref<PortalDocumentDto[]>([])

  // ── BEP ────────────────────────────────────────────────────────────────────
  const bepList = ref<PortalEducationPlanDto[]>([])

  // ── Goals ──────────────────────────────────────────────────────────────────
  const goalProgress = ref<PortalGoalProgressDto[]>([])

  // ── Meetings ───────────────────────────────────────────────────────────────
  const meetingHistory = ref<PortalMeetingDto[]>([])

  // ── Notifications ──────────────────────────────────────────────────────────
  const portalNotifications = ref<PaginatedResult<NotificationListItemDto>>(emptyPage<NotificationListItemDto>())

  const loading = ref(false)
  const error = ref<string | null>(null)

  // ── Actions ────────────────────────────────────────────────────────────────

  async function fetchMyStudents() {
    loading.value = true
    error.value = null
    try {
      const res = await portalService.getMyStudents()
      if (res.success && res.data) myStudents.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchStudent(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getStudent(studentId)
      if (res.success && res.data) currentStudent.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchDashboard(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getDashboard(studentId)
      if (res.success && res.data) dashboard.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchSessions(query: PortalSessionListQuery) {
    loading.value = true
    try {
      const res = await portalService.getSessions(query)
      if (res.success && res.data) sessionList.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchAttendance(query: PortalAttendanceListQuery) {
    loading.value = true
    try {
      const res = await portalService.getAttendance(query)
      if (res.success && res.data) attendanceList.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchPackages(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getPackages(studentId)
      if (res.success && res.data) packages.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchDocuments(query: PortalDocumentListQuery) {
    loading.value = true
    try {
      const res = await portalService.getDocuments(query)
      if (res.success && res.data) documentList.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchDevelopmentReports(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getDevelopmentReports(studentId)
      if (res.success && res.data) reportList.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchBep(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getBep(studentId)
      if (res.success && res.data) bepList.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchGoalProgress(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getGoalProgress(studentId)
      if (res.success && res.data) goalProgress.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchMeetings(studentId: string) {
    loading.value = true
    try {
      const res = await portalService.getMeetings(studentId)
      if (res.success && res.data) meetingHistory.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchPortalNotifications(page = 1, pageSize = 20) {
    loading.value = true
    try {
      const res = await portalService.getNotifications({ page, pageSize })
      if (res.success && res.data) portalNotifications.value = res.data
    } finally {
      loading.value = false
    }
  }

  function clearStudent() {
    currentStudent.value = null
    dashboard.value = null
    sessionList.value = emptyPage()
    attendanceList.value = emptyPage()
    packages.value = []
    documentList.value = emptyPage()
    reportList.value = []
    bepList.value = []
    goalProgress.value = []
    meetingHistory.value = []
  }

  return {
    myStudents, currentStudent, dashboard,
    sessionList, attendanceList,
    packages, documentList, reportList,
    bepList, goalProgress, meetingHistory,
    portalNotifications,
    loading, error,
    fetchMyStudents, fetchStudent, fetchDashboard,
    fetchSessions, fetchAttendance, fetchPackages,
    fetchDocuments, fetchDevelopmentReports,
    fetchBep, fetchGoalProgress, fetchMeetings,
    fetchPortalNotifications,
    clearStudent,
  }
})
