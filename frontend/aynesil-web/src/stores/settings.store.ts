/**
 * Settings Store
 * Loads corporation and user settings on authentication.
 * Provides typed accessors for common settings.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiService } from '@/services/api.service'

export const useSettingsStore = defineStore('settings', () => {
  const settings = ref<Record<string, unknown>>({})

  async function load() {
    try {
      const response = await apiService.get<Record<string, unknown>>('/settings')
      if (response.success && response.data) {
        settings.value = response.data
      }
    } catch {
      // Use defaults if settings fail to load
    }
  }

  function get<T>(key: string, defaultValue?: T): T | undefined {
    return (settings.value[key] as T) ?? defaultValue
  }

  return { settings, load, get }
})
