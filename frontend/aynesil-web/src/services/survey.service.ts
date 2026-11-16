/**
 * Survey & parent-feedback API service.
 * Wraps all /api/surveys endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  SurveyListItemDto, SurveyDto, SurveyListQuery, CreateSurveyPayload, UpdateSurveyPayload,
  SurveyResponseListItemDto, SurveyResponseListQuery, SurveyResultsDto,
  SubmitSurveyResponsePayload, PendingSurveyDto,
  ParentFeedbackListItemDto, ParentFeedbackListQuery, CreateParentFeedbackPayload,
} from '@/types/survey.types'

function qs(params: object): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const surveyService = {
  list: (query: SurveyListQuery) =>
    apiService.get<PaginatedResult<SurveyListItemDto>>(`/surveys${qs(query)}`),

  get: (id: string) => apiService.get<SurveyDto>(`/surveys/${id}`),

  /** Yeni anketin kimliğini döndürür. */
  create: (payload: CreateSurveyPayload) => apiService.post<string>('/surveys', payload),

  update: (id: string, payload: UpdateSurveyPayload) => apiService.put<void>(`/surveys/${id}`, payload),

  remove: (id: string) => apiService.delete<void>(`/surveys/${id}`),

  results: (id: string) => apiService.get<SurveyResultsDto>(`/surveys/${id}/results`),

  listResponses: (query: SurveyResponseListQuery) =>
    apiService.get<PaginatedResult<SurveyResponseListItemDto>>(`/surveys/responses${qs(query)}`),

  submitResponse: (payload: SubmitSurveyResponsePayload) =>
    apiService.post<string>('/surveys/responses', payload),

  pending: () => apiService.get<PendingSurveyDto[]>('/surveys/pending'),

  listFeedback: (query: ParentFeedbackListQuery) =>
    apiService.get<PaginatedResult<ParentFeedbackListItemDto>>(`/surveys/parent-feedback${qs(query)}`),

  myFeedback: (query: ParentFeedbackListQuery) =>
    apiService.get<PaginatedResult<ParentFeedbackListItemDto>>(`/surveys/parent-feedback/mine${qs(query)}`),

  createFeedback: (payload: CreateParentFeedbackPayload) =>
    apiService.post<ParentFeedbackListItemDto>('/surveys/parent-feedback', payload),
}
