/**
 * Educator / guardian ↔ user account link API.
 * Wraps /api/educators/{id}/user-account and /api/guardians/{id}/user-account (+ by-user lookups).
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

export const userAccountLinkService = {
  get: (resource: UserLinkResource, id: string) =>
    apiService.get<LinkedUserAccount | null>(`/${resource}/${id}/user-account`),

  link: (resource: UserLinkResource, id: string, userId: string) =>
    apiService.put<LinkedUserAccount>(`/${resource}/${id}/user-account`, { userId }),

  unlink: (resource: UserLinkResource, id: string) =>
    apiService.delete(`/${resource}/${id}/user-account`),

  create: (resource: UserLinkResource, id: string, payload: NewLinkedUserAccountPayload) =>
    apiService.post<CreatedLinkedUserAccount>(`/${resource}/${id}/user-account`, payload),

  educatorByUser: (userId: string) =>
    apiService.get<LinkedEducator | null>(`/educators/by-user/${userId}`),

  guardianByUser: (userId: string) =>
    apiService.get<LinkedGuardian | null>(`/guardians/by-user/${userId}`),
}
