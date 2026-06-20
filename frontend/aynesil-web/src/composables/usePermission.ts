/**
 * Composable for permission checks in component templates.
 * Usage: const { can, canAny } = usePermission()
 */
import { usePermissionStore } from '@/stores/permission.store'

export function usePermission() {
  const store = usePermissionStore()
  return {
    can: (code: string) => store.can(code),
    canAny: (...codes: string[]) => store.canAny(...codes),
    canAll: (...codes: string[]) => store.canAll(...codes),
  }
}
