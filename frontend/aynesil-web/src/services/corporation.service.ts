import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CorporationDto,
  CorporationListItemDto,
  CorporationSettingsDto,
  CorporationQuery,
  CreateCorporationRequest,
  UpdateCorporationRequest,
  UpdateCorporationSettingsRequest,
} from '@/types/corporation.types'

const BASE = '/corporations'

export const corporationService = {
  list(params: CorporationQuery) {
    return apiService.get<PaginatedResult<CorporationListItemDto>>(BASE, { params })
  },

  get(id: string) {
    return apiService.get<CorporationDto>(`${BASE}/${id}`)
  },

  getSettings(id: string) {
    return apiService.get<CorporationSettingsDto>(`${BASE}/${id}/settings`)
  },

  create(request: CreateCorporationRequest) {
    return apiService.post<CorporationDto>(BASE, request)
  },

  update(id: string, request: UpdateCorporationRequest) {
    return apiService.put<CorporationDto>(`${BASE}/${id}`, request)
  },

  updateSettings(id: string, request: UpdateCorporationSettingsRequest) {
    return apiService.put<CorporationSettingsDto>(`${BASE}/${id}/settings`, request)
  },

  remove(id: string) {
    return apiService.delete(`${BASE}/${id}`)
  },

  activate(id: string) {
    return apiService.post(`${BASE}/${id}/activate`)
  },

  deactivate(id: string) {
    return apiService.post(`${BASE}/${id}/deactivate`)
  },
}
