// Student, Guardian, and Case Management types
// Mirrors Aynesil.Application.Features.Students.Dtos

import type { PagedQuery } from './api.types'

// ── Student DTOs ──────────────────────────────────────────────────────────────

export interface StudentDto {
  id: string
  corporationId: string
  studentNo: string | null
  firstName: string
  lastName: string
  fullName: string
  nationalId: string | null
  birthDate: string | null
  gender: string | null
  primaryCampusId: string | null
  primaryCampusName: string | null
  statusId: string | null
  statusLabel: string | null
  leadId: string | null
  photoFileId: string | null
  notes: string | null
  rowVersion: number
  createdAt: string
  updatedAt: string
  guardians: StudentGuardianDto[]
  emergencyContacts: EmergencyContactDto[]
  campuses: StudentCampusDto[]
  diagnoses: DiagnosisDto[]
  developmentalProfiles: DevelopmentalProfileDto[]
}

export interface StudentListItemDto {
  id: string
  studentNo: string | null
  firstName: string
  lastName: string
  fullName: string
  birthDate: string | null
  gender: string | null
  primaryCampusId: string | null
  primaryCampusName: string | null
  statusId: string | null
  statusLabel: string | null
  createdAt: string
}

export interface StudentSummaryDto {
  id: string
  studentNo: string | null
  firstName: string
  lastName: string
  fullName: string
  birthDate: string | null
  photoFileId: string | null
  statusId: string | null
  statusLabel: string | null
  primaryCampusName: string | null
}

// ── Guardian DTOs ─────────────────────────────────────────────────────────────

export interface GuardianDto {
  id: string
  corporationId: string
  userId: string | null
  firstName: string
  lastName: string
  fullName: string
  nationalId: string | null
  email: string | null
  phone: string | null
  occupation: string | null
  addressLine: string | null
  hasPortalAccount: boolean
  rowVersion: number
  createdAt: string
  students: StudentGuardianDto[]
}

export interface GuardianListItemDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  email: string | null
  phone: string | null
  hasPortalAccount: boolean
  linkedStudentCount: number
}

export interface StudentGuardianDto {
  linkId: string
  guardianId: string
  guardianFullName: string
  guardianEmail: string | null
  guardianPhone: string | null
  relationshipId: string | null
  relationshipLabel: string | null
  isPrimary: boolean
  hasCustody: boolean
  portalAccess: boolean
  financialResponsible: boolean
}

// ── Emergency Contact ─────────────────────────────────────────────────────────

export interface EmergencyContactDto {
  id: string
  fullName: string
  relationship: string | null
  phone: string
  priority: number
}

export interface EmergencyContactInput {
  fullName: string
  relationship: string | null
  phone: string
  priority: number
}

// ── Campus Enrollment ─────────────────────────────────────────────────────────

export interface StudentCampusDto {
  id: string
  campusId: string
  campusName: string | null
  isPrimary: boolean
  activeFrom: string
  activeTo: string | null
  isActive: boolean
}

// ── Developmental Profile ─────────────────────────────────────────────────────

