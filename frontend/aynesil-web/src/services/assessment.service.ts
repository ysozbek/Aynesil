/**
 * Assessment & Evaluation API service.
 * Wraps all /api/assessment-templates and /api/assessment-sessions endpoints.
 * Uses existing apiService (Axios + JWT + refresh logic).
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  AssessmentTemplateDto,
  AssessmentTemplateListItemDto,
  AssessmentSectionDto,
  AssessmentItemDto,
  AssessmentSessionDto,
  AssessmentSessionListItemDto,
  AssessmentResponseDto,
  AssessmentReportDto,
  ProgramRecommendationDto,
  CreateTemplatePayload,
  UpdateTemplatePayload,
  SetTemplateActivePayload,
  UpsertTranslationPayload,
  AddSectionPayload,
  UpdateSectionPayload,
  AddItemPayload,
  UpdateItemPayload,
  CreateSessionPayload,
  UpdateSessionPayload,
  SubmitResponsesPayload,
  CreateReportPayload,
  UpdateReportPayload,
  FinalizeReportPayload,
  CreateRecommendationPayload,
  UpdateRecommendationPayload,
  TemplateListQuery,
  SessionListQuery,
} from '@/types/assessment.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const assessmentService = {
  // ── Template Queries ────────────────────────────────────────────────────────

  listTemplates: (query: TemplateListQuery) =>
    apiService.get<PaginatedResult<AssessmentTemplateListItemDto>>(
      `/assessment-templates${qs(query as Record<string, unknown>)}`
    ),

  getTemplate: (id: string) =>
    apiService.get<AssessmentTemplateDto>(`/assessment-templates/${id}`),

  // ── Template Commands ───────────────────────────────────────────────────────

  createTemplate: (payload: CreateTemplatePayload) =>
    apiService.post<AssessmentTemplateDto>('/assessment-templates', payload),

  updateTemplate: (id: string, payload: UpdateTemplatePayload) =>
    apiService.put<AssessmentTemplateDto>(`/assessment-templates/${id}`, payload),

  setTemplateActive: (id: string, payload: SetTemplateActivePayload) =>
    apiService.patch<AssessmentTemplateDto>(`/assessment-templates/${id}/active`, payload),

  createTemplateVersion: (id: string) =>
    apiService.post<AssessmentTemplateDto>(`/assessment-templates/${id}/version`),

  upsertTranslation: (id: string, locale: string, payload: UpsertTranslationPayload) =>
    apiService.put(`/assessment-templates/${id}/translations/${locale}`, payload),

  // ── Section Commands ────────────────────────────────────────────────────────

  addSection: (templateId: string, payload: AddSectionPayload) =>
    apiService.post<AssessmentTemplateDto>(`/assessment-templates/${templateId}/sections`, payload),

  updateSection: (sectionId: string, payload: UpdateSectionPayload) =>
    apiService.put<AssessmentSectionDto>(`/assessment-templates/sections/${sectionId}`, payload),

  deleteSection: (sectionId: string) =>
    apiService.delete(`/assessment-templates/sections/${sectionId}`),

  // ── Item Commands ───────────────────────────────────────────────────────────

  addItem: (sectionId: string, payload: AddItemPayload) =>
    apiService.post<AssessmentItemDto>(`/assessment-templates/sections/${sectionId}/items`, payload),

  updateItem: (itemId: string, payload: UpdateItemPayload) =>
    apiService.put<AssessmentItemDto>(`/assessment-templates/items/${itemId}`, payload),

  deleteItem: (itemId: string) =>
    apiService.delete(`/assessment-templates/items/${itemId}`),

  // ── Session Queries ─────────────────────────────────────────────────────────

  listSessions: (query: SessionListQuery) =>
    apiService.get<PaginatedResult<AssessmentSessionListItemDto>>(
      `/assessment-sessions${qs(query as Record<string, unknown>)}`
    ),

  getSession: (id: string) =>
    apiService.get<AssessmentSessionDto>(`/assessment-sessions/${id}`),

  getHistory: (leadId?: string, studentId?: string) =>
    apiService.get<AssessmentSessionListItemDto[]>(
      `/assessment-sessions/history${qs({ leadId, studentId })}`
    ),

  // ── Session Commands ────────────────────────────────────────────────────────

  createSession: (payload: CreateSessionPayload) =>
    apiService.post<AssessmentSessionDto>('/assessment-sessions', payload),

  updateSession: (id: string, payload: UpdateSessionPayload) =>
    apiService.put<AssessmentSessionDto>(`/assessment-sessions/${id}`, payload),

  deleteSession: (id: string) =>
    apiService.delete(`/assessment-sessions/${id}`),

  startSession: (id: string, rowVersion: number) =>
    apiService.post<AssessmentSessionDto>(`/assessment-sessions/${id}/start`, { rowVersion }),

  completeSession: (id: string, rowVersion: number) =>
    apiService.post<AssessmentSessionDto>(`/assessment-sessions/${id}/complete`, { rowVersion }),

  cancelSession: (id: string, rowVersion: number) =>
    apiService.post<AssessmentSessionDto>(`/assessment-sessions/${id}/cancel`, { rowVersion }),

  // ── Responses ───────────────────────────────────────────────────────────────

  submitResponses: (sessionId: string, payload: SubmitResponsesPayload) =>
    apiService.post<AssessmentResponseDto[]>(`/assessment-sessions/${sessionId}/responses`, payload),

  // ── Report ──────────────────────────────────────────────────────────────────

  getReport: (sessionId: string) =>
    apiService.get<AssessmentReportDto>(`/assessment-sessions/${sessionId}/report`),

  createReport: (sessionId: string, payload: CreateReportPayload) =>
    apiService.post<AssessmentReportDto>(`/assessment-sessions/${sessionId}/report`, payload),

  updateReport: (sessionId: string, payload: UpdateReportPayload) =>
    apiService.put<AssessmentReportDto>(`/assessment-sessions/${sessionId}/report`, payload),

  finalizeReport: (sessionId: string, payload: FinalizeReportPayload) =>
    apiService.post<AssessmentReportDto>(`/assessment-sessions/${sessionId}/report/finalize`, payload),

  // ── Recommendations ─────────────────────────────────────────────────────────

  getRecommendations: (sessionId: string) =>
    apiService.get<ProgramRecommendationDto[]>(`/assessment-sessions/${sessionId}/recommendations`),

  createRecommendation: (sessionId: string, payload: CreateRecommendationPayload) =>
    apiService.post<ProgramRecommendationDto>(`/assessment-sessions/${sessionId}/recommendations`, payload),

  updateRecommendation: (sessionId: string, recommendationId: string, payload: UpdateRecommendationPayload) =>
    apiService.put<ProgramRecommendationDto>(
      `/assessment-sessions/${sessionId}/recommendations/${recommendationId}`,
      payload
    ),
}
