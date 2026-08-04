/**
 * School Consultancy Management type definitions.
 * Mirrors Aynesil.Application.Features.Consultancy.Dtos
 */
import type { PagedQuery } from './api.types'

// ── Institution DTOs ──────────────────────────────────────────────────────────

export interface InstitutionListItemDto {
  id: string
  corporationId: string
  institutionTypeId?: string
  institutionTypeCode?: string
  name: string
  city?: string
  district?: string
  planCount: number
  visitCount: number
  createdAt: string
}

export interface InstitutionDto {
  id: string
  corporationId: string
  institutionTypeId?: string
  institutionTypeCode?: string
  name: string
  city?: string
  district?: string
  contactName?: string
  contactPhone?: string
  contactEmail?: string
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Consultancy Plan DTOs ─────────────────────────────────────────────────────

export interface ConsultancyPlanListItemDto {
  id: string
  corporationId: string
  institutionId: string
  institutionName: string
  consultancyTypeId?: string
  consultancyTypeCode?: string
  name: string
  periodStart?: string
  periodEnd?: string
  status: string
  visitCount: number
  reportCount: number
  createdAt: string
}

export interface ConsultancyPlanDto {
  id: string
  corporationId: string
  institutionId: string
  institutionName: string
  consultancyTypeId?: string
  consultancyTypeCode?: string
  name: string
  periodStart?: string
  periodEnd?: string
  scope?: string
  leadEducatorId?: string
  status: string
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── School Visit DTOs ─────────────────────────────────────────────────────────

export interface ObservationRecordDto {
  id: string
  corporationId: string
  schoolVisitId: string
  observationTypeId?: string
  observationTypeCode?: string
  subject?: string
  observation: string
  recommendations?: string
  createdAt: string
  createdBy?: string
}

export interface SchoolVisitListItemDto {
  id: string
  corporationId: string
  consultancyPlanId?: string
  planName?: string
  institutionId: string
  institutionName: string
  visitDate: string
  visitorId?: string
  purpose?: string
  status: string
  observationCount: number
  createdAt: string
}

export interface SchoolVisitDto {
  id: string
  corporationId: string
  consultancyPlanId?: string
  planName?: string
  institutionId: string
  institutionName: string
  visitDate: string
  visitorId?: string
  purpose?: string
  status: string
  createdAt: string
  observations: ObservationRecordDto[]
}

// ── Report DTOs ───────────────────────────────────────────────────────────────

export interface ConsultancyReportListItemDto {
  id: string
  corporationId: string
  consultancyPlanId?: string
  planName?: string
  schoolVisitId?: string
  visitDate?: string
  title: string
  hasFile: boolean
  authoredBy?: string
  createdAt: string
}

export interface ConsultancyReportDto {
  id: string
  corporationId: string
  consultancyPlanId?: string
  planName?: string
  schoolVisitId?: string
  visitDate?: string
  title: string
  summary?: string
  fileId?: string
  authoredBy?: string
  createdAt: string
}

// ── Agreement DTOs ────────────────────────────────────────────────────────────

export interface ConsultancyAgreementListItemDto {
  id: string
  corporationId: string
  consultancyPlanId: string
  planName: string
  institutionId: string
  institutionName: string
  agreementTypeId?: string
  agreementTypeCode?: string
  title: string
  startDate?: string
  endDate?: string
  signedDate?: string
  status: string
  hasFile: boolean
  createdAt: string
  updatedAt: string
}

export interface ConsultancyAgreementDto {
  id: string
  corporationId: string
  consultancyPlanId: string
  planName: string
  institutionId: string
  institutionName: string
  agreementTypeId?: string
  agreementTypeCode?: string
  title: string
  description?: string
  startDate?: string
  endDate?: string
  signedDate?: string
  status: string
  fileId?: string
  signedByName?: string
  createdAt: string
  createdBy?: string
  updatedAt: string
  updatedBy?: string
  rowVersion: number
}

// ── Follow-Up DTOs ────────────────────────────────────────────────────────────

export interface FollowUpActivityListItemDto {
  id: string
  corporationId: string
  consultancyPlanId?: string
  planName?: string
  schoolVisitId?: string
  visitDate?: string
  observationRecordId?: string
  title: string
  dueDate?: string
  assignedTo?: string
  status: string
  completedAt?: string
  createdAt: string
}

export interface FollowUpActivityDto {
  id: string
  corporationId: string
  consultancyPlanId?: string
  planName?: string
  schoolVisitId?: string
  visitDate?: string
  observationRecordId?: string
  title: string
  description?: string
  dueDate?: string
  assignedTo?: string
  status: string
  completedAt?: string
  completedBy?: string
  notes?: string
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Agreement Summary & Open Follow-up Report DTOs ────────────────────────────

export interface AgreementSummaryDto {
  planId: string
  planName: string
  institutionName: string
  totalAgreements: number
  draftCount: number
  sentCount: number
  signedCount: number
  expiredCount: number
  cancelledCount: number
}

export interface OpenFollowUpReportItemDto {
  activityId: string
  title: string
  consultancyPlanId?: string
  planName?: string
  schoolVisitId?: string
  visitDate?: string
  dueDate?: string
  isOverdue: boolean
  assignedTo?: string
  status: string
  createdAt: string
}

// ── Reporting DTOs ─────────────────────────────────────────────────────────────

export interface InstitutionReportDto {
  institutionId: string
  institutionName: string
  institutionTypeCode?: string
  city?: string
  totalPlans: number
  activePlans: number
  completedPlans: number
  totalVisits: number
  completedVisits: number
  totalObservations: number
  totalReports: number
}

export interface ConsultancyOutcomesDto {
  planId: string
  planName: string
  institutionName: string
  consultancyTypeCode?: string
  periodStart?: string
  periodEnd?: string
  status: string
  visitCount: number
  completedVisitCount: number
  observationCount: number
  reportCount: number
}

// ── Query Types ───────────────────────────────────────────────────────────────

export interface InstitutionListQuery extends PagedQuery {
  corporationId?: string
  institutionTypeId?: string
  city?: string
}

export interface PlanListQuery extends PagedQuery {
  corporationId?: string
  institutionId?: string
  consultancyTypeId?: string
  status?: string
}

export interface VisitListQuery extends PagedQuery {
  corporationId?: string
  consultancyPlanId?: string
  institutionId?: string
  status?: string
  from?: string
  to?: string
}

export interface AgreementListQuery extends PagedQuery {
  corporationId?: string
  consultancyPlanId?: string
  institutionId?: string
  agreementTypeId?: string
  status?: string
}

export interface FollowUpListQuery extends PagedQuery {
  corporationId?: string
  consultancyPlanId?: string
  schoolVisitId?: string
  observationRecordId?: string
  assignedTo?: string
  status?: string
  overdueOnly?: boolean
}

export interface OpenFollowUpReportQuery {
  corporationId?: string
  consultancyPlanId?: string
  assignedTo?: string
}

export interface AgreementSummaryQuery {
  corporationId?: string
  institutionId?: string
}

// ── Payload Types ─────────────────────────────────────────────────────────────

export interface CreateInstitutionPayload {
  corporationId: string
  institutionTypeId?: string
  name: string
  city?: string
  district?: string
  contactName?: string
  contactPhone?: string
  contactEmail?: string
}

export interface UpdateInstitutionPayload {
  institutionTypeId?: string
  name: string
  city?: string
  district?: string
  contactName?: string
  contactPhone?: string
  contactEmail?: string
  rowVersion: number
}

export interface CreatePlanPayload {
  corporationId: string
  institutionId: string
  consultancyTypeId?: string
  name: string
  periodStart?: string
  periodEnd?: string
  scope?: string
  leadEducatorId?: string
}

export interface CreateVisitPayload {
  corporationId: string
  consultancyPlanId?: string
  institutionId: string
  visitDate: string
  visitorId?: string
  purpose?: string
}

export interface AddObservationPayload {
  observationTypeId?: string
  subject?: string
  observation: string
  recommendations?: string
}

export interface CreateAgreementPayload {
  corporationId: string
  consultancyPlanId: string
  institutionId: string
  agreementTypeId?: string
  title: string
  description?: string
  startDate?: string
  endDate?: string
}

export interface UpdateAgreementPayload {
  agreementTypeId?: string
  title: string
  description?: string
  startDate?: string
  endDate?: string
  rowVersion: number
}

export interface SignAgreementPayload {
  signedByName: string
  signedDate: string
  rowVersion: number
}

export interface CreateFollowUpPayload {
  corporationId: string
  consultancyPlanId?: string
  schoolVisitId?: string
  observationRecordId?: string
  title: string
  description?: string
  dueDate?: string
  assignedTo?: string
}

export interface UpdateFollowUpPayload {
  title: string
  description?: string
  dueDate?: string
  assignedTo?: string
  notes?: string
  rowVersion: number
}

export interface CompleteFollowUpPayload {
  notes?: string
  rowVersion: number
}

export interface CancelFollowUpPayload {
  rowVersion: number
}
