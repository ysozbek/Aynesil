/**
 * Contract & Consent Management type definitions.
 * Mirrors Aynesil.Application.Features.Legal.Dtos.LegalDtos
 */
import type { PagedQuery } from './api.types'

// ── Contract Template DTOs ────────────────────────────────────────────────────

export interface ContractTemplateTranslationDto {
  locale: string
  title: string
  body: string
}

export interface ContractTemplateListItemDto {
  id: string
  corporationId: string
  code: string
  contractTypeId?: string
  contractTypeCode?: string
  version: number
  isCurrent: boolean
  effectiveFrom?: string
  createdAt: string
  updatedAt: string
}

export interface ContractTemplateDto {
  id: string
  corporationId: string
  code: string
  contractTypeId?: string
  contractTypeCode?: string
  version: number
  isCurrent: boolean
  effectiveFrom?: string
  translations: ContractTemplateTranslationDto[]
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Consent Template DTOs ─────────────────────────────────────────────────────

export interface ConsentTemplateTranslationDto {
  locale: string
  title: string
  body: string
}

export interface ConsentTemplateListItemDto {
  id: string
  corporationId: string
  code: string
  consentTypeId?: string
  consentTypeCode?: string
  version: number
  isCurrent: boolean
  isMandatory: boolean
  effectiveFrom?: string
  createdAt: string
  updatedAt: string
}

export interface ConsentTemplateDto {
  id: string
  corporationId: string
  code: string
  consentTypeId?: string
  consentTypeCode?: string
  version: number
  isCurrent: boolean
  isMandatory: boolean
  effectiveFrom?: string
  translations: ConsentTemplateTranslationDto[]
  createdAt: string
  updatedAt: string
  rowVersion: number
}

// ── Student Contract DTOs ─────────────────────────────────────────────────────

export interface StudentContractListItemDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName?: string
  templateId?: string
  templateCode?: string
  templateVersion?: number
  guardianId?: string
  status: string
  signedAt?: string
  signatureMethod?: string
  startsOn?: string
  endsOn?: string
  createdAt: string
}

export interface StudentContractDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName?: string
  templateId?: string
  templateCode?: string
  templateVersion?: number
  guardianId?: string
  status: string
  signedAt?: string
  signedByName?: string
  signatureMethod?: string
  signatureRef?: string
  signedFileId?: string
  startsOn?: string
  endsOn?: string
  createdAt: string
  createdBy?: string
  updatedAt: string
  rowVersion: number
}

// ── Student Consent DTOs ──────────────────────────────────────────────────────

export interface StudentConsentListItemDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName?: string
  consentTypeId?: string
  consentTypeCode?: string
  templateId?: string
  templateCode?: string
  templateVersion?: number
  guardianId?: string
  state: string
  grantedAt?: string
  withdrawnAt?: string
  validUntil?: string
  hasEvidence: boolean
  createdAt: string
}

export interface StudentConsentDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName?: string
  consentTypeId?: string
  consentTypeCode?: string
  templateId?: string
  templateCode?: string
  templateVersion?: number
  guardianId?: string
  state: string
  grantedAt?: string
  withdrawnAt?: string
  validUntil?: string
  evidenceFileId?: string
  createdAt: string
  createdBy?: string
  updatedAt: string
  rowVersion: number
}

// ── Report DTOs ───────────────────────────────────────────────────────────────

export interface ContractReportItemDto {
  studentId: string
  studentFullName: string
  totalContracts: number
  draftContracts: number
  activeContracts: number
  expiredContracts: number
  terminatedContracts: number
  latestSignedAt?: string
}

export interface ConsentReportItemDto {
  studentId: string
  studentFullName: string
  consentTypeId?: string
  consentTypeCode?: string
  hasGrantedConsent: boolean
  grantedAt?: string
  withdrawnAt?: string
  validUntil?: string
  isMandatory: boolean
}

export interface SignatureReportItemDto {
  contractId: string
  studentId: string
  studentFullName: string
  status: string
  signatureMethod?: string
  signatureRef?: string
  hasSignedFile: boolean
  signedAt?: string
  signedByName?: string
}

// ── Query Types ───────────────────────────────────────────────────────────────

export interface ContractListQuery extends PagedQuery {
  corporationId?: string
  studentId?: string
  templateId?: string
  status?: string
}

export interface ConsentListQuery extends PagedQuery {
  corporationId?: string
  studentId?: string
  consentTypeId?: string
  state?: string
}

export interface ContractTemplateListQuery extends PagedQuery {
  corporationId?: string
  contractTypeId?: string
  isCurrent?: boolean
}

export interface ConsentTemplateListQuery extends PagedQuery {
  corporationId?: string
  consentTypeId?: string
  isCurrent?: boolean
}

// ── Payload Types ─────────────────────────────────────────────────────────────

export interface CreateContractPayload {
  corporationId: string
  studentId: string
  templateId?: string
  guardianId?: string
  startsOn?: string
  endsOn?: string
}

export interface SignContractPayload {
  signatureMethod?: string
  signatureRef?: string
  rowVersion: number
}

export interface CreateConsentPayload {
  corporationId: string
  studentId: string
  consentTypeId?: string
  templateId?: string
  guardianId?: string
  validUntil?: string
}

export interface GrantConsentPayload {
  rowVersion: number
}

export interface WithdrawConsentPayload {
  rowVersion: number
}
