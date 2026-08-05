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
import { useLocaleStore } from './locale.store'
import type { MenuTreeNodeDto, MenuItemListItemDto, CreateMenuItemRequest, UpdateMenuItemRequest, SetMenuItemTranslationsRequest } from '@/types/menu-admin.types'

export interface MenuItem {
  id: string
  parentId?: string
  code: string
  label: string
  route?: string
  icon?: string
  sortOrder: number
  children?: MenuItem[]
}

export const useMenuStore = defineStore('menu', () => {
  const items = ref<MenuItem[]>([])
  const tree = ref<MenuItem[]>([])
  const loading = ref(false)

  async function load(force = false) {
    if (loading.value && !force) return
    loading.value = true
    try {
      const locale = useLocaleStore().current
      // /menus/me returns the current user's filtered+translated tree from the backend.
      // Permission filtering and feature-flag gating happen server-side.
      const response = await apiService.get<MenuTreeNodeDto[]>(`/menus/me?locale=${locale}`)
      if (response.success && response.data) {
        tree.value = mapTree(response.data)
        items.value = flattenTree(tree.value)
      } else {
        console.warn('[MenuStore] /menus/me returned success=false:', response)
      }
    } catch (err: unknown) {
      // Log full error details to browser console for debugging
      console.error('[MenuStore] /menus/me failed:', err)
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { status?: number; data?: unknown } }
        console.error('[MenuStore] Status:', axiosErr.response?.status, 'Body:', axiosErr.response?.data)
      }
    } finally {
      loading.value = false
    }
  }

  function mapTree(nodes: MenuTreeNodeDto[], parentId?: string): MenuItem[] {
    return nodes.map((node) => ({
      id: node.id,
      parentId,
      code: node.code,
      label: node.label,
      route: node.route,
      icon: node.icon,
      sortOrder: node.sortOrder,
      children: mapTree(node.children ?? [], node.id),
    }))
  }

  function flattenTree(nodes: MenuItem[]): MenuItem[] {
    return nodes.flatMap((n) => [n, ...flattenTree(n.children ?? [])])
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
  const navStore = useMenuStore()

  async function loadAdminTree() {
    adminLoading.value = true
    try {
      const res = await menuAdminService.tree(true)
      if (res.success && res.data) adminTree.value = res.data
    } finally {
      adminLoading.value = false
    }
  }

  /** Reload sidebar navigation after admin mutations (server cache is already invalidated). */
  async function refreshNav() {
    await navStore.load(true)
  }

  async function createItem(request: CreateMenuItemRequest) {
    const res = await menuAdminService.create(request)
    if (!res.success) throw new Error(res.message)
    await loadAdminTree()
    await refreshNav()
    return res.data!
  }

  async function updateItem(id: string, request: UpdateMenuItemRequest) {
    const res = await menuAdminService.update(id, request)
    if (!res.success) throw new Error(res.message)
    await loadAdminTree()
    await refreshNav()
    return res.data!
  }

  async function removeItem(id: string) {
    await menuAdminService.remove(id)
    await loadAdminTree()
    await refreshNav()
  }

  async function setTranslations(id: string, request: SetMenuItemTranslationsRequest) {
    const res = await menuAdminService.setTranslations(id, request)
    if (!res.success) throw new Error(res.message)
    await loadAdminTree()
    await refreshNav()
  }

  async function activateItem(id: string) {
    await menuAdminService.activate(id)
    await loadAdminTree()
    await refreshNav()
  }

  async function deactivateItem(id: string) {
    await menuAdminService.deactivate(id)
    await loadAdminTree()
    await refreshNav()
  }

  return { adminTree, adminLoading, loadAdminTree, createItem, updateItem, removeItem, setTranslations, activateItem, deactivateItem }
}
