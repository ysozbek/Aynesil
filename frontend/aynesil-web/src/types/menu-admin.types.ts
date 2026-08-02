import type { PagedQuery } from './api.types'

export interface MenuItemTranslationDto {
  locale: string
  label: string
}

export interface MenuItemListItemDto {
  id: string
  corporationId?: string
  parentId?: string
  code: string
  route?: string
  icon?: string
  sortOrder: number
  requiredPermissionId?: string
  requiredPermissionCode?: string
  featureFlag?: string
  isActive: boolean
  translations: MenuItemTranslationDto[]
  createdAt: string
  updatedAt: string
}

export interface MenuItemDto extends MenuItemListItemDto {
  rowVersion: number
}

export interface MenuTreeNodeDto {
  id: string
  code: string
  label: string
  route?: string
  icon?: string
  sortOrder: number
  children: MenuTreeNodeDto[]
}

export interface CreateMenuItemRequest {
  parentId?: string
  code: string
  route?: string
  icon?: string
  sortOrder: number
  requiredPermissionId?: string
  featureFlag?: string
  translations: MenuItemTranslationDto[]
}

export interface UpdateMenuItemRequest {
  parentId?: string
  route?: string
  icon?: string
  sortOrder: number
  requiredPermissionId?: string
  featureFlag?: string
  rowVersion: number
}

export interface SetMenuItemTranslationsRequest {
  translations: MenuItemTranslationDto[]
}

export interface MenuAdminQuery extends PagedQuery {
  parentId?: string
  isActive?: boolean
  includePlatformDefaults?: boolean
}
