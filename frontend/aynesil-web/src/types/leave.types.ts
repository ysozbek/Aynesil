/**
 * Leave Management type definitions.
 * Mirrors Aynesil.Application.Features.Leaves.Dtos.LeaveDtos
 */
import type { PagedQuery } from './api.types'

// ── Leave Request DTOs ─────────────────────────────────────────────────────────

export interface LeaveApprovalDto {
  id: string
  leaveRequestId: string
  stepNo: number
  approverId?: string
  decision: string
  comment?: string
  decidedAt?: string
}

export interface LeaveRequestListItemDto {
  id: string
  corporationId: string
  educatorId: string
  educatorFullName?: string
  leaveTypeId?: string
  leaveTypeCode?: string
  unit: string
  startsAt: string
  endsAt: string
  quantity?: number
  status: string
  createdAt: string
  updatedAt: string
}

export interface LeaveRequestDto {
  id: string
  corporationId: string
  educatorId: string
  educatorFullName?: string
  leaveTypeId?: string
  leaveTypeCode?: string
  unit: string
  startsAt: string
  endsAt: string
  quantity?: number
  reason?: string
  status: string
  createdAt: string
  createdBy?: string
  updatedAt: string
  rowVersion: number
  approvals: LeaveApprovalDto[]
}

// ── Leave Balance DTOs ────────────────────────────────────────────────────────

export interface LeaveBalanceDto {
  id: string
  corporationId: string
  educatorId: string
  educatorFullName?: string
  leaveTypeId?: string
  leaveTypeCode?: string
  periodYear: number
  entitled: number
  used: number
  remaining: number
  unit: string
}

// ── Calendar DTOs ─────────────────────────────────────────────────────────────

export interface LeaveCalendarItemDto {
  id: string
  educatorId: string
  educatorFullName?: string
  leaveTypeId?: string
  leaveTypeCode?: string
  unit: string
  startsAt: string
  endsAt: string
  quantity?: number
  status: string
}

// ── Session Impact DTOs ───────────────────────────────────────────────────────

export interface LeaveSessionImpactDto {
  sessionId: string
  sessionStartsAt: string
  sessionEndsAt: string
  sessionTitle?: string
  sessionStatus: string
}

// ── Report DTOs ───────────────────────────────────────────────────────────────

export interface LeaveUsageReportItemDto {
  educatorId: string
  educatorFullName: string
  leaveTypeId?: string
  leaveTypeCode?: string
  periodYear: number
  entitled: number
  used: number
  remaining: number
  unit: string
  requestCount: number
}

export interface LeaveTrendItemDto {
  year: number
  month: number
  requestCount: number
  approvedCount: number
  rejectedCount: number
  cancelledCount: number
  totalDaysApproved: number
}

// ── Query Types ───────────────────────────────────────────────────────────────

export interface LeaveRequestListQuery extends PagedQuery {
  corporationId?: string
  educatorId?: string
  leaveTypeId?: string
  status?: string
  unit?: string
  from?: string
  to?: string
}

export interface LeaveBalanceQuery {
  corporationId?: string
  educatorId?: string
  leaveTypeId?: string
  periodYear?: number
}

export interface LeaveCalendarQuery {
  corporationId?: string
  from?: string
  to?: string
  educatorId?: string
}

export interface LeaveReportQuery {
  corporationId?: string
  periodYear?: number
  leaveTypeId?: string
  educatorId?: string
}

// ── Payload Types ─────────────────────────────────────────────────────────────

export interface CreateLeaveRequestPayload {
  corporationId: string
  educatorId: string
  leaveTypeId?: string
  unit: string
  startsAt: string
  endsAt: string
  quantity?: number
  reason?: string
}

export interface UpdateLeaveRequestPayload {
  leaveTypeId?: string
  unit: string
  startsAt: string
  endsAt: string
  quantity?: number
  reason?: string
  rowVersion: number
}

export interface ApproveLeavePayload {
  comment?: string
  rowVersion: number
}

export interface RejectLeavePayload {
  comment?: string
  rowVersion: number
}

export interface CancelLeavePayload {
  rowVersion: number
}

export interface SetLeaveEntitlementPayload {
  entitled: number
}

export interface CarryForwardPayload {
  corporationId: string
  fromYear: number
  toYear: number
  leaveTypeId?: string
}
