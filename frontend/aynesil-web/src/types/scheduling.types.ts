/**
 * Scheduling & Session type definitions.
 * Mirrors Aynesil.Application.Features.Scheduling.Dtos.SchedulingDtos
 */
import type { PagedQuery } from './api.types'

// ── Rooms ──────────────────────────────────────────────────────────────────────

export interface RoomDto {
  id: string
  corporationId: string
  campusId: string
  campusName: string
  code: string
  name: string
  capacity: number
  isVirtual: boolean
  isActive: boolean
  rowVersion: string
}

export interface RoomListItemDto {
  id: string
  campusId: string
  campusName: string
  code: string
  name: string
  capacity: number
  isVirtual: boolean
  isActive: boolean
}

export interface RoomListQuery extends PagedQuery {
  corporationId: string
  campusId?: string
  isActive?: boolean
}

export interface CreateRoomPayload {
  corporationId: string
  campusId: string
  code: string
  name: string
  capacity: number
  isVirtual: boolean
}

export interface UpdateRoomPayload {
  name: string
  capacity: number
  isVirtual: boolean
  rowVersion: string
}

// ── Calendar Entries ───────────────────────────────────────────────────────────

export interface CalendarEntryDto {
  id: string
  corporationId: string
  campusId: string
  title: string
  entryType: string
  startsAt: string
  endsAt: string
  isAllDay: boolean
  description?: string
}

export interface CreateCalendarEntryPayload {
  corporationId: string
  campusId: string
  title: string
  entryType: string
  startsAt: string
  endsAt: string
  isAllDay: boolean
  description?: string
}

// ── Recurring Schedules ────────────────────────────────────────────────────────

export interface RecurringScheduleDto {
  id: string
  corporationId: string
  campusId: string
  roomId?: string
  roomName?: string
  sessionTypeId: string
  sessionTypeLabel?: string
  frequency: string
  intervalCount: number
  byWeekday?: string[]
  startTime: string
  durationMinutes: number
  rangeStart: string
  rangeEnd?: string
  maxOccurrences?: number
  isActive: boolean
  generatedCount: number
  educatorIds: string[]
  rowVersion: string
}

export interface RecurringScheduleListItemDto {
  id: string
  campusId: string
  campusName: string
  roomName?: string
  sessionTypeLabel?: string
  frequency: string
  startTime: string
  durationMinutes: number
  rangeStart: string
  rangeEnd?: string
  isActive: boolean
  generatedCount: number
}

export interface RecurringScheduleListQuery extends PagedQuery {
  corporationId: string
  campusId?: string
  isActive?: boolean
}

export interface CreateRecurringSchedulePayload {
  corporationId: string
  campusId: string
  roomId?: string
  sessionTypeId: string
  frequency: string
  intervalCount: number
  byWeekday?: string[]
  startTime: string
  durationMinutes: number
  rangeStart: string
  rangeEnd?: string
  maxOccurrences?: number
  educatorIds?: string[]
}

export interface AddRecurringExceptionPayload {
  date: string
  reason?: string
}

export interface BulkCancelPayload {
  from: string
  to: string
  reason?: string
}

export interface BulkReassignRoomPayload {
  newRoomId: string
  from: string
  to: string
}

// ── Sessions ──────────────────────────────────────────────────────────────────

export interface SessionParticipantDto {
  studentId: string
  studentFullName: string
  role: string
}

export interface SessionEducatorDto {
  educatorId: string
  educatorFullName: string
  role: string
}

export interface SessionGoalDto {
  studentGoalId: string
  goalStatement: string
  workedOn: boolean
  progressNote?: string
  measuredValue?: number
}

export interface SessionNoteDto {
  id: string
  sessionId: string
  body: string
  parentVisible: boolean
  authorName: string
  createdAt: string
  rowVersion: string
}

export interface AttendanceDto {
  id: string
  sessionId: string
  studentId: string
  studentFullName: string
  status: string
  reasonId?: string
  reasonLabel?: string
  minutesAttended?: number
  note?: string
  recordedAt: string
  recordedByName: string
}

export interface SessionDto {
  id: string
  corporationId: string
  campusId: string
  campusName: string
  roomId?: string
  roomName?: string
  sessionTypeId: string
  sessionTypeLabel?: string
  recurringScheduleId?: string
  title: string
  startsAt: string
  endsAt: string
  status: string
  isMakeup: boolean
  makeupRequestId?: string
  participants: SessionParticipantDto[]
  educators: SessionEducatorDto[]
  goals: SessionGoalDto[]
  notes: SessionNoteDto[]
  attendances: AttendanceDto[]
  rowVersion: string
}

