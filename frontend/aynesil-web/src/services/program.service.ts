/**
 * Program, Enrollment, and Student-Program API service.
 * Wraps /api/programs, /api/enrollments, /api/student-programs endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  ProgramDto,
  ProgramListItemDto,
  ProgramServiceDto,
  ProgramTranslationDto,
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

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const programService = {
  // ── Program Queries ─────────────────────────────────────────────────────────

  listPrograms: (query: ProgramListQuery) =>
    apiService.get<PaginatedResult<ProgramListItemDto>>(
      `/programs${qs(query as Record<string, unknown>)}`
    ),

  getProgram: (id: string) =>
    apiService.get<ProgramDto>(`/programs/${id}`),

  // ── Program CRUD ────────────────────────────────────────────────────────────

  createProgram: (payload: CreateProgramPayload) =>
    apiService.post<ProgramDto>('/programs', payload),

  updateProgram: (id: string, payload: UpdateProgramPayload) =>
    apiService.put<ProgramDto>(`/programs/${id}`, payload),

  deleteProgram: (id: string) =>
    apiService.delete(`/programs/${id}`),

  setTranslation: (id: string, locale: string, payload: SetTranslationPayload) =>
    apiService.put<ProgramTranslationDto>(`/programs/${id}/translations/${locale}`, payload),

  // ── Program Service Commands ────────────────────────────────────────────────

  addService: (id: string, payload: AddProgramServicePayload) =>
    apiService.post<ProgramServiceDto>(`/programs/${id}/services`, payload),

  updateService: (id: string, serviceId: string, payload: UpdateProgramServicePayload) =>
    apiService.put<ProgramServiceDto>(`/programs/${id}/services/${serviceId}`, payload),

  deleteService: (id: string, serviceId: string) =>
    apiService.delete(`/programs/${id}/services/${serviceId}`),
}

export const enrollmentService = {
  // ── Enrollment Queries ──────────────────────────────────────────────────────

  listEnrollments: (query: EnrollmentListQuery) =>
    apiService.get<PaginatedResult<EnrollmentListItemDto>>(
      `/enrollments${qs(query as Record<string, unknown>)}`
    ),

  getEnrollment: (id: string) =>
    apiService.get<EnrollmentDto>(`/enrollments/${id}`),

  // ── Enrollment Commands ─────────────────────────────────────────────────────

  createEnrollment: (payload: CreateEnrollmentPayload) =>
    apiService.post<EnrollmentDto>('/enrollments', payload),

  changeStatus: (id: string, payload: ChangeEnrollmentStatusPayload) =>
    apiService.post<EnrollmentDto>(`/enrollments/${id}/status`, payload),

  endEnrollment: (id: string, payload: EndEnrollmentPayload) =>
    apiService.post<EnrollmentDto>(`/enrollments/${id}/end`, payload),

  // ── Student Program Assignment (nested under enrollment) ────────────────────

  assignProgram: (enrollmentId: string, payload: AssignStudentToProgramPayload) =>
    apiService.post<StudentProgramDto>(`/enrollments/${enrollmentId}/programs`, payload),

  updateStudentProgram: (enrollmentId: string, spId: string, payload: UpdateStudentProgramPayload) =>
    apiService.put<StudentProgramDto>(`/enrollments/${enrollmentId}/programs/${spId}`, payload),

  removeStudentProgram: (enrollmentId: string, spId: string) =>
    apiService.delete(`/enrollments/${enrollmentId}/programs/${spId}`),
}

export const studentProgramService = {
  // ── Student Programs Standalone ─────────────────────────────────────────────

  listStudentPrograms: (query: StudentProgramListQuery) =>
    apiService.get<PaginatedResult<StudentProgramListItemDto>>(
      `/student-programs${qs(query as Record<string, unknown>)}`
    ),

  getStudentProgram: (id: string) =>
    apiService.get<StudentProgramDto>(`/student-programs/${id}`),

  assignStudentToProgram: (payload: AssignStudentToProgramPayload) =>
    apiService.post<StudentProgramDto>('/student-programs', payload),
}
