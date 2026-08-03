// Educator Management types
// Mirrors Aynesil.Application.Features.Educators.Dtos

import type { PagedQuery } from './api.types'

// ── Educator DTOs ─────────────────────────────────────────────────────────────

export interface EducatorDto {
  id: string
  corporationId: string
  userId: string | null
  firstName: string
  lastName: string
  fullName: string
  titleId: string | null
  titleLabel: string | null
  email: string | null
  phone: string | null
  employmentType: string | null
  hireDate: string | null
  isActive: boolean
  primaryCampusId: string | null
  primaryCampusName: string | null
  rowVersion: number
  createdAt: string
  updatedAt: string
  campuses: EducatorCampusDto[]
  specialties: EducatorSpecialtyDto[]
  certifications: EducatorCertificationDto[]
  supervisors: EducatorHierarchyDto[]
  subordinates: EducatorHierarchyDto[]
}

export interface EducatorListItemDto {
  id: string
  corporationId: string
  firstName: string
  lastName: string
  fullName: string
  titleId: string | null
  titleLabel: string | null
  email: string | null
  phone: string | null
  employmentType: string | null
  isActive: boolean
  primaryCampusId: string | null
  primaryCampusName: string | null
  specialtyCount: number
  createdAt: string
}

export interface EducatorSummaryDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  titleId: string | null
  titleLabel: string | null
  isActive: boolean
  primaryCampusName: string | null
}

// ── Sub-resource DTOs ─────────────────────────────────────────────────────────

export interface EducatorCampusDto {
  id: string
  campusId: string
  campusName: string | null
  isPrimary: boolean
  activeFrom: string
  activeTo: string | null
  isActive: boolean
}

export interface EducatorSpecialtyDto {
  id: string
  specialtyId: string
  specialtyLabel: string | null
}

export interface EducatorCertificationDto {
  id: string
  certificationTypeId: string | null
  certificationTypeLabel: string | null
  name: string
  issuer: string | null
  issuedOn: string | null
  expiresOn: string | null
  isExpired: boolean
  fileId: string | null
  createdAt: string
  rowVersion: number
}

export interface EducatorHierarchyDto {
  id: string
  educatorId: string
  educatorFullName: string
  supervisorId: string
  supervisorFullName: string
  relationshipId: string | null
  relationshipLabel: string | null
  campusId: string | null
  campusName: string | null
  activeFrom: string
  activeTo: string | null
  isActive: boolean
}

export interface EducatorAvailabilityDto {
  id: string
  fullName: string
  isActive: boolean
  activeCampuses: EducatorCampusDto[]
  specialties: EducatorSpecialtyDto[]
  activeStudentProgramCount: number
}

export interface EducatorUtilizationDto {
  id: string
  fullName: string
  titleLabel: string | null
  primaryCampusName: string | null
  activeStudentProgramCount: number
  totalStudentProgramCount: number
  specialtyCount: number
  certificationCount: number
}

// ── Queries ───────────────────────────────────────────────────────────────────

export interface EducatorListQuery extends PagedQuery {
  corporationId?: string
  campusId?: string
  titleId?: string
  specialtyId?: string
  isActive?: boolean
  employmentType?: string
}

export interface UtilizationQuery {
  corporationId: string
  campusId?: string
  activeOnly?: boolean
}

// ── Payloads ──────────────────────────────────────────────────────────────────

export interface CreateEducatorPayload {
  corporationId: string
  firstName: string
  lastName: string
  titleId: string | null
  email: string | null
  phone: string | null
  employmentType: string | null
  hireDate: string | null
  primaryCampusId: string | null
}

export interface UpdateEducatorPayload {
  firstName: string
  lastName: string
  titleId: string | null
  email: string | null
  phone: string | null
  employmentType: string | null
  hireDate: string | null
  primaryCampusId: string | null
  rowVersion: number
}

export interface AssignSpecialtyPayload {
  specialtyId: string
}

export interface AssignCampusPayload {
  campusId: string
  isPrimary: boolean
  activeFrom: string | null
}

export interface EndCampusAssignmentPayload {
  endDate: string | null
}

export interface AddCertificationPayload {
  name: string
  certificationTypeId: string | null
  issuer: string | null
  issuedOn: string | null
  expiresOn: string | null
  fileId: string | null
}

export interface UpdateCertificationPayload extends AddCertificationPayload {
  rowVersion: number
}

export interface LinkHierarchyPayload {
  supervisorId: string
  relationshipId: string | null
  campusId: string | null
  activeFrom: string | null
}

export interface EndHierarchyPayload {
  endDate: string | null
}
