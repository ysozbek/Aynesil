/**
 * Leave Management API service.
 * Wraps all /api/leave endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  LeaveRequestListItemDto,
  LeaveRequestDto,
  LeaveApprovalDto,
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

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const leaveService = {
  // ── Leave Requests ─────────────────────────────────────────────────────────

  list: (query: LeaveRequestListQuery) =>
    apiService.get<PaginatedResult<LeaveRequestListItemDto>>(
      `/leave/requests${qs(query as Record<string, unknown>)}`
    ),

  get: (id: string) =>
    apiService.get<LeaveRequestDto>(`/leave/requests/${id}`),

  // Backend `SubmitLeaveRequestCommand` returns a Guid; use `fetchLeave` to
  // load the full DTO after creation (see src/Aynesil.Application/Features/Leaves).
  create: (payload: CreateLeaveRequestPayload) =>
    apiService.post<string>('/leave/requests', payload),

  update: (id: string, payload: UpdateLeaveRequestPayload) =>
    apiService.put<LeaveRequestDto>(`/leave/requests/${id}`, payload),

  // Backend approve/reject return `LeaveApprovalDto` (a single approval step),
  // not the full LeaveRequestDto — callers refetch the request after action.
  approve: (id: string, payload: ApproveLeavePayload) =>
    apiService.post<LeaveApprovalDto>(`/leave/requests/${id}/approve`, payload),

  reject: (id: string, payload: RejectLeavePayload) =>
    apiService.post<LeaveApprovalDto>(`/leave/requests/${id}/reject`, payload),

  // Backend cancel is NoContent — body is ignored by the API but the frontend
  // still sends rowVersion for future-proofing.
  cancel: (id: string, payload: CancelLeavePayload) =>
    apiService.post<void>(`/leave/requests/${id}/cancel`, payload),

  getSessionImpact: (id: string) =>
    apiService.get<LeaveSessionImpactDto[]>(`/leave/requests/${id}/session-impact`),

  // ── Calendar ───────────────────────────────────────────────────────────────

  getCalendar: (query: LeaveCalendarQuery) =>
    apiService.get<LeaveCalendarItemDto[]>(
      `/leave/calendar${qs(query as Record<string, unknown>)}`
    ),

  // ── Balances ───────────────────────────────────────────────────────────────

  getBalances: (query: LeaveBalanceQuery) =>
    apiService.get<LeaveBalanceDto[]>(
      `/leave/balances${qs(query as Record<string, unknown>)}`
    ),

  setEntitlement: (balanceId: string, payload: SetLeaveEntitlementPayload) =>
    apiService.patch<LeaveBalanceDto>(`/leave/balances/${balanceId}/entitlement`, payload),

  carryForward: (payload: CarryForwardPayload) =>
    apiService.post('/leave/balances/carry-forward', payload),

  // ── Reports ────────────────────────────────────────────────────────────────

  getUsageReport: (query: LeaveReportQuery) =>
    apiService.get<LeaveUsageReportItemDto[]>(
      `/leave/reports/usage${qs(query as Record<string, unknown>)}`
    ),

  getTrendReport: (query: LeaveReportQuery) =>
    apiService.get<LeaveTrendItemDto[]>(
      `/leave/reports/trends${qs(query as Record<string, unknown>)}`
    ),
}
