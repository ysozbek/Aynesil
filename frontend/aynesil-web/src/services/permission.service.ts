import { apiService } from './api.service'
import type { PaginatedResult } from '@/types/api.types'
import type { PermissionDto, PermissionListItemDto, PermissionQuery } from '@/types/permission.types'

const BASE = '/permissions'

export const permissionService = {
  list(params: PermissionQuery) {
    return apiService.get<PaginatedResult<PermissionListItemDto>>(BASE, { params })
  },

  get(id: string) {
    return apiService.get<PermissionDto>(`${BASE}/${id}`)
  },

  /** Backend pageSize'ı en fazla 200'e kırpar; tüm sayfaları döngüyle çekip birleştirir. */
  async listAll(): Promise<PermissionListItemDto[]> {
    const pageSize = 200
    const items: PermissionListItemDto[] = []
    let page = 1
    let totalPages = 1
    do {
      const res = await apiService.get<PaginatedResult<PermissionListItemDto>>(BASE, {
        params: { page, pageSize },
      })
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzin kataloğu yüklenemedi.')
      items.push(...res.data.items)
      totalPages = res.data.totalPages
      page++
    } while (page <= totalPages)
    return items
  },
}
