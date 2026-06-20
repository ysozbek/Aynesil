import { defineStore } from 'pinia'
import { computed } from 'vue'
import { useAuthStore } from './auth.store'

/**
 * Permission store — thin wrapper over auth store's permissions array.
 * Provides a convenient `can(code)` composable used by route guards and components.
 * Permissions come from the JWT token — no separate API call needed.
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

  return { permissions, can, canAny, canAll }
})
