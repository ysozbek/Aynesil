/**
 * Parent Portal type definitions.
 * Mirrors Aynesil.Application.Features.Portal.Dtos.PortalDtos
 * and Aynesil.Application.Features.Students.Dtos.StudentProjection (portal subset)
 */

// ── Student Overview ───────────────────────────────────────────────────────────

export interface PortalStudentDto {
  id: string
  fullName: string
  dateOfBirth?: string
  photoUrl?: string
  corporationId: string
  branchId?: string
  enrollmentStatus?: string
  primaryDiagnosis?: string
  canViewReports: boolean
  canViewFinance: boolean
  canViewSessions: boolean
  canViewGoals: boolean
  canViewDocuments: boolean
}

// ── Dashboard ──────────────────────────────────────────────────────────────────

export interface PortalDashboardDto {
  studentId: string
  upcomingSessions?: number
  unreadNotifications: number
  packageBalance?: number
  activeGoals?: number
}

// ── Sessions ───────────────────────────────────────────────────────────────────

export interface PortalSessionDto {
  id: string
  title?: string
  startsAt: string
  endsAt: string
  status: string
}

// ── Attendance ─────────────────────────────────────────────────────────────────

export interface PortalAttendanceDto {
  sessionId: string
  sessionTitle?: string
  sessionStartsAt: string
  attendanceStatus: string
  reasonId?: string
}

// ── Packages ───────────────────────────────────────────────────────────────────

export interface PortalPackageDto {
  id: string
  studentId: string
  totalCredits: number
  remainingCredits: number
  expiresOn?: string
  status: string
}

// ── Documents ──────────────────────────────────────────────────────────────────

export interface PortalDocumentDto {
  fileId: string
  originalName: string
  purpose?: string
  mimeType?: string
  byteSize?: number
  createdAt: string
}

// ── Education Plan (BEP) ───────────────────────────────────────────────────────

export interface PortalEducationPlanDto {
  id: string
  title?: string
  version: number
  status: string
  effectiveFrom?: string
  effectiveTo?: string
}

// ── Goal Progress ──────────────────────────────────────────────────────────────

export interface PortalGoalProgressDto {
  goalId: string
  statement: string
  horizon?: string
  status: string
  percentComplete?: number
  trend?: string
  targetDate?: string
}

// ── Meeting History ────────────────────────────────────────────────────────────

export interface PortalMeetingDto {
  id: string
  title: string
  scheduledAt?: string
  endsAt?: string
  status: string
  location?: string
  guardianAttendance?: string
}

// ── Query types ────────────────────────────────────────────────────────────────

export interface PortalSessionListQuery {
  studentId: string
  page?: number
  pageSize?: number
  from?: string
  to?: string
  status?: string
}

export interface PortalAttendanceListQuery {
  studentId: string
  page?: number
  pageSize?: number
  from?: string
  to?: string
}

export interface PortalDocumentListQuery {
  studentId: string
  page?: number
  pageSize?: number
}
