/**
 * Camera & Live Session Management API service.
 * Wraps all /api/cameras endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CameraListItemDto,
  CameraDto,
  ViewingAuthorizationDto,
  ViewingLogDto,
  CameraListQuery,
  ViewingAuthorizationQuery,
  ViewingLogQuery,
  CreateCameraPayload,
  UpdateCameraPayload,
  AssignRoomPayload,
  AssignSessionPayload,
  CreateAuthorizationPayload,
  StartViewingPayload,
} from '@/types/camera.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const cameraService = {
  // ── Cameras ────────────────────────────────────────────────────────────────

  list: (query: CameraListQuery) =>
    apiService.get<PaginatedResult<CameraListItemDto>>(
      `/cameras${qs(query as Record<string, unknown>)}`
    ),

  get: (id: string) =>
    apiService.get<CameraDto>(`/cameras/${id}`),

  create: (payload: CreateCameraPayload) =>
    apiService.post<CameraDto>('/cameras', payload),

  update: (id: string, payload: UpdateCameraPayload) =>
    apiService.put<CameraDto>(`/cameras/${id}`, payload),

  delete: (id: string) =>
    apiService.delete(`/cameras/${id}`),

  setActive: (id: string, isActive: boolean) =>
    apiService.patch(`/cameras/${id}/active`, { isActive }),

  // ── Room Assignments ───────────────────────────────────────────────────────

  assignRoom: (cameraId: string, payload: AssignRoomPayload) =>
    apiService.post(`/cameras/${cameraId}/rooms`, payload),

  removeRoom: (cameraId: string, roomId: string) =>
    apiService.delete(`/cameras/${cameraId}/rooms/${roomId}`),

  // ── Session Assignments ────────────────────────────────────────────────────

  assignSession: (cameraId: string, payload: AssignSessionPayload) =>
    apiService.post(`/cameras/${cameraId}/sessions`, payload),

  removeSession: (cameraId: string, sessionId: string) =>
    apiService.delete(`/cameras/${cameraId}/sessions/${sessionId}`),

  // ── Viewing Authorizations ─────────────────────────────────────────────────

  listAuthorizations: (query: ViewingAuthorizationQuery) =>
    apiService.get<PaginatedResult<ViewingAuthorizationDto>>(
      `/cameras/authorizations${qs(query as Record<string, unknown>)}`
    ),

  getAuthorization: (id: string) =>
    apiService.get<ViewingAuthorizationDto>(`/cameras/authorizations/${id}`),

  createAuthorization: (payload: CreateAuthorizationPayload) =>
    apiService.post<ViewingAuthorizationDto>('/cameras/authorizations', payload),

  revokeAuthorization: (id: string) =>
    apiService.post(`/cameras/authorizations/${id}/revoke`),

  // ── Viewing Logs ───────────────────────────────────────────────────────────

  startViewing: (payload: StartViewingPayload) =>
    apiService.post<{ logId: number }>('/cameras/viewing/start', payload),

  endViewing: (logId: number) =>
    apiService.post(`/cameras/viewing/${logId}/end`),

  listViewingLogs: (query: ViewingLogQuery) =>
    apiService.get<PaginatedResult<ViewingLogDto>>(
      `/cameras/viewing-logs${qs(query as Record<string, unknown>)}`
    ),
}
