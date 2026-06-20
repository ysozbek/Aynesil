/**
 * Locale Store
 * Manages the active locale and loads translations from the API.
 * Falls back to 'tr' (platform default) if the user's locale is not available.
 * Works in concert with Vue I18n — sets i18n.global.locale on change.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiService } from '@/services/api.service'

export type SupportedLocale = 'tr' | 'en' | string

export const useLocaleStore = defineStore('locale', () => {
  const current = ref<SupportedLocale>(
    localStorage.getItem('locale') || 'tr'
  )
  const messages = ref<Record<string, Record<string, string>>>({})
  const loading = ref(false)

  async function loadMessages(locale: SupportedLocale, namespace = 'ui') {
    if (messages.value[locale]) return
    loading.value = true
    try {
      const response = await apiService.get<Record<string, string>>(
        `/localization/messages?locale=${locale}&namespace=${namespace}`
      )
      if (response.success && response.data) {
        messages.value[locale] = response.data
      }
    } catch {
      // Silently fall back to default translations
    } finally {
      loading.value = false
    }
  }

  async function setLocale(locale: SupportedLocale) {
    current.value = locale
    localStorage.setItem('locale', locale)
    await loadMessages(locale)
  }

  return { current, messages, loading, loadMessages, setLocale }
})
