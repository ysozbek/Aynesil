/**
 * Goal Library, Goal Template, Student Goal, Progress, and Analytics API service.
 * Wraps all /api/goals endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  GoalLibraryDto,
  GoalLibraryListItemDto,
  GoalTemplateDto,
  GoalTemplateListItemDto,
  GoalTemplateTranslationDto,
  StudentGoalDto,
  StudentGoalListItemDto,
  GoalProgressDto,
  GoalTrendDto,
  StudentGoalSummaryDto,
  GoalSuccessRateDto,
  GoalLibraryListQuery,
  GoalTemplateListQuery,
  StudentGoalListQuery,
  GoalProgressQuery,
  SuccessRatesQuery,
  CreateGoalLibraryPayload,
  UpdateGoalLibraryPayload,
  CreateGoalTemplatePayload,
  UpdateGoalTemplatePayload,
  SetGoalTemplateTranslationPayload,
  CreateStudentGoalPayload,
  UpdateStudentGoalPayload,
  ChangeGoalStatusPayload,
  RecordProgressPayload,
} from '@/types/goal.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const goalService = {
  // ── Goal Libraries ──────────────────────────────────────────────────────────

  listLibraries: (query: GoalLibraryListQuery) =>
    apiService.get<PaginatedResult<GoalLibraryListItemDto>>(
      `/goals/libraries${qs(query as Record<string, unknown>)}`
    ),

  getLibrary: (id: string) =>
    apiService.get<GoalLibraryDto>(`/goals/libraries/${id}`),

  createLibrary: (payload: CreateGoalLibraryPayload) =>
    apiService.post<GoalLibraryDto>('/goals/libraries', payload),

  updateLibrary: (id: string, payload: UpdateGoalLibraryPayload) =>
    apiService.put<GoalLibraryDto>(`/goals/libraries/${id}`, payload),

  deleteLibrary: (id: string) =>
    apiService.delete(`/goals/libraries/${id}`),

  // ── Goal Templates ──────────────────────────────────────────────────────────

  listTemplates: (query: GoalTemplateListQuery) =>
    apiService.get<PaginatedResult<GoalTemplateListItemDto>>(
      `/goals/templates${qs(query as Record<string, unknown>)}`
    ),

  getTemplate: (id: string) =>
    apiService.get<GoalTemplateDto>(`/goals/templates/${id}`),

  createTemplate: (payload: CreateGoalTemplatePayload) =>
    apiService.post<GoalTemplateDto>('/goals/templates', payload),

  updateTemplate: (id: string, payload: UpdateGoalTemplatePayload) =>
    apiService.put<GoalTemplateDto>(`/goals/templates/${id}`, payload),

  setTemplateTranslation: (id: string, locale: string, payload: SetGoalTemplateTranslationPayload) =>
    apiService.put<GoalTemplateTranslationDto>(`/goals/templates/${id}/translations/${locale}`, payload),

  deleteTemplate: (id: string) =>
    apiService.delete(`/goals/templates/${id}`),

  // ── Student Goals ───────────────────────────────────────────────────────────

  listStudentGoals: (query: StudentGoalListQuery) =>
    apiService.get<PaginatedResult<StudentGoalListItemDto>>(
      `/goals/student-goals${qs(query as Record<string, unknown>)}`
    ),

  getStudentGoal: (id: string) =>
    apiService.get<StudentGoalDto>(`/goals/student-goals/${id}`),

  createStudentGoal: (payload: CreateStudentGoalPayload) =>
    apiService.post<StudentGoalDto>('/goals/student-goals', payload),

  updateStudentGoal: (id: string, payload: UpdateStudentGoalPayload) =>
    apiService.put<StudentGoalDto>(`/goals/student-goals/${id}`, payload),

  changeGoalStatus: (id: string, payload: ChangeGoalStatusPayload) =>
    apiService.post<StudentGoalDto>(`/goals/student-goals/${id}/status`, payload),

  deleteStudentGoal: (id: string) =>
    apiService.delete(`/goals/student-goals/${id}`),

  // ── Goal Progress ───────────────────────────────────────────────────────────

  getProgress: (goalId: string, query?: GoalProgressQuery) =>
    apiService.get<GoalProgressDto[]>(
      `/goals/student-goals/${goalId}/progress${qs((query ?? {}) as Record<string, unknown>)}`
    ),

  getTrend: (goalId: string) =>
    apiService.get<GoalTrendDto>(`/goals/student-goals/${goalId}/trend`),

  recordProgress: (goalId: string, payload: RecordProgressPayload) =>
    apiService.post<GoalProgressDto>(`/goals/student-goals/${goalId}/progress`, payload),

  // ── Analytics ───────────────────────────────────────────────────────────────

  getStudentSummary: (corporationId: string, studentId: string) =>
    apiService.get<StudentGoalSummaryDto>(
      `/goals/analytics/student-summary?corporationId=${corporationId}&studentId=${studentId}`
    ),

  getSuccessRates: (query: SuccessRatesQuery) =>
    apiService.get<GoalSuccessRateDto[]>(
      `/goals/analytics/success-rates${qs(query as Record<string, unknown>)}`
    ),
}
