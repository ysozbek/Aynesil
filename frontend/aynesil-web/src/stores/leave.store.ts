/**
 * Leave Management store — requests, approvals, balances, calendar, reports.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { leaveService } from '@/services/leave.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  LeaveRequestListItemDto,
  LeaveRequestDto,
  LeaveBalanceDto,
  LeaveCalendarItemDto,
  LeaveSessionImpactDto,
  LeaveUsageReportItemDto,
  LeaveTrendItemDto,
  LeaveRequestListQuery,
  LeaveBalanceQuery,
  LeaveCalendarQuery,
  LeaveReportQuery,
  CreateLeaveRequestPayload,
  UpdateLeaveRequestPayload,
  ApproveLeavePayload,
  RejectLeavePayload,
  CancelLeavePayload,
  SetLeaveEntitlementPayload,
  CarryForwardPayload,
} from '@/types/leave.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useLeaveStore = defineStore('leave', () => {
  // ── State ──────────────────────────────────────────────────────────────────
  const leaveList = ref<PaginatedResult<LeaveRequestListItemDto>>(emptyPage<LeaveRequestListItemDto>())
  const currentLeave = ref<LeaveRequestDto | null>(null)
  const balances = ref<LeaveBalanceDto[]>([])
  const calendar = ref<LeaveCalendarItemDto[]>([])
  const sessionImpact = ref<LeaveSessionImpactDto[]>([])
  const usageReport = ref<LeaveUsageReportItemDto[]>([])
  const trendReport = ref<LeaveTrendItemDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Leave Requests ─────────────────────────────────────────────────────────

  async function fetchLeaves(query: LeaveRequestListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await leaveService.list(query)
      if (res.success && res.data) leaveList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchLeave(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await leaveService.get(id)
      if (res.success && res.data) currentLeave.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createLeave(payload: CreateLeaveRequestPayload): Promise<LeaveRequestDto> {
    saving.value = true
    try {
      const res = await leaveService.create(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzin talebi oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateLeave(id: string, payload: UpdateLeaveRequestPayload): Promise<LeaveRequestDto> {
    saving.value = true
    try {
      const res = await leaveService.update(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzin talebi güncellenemedi.')
      currentLeave.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function approveLeave(id: string, payload: ApproveLeavePayload) {
    saving.value = true
    try {
      const res = await leaveService.approve(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzin onaylanamadı.')
      if (currentLeave.value?.id === id) currentLeave.value = res.data
    } finally {
      saving.value = false
    }
  }

  async function rejectLeave(id: string, payload: RejectLeavePayload) {
    saving.value = true
    try {
      const res = await leaveService.reject(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzin reddedilemedi.')
      if (currentLeave.value?.id === id) currentLeave.value = res.data
    } finally {
      saving.value = false
    }
  }

  async function cancelLeave(id: string, payload: CancelLeavePayload) {
    saving.value = true
    try {
      const res = await leaveService.cancel(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzin iptal edilemedi.')
      if (currentLeave.value?.id === id) currentLeave.value = res.data
    } finally {
      saving.value = false
    }
  }

  async function fetchSessionImpact(id: string) {
    loading.value = true
    try {
      const res = await leaveService.getSessionImpact(id)
      if (res.success && res.data) sessionImpact.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  // ── Calendar ───────────────────────────────────────────────────────────────

  async function fetchCalendar(query: LeaveCalendarQuery) {
    loading.value = true
    try {
      const res = await leaveService.getCalendar(query)
      if (res.success && res.data) calendar.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  // ── Balances ───────────────────────────────────────────────────────────────

  async function fetchBalances(query: LeaveBalanceQuery) {
    loading.value = true
    try {
      const res = await leaveService.getBalances(query)
      if (res.success && res.data) balances.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function setEntitlement(balanceId: string, payload: SetLeaveEntitlementPayload) {
    saving.value = true
    try {
      const res = await leaveService.setEntitlement(balanceId, payload)
      if (!res.success) throw new Error(res.message ?? 'Hak güncellenemedi.')
      await fetchBalances({})
    } finally {
      saving.value = false
    }
  }

  async function carryForward(payload: CarryForwardPayload) {
    saving.value = true
    try {
      const res = await leaveService.carryForward(payload)
      if (!res.success) throw new Error(res.message ?? 'Devir işlemi yapılamadı.')
    } finally {
      saving.value = false
    }
  }

  // ── Reports ────────────────────────────────────────────────────────────────

  async function fetchUsageReport(query: LeaveReportQuery) {
    loading.value = true
    try {
      const res = await leaveService.getUsageReport(query)
      if (res.success && res.data) usageReport.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchTrendReport(query: LeaveReportQuery) {
    loading.value = true
    try {
      const res = await leaveService.getTrendReport(query)
      if (res.success && res.data) trendReport.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  function clearCurrent() {
    currentLeave.value = null
    sessionImpact.value = []
  }

  return {
    leaveList, currentLeave, balances, calendar,
    sessionImpact, usageReport, trendReport,
    loading, saving, error,
    fetchLeaves, fetchLeave, createLeave, updateLeave,
    approveLeave, rejectLeave, cancelLeave, fetchSessionImpact,
    fetchCalendar, fetchBalances, setEntitlement, carryForward,
    fetchUsageReport, fetchTrendReport, clearCurrent,
  }
})
