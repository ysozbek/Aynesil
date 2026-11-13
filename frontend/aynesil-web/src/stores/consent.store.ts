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
  GrantConsentPayload,
  CreateConsentTemplatePayload, UpdateConsentTemplatePayload, NewTemplateVersionPayload,
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

  async function createTemplate(payload: CreateConsentTemplatePayload): Promise<ConsentTemplateDto> {
    saving.value = true
    try {
      const res = await legalService.createConsentTemplate(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateTemplate(id: string, payload: UpdateConsentTemplatePayload) {
    saving.value = true
    try {
      const res = await legalService.updateConsentTemplate(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Şablon güncellenemedi.')
    } finally { saving.value = false }
  }

  async function newTemplateVersion(id: string, payload: NewTemplateVersionPayload): Promise<ConsentTemplateDto> {
    saving.value = true
    try {
      const res = await legalService.newConsentTemplateVersion(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Yeni sürüm oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function fetchConsents(query: ConsentListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.listConsents(query)
      if (res.success && res.data) consents.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchConsent(id: string, silent = false) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.getConsent(id)
      if (res.success && res.data) currentConsent.value = res.data
    } catch (e: unknown) {
      // silent: güncelleme sonrası yeniden çekme hatası kullanıcıya yansıtılmaz.
      if (silent) console.warn(`fetchConsent yeniden çekilemedi`, e)
      else error.value = (e as Error).message
    }
    finally { loading.value = false }
  }

  async function grantConsent(payload: GrantConsentPayload): Promise<StudentConsentDto> {
    saving.value = true
    try {
      const res = await legalService.grantConsent(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rıza verilemedi.')
      currentConsent.value = res.data
      return res.data
    } finally { saving.value = false }
  }

  async function withdrawConsent(id: string) {
    saving.value = true
    try {
      // 204 NoContent: no res.data; reload the record to reflect the withdrawn state.
      const res = await legalService.withdrawConsent(id)
      if (!res.success) throw new Error(res.message ?? 'Rıza geri alınamadı.')
      await fetchConsent(id, true)
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
    fetchTemplates, fetchTemplate, createTemplate, updateTemplate, newTemplateVersion, fetchConsents, fetchConsent,
    grantConsent, withdrawConsent, fetchConsentReport, clearCurrent,
  }
})
