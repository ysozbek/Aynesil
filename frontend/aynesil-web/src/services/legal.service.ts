/**
 * Contract & Consent Management API service.
 * Wraps all /api/legal endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  ContractTemplateListItemDto, ContractTemplateDto,
  ConsentTemplateListItemDto, ConsentTemplateDto,
  StudentContractListItemDto, StudentContractDto,
  StudentConsentListItemDto, StudentConsentDto,
  ContractReportItemDto, ConsentReportItemDto, SignatureReportItemDto,
  ContractListQuery, ConsentListQuery,
  ContractTemplateListQuery, ConsentTemplateListQuery,
  CreateContractPayload, SignContractPayload,
  CreateConsentPayload, GrantConsentPayload, WithdrawConsentPayload,
} from '@/types/legal.types'

function qs(params: Record<string, unknown>): string {
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
      `/legal/contract-templates${qs(query as Record<string, unknown>)}`
    ),

  getContractTemplate: (id: string) =>
    apiService.get<ContractTemplateDto>(`/legal/contract-templates/${id}`),

  createContractTemplate: (payload: Partial<ContractTemplateDto>) =>
    apiService.post<ContractTemplateDto>('/legal/contract-templates', payload),

  // ── Student Contracts ──────────────────────────────────────────────────────
  listContracts: (query: ContractListQuery) =>
    apiService.get<PaginatedResult<StudentContractListItemDto>>(
      `/legal/contracts${qs(query as Record<string, unknown>)}`
    ),

  getContract: (id: string) =>
    apiService.get<StudentContractDto>(`/legal/contracts/${id}`),

  createContract: (payload: CreateContractPayload) =>
    apiService.post<StudentContractDto>('/legal/contracts', payload),

  sendContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/send`),

  signContract: (id: string, payload: SignContractPayload) =>
    apiService.post<StudentContractDto>(`/legal/contracts/${id}/sign`, payload),

  activateContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/activate`),

  expireContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/expire`),

  terminateContract: (id: string) =>
    apiService.post(`/legal/contracts/${id}/terminate`),

  // ── Consent Templates ──────────────────────────────────────────────────────
  listConsentTemplates: (query: ConsentTemplateListQuery) =>
    apiService.get<PaginatedResult<ConsentTemplateListItemDto>>(
      `/legal/consent-templates${qs(query as Record<string, unknown>)}`
    ),

  getConsentTemplate: (id: string) =>
    apiService.get<ConsentTemplateDto>(`/legal/consent-templates/${id}`),

  createConsentTemplate: (payload: Partial<ConsentTemplateDto>) =>
    apiService.post<ConsentTemplateDto>('/legal/consent-templates', payload),

  // ── Student Consents ───────────────────────────────────────────────────────
  listConsents: (query: ConsentListQuery) =>
    apiService.get<PaginatedResult<StudentConsentListItemDto>>(
      `/legal/consents${qs(query as Record<string, unknown>)}`
    ),

  getConsent: (id: string) =>
    apiService.get<StudentConsentDto>(`/legal/consents/${id}`),

  createConsent: (payload: CreateConsentPayload) =>
    apiService.post<StudentConsentDto>('/legal/consents', payload),

  grantConsent: (id: string, payload: GrantConsentPayload) =>
    apiService.post<StudentConsentDto>(`/legal/consents/${id}/grant`, payload),

  withdrawConsent: (id: string, payload: WithdrawConsentPayload) =>
    apiService.post<StudentConsentDto>(`/legal/consents/${id}/withdraw`, payload),

  addConsentEvidence: (id: string, fileId: string) =>
    apiService.post(`/legal/consents/${id}/evidence`, { fileId }),

  // ── Reports ────────────────────────────────────────────────────────────────
  getContractReport: (query: { corporationId?: string } & Record<string, unknown>) =>
    apiService.get<ContractReportItemDto[]>(`/legal/reports/contracts${qs(query)}`),

  getConsentReport: (query: { corporationId?: string } & Record<string, unknown>) =>
    apiService.get<ConsentReportItemDto[]>(`/legal/reports/consents${qs(query)}`),

  getSignatureReport: (query: { corporationId?: string } & Record<string, unknown>) =>
    apiService.get<SignatureReportItemDto[]>(`/legal/reports/signatures${qs(query)}`),
}
