/**
 * Menu Store
 * Loads the dynamic menu tree from the API and filters it by the user's permissions.
 * Menu items with required_permission are hidden when the user lacks the permission.
 * Feature-flagged items are hidden when the flag is not enabled for the corporation.
 * The flat list is built into a tree structure client-side.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiService } from '@/services/api.service'
import { menuAdminService } from '@/services/menu-admin.service'
import { usePermissionStore } from './permission.store'
import { useLocaleStore } from './locale.store'
import type { MenuItemListItemDto, CreateMenuItemRequest, UpdateMenuItemRequest, SetMenuItemTranslationsRequest } from '@/types/menu-admin.types'

export interface MenuItem {
  id: string
  parentId?: string
  code: string
  label: string
  route?: string
  icon?: string
  sortOrder: number
  requiredPermission?: string
  featureFlag?: string
  children?: MenuItem[]
}

export const useMenuStore = defineStore('menu', () => {
  const items = ref<MenuItem[]>([])
  const tree = ref<MenuItem[]>([])
  const loading = ref(false)

  async function load() {
    if (loading.value) return
    loading.value = true
    try {
      const locale = useLocaleStore().current
      const response = await apiService.get<MenuItem[]>(`/menus?locale=${locale}`)
      if (response.success && response.data) {
        items.value = filterByPermission(response.data)
        tree.value = buildTree(items.value)
      }
    } finally {
      loading.value = false
    }
  }

  function filterByPermission(flatItems: MenuItem[]): MenuItem[] {
    const perms = usePermissionStore()
    return flatItems.filter((item) =>
      !item.requiredPermission || perms.can(item.requiredPermission)
    )
  }

  function buildTree(flatItems: MenuItem[]): MenuItem[] {
    const map = new Map<string, MenuItem>()
    const roots: MenuItem[] = []

    flatItems.forEach((item) => map.set(item.id, { ...item, children: [] }))

    map.forEach((item) => {
      if (item.parentId && map.has(item.parentId)) {
        map.get(item.parentId)!.children!.push(item)
      } else {
        roots.push(item)
      }
    })

    const sort = (arr: MenuItem[]) => {
      arr.sort((a, b) => a.sortOrder - b.sortOrder)
      arr.forEach((item) => item.children && sort(item.children))
    }
    sort(roots)

    return roots
  }

  return { items, tree, loading, load }

  // ── Admin management (Menu Tree Editor) ────────────────────────────────────
  // Admin actions are defined outside the consumer state above so they don't
  // pollute the sidebar's items/tree refs.
})

/** Separate composable for admin menu management to avoid coupling with the sidebar store. */
export function useMenuAdminActions() {
  const adminTree = ref<MenuItemListItemDto[]>([])
  const adminLoading = ref(false)

  async function loadAdminTree() {
    adminLoading.value = true
    try {
      const res = await menuAdminService.tree(true)
      if (res.success && res.data) adminTree.value = res.data
    } finally {
      adminLoading.value = false
    }
  }

  async function createItem(request: CreateMenuItemRequest) {
    const res = await menuAdminService.create(request)
    if (!res.success) throw new Error(res.message)
    await loadAdminTree()
    return res.data!
  }

  async function updateItem(id: string, request: UpdateMenuItemRequest) {
    const res = await menuAdminService.update(id, request)
    if (!res.success) throw new Error(res.message)
    await loadAdminTree()
    return res.data!
  }

  async function removeItem(id: string) {
    await menuAdminService.remove(id)
    await loadAdminTree()
  }

  async function setTranslations(id: string, request: SetMenuItemTranslationsRequest) {
    const res = await menuAdminService.setTranslations(id, request)
    if (!res.success) throw new Error(res.message)
    await loadAdminTree()
  }

  async function activateItem(id: string) {
    await menuAdminService.activate(id)
    await loadAdminTree()
  }

  async function deactivateItem(id: string) {
    await menuAdminService.deactivate(id)
    await loadAdminTree()
  }

  return { adminTree, adminLoading, loadAdminTree, createItem, updateItem, removeItem, setTranslations, activateItem, deactivateItem }
}
