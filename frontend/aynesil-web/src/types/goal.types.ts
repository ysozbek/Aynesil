// Goal Library, Goal Template, Student Goal, Progress, and Analytics types
// Mirrors Aynesil.Application.Features.Goals.Dtos

import type { PagedQuery } from './api.types'

// ── Goal Library DTOs ─────────────────────────────────────────────────────────

export interface GoalLibraryDto {
  id: string
  corporationId: string | null
  name: string
  description: string | null
  templateCount: number
  createdAt: string
  updatedAt: string
  rowVersion: number
}

export interface GoalLibraryListItemDto {
  id: string
  corporationId: string | null
  name: string
  description: string | null
  templateCount: number
  createdAt: string
}

// ── Goal Template DTOs ────────────────────────────────────────────────────────

export interface GoalTemplateDto {
  id: string
  corporationId: string | null
  libraryId: string | null
  libraryName: string | null
  categoryId: string | null
  categoryLabel: string | null
  developmentAreaId: string | null
  developmentAreaLabel: string | null
  code: string | null
  statement: string
  defaultCriteria: string | null
  createdAt: string
  updatedAt: string
  rowVersion: number
  translations: GoalTemplateTranslationDto[]
}

export interface GoalTemplateListItemDto {
  id: string
  corporationId: string | null
  libraryId: string | null
  libraryName: string | null
  categoryId: string | null
  categoryLabel: string | null
  developmentAreaId: string | null
  developmentAreaLabel: string | null
  code: string | null
  statement: string
  createdAt: string
}

export interface GoalTemplateTranslationDto {
  locale: string
  statement: string
  defaultCriteria: string | null
}

// ── Student Goal DTOs ─────────────────────────────────────────────────────────

export interface StudentGoalDto {
  id: string
  corporationId: string
  studentId: string
  templateId: string | null
  categoryId: string | null
  categoryLabel: string | null
  developmentAreaId: string | null
  developmentAreaLabel: string | null
  horizon: string
  parentGoalId: string | null
  statement: string
  masteryCriteria: string | null
  baseline: string | null
  targetValue: number | null
  status: string
  startDate: string | null
  targetDate: string | null
  achievedDate: string | null
  createdAt: string
  updatedAt: string
  rowVersion: number
  recentProgress: GoalProgressDto[]
}

export interface StudentGoalListItemDto {
  id: string
  studentId: string
  categoryId: string | null
  categoryLabel: string | null
  developmentAreaId: string | null
  developmentAreaLabel: string | null
  horizon: string
  statement: string
  status: string
  targetDate: string | null
  achievedDate: string | null
  latestPercentComplete: number | null
  latestTrend: string | null
  createdAt: string
}

// ── Goal Progress DTOs ────────────────────────────────────────────────────────

export interface GoalProgressDto {
  id: string
  studentGoalId: string
  sessionId: string | null
  measuredOn: string
  measuredValue: number | null
  percentComplete: number | null
  trend: string | null
  note: string | null
  recordedBy: string | null
  createdAt: string
}

// ── Analytics DTOs ────────────────────────────────────────────────────────────

export interface GoalTrendDto {
  studentGoalId: string
  statement: string
  horizon: string
  status: string
  progressSeries: GoalProgressDto[]
  latestPercentComplete: number | null
  currentTrend: string | null
}

export interface StudentGoalSummaryDto {
  studentId: string
  studentName: string
  totalGoals: number
  activeGoals: number
  achievedGoals: number
  discontinuedGoals: number
  onHoldGoals: number
  achievementRate: number
  byDevelopmentArea: DevelopmentAreaProgressDto[]
}

export interface DevelopmentAreaProgressDto {
  developmentAreaId: string | null
  developmentAreaLabel: string | null
  goalCount: number
  achievedCount: number
  achievementRate: number
}

export interface GoalSuccessRateDto {
  categoryId: string | null
  categoryLabel: string | null
  totalGoals: number
  achievedGoals: number
  successRate: number
  averageTrend: string | null
}

// ── Queries ───────────────────────────────────────────────────────────────────

export interface GoalLibraryListQuery extends PagedQuery {
  corporationId?: string
}

export interface GoalTemplateListQuery extends PagedQuery {
  corporationId?: string
  libraryId?: string
  categoryId?: string
  developmentAreaId?: string
}

export interface StudentGoalListQuery extends PagedQuery {
  corporationId?: string
  studentId?: string
  horizon?: string
  status?: string
  categoryId?: string
  developmentAreaId?: string
}

export interface GoalProgressQuery {
  from?: string
  to?: string
}

export interface SuccessRatesQuery {
  corporationId: string
  campusId?: string
  from?: string
  to?: string
}

// ── Payloads ──────────────────────────────────────────────────────────────────

export interface CreateGoalLibraryPayload {
  corporationId: string | null
  name: string
  description: string | null
}

export interface UpdateGoalLibraryPayload {
  name: string
  description: string | null
  rowVersion: number
}

export interface CreateGoalTemplatePayload {
  corporationId: string | null
  libraryId: string | null
  categoryId: string | null
  developmentAreaId: string | null
  code: string | null
  statement: string
  defaultCriteria: string | null
}

export interface UpdateGoalTemplatePayload {
  libraryId: string | null
  categoryId: string | null
  developmentAreaId: string | null
  code: string | null
  statement: string
  defaultCriteria: string | null
  rowVersion: number
}

export interface SetGoalTemplateTranslationPayload {
  statement: string
  defaultCriteria: string | null
}

export interface CreateStudentGoalPayload {
  corporationId: string
  studentId: string
  statement: string
  horizon: string
  templateId: string | null
  categoryId: string | null
  developmentAreaId: string | null
  parentGoalId: string | null
  masteryCriteria: string | null
  baseline: string | null
  targetValue: number | null
  startDate: string | null
  targetDate: string | null
}

export interface UpdateStudentGoalPayload {
  statement: string
  categoryId: string | null
  developmentAreaId: string | null
  masteryCriteria: string | null
  baseline: string | null
  targetValue: number | null
  startDate: string | null
  targetDate: string | null
  rowVersion: number
}

export interface ChangeGoalStatusPayload {
  newStatus: string
  achievedDate: string | null
}

export interface RecordProgressPayload {
  measuredOn: string
  measuredValue: number | null
  percentComplete: number | null
  trend: string | null
  note: string | null
  sessionId: string | null
}
