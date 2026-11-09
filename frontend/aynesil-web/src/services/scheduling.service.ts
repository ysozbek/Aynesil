/**
 * Scheduling API service.
 * Wraps all /api/scheduling endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  RoomDto,
  RoomListItemDto,
  RoomListQuery,
  CreateRoomPayload,
  UpdateRoomPayload,
  CalendarEntryDto,
  CreateCalendarEntryPayload,
  RecurringScheduleDto,
  RecurringScheduleListItemDto,
  RecurringScheduleListQuery,
  CreateRecurringSchedulePayload,
  AddRecurringExceptionPayload,
  BulkGeneratePayload,
  BulkCancelPayload,
  BulkReassignRoomPayload,
  SessionDto,
  SessionListItemDto,
  SessionListQuery,
  CreateSessionPayload,
  RescheduleSessionPayload,
  CompleteSessionPayload,
  CancelSessionPayload,
  AddParticipantPayload,
  AddEducatorPayload,
  UpdateSessionGoalPayload,
  CreateSessionNotePayload,
  UpdateSessionNotePayload,
  AttendanceDto,
  AttendanceSummaryDto,
  RecordAttendancePayload,
  BulkAttendancePayload,
  StudentAttendanceQuery,
  MakeupRequestDto,
  MakeupRequestListItemDto,
  MakeupRequestListQuery,
  CreateMakeupRequestPayload,
  ApproveMakeupRequestPayload,
  RejectMakeupRequestPayload,
  AssignMakeupSessionPayload,
  CalendarViewDto,
  CalendarQuery,
  BulkOperationResultDto,
} from '@/types/scheduling.types'

function qs(params: object): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const schedulingService = {
  // ── Rooms ──────────────────────────────────────────────────────────────────

  listRooms: (query: RoomListQuery) =>
    apiService.get<PaginatedResult<RoomListItemDto>>(
      `/scheduling/rooms${qs(query)}`
    ),

  getRoom: (id: string) =>
    apiService.get<RoomDto>(`/scheduling/rooms/${id}`),

  createRoom: (payload: CreateRoomPayload) =>
    apiService.post<RoomDto>('/scheduling/rooms', payload),

  updateRoom: (id: string, payload: UpdateRoomPayload) =>
    apiService.put<RoomDto>(`/scheduling/rooms/${id}`, payload),

  deactivateRoom: (id: string) =>
    apiService.post(`/scheduling/rooms/${id}/deactivate`),

  deleteRoom: (id: string) =>
    apiService.delete(`/scheduling/rooms/${id}`),

  // ── Calendar Entries ───────────────────────────────────────────────────────

  listCalendarEntries: (corporationId: string, campusId?: string, from?: string, to?: string) =>
    apiService.get<CalendarEntryDto[]>(
      `/scheduling/calendar-entries${qs({ corporationId, campusId, from, to })}`
    ),

  createCalendarEntry: (payload: CreateCalendarEntryPayload) =>
    apiService.post<CalendarEntryDto>('/scheduling/calendar-entries', payload),

  deleteCalendarEntry: (id: string) =>
    apiService.delete(`/scheduling/calendar-entries/${id}`),

  // ── Recurring Schedules ────────────────────────────────────────────────────

  listRecurringSchedules: (query: RecurringScheduleListQuery) =>
    apiService.get<PaginatedResult<RecurringScheduleListItemDto>>(
      `/scheduling/recurring-schedules${qs(query)}`
    ),

  getRecurringSchedule: (id: string) =>
    apiService.get<RecurringScheduleDto>(`/scheduling/recurring-schedules/${id}`),

  createRecurringSchedule: (payload: CreateRecurringSchedulePayload) =>
    apiService.post<RecurringScheduleDto>('/scheduling/recurring-schedules', payload),

  deactivateRecurringSchedule: (id: string) =>
    apiService.post(`/scheduling/recurring-schedules/${id}/deactivate`),

  addRecurringException: (id: string, payload: AddRecurringExceptionPayload) =>
    apiService.post(`/scheduling/recurring-schedules/${id}/exceptions`, payload),

  /**
   * Materialises concrete session rows from a recurring schedule for the given window.
   * API requires `{ windowStart, windowEnd }` in the body.
   */
  generateSessions: (id: string, payload: BulkGeneratePayload) =>
    apiService.post<BulkOperationResultDto>(
      `/scheduling/recurring-schedules/${id}/generate`,
      payload
    ),

  bulkCancelSessions: (id: string, payload: BulkCancelPayload) =>
    apiService.post<BulkOperationResultDto>(`/scheduling/recurring-schedules/${id}/bulk-cancel`, payload),

  bulkReassignRoom: (id: string, payload: BulkReassignRoomPayload) =>
    apiService.post<BulkOperationResultDto>(`/scheduling/recurring-schedules/${id}/bulk-reassign-room`, payload),

  // ── Sessions ──────────────────────────────────────────────────────────────

  listSessions: (query: SessionListQuery) =>
    apiService.get<PaginatedResult<SessionListItemDto>>(
      `/scheduling/sessions${qs(query)}`
    ),

  getSession: (id: string) =>
    apiService.get<SessionDto>(`/scheduling/sessions/${id}`),

  createSession: (payload: CreateSessionPayload) =>
    apiService.post<SessionDto>('/scheduling/sessions', payload),

  rescheduleSession: (id: string, payload: RescheduleSessionPayload) =>
    apiService.put<SessionDto>(`/scheduling/sessions/${id}/reschedule`, payload),

  completeSession: (id: string, payload: CompleteSessionPayload) =>
    apiService.post<SessionDto>(`/scheduling/sessions/${id}/complete`, payload),

  cancelSession: (id: string, payload: CancelSessionPayload) =>
    apiService.post<SessionDto>(`/scheduling/sessions/${id}/cancel`, payload),

  noShowSession: (id: string, payload: { rowVersion: number }) =>
    apiService.post<SessionDto>(`/scheduling/sessions/${id}/no-show`, payload),

  deleteSession: (id: string) =>
    apiService.delete(`/scheduling/sessions/${id}`),

  // ── Session Participants ──────────────────────────────────────────────────

  addParticipant: (sessionId: string, payload: AddParticipantPayload) =>
    apiService.post(`/scheduling/sessions/${sessionId}/participants`, payload),

  removeParticipant: (sessionId: string, studentId: string) =>
    apiService.delete(`/scheduling/sessions/${sessionId}/participants/${studentId}`),

  // ── Session Educators ────────────────────────────────────────────────────

  addEducator: (sessionId: string, payload: AddEducatorPayload) =>
    apiService.post(`/scheduling/sessions/${sessionId}/educators`, payload),

  removeEducator: (sessionId: string, educatorId: string) =>
    apiService.delete(`/scheduling/sessions/${sessionId}/educators/${educatorId}`),

  // ── Session Goals ─────────────────────────────────────────────────────────

  updateSessionGoal: (sessionId: string, studentGoalId: string, payload: UpdateSessionGoalPayload) =>
    apiService.put(`/scheduling/sessions/${sessionId}/goals/${studentGoalId}`, payload),

  removeSessionGoal: (sessionId: string, studentGoalId: string) =>
    apiService.delete(`/scheduling/sessions/${sessionId}/goals/${studentGoalId}`),

  // ── Session Notes ────────────────────────────────────────────────────────

  addSessionNote: (sessionId: string, payload: CreateSessionNotePayload) =>
    apiService.post(`/scheduling/sessions/${sessionId}/notes`, payload),

  updateSessionNote: (sessionId: string, noteId: string, payload: UpdateSessionNotePayload) =>
    apiService.put(`/scheduling/sessions/${sessionId}/notes/${noteId}`, payload),

  deleteSessionNote: (sessionId: string, noteId: string) =>
    apiService.delete(`/scheduling/sessions/${sessionId}/notes/${noteId}`),

  // ── Attendance ────────────────────────────────────────────────────────────

  getSessionAttendance: (sessionId: string) =>
    apiService.get<AttendanceDto[]>(`/scheduling/sessions/${sessionId}/attendance`),

  recordAttendance: (sessionId: string, payload: RecordAttendancePayload) =>
    apiService.post<AttendanceDto>(`/scheduling/sessions/${sessionId}/attendance`, payload),

  bulkRecordAttendance: (sessionId: string, payload: BulkAttendancePayload) =>
    apiService.post<AttendanceDto[]>(`/scheduling/sessions/${sessionId}/attendance/bulk`, payload),

  /**
   * API requires `corporationId` on both student-attendance endpoints
   * (`SchedulingController.cs` /scheduling/students/{id}/attendance[+/summary]).
   */
  getStudentAttendance: (
    studentId: string,
    corporationId: string,
    query?: StudentAttendanceQuery
  ) =>
    apiService.get<PaginatedResult<AttendanceDto>>(
      `/scheduling/students/${studentId}/attendance${qs({
        corporationId,
        ...(query ?? {}),
      } as Record<string, unknown>)}`
    ),

  getAttendanceSummary: (studentId: string, corporationId: string) =>
    apiService.get<AttendanceSummaryDto>(
      `/scheduling/students/${studentId}/attendance/summary${qs({ corporationId })}`
    ),

  // ── Makeup Requests ────────────────────────────────────────────────────────

  listMakeupRequests: (query: MakeupRequestListQuery) =>
    apiService.get<PaginatedResult<MakeupRequestListItemDto>>(
      `/scheduling/makeup-requests${qs(query)}`
    ),

  getMakeupRequest: (id: string) =>
    apiService.get<MakeupRequestDto>(`/scheduling/makeup-requests/${id}`),

  createMakeupRequest: (payload: CreateMakeupRequestPayload) =>
    apiService.post<MakeupRequestDto>('/scheduling/makeup-requests', payload),

  approveMakeupRequest: (id: string, payload: ApproveMakeupRequestPayload) =>
    apiService.post<MakeupRequestDto>(`/scheduling/makeup-requests/${id}/approve`, payload),

  rejectMakeupRequest: (id: string, payload: RejectMakeupRequestPayload) =>
    apiService.post<MakeupRequestDto>(`/scheduling/makeup-requests/${id}/reject`, payload),

  assignMakeupSession: (id: string, payload: AssignMakeupSessionPayload) =>
    apiService.post<MakeupRequestDto>(`/scheduling/makeup-requests/${id}/assign-session`, payload),

  completeMakeupRequest: (id: string, payload: { rowVersion: number }) =>
    apiService.post<MakeupRequestDto>(`/scheduling/makeup-requests/${id}/complete`, payload),

  // ── Calendar Views ─────────────────────────────────────────────────────────
  // School + campus return `CalendarViewDto` ({ sessions, entries }).
  // Room / educator / student return `SessionListItemDto[]`.
  // Campus / educator / student all require `corporationId`.

  getSchoolCalendar: (corporationId: string, query: CalendarQuery) =>
    apiService.get<CalendarViewDto>(
      `/scheduling/calendar/school${qs({ corporationId, ...query })}`
    ),

  getCampusCalendar: (campusId: string, corporationId: string, query: CalendarQuery) =>
    apiService.get<CalendarViewDto>(
      `/scheduling/calendar/campus/${campusId}${qs({ corporationId, ...query })}`
    ),

  getRoomCalendar: (roomId: string, query: CalendarQuery) =>
    apiService.get<SessionListItemDto[]>(
      `/scheduling/calendar/room/${roomId}${qs(query)}`
    ),

  getEducatorCalendar: (educatorId: string, corporationId: string, query: CalendarQuery) =>
    apiService.get<SessionListItemDto[]>(
      `/scheduling/calendar/educator/${educatorId}${qs({ corporationId, ...query })}`
    ),

  getStudentCalendar: (studentId: string, corporationId: string, query: CalendarQuery) =>
    apiService.get<SessionListItemDto[]>(
      `/scheduling/calendar/student/${studentId}${qs({ corporationId, ...query })}`
    ),
}
