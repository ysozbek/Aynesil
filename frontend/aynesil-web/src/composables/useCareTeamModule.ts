/**
 * Bakım ekibi modülü bu kurumda açık mı? Arayüz modül bayrağını doğrudan bilmez; menü ağacı
 * (/menus/me) sunucuda bayrağa ve izne göre süzüldüğü için 'students-care-team' satırı varsa modül açıktır.
 */
import { computed } from 'vue'
import { useMenuStore } from '@/stores/menu.store'
import { usePermission } from '@/composables/usePermission'
import { Permissions } from '@/constants/permissions'

export function useCareTeamModule() {
  const menu = useMenuStore()
  const { can } = usePermission()
  const enabled = computed(() => menu.items.some((i) => i.code === 'students-care-team'))
  const canRead = computed(() => enabled.value && can(Permissions.CareTeam.Read))
  const canAssign = computed(() => enabled.value && can(Permissions.CareTeam.Assign))
  return { enabled, canRead, canAssign }
}
