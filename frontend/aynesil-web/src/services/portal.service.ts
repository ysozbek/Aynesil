/**
 * Parent Portal API service.
 * Wraps all /api/portal endpoints (read-only, guardian-scoped).
 */
import { apiService } from '@/services/api.service'
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
import type { NotificationListItemDto, NotificationListQuery } from '@/types/notification.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const portalService = {
  // ── Students ───────────────────────────────────────────────────────────────

  getMyStudents: () =>
    apiService.get<PortalStudentDto[]>('/portal/my-students'),

  getStudent: (studentId: string) =>
    apiService.get<PortalStudentDto>(`/portal/students/${studentId}`),

  // ── Dashboard ──────────────────────────────────────────────────────────────

  getDashboard: (studentId: string) =>
    apiService.get<PortalDashboardDto>(`/portal/students/${studentId}/dashboard`),

  // ── Sessions ───────────────────────────────────────────────────────────────

  getSessions: (query: PortalSessionListQuery) => {
    const { studentId, ...rest } = query
    return apiService.get<PaginatedResult<PortalSessionDto>>(
      `/portal/students/${studentId}/sessions${qs(rest as Record<string, unknown>)}`
    )
  },

  // ── Attendance ─────────────────────────────────────────────────────────────

  getAttendance: (query: PortalAttendanceListQuery) => {
    const { studentId, ...rest } = query
    return apiService.get<PaginatedResult<PortalAttendanceDto>>(
      `/portal/students/${studentId}/attendance${qs(rest as Record<string, unknown>)}`
    )
  },

  // ── Packages ───────────────────────────────────────────────────────────────

  getPackages: (studentId: string) =>
    apiService.get<PortalPackageDto[]>(`/portal/students/${studentId}/packages`),

  // ── Documents ──────────────────────────────────────────────────────────────

  getDocuments: (query: PortalDocumentListQuery) => {
    const { studentId, ...rest } = query
    return apiService.get<PaginatedResult<PortalDocumentDto>>(
      `/portal/students/${studentId}/documents${qs(rest as Record<string, unknown>)}`
    )
  },

  // ── Education Plan (BEP) ───────────────────────────────────────────────────

  getBep: (studentId: string) =>
    apiService.get<PortalEducationPlanDto[]>(`/portal/students/${studentId}/bep`),

  // ── Goal Progress ──────────────────────────────────────────────────────────

  getGoalProgress: (studentId: string) =>
    apiService.get<PortalGoalProgressDto[]>(`/portal/students/${studentId}/goal-progress`),

  // ── Meeting History ────────────────────────────────────────────────────────

  getMeetings: (studentId: string) =>
    apiService.get<PortalMeetingDto[]>(`/portal/students/${studentId}/meetings`),

  // ── Development Reports ────────────────────────────────────────────────────

  getDevelopmentReports: (studentId: string) =>
    apiService.get<PortalDocumentDto[]>(`/portal/students/${studentId}/development-reports`),

  // ── Portal Notifications ───────────────────────────────────────────────────

  getNotifications: (query?: Omit<NotificationListQuery, 'corporationId'>) =>
    apiService.get<PaginatedResult<NotificationListItemDto>>(
      `/portal/notifications${qs((query ?? {}) as Record<string, unknown>)}`
    ),
}
