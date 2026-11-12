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
  campusName: string | null
  roomTypeId?: string
  meetingUrl?: string
  code: string
  name: string
  capacity: number
  isVirtual: boolean
  isActive: boolean
  rowVersion: number
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
  campusId?: string
  isActive?: boolean
}

export interface CreateRoomPayload {
  /** Required for physical rooms; must be omitted for virtual rooms. */
  campusId?: string
  code: string
  name: string
  capacity: number
  isVirtual: boolean
}

export interface UpdateRoomPayload {
  code: string
  name: string
  capacity: number
  roomTypeId?: string
  meetingUrl?: string
  rowVersion: number
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
  campusId: string
  title: string
  entryType: string
  startsAt: string
  endsAt: string
  isAllDay: boolean
  description?: string
}

// ── Recurring Schedules ────────────────────────────────────────────────────────

export type RecurrenceFrequency = 'weekly' | 'biweekly' | 'monthly'

export type EducatorRole = 'lead' | 'assistant' | 'observer' | 'supervisor'

/** Mirrors EducatorAssignment / RecurringScheduleEducatorDto on the API. */
export interface RecurringScheduleEducator {
  educatorId: string
  role: EducatorRole
}

/** Mirrors RecurringScheduleDto on the API. Weekdays: 0 = Sunday … 6 = Saturday. */
export interface RecurringScheduleDto {
  id: string
  corporationId: string
  campusId?: string
  studentProgramId?: string
  sessionTypeId?: string
  roomId?: string
  frequency: RecurrenceFrequency
  intervalCount: number
  byWeekday?: number[]
  byMonthday?: number[]
  startTime: string
  durationMinutes: number
  rangeStart: string
  rangeEnd?: string
  maxOccurrences?: number
  isActive: boolean
  createdAt: string
  rowVersion: number
  programServiceId?: string
  educators: RecurringScheduleEducator[]
}

/** Mirrors RecurringScheduleListItemDto on the API. */
export interface RecurringScheduleListItemDto {
  id: string
  corporationId: string
  campusId?: string
  sessionTypeId?: string
  frequency: RecurrenceFrequency
  startTime: string
  durationMinutes: number
  rangeStart: string
  rangeEnd?: string
  isActive: boolean
  campusName?: string
  roomId?: string
  roomName?: string
  studentProgramId?: string
  studentFullName?: string
  programName?: string
  programServiceName?: string
  leadEducatorName?: string
  generatedCount: number
  /** 0 = Sunday … 6 = Saturday. */
  byWeekday?: number[]
}

export interface RecurringScheduleListQuery extends PagedQuery {
  campusId?: string
  isActive?: boolean
}

/** Mirrors CreateRecurringScheduleRequest on the API. Weekdays: 0 = Sunday … 6 = Saturday. */
export interface CreateRecurringSchedulePayload {
  campusId?: string
  studentProgramId?: string
  programServiceId?: string
  educators?: RecurringScheduleEducator[]
  roomId?: string
  sessionTypeId: string
  frequency: RecurrenceFrequency
  intervalCount: number
  byWeekday?: number[]
  byMonthday?: number[]
  /** HH:mm:ss */
  startTime: string
  durationMinutes: number
  rangeStart: string
  rangeEnd?: string
  maxOccurrences?: number
}

/** Mirrors AddRecurrenceExceptionRequest on the API. */
export interface AddRecurringExceptionPayload {
  /** ISO date (YYYY-MM-DD) for the occurrence being overridden. */
  exceptionDate: string
  /** `skip` | `reschedule` | `cancel`. */
  action: 'skip' | 'reschedule' | 'cancel'
  /** Required when action = 'reschedule'. */
  newStartAt?: string
  reason?: string
}

/** Mirrors BulkGenerateRequest on the API. */
export interface BulkGeneratePayload {
  /** ISO date (YYYY-MM-DD) start of the window. */
  windowStart: string
  /** ISO date (YYYY-MM-DD) end of the window. */
  windowEnd: string
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
  /** Package credits consumed by this session (deducted automatically on completion) */
  creditsConsumed: number
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
  authoredBy?: string
  authorName?: string
  createdAt: string
  rowVersion: number
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
  recordedBy?: string
  recordedByName?: string
}

