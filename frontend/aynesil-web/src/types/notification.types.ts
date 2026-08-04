/**
 * Notification type definitions.
 * Mirrors Aynesil.Application.Features.Notifications.Dtos.NotificationDtos
 */
import type { PagedQuery } from './api.types'

// ── Notification DTOs ──────────────────────────────────────────────────────────

export interface NotificationDto {
  id: string
  corporationId: string
  templateId?: string
  categoryId?: string
  categoryCode?: string
  recipientUserId?: string
  subject?: string
  body: string
  status: string
  createdAt: string
  readAt?: string
  isRead: boolean
}

export interface NotificationListItemDto {
  id: string
  categoryId?: string
  categoryCode?: string
  subject?: string
  body: string
  status: string
  createdAt: string
  readAt?: string
  isRead: boolean
}

export interface UnreadCountDto {
  count: number
}

// ── Template DTOs ──────────────────────────────────────────────────────────────

export interface NotificationTemplateTranslationDto {
  locale: string
  subject?: string
  body: string
}

export interface NotificationTemplateDto {
  id: string
  corporationId?: string
  code: string
  categoryId?: string
  categoryCode?: string
  typeId?: string
  typeCode?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
  translations: NotificationTemplateTranslationDto[]
}

export interface NotificationTemplateListItemDto {
  id: string
  corporationId?: string
  code: string
  categoryCode?: string
  typeCode?: string
  isActive: boolean
  updatedAt: string
}

// ── Trigger Config DTOs ────────────────────────────────────────────────────────

export interface NotificationTriggerConfigDto {
  id: string
  corporationId?: string
  triggerCode: string
  templateId?: string
  templateCode?: string
  offsetMinutes: number
  isActive: boolean
  channelIds: string[]
  updatedAt: string
  rowVersion: number
}

export interface NotificationTriggerConfigListItemDto {
  id: string
  corporationId?: string
  triggerCode: string
  templateCode?: string
  offsetMinutes: number
  isActive: boolean
  channelCount: number
}

// ── Preference DTOs ────────────────────────────────────────────────────────────

export interface NotificationPreferenceDto {
  id: string
  userId: string
  categoryId?: string
  categoryCode?: string
  channelId?: string
  channelCode?: string
  isEnabled: boolean
}

// ── Query / Payload Types ──────────────────────────────────────────────────────

export interface NotificationListQuery extends PagedQuery {
  isRead?: boolean
  categoryCode?: string
  from?: string
  to?: string
}

export interface NotificationTemplateListQuery extends PagedQuery {
  corporationId?: string
  isActive?: boolean
  categoryCode?: string
}

export interface CreateNotificationTemplatePayload {
  corporationId?: string
  code: string
  categoryId?: string
  typeId?: string
  isActive: boolean
  translations: NotificationTemplateTranslationDto[]
}

export interface UpdateNotificationTemplatePayload {
  code: string
  categoryId?: string
  typeId?: string
  isActive: boolean
  translations: NotificationTemplateTranslationDto[]
  rowVersion: number
}

export interface UpsertTriggerConfigPayload {
  corporationId?: string
  triggerCode: string
  templateId?: string
  offsetMinutes: number
  isActive: boolean
  channelIds: string[]
  rowVersion?: number
}

export interface UpdateNotificationPreferencesPayload {
  preferences: Array<{
    categoryId?: string
    channelId?: string
    isEnabled: boolean
  }>
}
