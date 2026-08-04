/**
 * Educator Performance & KPI type definitions.
 * Mirrors Aynesil.Application.Features.PerformanceKpi.Dtos.KpiDtos
 */
import type { PagedQuery } from './api.types'

// ── KPI Category ──────────────────────────────────────────────────────────────

export interface KpiCategoryDto {
  id: string
  code: string
  label?: string
}

// ── KPI Definition ────────────────────────────────────────────────────────────

export interface KpiDefinitionListItemDto {
  id: string
  corporationId?: string
  code: string
  name: string
  categoryId?: string
  categoryCode?: string
  unit?: string
  isActive: boolean
  updatedAt: string
}

export interface KpiDefinitionDto {
  id: string
  corporationId?: string
  code: string
  name: string
  categoryId?: string
  categoryCode?: string
  unit?: string
  spec: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── KPI Value ─────────────────────────────────────────────────────────────────

export interface KpiValueDto {
  id: string
  corporationId: string
  kpiId: string
  kpiCode: string
  kpiName: string
  kpiUnit?: string
  subjectType: string
  subjectId?: string
  periodStart: string
  periodEnd: string
  numericValue?: number
  computedAt: string
}

// ── Educator Performance Snapshot ─────────────────────────────────────────────

export interface EducatorPerformanceSnapshotListItemDto {
  id: string
  educatorId: string
  educatorFullName: string
  periodStart: string
  periodEnd: string
  sessionCount?: number
  attendanceRate?: number
  goalAchievementRate?: number
  parentFeedbackAvg?: number
  utilizationRate?: number
  computedAt: string
}

export interface EducatorPerformanceSnapshotDto {
  id: string
  corporationId: string
  educatorId: string
  educatorFullName: string
  periodStart: string
  periodEnd: string
  sessionCount?: number
  attendanceRate?: number
  goalAchievementRate?: number
  parentFeedbackAvg?: number
  utilizationRate?: number
  detail: string
  computedAt: string
}

// ── Parent Feedback ───────────────────────────────────────────────────────────

export interface ParentFeedbackDto {
  id: string
  corporationId: string
  guardianId?: string
  educatorId?: string
  sessionId?: string
  rating?: number
  comment?: string
  createdAt: string
}

export interface ParentFeedbackSummaryDto {
  id: string
  sessionId?: string
  createdAt: string
  rating: number
  comment?: string
}

// ── Dashboard DTOs ────────────────────────────────────────────────────────────

export interface PerformanceSummaryDto {
  periodStart: string
  periodEnd: string
  periodLabel: string
  sessionCount?: number
  attendanceRate?: number
  goalAchievementRate?: number
  parentFeedbackAvg?: number
  utilizationRate?: number
}

export interface TrendPointDto {
  periodStart: string
  periodEnd: string
  label: string
  value?: number
}

export interface KpiTrendDto {
  kpiCode: string
  kpiName: string
  unit?: string
  points: TrendPointDto[]
}

export interface EducatorSummaryDto {
  educatorId: string
  fullName: string
  titleCode?: string
  primaryCampusId?: string
  sessionCount?: number
  attendanceRate?: number
  goalAchievementRate?: number
  parentFeedbackAvg?: number
  utilizationRate?: number
  rank?: number
}

export interface RankingItemDto {
  rank: number
  educatorId: string
  fullName: string
  titleCode?: string
  kpiValue?: number
  kpiCode: string
  kpiName: string
  unit?: string
}

export interface EducatorDashboardDto {
  educatorId: string
  fullName: string
  titleCode?: string
  currentPeriod?: PerformanceSummaryDto
  previousPeriod?: PerformanceSummaryDto
  allKpiValues: KpiValueDto[]
  sessionCountTrend: TrendPointDto[]
  attendanceRateTrend: TrendPointDto[]
  recentFeedback: ParentFeedbackSummaryDto[]
}

export interface ManagerDashboardDto {
  corporationId: string
  campusId?: string
  periodStart: string
  periodEnd: string
  periodLabel: string
  totalEducators: number
  avgAttendanceRate?: number
  avgGoalAchievementRate?: number
  avgParentSatisfaction?: number
  avgUtilizationRate?: number
  topPerformers: EducatorSummaryDto[]
  educators: EducatorSummaryDto[]
}

export interface ExecutiveDashboardDto {
  corporationId: string
  periodStart: string
  periodEnd: string
  totalActiveEducators: number
  totalCompletedSessions: number
  corpAvgAttendanceRate?: number
  corpAvgGoalAchievementRate?: number
  corpAvgParentSatisfaction?: number
  corpAvgUtilizationRate?: number
  trends: KpiTrendDto[]
  topPerformers: EducatorSummaryDto[]
}

export interface KpiReportRowDto {
  educatorId: string
  fullName: string
  titleCode?: string
  periodStart: string
  periodEnd: string
  sessionCount?: number
  attendanceRate?: number
  goalAchievementRate?: number
  parentFeedbackAvg?: number
  utilizationRate?: number
  rank?: number
}

// ── Query Types ───────────────────────────────────────────────────────────────

export interface KpiDefinitionListQuery extends PagedQuery {
  corporationId?: string
  categoryId?: string
  isActive?: boolean
}

export interface SnapshotListQuery extends PagedQuery {
  corporationId?: string
  campusId?: string
  periodStart?: string
  periodEnd?: string
  educatorId?: string
}

export interface KpiReportQuery {
  corporationId?: string
  campusId?: string
  periodStart?: string
  periodEnd?: string
}

export interface DashboardQuery {
  corporationId?: string
  campusId?: string
  educatorId?: string
  periodType?: 'Monthly' | 'Quarterly' | 'Annual'
  periodStart?: string
  periodEnd?: string
}
