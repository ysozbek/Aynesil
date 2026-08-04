/**
 * Invoice store — invoice lifecycle and line management.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { financeService } from '@/services/finance.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  InvoiceDto,
  InvoiceListItemDto,
  InvoiceListQuery,
  CreateInvoicePayload,
  AddInvoiceLinePayload,
} from '@/types/finance.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useInvoiceStore = defineStore('invoice', () => {
  const invoiceList = ref<PaginatedResult<InvoiceListItemDto>>(emptyPage<InvoiceListItemDto>())
  const currentInvoice = ref<InvoiceDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchInvoices(query: InvoiceListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.listInvoices(query)
      if (res.success && res.data) invoiceList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchInvoice(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.getInvoice(id)
      if (res.success && res.data) currentInvoice.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createInvoice(payload: CreateInvoicePayload): Promise<InvoiceDto> {
    saving.value = true
    try {
      const res = await financeService.createInvoice(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Fatura oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function addInvoiceLine(id: string, payload: AddInvoiceLinePayload): Promise<InvoiceDto> {
    saving.value = true
    try {
      const res = await financeService.addInvoiceLine(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Fatura kalemi eklenemedi.')
      currentInvoice.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function removeInvoiceLine(id: string, lineId: string) {
    saving.value = true
    try {
      await financeService.removeInvoiceLine(id, lineId)
      if (currentInvoice.value?.id === id) await fetchInvoice(id)
    } finally {
      saving.value = false
    }
  }

  async function issueInvoice(id: string, rowVersion: string): Promise<InvoiceDto> {
    saving.value = true
    try {
      const res = await financeService.issueInvoice(id, { rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'Fatura kesilemedi.')
      currentInvoice.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function voidInvoice(id: string, reason: string, rowVersion: string): Promise<InvoiceDto> {
    saving.value = true
    try {
      const res = await financeService.voidInvoice(id, { reason, rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'Fatura iptal edilemedi.')
      currentInvoice.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentInvoice.value = null
  }

  return {
    invoiceList, currentInvoice, loading, saving, error,
    fetchInvoices, fetchInvoice, createInvoice,
    addInvoiceLine, removeInvoiceLine,
    issueInvoice, voidInvoice,
    clearCurrent,
  }
})
