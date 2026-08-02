import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { useAuthStore } from './auth.store'
import { permissionService } from '@/services/permission.service'
import type { PermissionListItemDto } from '@/types/permission.types'

/**
 * Permission store — thin wrapper over auth store's permissions array.
 * Provides a convenient `can(code)` composable used by route guards and components.
 * Permissions come from the JWT token — no separate API call needed.
 *
 * Extended with admin catalog loading for the Permission Explorer screen.
 */
export const usePermissionStore = defineStore('permission', () => {
  const auth = useAuthStore()

  const permissions = computed(() => auth.permissions)

  function can(code: string): boolean {
    return auth.hasPermission(code)
  }

  function canAny(...codes: string[]): boolean {
    return codes.some((c) => can(c))
  }

  function canAll(...codes: string[]): boolean {
    return codes.every((c) => can(c))
  }

  // ── Admin catalog (Permission Explorer) ───────────────────────────────────
  const catalog = ref<PermissionListItemDto[]>([])
  const catalogLoading = ref(false)
  const catalogLoaded = ref(false)

  async function loadCatalog(force = false) {
    if (catalogLoaded.value && !force) return
    catalogLoading.value = true
    try {
      const res = await permissionService.listAll()
      if (res.success && res.data) {
        catalog.value = res.data.items
        catalogLoaded.value = true
      }
    } finally {
      catalogLoading.value = false
    }
  }

  const catalogByResource = computed(() => {
    const grouped: Record<string, PermissionListItemDto[]> = {}
    for (const p of catalog.value) {
      if (!grouped[p.resource]) grouped[p.resource] = []
      grouped[p.resource].push(p)
    }
    return grouped
  })

  return { permissions, can, canAny, canAll, catalog, catalogLoading, catalogLoaded, catalogByResource, loadCatalog }
})
