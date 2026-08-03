// Program, Enrollment, and Student-Program types
// Mirrors Aynesil.Application.Features.Programs.Dtos

import type { PagedQuery } from './api.types'

// ── Program DTOs ──────────────────────────────────────────────────────────────

export interface ProgramDto {
  id: string
  corporationId: string
  code: string
  name: string
  programTypeId: string | null
  programTypeLabel: string | null
  description: string | null
  isActive: boolean
  rowVersion: number
  createdAt: string
  updatedAt: string
  services: ProgramServiceDto[]
  translations: ProgramTranslationDto[]
}

export interface ProgramListItemDto {
  id: string
  corporationId: string
  code: string
  name: string
  programTypeId: string | null
  programTypeLabel: string | null
  description: string | null
  isActive: boolean
  serviceCount: number
  createdAt: string
}

export interface ProgramServiceDto {
  id: string
  serviceTypeId: string | null
  serviceTypeLabel: string | null
  name: string
  defaultDurationMinutes: number | null
  defaultSessionsPerWeek: number | null
  sortOrder: number
}

export interface ProgramTranslationDto {
  locale: string
  name: string
  description: string | null
}

// ── Enrollment DTOs ───────────────────────────────────────────────────────────

export interface EnrollmentDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string | null
  campusId: string | null
  campusName: string | null
  statusId: string | null
  statusLabel: string | null
  enrolledOn: string
  endedOn: string | null
  terminationReason: string | null
  rowVersion: number
  createdAt: string
  updatedAt: string
  studentPrograms: StudentProgramDto[]
}

export interface EnrollmentListItemDto {
  id: string
  studentId: string
  studentFullName: string | null
  campusId: string | null
  campusName: string | null
  statusId: string | null
  statusLabel: string | null
  enrolledOn: string
  endedOn: string | null
  programCount: number
}

// ── Student Program DTOs ──────────────────────────────────────────────────────

export interface StudentProgramDto {
  id: string
  studentId: string
  programId: string
  programName: string | null
  programCode: string | null
  enrollmentId: string | null
  campusId: string | null
  campusName: string | null
  startDate: string | null
  endDate: string | null
  status: string
  rowVersion: number
  createdAt: string
  updatedAt: string
}

export interface StudentProgramListItemDto {
  id: string
  studentId: string
  programId: string
  programName: string
  programCode: string
  programTypeLabel: string | null
  campusId: string | null
  campusName: string | null
  startDate: string | null
  status: string
  createdAt: string
}

// ── Queries ───────────────────────────────────────────────────────────────────

export interface ProgramListQuery extends PagedQuery {
  corporationId?: string
  programTypeId?: string
  isActive?: boolean
}

export interface EnrollmentListQuery extends PagedQuery {
  studentId?: string
  corporationId?: string
  campusId?: string
  statusId?: string
  isActive?: boolean
}

export interface StudentProgramListQuery extends PagedQuery {
  studentId?: string
  corporationId?: string
  programId?: string
  campusId?: string
  status?: string
  enrollmentId?: string
}

// ── Payloads ──────────────────────────────────────────────────────────────────

export interface CreateProgramPayload {
  corporationId: string
  code: string
  name: string
  programTypeId: string | null
  description: string | null
}

export interface UpdateProgramPayload {
  code: string
  name: string
  programTypeId: string | null
  description: string | null
  rowVersion: number
}

export interface SetTranslationPayload {
  name: string
  description: string | null
}

export interface AddProgramServicePayload {
  name: string
  serviceTypeId: string | null
  defaultDurationMinutes: number | null
  defaultSessionsPerWeek: number | null
  sortOrder: number
}

export interface UpdateProgramServicePayload extends AddProgramServicePayload {
  // no additional fields
}

export interface CreateEnrollmentPayload {
  corporationId: string
  studentId: string
  campusId: string | null
  statusId: string | null
  enrolledOn: string | null
}

export interface ChangeEnrollmentStatusPayload {
  newStatusId: string
  rowVersion: number
}

export interface EndEnrollmentPayload {
  endedOn: string | null
  terminationReason: string | null
  rowVersion: number
}

export interface AssignStudentToProgramPayload {
  corporationId: string
  studentId: string
  programId: string
  enrollmentId: string | null
  campusId: string | null
  startDate: string | null
  endDate: string | null
}

export interface UpdateStudentProgramPayload {
  startDate: string | null
  endDate: string | null
  status: string
  rowVersion: number
}
