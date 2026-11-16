/**
 * Kurumda açık modüller (GET /modules/me). Sunucu durumu her istekte okur; menüden türetilmez.
 * Yüklenene kadar (ya da istek başarısızsa) hiçbir modül açık sayılmaz: ekranlar gizli kalır, uçlar zaten 403 verir.
 */
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { apiService } from '@/services/api.service'
import type { EnabledModulesDto } from '@/types/modules.types'
import type { ModuleCode } from '@/constants/modules'

export const useModulesStore = defineStore('modules', () => {
  const enabled = ref<string[]>([])
  const loaded = ref(false)

  async function load(force = false) {
    if (loaded.value && !force) return
    try {
      const res = await apiService.get<EnabledModulesDto>('/modules/me')
      enabled.value = res.success && res.data ? res.data.enabledModules : []
      loaded.value = true
    } catch (err: unknown) {
      console.error('[ModulesStore] /modules/me failed:', err)
    }
  }

  const isEnabled = computed(() => (code: ModuleCode) => enabled.value.includes(code))

  function clear() {
    enabled.value = []
    loaded.value = false
  }

  return { enabled, loaded, isEnabled, load, clear }
})
