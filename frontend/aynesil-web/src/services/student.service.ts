/**
 * Student, Guardian, and Case Management API service.
 * Wraps all /api/students and /api/guardians endpoints.
 * Uses existing apiService (Axios + JWT + refresh logic).
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  StudentDto,
  StudentListItemDto,
  StudentSummaryDto,
  StudentGuardianDto,
  StudentStatusHistoryDto,
  StudentCampusDto,
  DiagnosisDto,
  DevelopmentalProfileDto,
  CaseNoteDto,
  MedicalReportDto,
  DevelopmentReportDto,
  ExternalInstitutionReportDto,
  EmergencyContactDto,
  GuardianDto,
  GuardianListItemDto,
  GuardianPortalAccessDto,
  StudentListQuery,
  GuardianListQuery,
  CaseNoteListQuery,
  CreateStudentPayload,
  UpdateStudentPayload,
  ChangeStudentStatusPayload,
  EnrollAtCampusPayload,
  TransferStudentPayload,
  LinkGuardianPayload,
  UpdateGuardianLinkPayload,
  ReplaceEmergencyContactsPayload,
  UpsertDevProfilePayload,
  AddDiagnosisPayload,
  UpdateDiagnosisPayload,
  AddMedicalReportPayload,
  UpdateMedicalReportPayload,
  AddDevelopmentReportPayload,
  UpdateDevelopmentReportPayload,
  AddExternalReportPayload,
  AddCaseNotePayload,
  UpdateCaseNotePayload,
  CreateGuardianPayload,
  UpdateGuardianPayload,
  GrantPortalAccessPayload,
} from '@/types/student.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const studentService = {
  // ── Student Queries ─────────────────────────────────────────────────────────

  listStudents: (query: StudentListQuery) =>
    apiService.get<PaginatedResult<StudentListItemDto>>(
      `/students${qs(query as Record<string, unknown>)}`
    ),

  getStudent: (id: string) =>
    apiService.get<StudentDto>(`/students/${id}`),

  getSummary: (id: string) =>
    apiService.get<StudentSummaryDto>(`/students/${id}/summary`),

  getGuardians: (id: string) =>
    apiService.get<StudentGuardianDto[]>(`/students/${id}/guardians`),

  getStatusHistory: (id: string) =>
    apiService.get<StudentStatusHistoryDto[]>(`/students/${id}/status-history`),

  getCampuses: (id: string, activeOnly = false) =>
    apiService.get<StudentCampusDto[]>(`/students/${id}/campuses?activeOnly=${activeOnly}`),

  getDiagnoses: (id: string) =>
    apiService.get<DiagnosisDto[]>(`/students/${id}/diagnoses`),

  getDevelopmentalProfiles: (id: string) =>
    apiService.get<DevelopmentalProfileDto[]>(`/students/${id}/developmental-profiles`),

  getCaseNotes: (query: CaseNoteListQuery) =>
    apiService.get<PaginatedResult<CaseNoteDto>>(
      `/students/${query.studentId}/case-notes${qs({
        includeConfidential: query.includeConfidential,
        noteType: query.noteType,
        page: query.page,
        pageSize: query.pageSize,
      })}`
    ),

  getMedicalReports: (id: string) =>
    apiService.get<MedicalReportDto[]>(`/students/${id}/medical-reports`),

  getDevelopmentReports: (id: string) =>
    apiService.get<DevelopmentReportDto[]>(`/students/${id}/development-reports`),

  getExternalReports: (id: string) =>
    apiService.get<ExternalInstitutionReportDto[]>(`/students/${id}/external-reports`),

  // ── Student Commands ────────────────────────────────────────────────────────

  createStudent: (payload: CreateStudentPayload) =>
    apiService.post<StudentDto>('/students', payload),

  updateStudent: (id: string, payload: UpdateStudentPayload) =>
    apiService.put<StudentDto>(`/students/${id}`, payload),

  deleteStudent: (id: string) =>
    apiService.delete(`/students/${id}`),

  changeStatus: (id: string, payload: ChangeStudentStatusPayload) =>
    apiService.post<StudentDto>(`/students/${id}/status`, payload),

  // ── Campus Commands ─────────────────────────────────────────────────────────

  enrollAtCampus: (id: string, payload: EnrollAtCampusPayload) =>
    apiService.post<StudentCampusDto>(`/students/${id}/campuses`, payload),

  transferStudent: (id: string, payload: TransferStudentPayload) =>
    apiService.post<StudentDto>(`/students/${id}/transfer`, payload),

  endCampusEnrollment: (id: string, enrollmentId: string, endDate: string | null) =>
    apiService.patch<StudentCampusDto>(`/students/${id}/campuses/${enrollmentId}/end`, { endDate }),

  // ── Guardian Link Commands ──────────────────────────────────────────────────

  linkGuardian: (id: string, payload: LinkGuardianPayload) =>
    apiService.post<StudentGuardianDto>(`/students/${id}/guardians`, payload),

  updateGuardianLink: (id: string, linkId: string, payload: UpdateGuardianLinkPayload) =>
    apiService.put<StudentGuardianDto>(`/students/${id}/guardians/${linkId}`, payload),

  unlinkGuardian: (id: string, linkId: string) =>
    apiService.delete(`/students/${id}/guardians/${linkId}`),

  replaceEmergencyContacts: (id: string, payload: ReplaceEmergencyContactsPayload) =>
    apiService.put<EmergencyContactDto[]>(`/students/${id}/emergency-contacts`, payload),

  // ── Developmental Profile ───────────────────────────────────────────────────

  upsertDevelopmentalProfile: (id: string, payload: UpsertDevProfilePayload) =>
    apiService.put<DevelopmentalProfileDto>(`/students/${id}/developmental-profiles`, payload),

  // ── Diagnosis Commands ──────────────────────────────────────────────────────

  addDiagnosis: (id: string, payload: AddDiagnosisPayload) =>
    apiService.post<DiagnosisDto>(`/students/${id}/diagnoses`, payload),

  updateDiagnosis: (id: string, diagnosisId: string, payload: UpdateDiagnosisPayload) =>
    apiService.put<DiagnosisDto>(`/students/${id}/diagnoses/${diagnosisId}`, payload),

  deleteDiagnosis: (id: string, diagnosisId: string) =>
    apiService.delete(`/students/${id}/diagnoses/${diagnosisId}`),

  // ── Medical Report Commands ─────────────────────────────────────────────────

  addMedicalReport: (id: string, payload: AddMedicalReportPayload) =>
    apiService.post<MedicalReportDto>(`/students/${id}/medical-reports`, payload),

  updateMedicalReport: (id: string, reportId: string, payload: UpdateMedicalReportPayload) =>
    apiService.put<MedicalReportDto>(`/students/${id}/medical-reports/${reportId}`, payload),

  deleteMedicalReport: (id: string, reportId: string) =>
    apiService.delete(`/students/${id}/medical-reports/${reportId}`),

  // ── Development Report Commands ─────────────────────────────────────────────

  addDevelopmentReport: (id: string, payload: AddDevelopmentReportPayload) =>
    apiService.post<DevelopmentReportDto>(`/students/${id}/development-reports`, payload),

  updateDevelopmentReport: (id: string, reportId: string, payload: UpdateDevelopmentReportPayload) =>
    apiService.put<DevelopmentReportDto>(`/students/${id}/development-reports/${reportId}`, payload),

  deleteDevelopmentReport: (id: string, reportId: string) =>
    apiService.delete(`/students/${id}/development-reports/${reportId}`),

  // ── External Report Commands ────────────────────────────────────────────────

  addExternalReport: (id: string, payload: AddExternalReportPayload) =>
    apiService.post<ExternalInstitutionReportDto>(`/students/${id}/external-reports`, payload),

  deleteExternalReport: (id: string, reportId: string) =>
    apiService.delete(`/students/${id}/external-reports/${reportId}`),

  // ── Case Note Commands ──────────────────────────────────────────────────────

  addCaseNote: (id: string, payload: AddCaseNotePayload) =>
    apiService.post<CaseNoteDto>(`/students/${id}/case-notes`, payload),

  updateCaseNote: (id: string, noteId: string, payload: UpdateCaseNotePayload) =>
    apiService.put<CaseNoteDto>(`/students/${id}/case-notes/${noteId}`, payload),

  deleteCaseNote: (id: string, noteId: string) =>
    apiService.delete(`/students/${id}/case-notes/${noteId}`),
}

export const guardianService = {
  // ── Guardian Queries ────────────────────────────────────────────────────────

  listGuardians: (query: GuardianListQuery) =>
    apiService.get<PaginatedResult<GuardianListItemDto>>(
      `/guardians${qs(query as Record<string, unknown>)}`
    ),

  getGuardian: (id: string) =>
    apiService.get<GuardianDto>(`/guardians/${id}`),

  // ── Guardian Commands ───────────────────────────────────────────────────────

  createGuardian: (payload: CreateGuardianPayload) =>
    apiService.post<GuardianDto>('/guardians', payload),

  updateGuardian: (id: string, payload: UpdateGuardianPayload) =>
    apiService.put<GuardianDto>(`/guardians/${id}`, payload),

  deleteGuardian: (id: string) =>
    apiService.delete(`/guardians/${id}`),

  // ── Portal Access Commands ──────────────────────────────────────────────────

  grantPortalAccess: (guardianId: string, studentId: string, payload: GrantPortalAccessPayload) =>
    apiService.post<GuardianPortalAccessDto>(
      `/guardians/${guardianId}/students/${studentId}/portal-access`, payload
    ),

  revokePortalAccess: (guardianId: string, studentId: string) =>
    apiService.delete(`/guardians/${guardianId}/students/${studentId}/portal-access`),

  updatePortalPermissions: (guardianId: string, studentId: string, payload: GrantPortalAccessPayload) =>
    apiService.put<GuardianPortalAccessDto>(
      `/guardians/${guardianId}/students/${studentId}/portal-access`, payload
    ),
}
