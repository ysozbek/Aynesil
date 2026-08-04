/**
 * Credit ledger store — credit operations and student summary.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { financeService } from '@/services/finance.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CreditLedgerEntryDto,
  CreditSummaryDto,
  CreditLedgerQuery,
  ConsumeCreditPayload,
  GrantCreditPayload,
  RefundCreditPayload,
  AdjustCreditPayload,
} from '@/types/finance.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useCreditLedgerStore = defineStore('creditLedger', () => {
  const entryList = ref<PaginatedResult<CreditLedgerEntryDto>>(emptyPage<CreditLedgerEntryDto>())
  const summary = ref<CreditSummaryDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchCredits(query: CreditLedgerQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.listCredits(query)
      if (res.success && res.data) entryList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchCreditSummary(studentId: string) {
    loading.value = true
    try {
      const res = await financeService.getCreditSummary(studentId)
      if (res.success && res.data) summary.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function consumeCredits(payload: ConsumeCreditPayload): Promise<CreditLedgerEntryDto> {
    saving.value = true
    try {
      const res = await financeService.consumeCredits(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kredi tüketilemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function grantCredits(payload: GrantCreditPayload): Promise<CreditLedgerEntryDto> {
    saving.value = true
    try {
      const res = await financeService.grantCredits(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kredi verilemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function refundCredits(payload: RefundCreditPayload): Promise<CreditLedgerEntryDto> {
    saving.value = true
    try {
      const res = await financeService.refundCredits(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kredi iadesi yapılamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function adjustCredits(payload: AdjustCreditPayload): Promise<CreditLedgerEntryDto> {
    saving.value = true
    try {
      const res = await financeService.adjustCredits(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kredi düzeltmesi yapılamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearSummary() {
    summary.value = null
    entryList.value = emptyPage<CreditLedgerEntryDto>()
  }

  return {
    entryList, summary, loading, saving, error,
    fetchCredits, fetchCreditSummary,
    consumeCredits, grantCredits, refundCredits, adjustCredits,
    clearSummary,
  }
})
