/**
 * Payment store — transactions and refunds.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { financeService } from '@/services/finance.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  PaymentDto,
  PaymentListItemDto,
  PaymentListQuery,
  CreatePaymentPayload,
  RefundDto,
  RefundListItemDto,
  RefundListQuery,
  CreateRefundPayload,
} from '@/types/finance.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const usePaymentStore = defineStore('payment', () => {
  const transactionList = ref<PaginatedResult<PaymentListItemDto>>(emptyPage<PaymentListItemDto>())
  const currentTransaction = ref<PaymentDto | null>(null)
  const refundList = ref<PaginatedResult<RefundListItemDto>>(emptyPage<RefundListItemDto>())
  const currentRefund = ref<RefundDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Transactions ───────────────────────────────────────────────────────────

  async function fetchTransactions(query: PaymentListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.listTransactions(query)
      if (res.success && res.data) transactionList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchTransaction(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.getTransaction(id)
      if (res.success && res.data) currentTransaction.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createTransaction(payload: CreatePaymentPayload): Promise<PaymentDto> {
    saving.value = true
    try {
      const res = await financeService.createTransaction(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Ödeme kaydedilemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function captureTransaction(id: string, rowVersion: string): Promise<PaymentDto> {
    saving.value = true
    try {
      const res = await financeService.captureTransaction(id, { rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'Ödeme onaylanamadı.')
      currentTransaction.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function failTransaction(id: string, reason: string, rowVersion: string): Promise<PaymentDto> {
    saving.value = true
    try {
      const res = await financeService.failTransaction(id, { reason, rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'Ödeme başarısız işaretilemedi.')
      currentTransaction.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  // ── Refunds ────────────────────────────────────────────────────────────────

  async function fetchRefunds(query: RefundListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.listRefunds(query)
      if (res.success && res.data) refundList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchRefund(id: string) {
    loading.value = true
    try {
      const res = await financeService.getRefund(id)
      if (res.success && res.data) currentRefund.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createRefund(payload: CreateRefundPayload): Promise<RefundDto> {
    saving.value = true
    try {
      const res = await financeService.createRefund(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İade oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function processRefund(id: string, rowVersion: string): Promise<RefundDto> {
    saving.value = true
    try {
      const res = await financeService.processRefund(id, { rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'İade işlenemedi.')
      currentRefund.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentTransaction.value = null
    currentRefund.value = null
  }

  return {
    transactionList, currentTransaction,
    refundList, currentRefund,
    loading, saving, error,
    fetchTransactions, fetchTransaction, createTransaction, captureTransaction, failTransaction,
    fetchRefunds, fetchRefund, createRefund, processRefund,
    clearCurrent,
  }
})
