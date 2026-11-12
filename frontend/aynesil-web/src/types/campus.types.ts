import type { PagedQuery } from './api.types'

export interface CampusListItemDto {
  id: string
  corporationId: string
  corporationDisplayName: string
  code: string
  name: string
  city?: string
  district?: string
  phone?: string
  isActive: boolean
  createdAt: string
}

export interface CampusDto extends CampusListItemDto {
  addressLine?: string
  email?: string
  timezone?: string
  geoLat?: number
  geoLng?: number
  updatedAt: string
  rowVersion: number
}

export interface CreateCampusRequest {
  code: string
  name: string
  city?: string
  addressLine?: string
  district?: string
  phone?: string
  email?: string
  timezone?: string
  geoLat?: number
  geoLng?: number
}

export interface UpdateCampusRequest {
  name: string
  city?: string
  addressLine?: string
  district?: string
  phone?: string
  email?: string
  timezone?: string
  geoLat?: number
  geoLng?: number
  rowVersion: number
}

export interface CampusQuery extends PagedQuery {
  isActive?: boolean
}
