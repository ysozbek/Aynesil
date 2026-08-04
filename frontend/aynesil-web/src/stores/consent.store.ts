/**
 * Consent Management store.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { legalService } from '@/services/legal.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  ConsentTemplateListItemDto, ConsentTemplateDto,
  StudentConsentListItemDto, StudentConsentDto,
  ConsentReportItemDto,
  ConsentListQuery, ConsentTemplateListQuery,
  CreateConsentPayload, GrantConsentPayload, WithdrawConsentPayload,
} from '@/types/legal.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useConsentStore = defineStore('consent', () => {
  const templates = ref<PaginatedResult<ConsentTemplateListItemDto>>(emptyPage<ConsentTemplateListItemDto>())
  const currentTemplate = ref<ConsentTemplateDto | null>(null)
  const consents = ref<PaginatedResult<StudentConsentListItemDto>>(emptyPage<StudentConsentListItemDto>())
  const currentConsent = ref<StudentConsentDto | null>(null)
  const consentReport = ref<ConsentReportItemDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchTemplates(query: ConsentTemplateListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.listConsentTemplates(query)
      if (res.success && res.data) templates.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchTemplate(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.getConsentTemplate(id)
      if (res.success && res.data) currentTemplate.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchConsents(query: ConsentListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.listConsents(query)
      if (res.success && res.data) consents.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchConsent(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.getConsent(id)
      if (res.success && res.data) currentConsent.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createConsent(payload: CreateConsentPayload): Promise<StudentConsentDto> {
    saving.value = true
    try {
      const res = await legalService.createConsent(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rıza oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function grantConsent(id: string, payload: GrantConsentPayload) {
    saving.value = true
    try {
      const res = await legalService.grantConsent(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rıza verilemedi.')
      currentConsent.value = res.data
    } finally { saving.value = false }
  }

  async function withdrawConsent(id: string, payload: WithdrawConsentPayload) {
    saving.value = true
    try {
      const res = await legalService.withdrawConsent(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rıza geri alınamadı.')
      currentConsent.value = res.data
    } finally { saving.value = false }
  }

  async function fetchConsentReport(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await legalService.getConsentReport(query)
      if (res.success && res.data) consentReport.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  function clearCurrent() { currentConsent.value = null; currentTemplate.value = null }

  return {
    templates, currentTemplate, consents, currentConsent,
    consentReport, loading, saving, error,
    fetchTemplates, fetchTemplate, fetchConsents, fetchConsent,
    createConsent, grantConsent, withdrawConsent, fetchConsentReport, clearCurrent,
  }
})
