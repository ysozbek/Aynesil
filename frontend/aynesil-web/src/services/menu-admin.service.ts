import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  MenuItemDto,
  MenuItemListItemDto,
  MenuAdminQuery,
  CreateMenuItemRequest,
  UpdateMenuItemRequest,
  SetMenuItemTranslationsRequest,
  MenuItemTranslationDto,
} from '@/types/menu-admin.types'

const BASE = '/menus'

export const menuAdminService = {
  list(params: MenuAdminQuery) {
    return apiService.get<PaginatedResult<MenuItemListItemDto>>(BASE, { params })
  },

  tree(includeInactive = true) {
    return apiService.get<MenuItemListItemDto[]>(`${BASE}/tree`, {
      params: { includeInactive },
    })
  },

  get(id: string) {
    return apiService.get<MenuItemDto>(`${BASE}/${id}`)
  },

  create(request: CreateMenuItemRequest) {
    return apiService.post<MenuItemListItemDto>(BASE, request)
  },

  update(id: string, request: UpdateMenuItemRequest) {
    return apiService.put<MenuItemListItemDto>(`${BASE}/${id}`, request)
  },

  remove(id: string) {
    return apiService.delete(`${BASE}/${id}`)
  },

  setTranslations(id: string, request: SetMenuItemTranslationsRequest) {
    return apiService.put<MenuItemTranslationDto[]>(`${BASE}/${id}/translations`, request)
  },

  activate(id: string) {
    return apiService.post(`${BASE}/${id}/activate`)
  },

  deactivate(id: string) {
    return apiService.post(`${BASE}/${id}/deactivate`)
  },
}