export interface DevelopmentalProfileDto {
  id: string
  developmentAreaId: string | null
  developmentAreaLabel: string | null
  summary: string | null
  strengths: string | null
  needs: string | null
  assessedOn: string | null
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Diagnosis ─────────────────────────────────────────────────────────────────

export interface DiagnosisDto {
  id: string
  studentId: string
  categoryId: string | null
  categoryLabel: string | null
  icdCode: string | null
  description: string | null
  diagnosedOn: string | null
  diagnosedBy: string | null
  sourceFileId: string | null
  createdAt: string
  rowVersion: number
}

// ── Reports ───────────────────────────────────────────────────────────────────

export interface MedicalReportDto {
  id: string
  studentId: string
  title: string
  reportDate: string | null
  issuer: string | null
  summary: string | null
  fileId: string | null
  createdAt: string
  rowVersion: number
}

export interface DevelopmentReportDto {
  id: string
  studentId: string
  periodLabel: string | null
  reportDate: string | null
  authoredBy: string | null
  content: string | null
  fileId: string | null
  createdAt: string
  rowVersion: number
}

export interface ExternalInstitutionReportDto {
  id: string
  studentId: string
  institutionName: string
  institutionTypeId: string | null
  institutionTypeLabel: string | null
  reportDate: string | null
  summary: string | null
  fileId: string | null
  createdAt: string
  rowVersion: number
}

// ── Case Notes ────────────────────────────────────────────────────────────────

export interface CaseNoteDto {
  id: string
  studentId: string
  noteType: string | null
  body: string
  isConfidential: boolean
  authoredBy: string | null
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Portal Access ─────────────────────────────────────────────────────────────

export interface GuardianPortalAccessDto {
  id: string
  guardianId: string
  studentId: string
  canViewSessions: boolean
  canViewAttendance: boolean
  canViewReports: boolean
  canViewPlan: boolean
  canViewFinance: boolean
  canViewCamera: boolean
  grantedAt: string
  revokedAt: string | null
  isActive: boolean
}

// ── Status History ────────────────────────────────────────────────────────────

export interface StudentStatusHistoryDto {
  id: string
  statusId: string
  statusLabel: string | null
  reason: string | null
  changedAt: string
  changedBy: string | null
}

// ── Query / Payload Types ─────────────────────────────────────────────────────

export interface StudentListQuery extends PagedQuery {
  corporationId?: string
  campusId?: string
  statusId?: string
  hasLead?: boolean
  birthDateFrom?: string
  birthDateTo?: string
}

export interface GuardianListQuery extends PagedQuery {
  corporationId?: string
  hasPortalAccount?: boolean
}

export interface CaseNoteListQuery {
  studentId: string
  includeConfidential?: boolean
  noteType?: string
  page?: number
  pageSize?: number
}

// ── Create / Update Payloads ──────────────────────────────────────────────────

export interface CreateStudentPayload {
  corporationId: string
  firstName: string
  lastName: string
  studentNo: string | null
  nationalId: string | null
  birthDate: string | null
  gender: string | null
  primaryCampusId: string | null
  statusId: string | null
  leadId: string | null
  notes: string | null
}

export interface UpdateStudentPayload {
  firstName: string
  lastName: string
  studentNo: string | null
  nationalId: string | null
  birthDate: string | null
  gender: string | null
  primaryCampusId: string | null
  notes: string | null
  rowVersion: number
}

export interface ChangeStudentStatusPayload {
  newStatusId: string
  reason: string | null
  rowVersion: number
}

export interface EnrollAtCampusPayload {
  campusId: string
  isPrimary: boolean
  activeFrom: string | null
}

export interface TransferStudentPayload {
  newCampusId: string
  transferDate: string | null
  rowVersion: number
}

export interface LinkGuardianPayload {
  guardianId: string
  relationshipId: string | null
  isPrimary: boolean
  hasCustody: boolean
  portalAccess: boolean
  financialResponsible: boolean
}

export interface UpdateGuardianLinkPayload {
  relationshipId: string | null
  isPrimary: boolean
  hasCustody: boolean
  portalAccess: boolean
  financialResponsible: boolean
}

export interface ReplaceEmergencyContactsPayload {
  contacts: EmergencyContactInput[]
}

export interface UpsertDevProfilePayload {
  developmentAreaId: string | null
  summary: string | null
  strengths: string | null
  needs: string | null
  assessedOn: string | null
}

export interface AddDiagnosisPayload {
  categoryId: string | null
  icdCode: string | null
  description: string | null
  diagnosedOn: string | null
  diagnosedBy: string | null
  sourceFileId: string | null
}

export interface UpdateDiagnosisPayload extends AddDiagnosisPayload {
  rowVersion: number
}

export interface AddMedicalReportPayload {
  title: string
  reportDate: string | null
  issuer: string | null
  summary: string | null
  fileId: string | null
}

export interface UpdateMedicalReportPayload extends AddMedicalReportPayload {
  rowVersion: number
}

export interface AddDevelopmentReportPayload {
  periodLabel: string | null
  reportDate: string | null
  authoredBy: string | null
  content: string | null
  fileId: string | null
}

export interface UpdateDevelopmentReportPayload extends AddDevelopmentReportPayload {
  rowVersion: number
}

export interface AddExternalReportPayload {
  institutionName: string
  institutionTypeId: string | null
  reportDate: string | null
  summary: string | null
  fileId: string | null
}

export interface AddCaseNotePayload {
  noteType: string | null
  body: string
  isConfidential: boolean
  authoredBy: string | null
}

export interface UpdateCaseNotePayload {
  noteType: string | null
  body: string
  isConfidential: boolean
  rowVersion: number
}

export interface CreateGuardianPayload {
  corporationId: string
  firstName: string
  lastName: string
  nationalId: string | null
  email: string | null
  phone: string | null
  occupation: string | null
  addressLine: string | null
}

export interface UpdateGuardianPayload {
  firstName: string
  lastName: string
  nationalId: string | null
  email: string | null
  phone: string | null
  occupation: string | null
  addressLine: string | null
  rowVersion: number
}

export interface GrantPortalAccessPayload {
  canViewSessions: boolean
  canViewAttendance: boolean
  canViewReports: boolean
  canViewPlan: boolean
  canViewFinance: boolean
  canViewCamera: boolean
}
