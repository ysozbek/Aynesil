/**
 * BEP/IEP (Education Plan) API service.
 * Wraps all /api/education-plans endpoints including academic periods.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  AcademicPeriodDto,
  AcademicPeriodListItemDto,
  EducationPlanDto,
  EducationPlanListItemDto,
  StudentGoalSummaryReportDto,
  TrendReportRowDto,
  AcademicPeriodListQuery,
  EducationPlanListQuery,
  CreateAcademicPeriodPayload,
  UpdateAcademicPeriodPayload,
  CreateEducationPlanPayload,
  UpdateEducationPlanPayload,
  ApproveRejectPayload,
  RevisePayload,
  GuardianVisibilityPayload,
  AddGoalToPlanPayload,
  ReorderGoalsPayload,
  AddPlanReviewPayload,
} from '@/types/bep.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const bepService = {
  // ── Academic Periods ────────────────────────────────────────────────────────

  listAcademicPeriods: (query: AcademicPeriodListQuery) =>
    apiService.get<PaginatedResult<AcademicPeriodListItemDto>>(
      `/education-plans/academic-periods${qs(query as Record<string, unknown>)}`
    ),

  getAcademicPeriod: (id: string) =>
    apiService.get<AcademicPeriodDto>(`/education-plans/academic-periods/${id}`),

  createAcademicPeriod: (payload: CreateAcademicPeriodPayload) =>
    apiService.post<AcademicPeriodDto>('/education-plans/academic-periods', payload),

  updateAcademicPeriod: (id: string, payload: UpdateAcademicPeriodPayload) =>
    apiService.put<AcademicPeriodDto>(`/education-plans/academic-periods/${id}`, payload),

  setCurrentPeriod: (id: string) =>
    apiService.post<AcademicPeriodDto>(`/education-plans/academic-periods/${id}/set-current`),

  deleteAcademicPeriod: (id: string) =>
    apiService.delete(`/education-plans/academic-periods/${id}`),

  // ── Education Plans — Queries ───────────────────────────────────────────────

  listPlans: (query: EducationPlanListQuery) =>
    apiService.get<PaginatedResult<EducationPlanListItemDto>>(
      `/education-plans${qs(query as Record<string, unknown>)}`
    ),

  getPlan: (id: string) =>
    apiService.get<EducationPlanDto>(`/education-plans/${id}`),

  getGuardianVisiblePlan: (corporationId: string, studentId: string) =>
    apiService.get<EducationPlanDto>(
      `/education-plans/guardian-visible?corporationId=${corporationId}&studentId=${studentId}`
    ),

  // ── Education Plans — CRUD ──────────────────────────────────────────────────

  createPlan: (payload: CreateEducationPlanPayload) =>
    apiService.post<EducationPlanDto>('/education-plans', payload),

  updatePlan: (id: string, payload: UpdateEducationPlanPayload) =>
    apiService.put<EducationPlanDto>(`/education-plans/${id}`, payload),

  deletePlan: (id: string) =>
    apiService.delete(`/education-plans/${id}`),

  // ── Plan Workflow ───────────────────────────────────────────────────────────

  submitPlan: (id: string) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/submit`),

  approvePlan: (id: string, payload: ApproveRejectPayload) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/approve`, payload),

  rejectPlan: (id: string, payload: ApproveRejectPayload) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/reject`, payload),

  activatePlan: (id: string) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/activate`),

  closePlan: (id: string) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/close`),

  revisePlan: (id: string, payload: RevisePayload) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/revise`, payload),

  setGuardianVisibility: (id: string, payload: GuardianVisibilityPayload) =>
    apiService.patch<EducationPlanDto>(`/education-plans/${id}/guardian-visibility`, payload),

  // ── Plan Goals ──────────────────────────────────────────────────────────────

  addGoal: (id: string, payload: AddGoalToPlanPayload) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/goals`, payload),

  removeGoal: (id: string, planGoalId: string) =>
    apiService.delete<EducationPlanDto>(`/education-plans/${id}/goals/${planGoalId}`),

  reorderGoals: (id: string, payload: ReorderGoalsPayload) =>
    apiService.put<EducationPlanDto>(`/education-plans/${id}/goals/reorder`, payload),

  // ── Plan Reviews ────────────────────────────────────────────────────────────

  addReview: (id: string, payload: AddPlanReviewPayload) =>
    apiService.post<EducationPlanDto>(`/education-plans/${id}/reviews`, payload),

  // ── Reports ─────────────────────────────────────────────────────────────────

  getStudentGoalSummaryReport: (corporationId: string, studentId: string) =>
    apiService.get<StudentGoalSummaryReportDto>(
      `/education-plans/reports/student-summary?corporationId=${corporationId}&studentId=${studentId}`
    ),

  getTrendReport: (corporationId: string, studentId: string, from?: string, to?: string) =>
    apiService.get<TrendReportRowDto[]>(
      `/education-plans/reports/trend${qs({ corporationId, studentId, from, to })}`
    ),
}
