/**
 * Camera & Live Session Management type definitions.
 * Mirrors Aynesil.Application.Features.Cameras.Dtos.CameraDtos
 */
import type { PagedQuery } from './api.types'

// ── Camera DTOs ───────────────────────────────────────────────────────────────

export interface RoomCameraDto {
  id: string
  roomId: string
  roomCode?: string
  roomName?: string
  cameraId: string
  cameraCode: string
}

export interface SessionCameraDto {
  id: string
  sessionId: string
  sessionStartsAt: string
  sessionEndsAt: string
  cameraId: string
  cameraCode: string
}

export interface CameraListItemDto {
  id: string
  corporationId: string
  campusId?: string
  campusName?: string
  cameraTypeId?: string
  cameraTypeCode?: string
  code: string
  name: string
  streamProviderId?: string
  isActive: boolean
  createdAt: string
}

export interface CameraDto {
  id: string
  corporationId: string
  campusId?: string
  campusName?: string
  cameraTypeId?: string
  cameraTypeCode?: string
  code: string
  name: string
  streamProviderId?: string
  streamRef?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
  roomAssignments: RoomCameraDto[]
  sessionAssignments: SessionCameraDto[]
}

// ── Authorization DTOs ────────────────────────────────────────────────────────

export interface ViewingAuthorizationDto {
  id: string
  corporationId: string
  guardianId: string
  guardianFullName?: string
  studentId: string
  studentFullName?: string
  sessionId?: string
  consentId?: string
  accessTypeId?: string
  accessTypeCode?: string
  validFrom: string
  validTo: string
  grantedBy?: string
  isRevoked: boolean
  isCurrentlyValid: boolean
  createdAt: string
}

// ── Viewing Log DTOs ──────────────────────────────────────────────────────────

export interface ViewingLogDto {
  id: number
  corporationId: string
  guardianId?: string
  guardianFullName?: string
  userId?: string
  sessionId?: string
  cameraId?: string
  cameraCode?: string
  authorizationId?: string
  startedAt: string
  endedAt?: string
  durationSeconds?: number
  ipAddress?: string
}

// ── Query Types ───────────────────────────────────────────────────────────────

export interface CameraListQuery extends PagedQuery {
  corporationId?: string
  campusId?: string
  cameraTypeId?: string
  isActive?: boolean
}

export interface ViewingAuthorizationQuery extends PagedQuery {
  corporationId?: string
  guardianId?: string
  studentId?: string
  isRevoked?: boolean
  isCurrentlyValid?: boolean
}

export interface ViewingLogQuery extends PagedQuery {
  corporationId?: string
  cameraId?: string
  guardianId?: string
  from?: string
  to?: string
}

// ── Payload Types ─────────────────────────────────────────────────────────────

export interface CreateCameraPayload {
  corporationId: string
  campusId?: string
  cameraTypeId?: string
  code: string
  name: string
  streamProviderId?: string
  streamRef?: string
}

export interface UpdateCameraPayload {
  campusId?: string
  cameraTypeId?: string
  code: string
  name: string
  streamProviderId?: string
  streamRef?: string
  rowVersion: number
}

export interface AssignRoomPayload {
  roomId: string
}

export interface AssignSessionPayload {
  sessionId: string
}

export interface CreateAuthorizationPayload {
  guardianId: string
  studentId: string
  sessionId?: string
  consentId?: string
  accessTypeId?: string
  validFrom: string
  validTo: string
}

export interface StartViewingPayload {
  cameraId?: string
  sessionId?: string
  authorizationId?: string
}
