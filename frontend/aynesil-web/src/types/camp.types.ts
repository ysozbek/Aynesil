/**
 * Camp Management type definitions.
 * Mirrors Aynesil.Application.Features.Camps.Dtos.CampDtos
 */
import type { PagedQuery } from './api.types'

// ── Camp DTOs ─────────────────────────────────────────────────────────────────

export interface CampPeriodListItemDto {
  id: string
  campId: string
  name: string
  startDate: string
  endDate: string
  capacity?: number
  enrolledCount: number
  waitlistCount: number
}

export interface CampPeriodDto {
  id: string
  campId: string
  corporationId: string
  name: string
  startDate: string
  endDate: string
  capacity?: number
  enrolledCount: number
  waitlistCount: number
}

export interface CampListItemDto {
  id: string
  corporationId: string
  campusId?: string
  campTypeId?: string
  campTypeCode?: string
  code: string
  name: string
  location?: string
  capacity?: number
  isActive: boolean
  periodCount: number
  updatedAt: string
}

export interface CampDto {
  id: string
  corporationId: string
  campusId?: string
  campTypeId?: string
  campTypeCode?: string
  code: string
  name: string
  description?: string
  location?: string
  capacity?: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
  periods: CampPeriodListItemDto[]
}

// ── Enrollment DTOs ───────────────────────────────────────────────────────────

export interface CampEnrollmentListItemDto {
  id: string
  campPeriodId: string
  studentId: string
  studentPackageId?: string
  status: string
  enrolledAt: string
}

export interface CampEnrollmentDto {
  id: string
  corporationId: string
  campPeriodId: string
  studentId: string
  studentPackageId?: string
  status: string
  enrolledAt: string
  attendanceCount: number
  presentCount: number
  absentCount: number
}

// ── Attendance DTOs ───────────────────────────────────────────────────────────

export interface CampAttendanceDto {
  id: string
  campEnrollmentId: string
  attendanceDate: string
  status: string
  reasonId?: string
  recordedBy?: string
}

// ── Report DTOs ───────────────────────────────────────────────────────────────

export interface CampReportDto {
  id: string
  campEnrollmentId: string
  summary?: string
  fileId?: string
  authoredBy?: string
  createdAt: string
}

export interface CampEnrollmentSummaryDto {
  campPeriodId: string
  periodName: string
  startDate: string
  endDate: string
  capacity?: number
  totalEnrolled: number
  totalWaitlist: number
  totalWithdrawn: number
  totalCompleted: number
}

export interface CampAttendanceSummaryDto {
  enrollmentId: string
  studentId: string
  totalDays: number
  present: number
  absent: number
  late: number
  excused: number
  attendanceRatePct: number
}

export interface CampPerformanceDto {
  campId: string
  campCode: string
  campName: string
  totalPeriods: number
  totalEnrolled: number
  totalCompleted: number
  totalWithdrawn: number
  completionRatePct: number
  overallAttendanceRatePct: number
}

// ── Activity DTOs ─────────────────────────────────────────────────────────────

export interface CampActivityListItemDto {
  id: string
  campPeriodId: string
  activityTypeId?: string
  activityTypeCode?: string
  name: string
  startsAt?: string
  endsAt?: string
  location?: string
  capacity?: number
  isActive: boolean
  participationCount: number
}

export interface CampActivityDto {
  id: string
  corporationId: string
  campPeriodId: string
  activityTypeId?: string
  activityTypeCode?: string
  name: string
  description?: string
  startsAt?: string
  endsAt?: string
  location?: string
  capacity?: number
  sessionId?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Educator Assignment DTOs ──────────────────────────────────────────────────

export interface CampEducatorDto {
  id: string
  corporationId: string
  campId: string
  campPeriodId?: string
  campActivityId?: string
  educatorId: string
  role: string
  assignedAt: string
  assignedBy?: string
}

// ── Participation DTOs ────────────────────────────────────────────────────────

export interface CampActivityParticipationDto {
  id: string
  corporationId: string
  campActivityId: string
  campEnrollmentId: string
  status: string
  notes?: string
  recordedBy?: string
  recordedAt: string
}

// ── Query Types ───────────────────────────────────────────────────────────────

export interface CampListQuery extends PagedQuery {
  corporationId?: string
  campusId?: string
  campTypeId?: string
  isActive?: boolean
}

export interface CampEnrollmentQuery extends PagedQuery {
  campPeriodId?: string
  studentId?: string
  status?: string
}

// ── Payload Types ─────────────────────────────────────────────────────────────

export interface CreateCampPayload {
  corporationId: string
  campusId?: string
  campTypeId?: string
  code: string
  name: string
  description?: string
  location?: string
  capacity?: number
}

export interface UpdateCampPayload {
  campTypeId?: string
  name: string
  description?: string
  location?: string
  capacity?: number
  rowVersion: number
}

export interface CreatePeriodPayload {
  name: string
  startDate: string
  endDate: string
  capacity?: number
}

export interface EnrollStudentPayload {
  studentId: string
  studentPackageId?: string
}

export interface RecordAttendancePayload {
  attendanceDate: string
  status: string
  reasonId?: string
}
