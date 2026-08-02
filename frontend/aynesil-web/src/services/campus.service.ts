import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CampusDto,
  CampusListItemDto,
  CampusQuery,
  CreateCampusRequest,
  UpdateCampusRequest,
} from '@/types/campus.types'

const BASE = '/campuses'

export const campusService = {
  list(params: CampusQuery) {
    return apiService.get<PaginatedResult<CampusListItemDto>>(BASE, { params })
  },

  get(id: string) {
    return apiService.get<CampusDto>(`${BASE}/${id}`)
  },

  create(request: CreateCampusRequest) {
    return apiService.post<CampusDto>(BASE, request)
  },

  update(id: string, request: UpdateCampusRequest) {
    return apiService.put<CampusDto>(`${BASE}/${id}`, request)
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
