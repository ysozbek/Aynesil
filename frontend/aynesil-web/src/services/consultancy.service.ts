/**
 * School Consultancy Management API service.
 * Wraps all /api/consultancy endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  InstitutionListItemDto, InstitutionDto,
  ConsultancyPlanListItemDto, ConsultancyPlanDto,
  SchoolVisitListItemDto, SchoolVisitDto,
  ConsultancyReportListItemDto, ConsultancyReportDto,
  ConsultancyAgreementListItemDto, ConsultancyAgreementDto,
  FollowUpActivityListItemDto, FollowUpActivityDto,
  AgreementSummaryDto, OpenFollowUpReportItemDto,
  InstitutionReportDto, ConsultancyOutcomesDto,
  InstitutionListQuery, PlanListQuery, VisitListQuery,
  AgreementListQuery, FollowUpListQuery,
  OpenFollowUpReportQuery, AgreementSummaryQuery,
  CreateInstitutionPayload, UpdateInstitutionPayload,
  CreatePlanPayload, CreateVisitPayload,
  AddObservationPayload,
  CreateAgreementPayload, UpdateAgreementPayload, SignAgreementPayload,
  CreateFollowUpPayload, UpdateFollowUpPayload,
  CompleteFollowUpPayload,
} from '@/types/consultancy.types'

function qs(params: object): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const consultancyService = {
  // ── Institutions ───────────────────────────────────────────────────────────
  listInstitutions: (query: InstitutionListQuery) =>
    apiService.get<PaginatedResult<InstitutionListItemDto>>(
      `/consultancy/institutions${qs(query)}`
    ),

  getInstitution: (id: string) =>
    apiService.get<InstitutionDto>(`/consultancy/institutions/${id}`),

  createInstitution: (payload: CreateInstitutionPayload) =>
    apiService.post<InstitutionDto>('/consultancy/institutions', payload),

  updateInstitution: (id: string, payload: UpdateInstitutionPayload) =>
    apiService.put<InstitutionDto>(`/consultancy/institutions/${id}`, payload),

  // ── Plans ──────────────────────────────────────────────────────────────────
  listPlans: (query: PlanListQuery) =>
    apiService.get<PaginatedResult<ConsultancyPlanListItemDto>>(
      `/consultancy/plans${qs(query)}`
    ),

  getPlan: (id: string) =>
    apiService.get<ConsultancyPlanDto>(`/consultancy/plans/${id}`),

  createPlan: (payload: CreatePlanPayload) =>
    apiService.post<ConsultancyPlanDto>('/consultancy/plans', payload),

  updatePlan: (id: string, payload: Partial<CreatePlanPayload> & { rowVersion: number }) =>
    // Backend returns 204 NoContent (no body) — callers should refetch the plan.
    apiService.put<null>(`/consultancy/plans/${id}`, payload),

  activatePlan: (id: string) =>
    apiService.post(`/consultancy/plans/${id}/activate`),

  completePlan: (id: string) =>
    apiService.post(`/consultancy/plans/${id}/complete`),

  cancelPlan: (id: string) =>
    apiService.post(`/consultancy/plans/${id}/cancel`),

  // ── Visits ─────────────────────────────────────────────────────────────────
  listVisits: (query: VisitListQuery) =>
    apiService.get<PaginatedResult<SchoolVisitListItemDto>>(
      `/consultancy/visits${qs(query)}`
    ),

  getVisit: (id: string) =>
    apiService.get<SchoolVisitDto>(`/consultancy/visits/${id}`),

  createVisit: (payload: CreateVisitPayload) =>
    apiService.post<SchoolVisitDto>('/consultancy/visits', payload),

  completeVisit: (id: string) =>
    apiService.post(`/consultancy/visits/${id}/complete`),

  cancelVisit: (id: string) =>
    apiService.post(`/consultancy/visits/${id}/cancel`),

  // ── Observations ───────────────────────────────────────────────────────────
  addObservation: (visitId: string, payload: AddObservationPayload) =>
    apiService.post(`/consultancy/visits/${visitId}/observations`, payload),

  // ── Reports ────────────────────────────────────────────────────────────────
  listReports: (query: { consultancyPlanId?: string } & Record<string, unknown>) =>
    apiService.get<PaginatedResult<ConsultancyReportListItemDto>>(
      `/consultancy/reports${qs(query)}`
    ),

  getReport: (id: string) =>
    apiService.get<ConsultancyReportDto>(`/consultancy/reports/${id}`),

  createReport: (payload: Partial<ConsultancyReportDto>) =>
    apiService.post<ConsultancyReportDto>('/consultancy/reports', payload),

  // ── Agreements ─────────────────────────────────────────────────────────────
  listAgreements: (query: AgreementListQuery) =>
    apiService.get<PaginatedResult<ConsultancyAgreementListItemDto>>(
      `/consultancy/agreements${qs(query)}`
    ),

  getAgreement: (id: string) =>
    apiService.get<ConsultancyAgreementDto>(`/consultancy/agreements/${id}`),

  createAgreement: (payload: CreateAgreementPayload) =>
    apiService.post<ConsultancyAgreementDto>('/consultancy/agreements', payload),

  updateAgreement: (id: string, payload: UpdateAgreementPayload) =>
    apiService.put<ConsultancyAgreementDto>(`/consultancy/agreements/${id}`, payload),

  sendAgreement: (id: string) =>
    apiService.post(`/consultancy/agreements/${id}/send`),

  signAgreement: (id: string, payload: SignAgreementPayload) =>
    apiService.post(`/consultancy/agreements/${id}/sign`, payload),

  expireAgreement: (id: string) =>
    apiService.post(`/consultancy/agreements/${id}/expire`),

  cancelAgreement: (id: string) =>
    apiService.post(`/consultancy/agreements/${id}/cancel`),

  deleteAgreement: (id: string) =>
    apiService.delete(`/consultancy/agreements/${id}`),

  getAgreementSummary: (query: AgreementSummaryQuery) =>
    apiService.get<AgreementSummaryDto[]>(
      `/consultancy/reporting/agreements${qs(query)}`
    ),

  // ── Follow-Ups ─────────────────────────────────────────────────────────────
  listFollowUps: (query: FollowUpListQuery) =>
    apiService.get<PaginatedResult<FollowUpActivityListItemDto>>(
      `/consultancy/follow-ups${qs(query)}`
    ),

  getFollowUp: (id: string) =>
    apiService.get<FollowUpActivityDto>(`/consultancy/follow-ups/${id}`),

  createFollowUp: (payload: CreateFollowUpPayload) =>
    apiService.post<FollowUpActivityDto>('/consultancy/follow-ups', payload),

  updateFollowUp: (id: string, payload: UpdateFollowUpPayload) =>
    apiService.put<FollowUpActivityDto>(`/consultancy/follow-ups/${id}`, payload),

  startFollowUp: (id: string) =>
    apiService.post(`/consultancy/follow-ups/${id}/start`),

  completeFollowUp: (id: string, payload: CompleteFollowUpPayload) =>
    apiService.post(`/consultancy/follow-ups/${id}/complete`, payload),

  cancelFollowUp: (id: string) =>
    apiService.post(`/consultancy/follow-ups/${id}/cancel`),

  deleteFollowUp: (id: string) =>
    apiService.delete(`/consultancy/follow-ups/${id}`),

  getOpenFollowUpsReport: (query: OpenFollowUpReportQuery) =>
    apiService.get<OpenFollowUpReportItemDto[]>(
      `/consultancy/reporting/follow-ups/open${qs(query)}`
    ),

  // ── Analytics ─────────────────────────────────────────────────────────────
  // URL corrected: /consultancy/reporting/* (not /consultancy/reports/*)
  getInstitutionReport: (query: { institutionTypeId?: string }) =>
    apiService.get<InstitutionReportDto[]>(
      `/consultancy/reporting/institutions${qs(query)}`
    ),

  getOutcomes: (query: { institutionId?: string; status?: string }) =>
    apiService.get<ConsultancyOutcomesDto[]>(
      `/consultancy/reporting/outcomes${qs(query)}`
    ),

  getVisitHistory: (query: { institutionId?: string; from?: string; to?: string }) =>
    apiService.get<unknown[]>(
      `/consultancy/reporting/visit-history${qs(query)}`
    ),
}
