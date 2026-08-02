import type { PagedQuery } from './api.types'

export interface CorporationListItemDto {
  id: string
  code: string
  legalName: string
  displayName: string
  defaultLocale: string
  defaultCurrency: string
  timezone: string
  status: string
  campusCount: number
  createdAt: string
}

export interface CorporationDto extends CorporationListItemDto {
  taxOffice?: string
  taxNumber?: string
  settings: string
  updatedAt: string
  rowVersion: number
}

export interface CorporationSettingsDto {
  corporationId: string
  code: string
  displayName: string
  defaultLocale: string
  defaultCurrency: string
  timezone: string
  taxOffice?: string
  taxNumber?: string
  settings: string
}

export interface CreateCorporationRequest {
  code: string
  legalName: string
  displayName: string
  defaultLocale: string
  defaultCurrency: string
  timezone: string
  taxOffice?: string
  taxNumber?: string
}

export interface UpdateCorporationRequest {
  legalName: string
  displayName: string
  defaultLocale: string
  defaultCurrency: string
  timezone: string
  taxOffice?: string
  taxNumber?: string
  rowVersion: number
}

export interface UpdateCorporationSettingsRequest {
  defaultLocale: string
  defaultCurrency: string
  timezone: string
  taxOffice?: string
  taxNumber?: string
  settings: string
  rowVersion: number
}

export interface CorporationQuery extends PagedQuery {
  status?: string
}
