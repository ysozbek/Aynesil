/**
 * Composable for permission checks in component templates.
 * Usage: const { can, canAny } = usePermission()
 */
import { usePermissionStore } from '@/stores/permission.store'
import type { PermissionCode } from '@/constants/permissions'

export function usePermission() {
  const store = usePermissionStore()
  return {
    can: (code: PermissionCode) => store.can(code),
    canAny: (...codes: PermissionCode[]) => store.canAny(...codes),
    canAll: (...codes: PermissionCode[]) => store.canAll(...codes),
  }
}
