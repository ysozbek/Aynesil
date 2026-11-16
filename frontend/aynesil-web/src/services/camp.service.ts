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
  CampListQuery, CampEnrollmentQuery, CampAttendanceQuery,
  UpdateCampActivityPayload, ParticipationInput, BulkAttendanceInput, CreateCampReportPayload,
  CampPerformanceQuery, CampActivityListQuery, CampParticipationListQuery,
  CreateCampPayload, UpdateCampPayload,
  CreatePeriodPayload, UpdatePeriodPayload, EnrollStudentPayload,
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

  activate: (id: string) => apiService.post(`/camps/${id}/activate`),
  deactivate: (id: string) => apiService.post(`/camps/${id}/deactivate`),
  remove: (id: string) => apiService.delete(`/camps/${id}`),

  // ── Periods ────────────────────────────────────────────────────────────────
  updatePeriod: (periodId: string, payload: UpdatePeriodPayload) =>
    apiService.put<void>(`/camps/periods/${periodId}`, payload),

  deletePeriod: (periodId: string) =>
    apiService.delete(`/camps/periods/${periodId}`),

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

  moveToWaitlist: (enrollmentId: string) =>
    apiService.post(`/camps/enrollments/${enrollmentId}/waitlist`),

  /** Dönem sonunda tüm 'kayıtlı' öğrencileri tamamlar; tamamlanan sayıyı döner. */
  bulkComplete: (periodId: string) =>
    apiService.post<number>(`/camps/periods/${periodId}/bulk-complete`),

  withdraw: (enrollmentId: string) =>
    apiService.post(`/camps/enrollments/${enrollmentId}/withdraw`),

  completeEnrollment: (enrollmentId: string) =>
    apiService.post(`/camps/enrollments/${enrollmentId}/complete`),

  // ── Attendance ─────────────────────────────────────────────────────────────
  /** POST /camps/attendance — enrollment id travels in the body (CampEnrollmentId). */
  recordAttendance: (enrollmentId: string, payload: RecordAttendancePayload) =>
    apiService.post<CampAttendanceDto>('/camps/attendance', { campEnrollmentId: enrollmentId, ...payload }),

  /** Günlük yoklama listesi; `campPeriodId` + gün aralığı ile dönemin o günkü kayıtları. */
  listAttendance: (query: CampAttendanceQuery) =>
    apiService.get<PaginatedResult<CampAttendanceDto>>(`/camps/attendance${qs(query)}`),

  /** Toplu yoklama: yalnızca dönemin `enrolled` öğrencileri; aynı gün kaydı olanlar atlanır. Eklenen sayıyı döner. */
  bulkRecordAttendance: (periodId: string, attendanceDate: string, records: BulkAttendanceInput[]) =>
    apiService.post<number>(`/camps/periods/${periodId}/attendance/bulk`, { attendanceDate, records }),

  /** Yoklama düzeltme (durum / gerekçe). */
  updateAttendance: (attendanceId: string, status: string, reasonId?: string) =>
    apiService.patch<void>(`/camps/attendance/${attendanceId}`, { status, reasonId: reasonId ?? null }),

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

  /** Kamp sonu raporu yazar; yalnızca tamamlanmış kayıt için (aksi halde 409). */
  createCampReport: (enrollmentId: string, payload: CreateCampReportPayload) =>
    apiService.post<CampReportDto>(`/camps/enrollments/${enrollmentId}/reports`, payload),

  // ── Activities ─────────────────────────────────────────────────────────────
  listActivities: (periodId: string, query: CampActivityListQuery = {}) =>
    apiService.get<PaginatedResult<CampActivityListItemDto>>(
      `/camps/activities${qs({ ...query, campPeriodId: periodId })}`),

  createActivity: (periodId: string, payload: CreateCampActivityPayload) =>
    apiService.post<CampActivityDto>(`/camps/periods/${periodId}/activities`, payload),

  getActivity: (activityId: string) =>
    apiService.get<CampActivityDto>(`/camps/activities/${activityId}`),

  updateActivity: (activityId: string, payload: UpdateCampActivityPayload) =>
    apiService.put<void>(`/camps/activities/${activityId}`, { id: activityId, ...payload }),

  deleteActivity: (activityId: string) =>
    apiService.delete(`/camps/activities/${activityId}`),

  // ── Educators ─────────────────────────────────────────────────────────────
  listEducators: (campId: string) =>
    apiService.get<CampEducatorDto[]>(`/camps/educators${qs({ campId })}`),

  assignEducator: (campId: string, payload: AssignCampEducatorPayload) =>
    apiService.post<CampEducatorDto>(`/camps/${campId}/educators`, payload),

  updateEducatorRole: (assignmentId: string, role: string) =>
    apiService.patch<void>(`/camps/educators/${assignmentId}/role`, { role }),

  removeEducator: (assignmentId: string) =>
    apiService.delete(`/camps/educators/${assignmentId}`),

  // ── Participations ─────────────────────────────────────────────────────────
  listParticipations: (activityId: string, query: CampParticipationListQuery = {}) =>
    apiService.get<PaginatedResult<CampActivityParticipationDto>>(
      `/camps/participations${qs({ ...query, campActivityId: activityId })}`),

  /** Tekil katılım kaydı (etkinlik kontenjanı ve dönem kuralları sunucuda denetlenir). */
  registerParticipation: (activityId: string, input: ParticipationInput) =>
    apiService.post<CampActivityParticipationDto>(`/camps/activities/${activityId}/participations`, {
      campActivityId: activityId, campEnrollmentId: input.enrollmentId, status: input.status, notes: input.notes ?? null,
    }),

  /** Toplu katılım: aynı etkinlikte kaydı olanlar atlanır; eklenen sayıyı döner. */
  bulkRegisterParticipation: (activityId: string, records: ParticipationInput[]) =>
    apiService.post<number>(`/camps/activities/${activityId}/participations/bulk`, {
      campActivityId: activityId,
      records: records.map((r) => ({ enrollmentId: r.enrollmentId, status: r.status, notes: r.notes ?? null })),
    }),

  /** Katılım düzeltme (durum / not). */
  updateParticipation: (participationId: string, status: string, notes?: string) =>
    apiService.patch<void>(`/camps/participations/${participationId}`, { id: participationId, status, notes: notes ?? null }),
}
