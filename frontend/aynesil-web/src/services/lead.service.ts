/**
 * CRM / Lead Management API service.
 * Wraps all /api/leads and /api/interviews endpoints.
 * Uses existing apiService (Axios + JWT + refresh logic).
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  LeadDto,
  LeadListItemDto,
  LeadActivityDto,
  LeadStatusHistoryDto,
  InterviewDto,
  PipelineSummaryDto,
  ConversionReportDto,
  CreateLeadPayload,
  UpdateLeadPayload,
  ChangeLeadStatusPayload,
  AssignLeadPayload,
  ConvertLeadPayload,
  LogActivityPayload,
  ScheduleInterviewPayload,
  CompleteInterviewPayload,
  RescheduleInterviewPayload,
  LeadListQuery,
  FollowUpsQuery,
  ConversionReportQuery,
} from '@/types/crm.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const leadService = {
  // ── Lead Queries ────────────────────────────────────────────────────────────

  list: (query: LeadListQuery) =>
    apiService.get<PaginatedResult<LeadListItemDto>>(`/leads${qs(query as Record<string, unknown>)}`),

  getById: (id: string) =>
    apiService.get<LeadDto>(`/leads/${id}`),

  getPipelineSummary: (corporationId: string, campusId?: string) =>
    apiService.get<PipelineSummaryDto>(`/leads/pipeline${qs({ corporationId, campusId })}`),

  getFollowUpsDue: (query: FollowUpsQuery) =>
    apiService.get<PaginatedResult<LeadActivityDto>>(`/leads/followups${qs(query as Record<string, unknown>)}`),

  getConversionReport: (query: ConversionReportQuery) =>
    apiService.get<ConversionReportDto>(`/leads/reports/conversion${qs(query as Record<string, unknown>)}`),

  getStatusHistory: (leadId: string) =>
    apiService.get<LeadStatusHistoryDto[]>(`/leads/${leadId}/history`),

  getActivities: (leadId: string, page = 1, pageSize = 20) =>
    apiService.get<PaginatedResult<LeadActivityDto>>(`/leads/${leadId}/activities${qs({ page, pageSize })}`),

  getInterviews: (leadId: string) =>
    apiService.get<InterviewDto[]>(`/leads/${leadId}/interviews`),

  // ── Lead Commands ───────────────────────────────────────────────────────────

  create: (payload: CreateLeadPayload) =>
    apiService.post<LeadDto>('/leads', payload),

  update: (id: string, payload: UpdateLeadPayload) =>
    apiService.put<LeadDto>(`/leads/${id}`, payload),

  delete: (id: string) =>
    apiService.delete(`/leads/${id}`),

  changeStatus: (id: string, payload: ChangeLeadStatusPayload) =>
    apiService.post<LeadDto>(`/leads/${id}/status`, payload),

  assign: (id: string, payload: AssignLeadPayload) =>
    apiService.post<LeadDto>(`/leads/${id}/assign`, payload),

  convert: (id: string, payload: ConvertLeadPayload) =>
    apiService.post<LeadDto>(`/leads/${id}/convert`, payload),

  logActivity: (leadId: string, payload: LogActivityPayload) =>
    apiService.post<LeadActivityDto>(`/leads/${leadId}/activities`, payload),

  scheduleInterview: (leadId: string, payload: ScheduleInterviewPayload) =>
    apiService.post<InterviewDto>(`/leads/${leadId}/interviews`, payload),

  // ── Interview Commands ──────────────────────────────────────────────────────

  completeInterview: (id: string, payload: CompleteInterviewPayload) =>
    apiService.post<InterviewDto>(`/interviews/${id}/complete`, payload),

  cancelInterview: (id: string, rowVersion: number) =>
    apiService.post(`/interviews/${id}/cancel`, { rowVersion }),

  rescheduleInterview: (id: string, payload: RescheduleInterviewPayload) =>
    apiService.post<InterviewDto>(`/interviews/${id}/reschedule`, payload),

  markInterviewNoShow: (id: string, rowVersion: number) =>
    apiService.post(`/interviews/${id}/no-show`, { rowVersion }),
}
