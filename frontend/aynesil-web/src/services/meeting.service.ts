/**
 * Meeting Management API service.
 * Wraps all /api/meetings endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  MeetingDto,
  MeetingListItemDto,
  MeetingListQuery,
  MeetingCalendarItemDto,
  MeetingCalendarQuery,
  ScheduleMeetingPayload,
  UpdateMeetingPayload,
  AddParticipantPayload,
  UpdateAttendancePayload,
  AddOutcomePayload,
  UpdateOutcomePayload,
  AddFollowUpPayload,
  UpdateFollowUpPayload,
  UpdateFollowUpStatusPayload,
} from '@/types/meeting.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const meetingService = {
  // ── Meetings ───────────────────────────────────────────────────────────────

  list: (query: MeetingListQuery) =>
    apiService.get<PaginatedResult<MeetingListItemDto>>(
      `/meetings${qs(query as Record<string, unknown>)}`
    ),

  get: (id: string) =>
    apiService.get<MeetingDto>(`/meetings/${id}`),

  schedule: (payload: ScheduleMeetingPayload) =>
    apiService.post<MeetingDto>('/meetings', payload),

  update: (id: string, payload: UpdateMeetingPayload) =>
    apiService.put<MeetingDto>(`/meetings/${id}`, payload),

  delete: (id: string) =>
    apiService.delete(`/meetings/${id}`),

  complete: (id: string) =>
    apiService.post<MeetingDto>(`/meetings/${id}/complete`),

  cancel: (id: string) =>
    apiService.post<MeetingDto>(`/meetings/${id}/cancel`),

  // ── Participants ───────────────────────────────────────────────────────────

  addParticipant: (meetingId: string, payload: AddParticipantPayload) =>
    apiService.post(`/meetings/${meetingId}/participants`, payload),

  updateParticipantAttendance: (participantId: string, payload: UpdateAttendancePayload) =>
    apiService.patch(`/meetings/participants/${participantId}/attendance`, payload),

  removeParticipant: (participantId: string) =>
    apiService.delete(`/meetings/participants/${participantId}`),

  // ── Outcomes ───────────────────────────────────────────────────────────────

  addOutcome: (meetingId: string, payload: AddOutcomePayload) =>
    apiService.post(`/meetings/${meetingId}/outcomes`, payload),

  updateOutcome: (outcomeId: string, payload: UpdateOutcomePayload) =>
    apiService.put(`/meetings/outcomes/${outcomeId}`, payload),

  // ── Follow-Ups ─────────────────────────────────────────────────────────────

  addFollowUp: (meetingId: string, payload: AddFollowUpPayload) =>
    apiService.post(`/meetings/${meetingId}/follow-ups`, payload),

  updateFollowUp: (followUpId: string, payload: UpdateFollowUpPayload) =>
    apiService.put(`/meetings/follow-ups/${followUpId}`, payload),

  updateFollowUpStatus: (followUpId: string, payload: UpdateFollowUpStatusPayload) =>
    apiService.patch(`/meetings/follow-ups/${followUpId}/status`, payload),

  // ── Calendar ───────────────────────────────────────────────────────────────

  getCalendar: (query: MeetingCalendarQuery) =>
    apiService.get<MeetingCalendarItemDto[]>(
      `/meetings/calendar${qs(query as Record<string, unknown>)}`
    ),
}
