/**
 * Notification Preference store — re-exports from notification store for convenience.
 * Dedicated store module for preference management (channel/category).
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { notificationService } from '@/services/notification.service'
import type {
  NotificationPreferenceDto,
  UpdateNotificationPreferencesPayload,
} from '@/types/notification.types'

export const useNotificationPreferenceStore = defineStore('notificationPreference', () => {
  const preferences = ref<NotificationPreferenceDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchPreferences() {
    loading.value = true
    error.value = null
    try {
      const res = await notificationService.getPreferences()
      if (res.success && res.data) preferences.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function savePreferences(payload: UpdateNotificationPreferencesPayload) {
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
    preferences,
    loading, saving, error,
    fetchPreferences, savePreferences,
  }
})
