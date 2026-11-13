/**
 * Contract & Consent Management API service.
 * Wraps all /api/legal endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CreateConsentTemplatePayload, CreateContractTemplatePayload,
  UpdateConsentTemplatePayload, UpdateContractTemplatePayload, NewTemplateVersionPayload,
} from '@/types/legal.types'
import type {
  ContractTemplateListItemDto, ContractTemplateDto,
  ConsentTemplateListItemDto, ConsentTemplateDto,
  StudentContractListItemDto, StudentContractDto,
  StudentConsentListItemDto, StudentConsentDto,
  ContractReportItemDto, ConsentReportItemDto, SignatureReportItemDto,
  ContractListQuery, ConsentListQuery,
  ContractTemplateListQuery, ConsentTemplateListQuery,
  CreateContractPayload, SignContractPayload,
  GrantConsentPayload,
} from '@/types/legal.types'

function qs(params: object): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const legalService = {
  // ── Contract Templates ─────────────────────────────────────────────────────
  listContractTemplates: (query: ContractTemplateListQuery) =>
    apiService.get<PaginatedResult<ContractTemplateListItemDto>>(
      `/legal/contract-templates${qs(query)}`
    ),

  getContractTemplate: (id: string) =>
    apiService.get<ContractTemplateDto>(`/legal/contract-templates/${id}`),

  createContractTemplate: (payload: CreateContractTemplatePayload) =>
    apiService.post<ContractTemplateDto>('/legal/contract-templates', payload),

  /** Güncel sürümün türünü/geçerliliğini/metnini günceller (kullanılmış sürümde backend reddeder). */
  updateContractTemplate: (id: string, payload: UpdateContractTemplatePayload) =>
    apiService.put<void>(`/legal/contract-templates/${id}`, payload),

  /** Güncel sürümü arşivler, yeni sürümü (v+1) üretir. */
  newContractTemplateVersion: (id: string, payload: NewTemplateVersionPayload) =>
    apiService.post<ContractTemplateDto>(`/legal/contract-templates/${id}/new-version`, payload),

  /** Bir şablon kodunun tüm sürümleri (arşiv dahil). */
  listContractTemplateVersions: (code: string) =>
    apiService.get<ContractTemplateListItemDto[]>(`/legal/contract-templates/${code}/versions`),

  // ── Student Contracts ──────────────────────────────────────────────────────
  listContracts: (query: ContractListQuery) =>
    apiService.get<PaginatedResult<StudentContractListItemDto>>(
      `/legal/contracts${qs(query)}`
    ),

  getContract: (id: string) =>
    apiService.get<StudentContractDto>(`/legal/contracts/${id}`),

  createContract: (payload: CreateContractPayload) =>
    apiService.post<StudentContractDto>('/legal/contracts', payload),

  sendContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/send`),

  signContract: (id: string, payload: SignContractPayload) =>
    apiService.post<void>(`/legal/contracts/${id}/sign`, payload),

  /** Backend binds a body (`ActivateStudentContractCommand`); `signedFileId` is optional. */
  activateContract: (id: string, payload: { signedFileId?: string } = {}) =>
    apiService.post(`/legal/contracts/${id}/activate`, payload),

  expireContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/expire`),

  terminateContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/terminate`),

  // ── Consent Templates ──────────────────────────────────────────────────────
  listConsentTemplates: (query: ConsentTemplateListQuery) =>
    apiService.get<PaginatedResult<ConsentTemplateListItemDto>>(
      `/legal/consent-templates${qs(query)}`
    ),

  getConsentTemplate: (id: string) =>
    apiService.get<ConsentTemplateDto>(`/legal/consent-templates/${id}`),

  createConsentTemplate: (payload: CreateConsentTemplatePayload) =>
    apiService.post<ConsentTemplateDto>('/legal/consent-templates', payload),

  updateConsentTemplate: (id: string, payload: UpdateConsentTemplatePayload) =>
    apiService.put<void>(`/legal/consent-templates/${id}`, payload),

  newConsentTemplateVersion: (id: string, payload: NewTemplateVersionPayload) =>
    apiService.post<ConsentTemplateDto>(`/legal/consent-templates/${id}/new-version`, payload),

  listConsentTemplateVersions: (code: string) =>
    apiService.get<ConsentTemplateListItemDto[]>(`/legal/consent-templates/${code}/versions`),

  // ── Student Consents ───────────────────────────────────────────────────────
  listConsents: (query: ConsentListQuery) =>
    apiService.get<PaginatedResult<StudentConsentListItemDto>>(
      `/legal/consents${qs(query)}`
    ),

  getConsent: (id: string) =>
    apiService.get<StudentConsentDto>(`/legal/consents/${id}`),

  /** Single endpoint: creates the consent already granted. */
  grantConsent: (payload: GrantConsentPayload) =>
    apiService.post<StudentConsentDto>('/legal/consents/grant', payload),

  /** Withdraw returns 204 (no body, no payload). */
  withdrawConsent: (id: string) =>
    apiService.post(`/legal/consents/${id}/withdraw`),

  addConsentEvidence: (id: string, evidenceFileId: string) =>
    apiService.post(`/legal/consents/${id}/evidence`, { evidenceFileId }),

  // ── Reports ────────────────────────────────────────────────────────────────
  getContractReport: (query: Record<string, unknown>) =>
    apiService.get<ContractReportItemDto[]>(`/legal/reporting/contracts${qs(query)}`),

  getConsentReport: (query: Record<string, unknown>) =>
    apiService.get<ConsentReportItemDto[]>(`/legal/reporting/consents${qs(query)}`),

  getSignatureReport: (query: Record<string, unknown>) =>
    apiService.get<SignatureReportItemDto[]>(`/legal/reporting/signatures${qs(query)}`),
}
