/**
 * Meeting Management type definitions.
 * Mirrors Aynesil.Application.Features.Meetings.Dtos.MeetingDtos
 */
import type { PagedQuery } from './api.types'

// ── Meeting DTOs ───────────────────────────────────────────────────────────────

export interface MeetingListItemDto {
  id: string
  corporationId: string
  campusId?: string
  meetingTypeId?: string
  meetingTypeCode?: string
  title: string
  location?: string
  scheduledAt?: string
  endsAt?: string
  status: string
  organizerId?: string
  participantCount: number
  updatedAt: string
}

export interface MeetingParticipantDto {
  id: string
  meetingId: string
  corporationId: string
  participantType: string
  userId?: string
  guardianId?: string
  leadId?: string
  externalName?: string
  attendance?: string
}

export interface MeetingOutcomeDto {
  id: string
  meetingId: string
  summary?: string
  decisions?: string
  createdAt: string
  createdBy?: string
}

export interface MeetingFollowUpDto {
  id: string
  meetingId: string
  action: string
  assigneeId?: string
  dueDate?: string
  status: string
  createdAt: string
}

export interface MeetingDto {
  id: string
  corporationId: string
  campusId?: string
  meetingTypeId?: string
  meetingTypeCode?: string
  title: string
  location?: string
  roomId?: string
  scheduledAt?: string
  endsAt?: string
  status: string
  organizerId?: string
  createdAt: string
  createdBy?: string
  updatedAt: string
  rowVersion: number
  participants: MeetingParticipantDto[]
  outcomes: MeetingOutcomeDto[]
  followUps: MeetingFollowUpDto[]
}

// ── Calendar DTOs ──────────────────────────────────────────────────────────────

export interface MeetingCalendarItemDto {
  id: string
  title: string
  scheduledAt?: string
  endsAt?: string
  meetingTypeId?: string
  meetingTypeCode?: string
  status: string
  campusId?: string
  location?: string
  organizerId?: string
  participantCount: number
}

// ── Query Types ────────────────────────────────────────────────────────────────

export interface MeetingListQuery extends PagedQuery {
  corporationId?: string
  campusId?: string
  meetingTypeId?: string
  status?: string
  from?: string
  to?: string
  organizerId?: string
  participantUserId?: string
  participantGuardianId?: string
}

export interface MeetingCalendarQuery {
  corporationId?: string
  campusId?: string
  from?: string
  to?: string
}

// ── Payload Types ──────────────────────────────────────────────────────────────

export interface ParticipantInput {
  participantType: string
  userId?: string
  guardianId?: string
  leadId?: string
  externalName?: string
}

export interface ScheduleMeetingPayload {
  corporationId: string
  campusId?: string
  meetingTypeId?: string
  title: string
  location?: string
  roomId?: string
  scheduledAt?: string
  endsAt?: string
  participants?: ParticipantInput[]
}

export interface UpdateMeetingPayload {
  meetingTypeId?: string
  title: string
  location?: string
  roomId?: string
  scheduledAt?: string
  endsAt?: string
  rowVersion: number
}

export interface AddParticipantPayload {
  participantType: string
  userId?: string
  guardianId?: string
  leadId?: string
  externalName?: string
}

export interface UpdateAttendancePayload {
  attendance: string
}

export interface AddOutcomePayload {
  summary?: string
  decisions?: string
}

export interface UpdateOutcomePayload {
  summary?: string
  decisions?: string
}

export interface AddFollowUpPayload {
  action: string
  assigneeId?: string
  dueDate?: string
}

export interface UpdateFollowUpPayload {
  action: string
  assigneeId?: string
  dueDate?: string
}

export interface UpdateFollowUpStatusPayload {
  status: string
}
