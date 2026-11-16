/** Bakım ekibi: modül açık (GET /modules/me) VE izin var. */
import { computed } from 'vue'
import { useModulesStore } from '@/stores/modules.store'
import { usePermission } from '@/composables/usePermission'
import { Permissions } from '@/constants/permissions'
import { Modules } from '@/constants/modules'

export function useCareTeamModule() {
  const modules = useModulesStore()
  const { can } = usePermission()
  const enabled = computed(() => modules.isEnabled(Modules.CareTeam))
  const canRead = computed(() => enabled.value && can(Permissions.CareTeam.Read))
  const canAssign = computed(() => enabled.value && can(Permissions.CareTeam.Assign))
  return { enabled, canRead, canAssign }
}
