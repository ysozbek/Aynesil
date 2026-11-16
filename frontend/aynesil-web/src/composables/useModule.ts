/**
 * Bir ekranın/sekmenin görünürlüğü = modül açık VE ilgili izin var.
 * Kullanım: const { visible } = useModule(Modules.CareTeam); visible(Permissions.CareTeam.Read)
 */
import { useModulesStore } from '@/stores/modules.store'
import { usePermission } from '@/composables/usePermission'
import type { ModuleCode } from '@/constants/modules'
import type { PermissionCode } from '@/constants/permissions'

export function useModule(code: ModuleCode) {
  const modules = useModulesStore()
  const { can } = usePermission()
  return {
    enabled: () => modules.isEnabled(code),
    visible: (permission: PermissionCode) => modules.isEnabled(code) && can(permission),
  }
}
