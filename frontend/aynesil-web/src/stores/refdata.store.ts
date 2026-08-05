/**
 * Reference Data Store
 * Lazy-loads reference value lists on first request and caches them in memory.
 * Used by dropdowns, selects, and filters throughout the application.
 * Cache is cleared on login/logout to ensure tenant-specific values refresh.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiService } from '@/services/api.service'

export interface RefValueItem {
  id: string
  code: string
  label: string
  shortLabel?: string
  sortOrder: number
  isDefault: boolean
  isSystem: boolean
  metadata: string
  isActive: boolean
  isTenantOwned: boolean
}

export interface CreateRefValuePayload {
  code: string
  label: string
  sortOrder?: number
  isDefault?: boolean
}

export interface UpdateRefValuePayload {
  label: string
  sortOrder?: number
  isDefault?: boolean
}

export const useRefDataStore = defineStore('refData', () => {
  const cache = ref<Record<string, RefValueItem[]>>({})
  const loading = ref<Record<string, boolean>>({})

  async function getValues(typeCode: string, activeOnly = true): Promise<RefValueItem[]> {
    const cacheKey = `${typeCode}:${activeOnly}`
    if (cache.value[cacheKey]) return cache.value[cacheKey]
    if (loading.value[cacheKey]) {
      // Wait for in-flight request
      await new Promise((r) => setTimeout(r, 50))
      return cache.value[cacheKey] ?? []
    }

    loading.value[cacheKey] = true
    try {
      const response = await apiService.get<RefValueItem[]>(
        `/reference-data/${typeCode}?activeOnly=${activeOnly}`
      )
      if (response.success && response.data) {
        cache.value[cacheKey] = response.data
        return response.data
      }
    } finally {
      loading.value[cacheKey] = false
    }
    return []
  }

  function getDefault(typeCode: string): RefValueItem | undefined {
    const values = cache.value[`${typeCode}:true`] || []
    return values.find((v) => v.isDefault) ?? values[0]
  }

  function clearCache() {
    cache.value = {}
  }

  async function createValue(typeCode: string, payload: CreateRefValuePayload): Promise<RefValueItem> {
    const response = await apiService.post<RefValueItem>(`/reference-data/${typeCode}`, payload)
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Create failed')
    }
    clearCache()
    return response.data
  }

  async function updateValue(id: string, payload: UpdateRefValuePayload): Promise<RefValueItem> {
    const response = await apiService.put<RefValueItem>(`/reference-data/values/${id}`, payload)
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Update failed')
    }
    clearCache()
    return response.data
  }

  async function setActive(id: string, isActive: boolean): Promise<void> {
    const response = await apiService.put(`/reference-data/values/${id}/active`, { isActive })
    if (!response.success) {
      throw new Error(response.message || 'Status update failed')
    }
    clearCache()
  }

  return { getValues, getDefault, clearCache, createValue, updateValue, setActive }
})
