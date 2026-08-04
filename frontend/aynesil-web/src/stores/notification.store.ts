/**
 * Notification inbox store — personal notifications, unread count.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { notificationService } from '@/services/notification.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  NotificationListItemDto,
  NotificationListQuery,
  NotificationPreferenceDto,
  UpdateNotificationPreferencesPayload,
} from '@/types/notification.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useNotificationStore = defineStore('notification', () => {
  // ── State ──────────────────────────────────────────────────────────────────
  const notificationList = ref<PaginatedResult<NotificationListItemDto>>(emptyPage<NotificationListItemDto>())
  const unreadCount = ref(0)
  const preferences = ref<NotificationPreferenceDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Actions ────────────────────────────────────────────────────────────────

  async function fetchNotifications(query: NotificationListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await notificationService.list(query)
      if (res.success && res.data) notificationList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchUnreadCount() {
    try {
      const res = await notificationService.getUnreadCount()
      if (res.success && res.data) unreadCount.value = res.data.count
    } catch {
      // silent — badge update should not break UI
    }
  }

  async function markRead(id: string) {
    saving.value = true
    try {
      await notificationService.markRead(id)
      const item = notificationList.value.items.find(n => n.id === id)
      if (item) {
        item.isRead = true
        item.readAt = new Date().toISOString()
      }
      if (unreadCount.value > 0) unreadCount.value--
    } finally {
      saving.value = false
    }
  }

  async function markAllRead() {
    saving.value = true
    try {
      await notificationService.markAllRead()
      notificationList.value.items.forEach(n => {
        n.isRead = true
        n.readAt = new Date().toISOString()
      })
      unreadCount.value = 0
    } finally {
      saving.value = false
    }
  }

  async function fetchPreferences() {
    loading.value = true
    try {
      const res = await notificationService.getPreferences()
      if (res.success && res.data) preferences.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function updatePreferences(payload: UpdateNotificationPreferencesPayload) {
    saving.value = true
    try {
      const res = await notificationService.updatePreferences(payload)
      if (!res.success) throw new Error(res.message ?? 'Tercihler kaydedilemedi.')
      if (res.data) preferences.value = res.data
    } finally {
      saving.value = false
    }
  }

  return {
    notificationList, unreadCount, preferences,
    loading, saving, error,
    fetchNotifications, fetchUnreadCount,
    markRead, markAllRead,
    fetchPreferences, updatePreferences,
  }
})
