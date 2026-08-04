/**
 * Notification API service.
 * Wraps /api/notifications, /api/notification-templates, /api/notification-triggers endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  NotificationDto,
  NotificationListItemDto,
  NotificationListQuery,
  UnreadCountDto,
  NotificationPreferenceDto,
  UpdateNotificationPreferencesPayload,
  NotificationTemplateDto,
  NotificationTemplateListItemDto,
  NotificationTemplateListQuery,
  CreateNotificationTemplatePayload,
  UpdateNotificationTemplatePayload,
  NotificationTriggerConfigDto,
  NotificationTriggerConfigListItemDto,
  UpsertTriggerConfigPayload,
} from '@/types/notification.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const notificationService = {
  // ── Notifications (inbox) ──────────────────────────────────────────────────

  list: (query: NotificationListQuery) =>
    apiService.get<PaginatedResult<NotificationListItemDto>>(
      `/notifications${qs(query as Record<string, unknown>)}`
    ),

  getUnreadCount: () =>
    apiService.get<UnreadCountDto>('/notifications/unread-count'),

  markRead: (id: string) =>
    apiService.patch(`/notifications/${id}/read`),

  markAllRead: () =>
    apiService.patch('/notifications/mark-all-read'),

  // ── Preferences ────────────────────────────────────────────────────────────

  getPreferences: () =>
    apiService.get<NotificationPreferenceDto[]>('/notifications/preferences'),

  updatePreferences: (payload: UpdateNotificationPreferencesPayload) =>
    apiService.put<NotificationPreferenceDto[]>('/notifications/preferences', payload),

  // ── Templates ──────────────────────────────────────────────────────────────

  listTemplates: (query: NotificationTemplateListQuery) =>
    apiService.get<PaginatedResult<NotificationTemplateListItemDto>>(
      `/notification-templates${qs(query as Record<string, unknown>)}`
    ),

  getTemplate: (id: string) =>
    apiService.get<NotificationTemplateDto>(`/notification-templates/${id}`),

  createTemplate: (payload: CreateNotificationTemplatePayload) =>
    apiService.post<NotificationTemplateDto>('/notification-templates', payload),

  updateTemplate: (id: string, payload: UpdateNotificationTemplatePayload) =>
    apiService.put<NotificationTemplateDto>(`/notification-templates/${id}`, payload),

  deleteTemplate: (id: string) =>
    apiService.delete(`/notification-templates/${id}`),

  // ── Trigger Configs ────────────────────────────────────────────────────────

  listTriggers: (corporationId?: string) =>
    apiService.get<PaginatedResult<NotificationTriggerConfigListItemDto>>(
      `/notification-triggers${qs({ corporationId } as Record<string, unknown>)}`
    ),

  upsertTrigger: (payload: UpsertTriggerConfigPayload) =>
    apiService.put<NotificationTriggerConfigDto>('/notification-triggers', payload),

  deleteTrigger: (id: string) =>
    apiService.delete(`/notification-triggers/${id}`),
}
