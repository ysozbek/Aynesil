/**
 * Camp Management API service.
 * Wraps all /api/camps endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type { AssignCampEducatorPayload, CreateCampActivityPayload } from '@/types/camp.types'
import type {
  CampListItemDto, CampDto,
  CampPeriodDto,
  CampEnrollmentListItemDto, CampEnrollmentDto,
  CampAttendanceDto,
  CampReportDto,
  CampEnrollmentSummaryDto,
  CampAttendanceSummaryDto,
  CampPerformanceDto,
  CampActivityListItemDto, CampActivityDto,
  CampEducatorDto,
  CampActivityParticipationDto,
  CampListQuery, CampEnrollmentQuery,
  CampPerformanceQuery, CampActivityListQuery, CampParticipationListQuery,
  CreateCampPayload, UpdateCampPayload,
  CreatePeriodPayload, EnrollStudentPayload,
  RecordAttendancePayload,
} from '@/types/camp.types'

function qs(params: object): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const campService = {
  // ── Camps ──────────────────────────────────────────────────────────────────
  list: (query: CampListQuery) =>
    apiService.get<PaginatedResult<CampListItemDto>>(`/camps${qs(query)}`),

  get: (id: string) =>
    apiService.get<CampDto>(`/camps/${id}`),

  create: (payload: CreateCampPayload) =>
    apiService.post<CampDto>('/camps', payload),

  update: (id: string, payload: UpdateCampPayload) =>
    apiService.put<void>(`/camps/${id}`, payload),

  // ── Periods ────────────────────────────────────────────────────────────────
  createPeriod: (campId: string, payload: CreatePeriodPayload) =>
    apiService.post<CampPeriodDto>(`/camps/${campId}/periods`, payload),

  getPeriod: (periodId: string) =>
    apiService.get<CampPeriodDto>(`/camps/periods/${periodId}`),

  // ── Enrollments ────────────────────────────────────────────────────────────
  listEnrollments: (periodId: string, query?: CampEnrollmentQuery) =>
    apiService.get<PaginatedResult<CampEnrollmentListItemDto>>(
      `/camps/enrollments${qs({ ...(query ?? {}), campPeriodId: periodId } as Record<string, unknown>)}`
    ),

  getEnrollment: (enrollmentId: string) =>
    apiService.get<CampEnrollmentDto>(`/camps/enrollments/${enrollmentId}`),

  enroll: (periodId: string, payload: EnrollStudentPayload) =>
    apiService.post<CampEnrollmentDto>(`/camps/periods/${periodId}/enrollments`, payload),

  promoteFromWaitlist: (enrollmentId: string) =>
    apiService.post(`/camps/enrollments/${enrollmentId}/promote`),

  withdraw: (enrollmentId: string) =>
    apiService.post(`/camps/enrollments/${enrollmentId}/withdraw`),

  completeEnrollment: (enrollmentId: string) =>
    apiService.post(`/camps/enrollments/${enrollmentId}/complete`),

  // ── Attendance ─────────────────────────────────────────────────────────────
  /** POST /camps/attendance — enrollment id travels in the body (CampEnrollmentId). */
  recordAttendance: (enrollmentId: string, payload: RecordAttendancePayload) =>
    apiService.post<CampAttendanceDto>('/camps/attendance', { campEnrollmentId: enrollmentId, ...payload }),

  /** Backend returns one attendance summary row per enrolled student of the period. */
  getAttendanceSummary: (periodId: string) =>
    apiService.get<CampAttendanceSummaryDto[]>(`/camps/periods/${periodId}/attendance-summary`),

  // ── Reports & Analytics ────────────────────────────────────────────────────
  /** Backend returns one summary row per period of the camp. */
  getEnrollmentSummary: (campId: string) =>
    apiService.get<CampEnrollmentSummaryDto[]>(`/camps/${campId}/enrollment-summary`),

  /** Performance report is tenant-wide (one row per camp); optional campus / camp type filters. */
  getPerformance: (query: CampPerformanceQuery = {}) =>
    apiService.get<CampPerformanceDto[]>(`/camps/performance${qs(query)}`),

  /** End-of-camp reports of an enrollment (a list; there is no single "report" resource). */
  listCampReports: (enrollmentId: string) =>
    apiService.get<CampReportDto[]>(`/camps/enrollments/${enrollmentId}/reports`),

  // ── Activities ─────────────────────────────────────────────────────────────
  listActivities: (periodId: string, query: CampActivityListQuery = {}) =>
    apiService.get<PaginatedResult<CampActivityListItemDto>>(
      `/camps/activities${qs({ ...query, campPeriodId: periodId })}`),

  createActivity: (periodId: string, payload: CreateCampActivityPayload) =>
    apiService.post<CampActivityDto>(`/camps/periods/${periodId}/activities`, payload),

  // ── Educators ─────────────────────────────────────────────────────────────
  listEducators: (campId: string) =>
    apiService.get<CampEducatorDto[]>(`/camps/educators${qs({ campId })}`),

  assignEducator: (campId: string, payload: AssignCampEducatorPayload) =>
    apiService.post<CampEducatorDto>(`/camps/${campId}/educators`, payload),

  // ── Participations ─────────────────────────────────────────────────────────
  listParticipations: (activityId: string, query: CampParticipationListQuery = {}) =>
    apiService.get<PaginatedResult<CampActivityParticipationDto>>(
      `/camps/participations${qs({ ...query, campActivityId: activityId })}`),
}
