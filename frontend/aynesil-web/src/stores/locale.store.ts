/**
 * Locale Store
 * Manages the active locale. UI texts are bundled (i18n/locales/*.ts); there is no
 * server-side message endpoint, so nothing is loaded from the API.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'

export type SupportedLocale = 'tr' | 'en' | string

export const useLocaleStore = defineStore('locale', () => {
  const current = ref<SupportedLocale>(
    localStorage.getItem('locale') || 'tr'
  )

  function setLocale(locale: SupportedLocale) {
    current.value = locale
    localStorage.setItem('locale', locale)
  }

  return { current, setLocale }
})
