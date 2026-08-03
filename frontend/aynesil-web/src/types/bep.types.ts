// BEP / IEP (Education Plan) types
// Mirrors Aynesil.Application.Features.Plans.Dtos

import type { PagedQuery } from './api.types'

// ── Academic Period DTOs ──────────────────────────────────────────────────────

export interface AcademicPeriodDto {
  id: string
  corporationId: string
  name: string
  termId: string | null
  termLabel: string | null
  startDate: string
  endDate: string
  isCurrent: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
}

export interface AcademicPeriodListItemDto {
  id: string
  name: string
  termId: string | null
  termLabel: string | null
  startDate: string
  endDate: string
  isCurrent: boolean
}

// ── Education Plan DTOs ───────────────────────────────────────────────────────

export interface EducationPlanDto {
  id: string
  corporationId: string
  studentId: string
  studentName: string
  academicPeriodId: string | null
  academicPeriodName: string | null
  campusId: string | null
  campusName: string | null
  title: string
  version: number
  status: string
  effectiveFrom: string | null
  effectiveTo: string | null
  preparedBy: string | null
  preparedByName: string | null
  approvedBy: string | null
  approvedByName: string | null
  approvedAt: string | null
  guardianVisible: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
  longTermGoals: EducationPlanGoalDto[]
  shortTermGoals: EducationPlanGoalDto[]
  reviews: EducationPlanReviewDto[]
  approvals: EducationPlanApprovalDto[]
  revisions: EducationPlanRevisionDto[]
}

export interface EducationPlanListItemDto {
  id: string
  studentId: string
  studentName: string
  academicPeriodId: string | null
  academicPeriodName: string | null
  title: string
  version: number
  status: string
  effectiveFrom: string | null
  effectiveTo: string | null
  guardianVisible: boolean
  createdAt: string
}

// ── Plan Sub-resource DTOs ────────────────────────────────────────────────────

export interface EducationPlanGoalDto {
  id: string
  studentGoalId: string
  statement: string
  horizon: string
  goalStatus: string
  categoryId: string | null
  categoryLabel: string | null
  targetDate: string | null
  achievedDate: string | null
  sortOrder: number
  latestPercentComplete: number | null
  latestTrend: string | null
}

export interface EducationPlanReviewDto {
  id: string
  reviewedOn: string
  reviewerId: string | null
  reviewerName: string | null
  summary: string | null
  outcome: string | null
  createdAt: string
}

export interface EducationPlanApprovalDto {
  id: string
  approverId: string | null
  approverName: string | null
  decision: string
  comment: string | null
  decidedAt: string
}

export interface EducationPlanRevisionDto {
  id: string
  fromVersion: number
  toVersion: number
  changeSummary: string | null
  revisedBy: string | null
  revisedByName: string | null
  revisedAt: string
}

// ── Report DTOs ───────────────────────────────────────────────────────────────

export interface StudentGoalSummaryReportDto {
  studentId: string
  studentName: string
  plans: EducationPlanListItemDto[]
  totalGoals: number
  achievedGoals: number
  achievementRate: number
}

export interface TrendReportRowDto {
  studentGoalId: string
  statement: string
  horizon: string
  targetDate: string | null
  latestPercentComplete: number | null
  currentTrend: string | null
  measurementCount: number
}

// ── Queries ───────────────────────────────────────────────────────────────────

export interface AcademicPeriodListQuery extends PagedQuery {
  corporationId?: string
  isCurrent?: boolean
}

export interface EducationPlanListQuery extends PagedQuery {
  corporationId?: string
  studentId?: string
  campusId?: string
  academicPeriodId?: string
  status?: string
  guardianVisible?: boolean
}

// ── Payloads ──────────────────────────────────────────────────────────────────

export interface CreateAcademicPeriodPayload {
  corporationId: string
  name: string
  startDate: string
  endDate: string
  termId: string | null
  isCurrent: boolean
}

export interface UpdateAcademicPeriodPayload {
  name: string
  startDate: string
  endDate: string
  termId: string | null
  rowVersion: number
}

export interface CreateEducationPlanPayload {
  corporationId: string
  studentId: string
  title: string
  academicPeriodId: string | null
  campusId: string | null
  preparedBy: string | null
  effectiveFrom: string | null
  effectiveTo: string | null
}

export interface UpdateEducationPlanPayload {
  title: string
  academicPeriodId: string | null
  campusId: string | null
  preparedBy: string | null
  effectiveFrom: string | null
  effectiveTo: string | null
  rowVersion: number
}

export interface ApproveRejectPayload {
  approverId: string
  comment: string | null
}

export interface RevisePayload {
  changeSummary: string | null
}

export interface GuardianVisibilityPayload {
  visible: boolean
}

export interface AddGoalToPlanPayload {
  studentGoalId: string
  horizon: string
  sortOrder: number
}

export interface ReorderGoalsPayload {
  items: PlanGoalOrderItem[]
}

export interface PlanGoalOrderItem {
  planGoalId: string
  sortOrder: number
}

export interface AddPlanReviewPayload {
  reviewedOn: string
  reviewerId: string | null
  summary: string | null
  outcome: string | null
}