export interface SessionDto {
  id: string
  corporationId: string
  campusId: string
  campusName?: string
  roomId?: string
  roomName?: string
  sessionTypeId: string
  sessionTypeName?: string
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
  rowVersion: number
}

export interface SessionListItemDto {
  id: string
  campusId?: string
  campusName?: string
  roomName?: string
  sessionTypeId: string
  sessionTypeName?: string
  title: string
  startsAt: string
  endsAt: string
  status: string
  isMakeup: boolean
  participantCount: number
  educatorCount: number
}

export interface SessionListQuery extends PagedQuery {
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

/** Mirrors ScheduleSessionRequest on the API. */
export interface CreateSessionPayload {
  sessionTypeId: string
  startsAt: string
  endsAt: string
  campusId?: string
  roomId?: string
  recurringScheduleId?: string
  programServiceId?: string
  title?: string
  isMakeup: boolean
  participantStudentIds: string[]
  educatorAssignments: RecurringScheduleEducator[]
}

export interface RescheduleSessionPayload {
  newStartsAt: string
  newEndsAt: string
  roomId?: string
  rowVersion: number
}

export interface CompleteSessionPayload {
  notes?: string
  rowVersion: number
}

export interface CancelSessionPayload {
  reason?: string
  rowVersion: number
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
  rowVersion: number
}

// ── Attendance ────────────────────────────────────────────────────────────────

export interface AttendanceSummaryDto {
  studentId: string
  studentFullName: string
  totalSessions: number
  presentCount: number
  absentCount: number
  lateCount: number
  excusedCount: number
  leftEarlyCount: number
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
// Mirrors Aynesil.Application.Features.Scheduling.Dtos.MakeupRequestDto.

export interface MakeupRequestDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  missedSessionId?: string
  missedReasonId?: string
  /** `requested` | `approved` | `scheduled` | `completed` | `rejected` | `expired`. */
  status: string
  requestedBy?: string
  requestedAt: string
  makeupSessionId?: string
  completedAt?: string
  expiresOn?: string
  note?: string
  updatedAt: string
  rowVersion: number
}

/** Mirrors MakeupRequestListItemDto on the API. */
export interface MakeupRequestListItemDto {
  id: string
  studentId: string
  studentFullName: string
  missedSessionId?: string
  status: string
  requestedAt: string
  expiresOn?: string
}

export interface MakeupRequestListQuery extends PagedQuery {
  studentId?: string
  status?: string
}

export interface CreateMakeupRequestPayload {
  studentId: string
  missedSessionId?: string
  missedReasonId?: string
  note?: string
  expiresOn?: string
}

export interface ApproveMakeupRequestPayload {
  rowVersion: number
}

export interface RejectMakeupRequestPayload {
  note?: string
  rowVersion: number
}

export interface AssignMakeupSessionPayload {
  makeupSessionId: string
  rowVersion: number
}

// ── Calendar Views ────────────────────────────────────────────────────────────
// School and campus calendars return `CalendarViewDto` ({ sessions, entries });
// room / educator / student calendars return `SessionListItemDto[]`.

export interface CalendarViewDto {
  sessions: SessionListItemDto[]
  entries: CalendarEntryDto[]
}

/**
 * Normalised calendar event used by the Vue calendar UI.
 * Built client-side from the raw API responses — not a backend DTO.
 */
export interface CalendarEventDto {
  id: string
  title: string
  start: string
  end: string
  type: 'session' | 'calendar_entry'
  status?: string
  roomName?: string
  participantCount?: number
  isMakeup?: boolean
}

export interface CalendarQuery {
  from: string
  to: string
}

// ── Bulk Operation Result ──────────────────────────────────────────────────────

export interface BulkOperationResultDto {
  affectedCount: number
  message: string
  roomConflictSkipped: number
  educatorConflictSkipped: number
}

// ── Conflict Check ─────────────────────────────────────────────────────────────

export interface ConflictCheckDto {
  hasRoomConflict: boolean
  hasEducatorConflict: boolean
  conflictDetails?: string[]
}