export interface SessionListItemDto {
  id: string
  campusName: string
  roomName?: string
  sessionTypeLabel?: string
  title: string
  startsAt: string
  endsAt: string
  status: string
  isMakeup: boolean
  participantCount: number
  educatorCount: number
}

export interface SessionListQuery extends PagedQuery {
  corporationId: string
  campusId?: string
  roomId?: string
  educatorId?: string
  studentId?: string
  sessionTypeId?: string
  status?: string
  from?: string
  to?: string
  isMakeup?: boolean
}

export interface CreateSessionPayload {
  corporationId: string
  campusId: string
  roomId?: string
  sessionTypeId: string
  recurringScheduleId?: string
  title: string
  startsAt: string
  endsAt: string
}

export interface RescheduleSessionPayload {
  startsAt: string
  endsAt: string
  roomId?: string
  rowVersion: string
}

export interface CompleteSessionPayload {
  notes?: string
  rowVersion: string
}

export interface CancelSessionPayload {
  reason?: string
  rowVersion: string
}

export interface AddParticipantPayload {
  studentId: string
  role?: string
}

export interface AddEducatorPayload {
  educatorId: string
  role?: string
}

export interface UpdateSessionGoalPayload {
  workedOn: boolean
  progressNote?: string
  measuredValue?: number
}

export interface CreateSessionNotePayload {
  body: string
  parentVisible: boolean
}

export interface UpdateSessionNotePayload {
  body: string
  parentVisible: boolean
  rowVersion: string
}

// ── Attendance ────────────────────────────────────────────────────────────────

export interface AttendanceSummaryDto {
  studentId: string
  studentFullName: string
  totalSessions: number
  present: number
  absent: number
  late: number
  excused: number
  leftEarly: number
  attendanceRate: number
}

export interface RecordAttendancePayload {
  studentId: string
  status: string
  reasonId?: string
  minutesAttended?: number
  note?: string
}

export interface BulkAttendancePayload {
  entries: RecordAttendancePayload[]
}

export interface StudentAttendanceQuery extends PagedQuery {
  from?: string
  to?: string
}

// ── Makeup Requests ────────────────────────────────────────────────────────────

export interface MakeupRequestDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  missedSessionId: string
  missedSessionTitle: string
  missedSessionDate: string
  status: string
  makeupSessionId?: string
  makeupSessionDate?: string
  requestedBy: string
  requestedAt: string
  approvedBy?: string
  approvedAt?: string
  expiresOn?: string
  reason?: string
  rowVersion: string
}

export interface MakeupRequestListItemDto {
  id: string
  studentFullName: string
  missedSessionTitle: string
  missedSessionDate: string
  status: string
  makeupSessionDate?: string
  expiresOn?: string
  requestedAt: string
}

export interface MakeupRequestListQuery extends PagedQuery {
  corporationId: string
  studentId?: string
  status?: string
  from?: string
  to?: string
}

export interface CreateMakeupRequestPayload {
  corporationId: string
  studentId: string
  missedSessionId: string
  reason?: string
  expiresOn?: string
}

export interface ApproveMakeupRequestPayload {
  rowVersion: string
}

export interface RejectMakeupRequestPayload {
  reason?: string
  rowVersion: string
}

export interface AssignMakeupSessionPayload {
  makeupSessionId: string
  rowVersion: string
}

// ── Calendar Views ────────────────────────────────────────────────────────────

export interface CalendarEventDto {
  id: string
  title: string
  start: string
  end: string
  type: 'session' | 'calendar_entry' | 'recurring'
  status?: string
  roomName?: string
  educatorNames?: string[]
  participantCount?: number
  isMakeup?: boolean
  color?: string
}

export interface CalendarQuery {
  from: string
  to: string
}

// ── Bulk Operation Result ──────────────────────────────────────────────────────

export interface BulkOperationResultDto {
  affectedCount: number
  message: string
}

// ── Conflict Check ─────────────────────────────────────────────────────────────

export interface ConflictCheckDto {
  hasRoomConflict: boolean
  hasEducatorConflict: boolean
  conflictDetails?: string[]
}
