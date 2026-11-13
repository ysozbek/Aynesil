/**
 * Educator / guardian ↔ user account link API.
 * Wraps /api/educators/{id}/user-account and /api/guardians/{id}/user-account (+ by-user lookups).
 * Paths are written out per resource (not `/${resource}/…`) so scripts/smoke-api can extract them.
 */
import { apiService } from '@/services/api.service'
import type {
  CreatedLinkedUserAccount,
  LinkedEducator,
  LinkedGuardian,
  LinkedUserAccount,
  NewLinkedUserAccountPayload,
  UserLinkResource,
} from '@/types/userAccountLink.types'

const educators = {
  get: (id: string) => apiService.get<LinkedUserAccount | null>(`/educators/${id}/user-account`),
  link: (id: string, userId: string) => apiService.put<LinkedUserAccount>(`/educators/${id}/user-account`, { userId }),
  unlink: (id: string) => apiService.delete(`/educators/${id}/user-account`),
  create: (id: string, payload: NewLinkedUserAccountPayload) =>
    apiService.post<CreatedLinkedUserAccount>(`/educators/${id}/user-account`, payload),
}

const guardians = {
  get: (id: string) => apiService.get<LinkedUserAccount | null>(`/guardians/${id}/user-account`),
  link: (id: string, userId: string) => apiService.put<LinkedUserAccount>(`/guardians/${id}/user-account`, { userId }),
  unlink: (id: string) => apiService.delete(`/guardians/${id}/user-account`),
  create: (id: string, payload: NewLinkedUserAccountPayload) =>
    apiService.post<CreatedLinkedUserAccount>(`/guardians/${id}/user-account`, payload),
}

const byResource: Record<UserLinkResource, typeof educators> = { educators, guardians }

export const userAccountLinkService = {
  get: (resource: UserLinkResource, id: string) => byResource[resource].get(id),
  link: (resource: UserLinkResource, id: string, userId: string) => byResource[resource].link(id, userId),
  unlink: (resource: UserLinkResource, id: string) => byResource[resource].unlink(id),
  create: (resource: UserLinkResource, id: string, payload: NewLinkedUserAccountPayload) =>
    byResource[resource].create(id, payload),

  educatorByUser: (userId: string) => apiService.get<LinkedEducator | null>(`/educators/by-user/${userId}`),
  guardianByUser: (userId: string) => apiService.get<LinkedGuardian | null>(`/guardians/by-user/${userId}`),
}
