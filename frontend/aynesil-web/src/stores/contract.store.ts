/**
 * Contract Management store.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { legalService } from '@/services/legal.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  ContractTemplateListItemDto, ContractTemplateDto,
  StudentContractListItemDto, StudentContractDto,
  ContractReportItemDto, SignatureReportItemDto,
  ContractListQuery, ContractTemplateListQuery,
  CreateContractPayload, SignContractPayload,
  CreateContractTemplatePayload, UpdateContractTemplatePayload, NewTemplateVersionPayload,
} from '@/types/legal.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useContractStore = defineStore('contract', () => {
  const templates = ref<PaginatedResult<ContractTemplateListItemDto>>(emptyPage<ContractTemplateListItemDto>())
  const currentTemplate = ref<ContractTemplateDto | null>(null)
  const contracts = ref<PaginatedResult<StudentContractListItemDto>>(emptyPage<StudentContractListItemDto>())
  const currentContract = ref<StudentContractDto | null>(null)
  const contractReport = ref<ContractReportItemDto[]>([])
  const signatureReport = ref<SignatureReportItemDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchTemplates(query: ContractTemplateListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.listContractTemplates(query)
      if (res.success && res.data) templates.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchTemplate(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.getContractTemplate(id)
      if (res.success && res.data) currentTemplate.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createTemplate(payload: CreateContractTemplatePayload): Promise<ContractTemplateDto> {
    saving.value = true
    try {
      const res = await legalService.createContractTemplate(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateTemplate(id: string, payload: UpdateContractTemplatePayload) {
    saving.value = true
    try {
      const res = await legalService.updateContractTemplate(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Şablon güncellenemedi.')
    } finally { saving.value = false }
  }

  async function newTemplateVersion(id: string, payload: NewTemplateVersionPayload): Promise<ContractTemplateDto> {
    saving.value = true
    try {
      const res = await legalService.newContractTemplateVersion(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Yeni sürüm oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function fetchContracts(query: ContractListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.listContracts(query)
      if (res.success && res.data) contracts.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchContract(id: string, silent = false) {
    loading.value = true; error.value = null
    try {
      const res = await legalService.getContract(id)
      if (res.success && res.data) currentContract.value = res.data
    } catch (e: unknown) {
      // silent: güncelleme sonrası yeniden çekme hatası kullanıcıya yansıtılmaz.
      if (silent) console.warn(`fetchContract yeniden çekilemedi`, e)
      else error.value = (e as Error).message
    }
    finally { loading.value = false }
  }

  async function createContract(payload: CreateContractPayload): Promise<StudentContractDto> {
    saving.value = true
    try {
      const res = await legalService.createContract(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Sözleşme oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function sendContract(id: string) {
    saving.value = true
    try {
      const res = await legalService.sendContract(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme gönderilemedi.')
      await fetchContract(id)
    } finally { saving.value = false }
  }

  async function signContract(id: string, payload: SignContractPayload) {
    saving.value = true
    try {
      const res = await legalService.signContract(id, payload)
      if (!res.success) throw new Error(res.message ?? 'İmzalanamadı.')
      await fetchContract(id, true)
    } finally { saving.value = false }
  }

  async function terminateContract(id: string) {
    saving.value = true
    try {
      const res = await legalService.terminateContract(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme feshedilemedi.')
      await fetchContract(id)
    } finally { saving.value = false }
  }

  async function activateContract(id: string) {
    saving.value = true
    try {
      const res = await legalService.activateContract(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme etkinleştirilemedi.')
      await fetchContract(id, true)
    } finally { saving.value = false }
  }

  async function expireContract(id: string) {
    saving.value = true
    try {
      const res = await legalService.expireContract(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme sona erdirilemedi.')
      await fetchContract(id, true)
    } finally { saving.value = false }
  }

  async function fetchContractReport(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await legalService.getContractReport(query)
      if (res.success && res.data) contractReport.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchSignatureReport(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await legalService.getSignatureReport(query)
      if (res.success && res.data) signatureReport.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  function clearCurrent() { currentContract.value = null; currentTemplate.value = null }

  return {
    templates, currentTemplate, contracts, currentContract,
    contractReport, signatureReport, loading, saving, error,
    fetchTemplates, createTemplate, updateTemplate, newTemplateVersion, fetchTemplate, fetchContracts, fetchContract,
    createContract, sendContract, activateContract, expireContract, signContract, terminateContract,
    fetchContractReport, fetchSignatureReport, clearCurrent,
  }
})
