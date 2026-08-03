/**
 * Educator Management API service.
 * Wraps all /api/educators endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  EducatorDto,
  EducatorListItemDto,
  EducatorCampusDto,
  EducatorSpecialtyDto,
  EducatorCertificationDto,
  EducatorHierarchyDto,
  EducatorAvailabilityDto,
  EducatorUtilizationDto,
  EducatorListQuery,
  UtilizationQuery,
  CreateEducatorPayload,
  UpdateEducatorPayload,
  AssignSpecialtyPayload,
  AssignCampusPayload,
  EndCampusAssignmentPayload,
  AddCertificationPayload,
  UpdateCertificationPayload,
  LinkHierarchyPayload,
  EndHierarchyPayload,
} from '@/types/educator.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const educatorService = {
  // ── Queries ─────────────────────────────────────────────────────────────────

  listEducators: (query: EducatorListQuery) =>
    apiService.get<PaginatedResult<EducatorListItemDto>>(
      `/educators${qs(query as Record<string, unknown>)}`
    ),

  getEducator: (id: string) =>
    apiService.get<EducatorDto>(`/educators/${id}`),

  getAvailability: (id: string) =>
    apiService.get<EducatorAvailabilityDto>(`/educators/${id}/availability`),

  getUtilization: (query: UtilizationQuery) =>
    apiService.get<EducatorUtilizationDto[]>(
      `/educators/utilization${qs(query as Record<string, unknown>)}`
    ),

  // ── CRUD Commands ───────────────────────────────────────────────────────────

  createEducator: (payload: CreateEducatorPayload) =>
    apiService.post<EducatorDto>('/educators', payload),

  updateEducator: (id: string, payload: UpdateEducatorPayload) =>
    apiService.put<EducatorDto>(`/educators/${id}`, payload),

  deleteEducator: (id: string) =>
    apiService.delete(`/educators/${id}`),

  activateEducator: (id: string) =>
    apiService.post<EducatorDto>(`/educators/${id}/activate`),

  deactivateEducator: (id: string) =>
    apiService.post<EducatorDto>(`/educators/${id}/deactivate`),

  // ── Specialty Commands ──────────────────────────────────────────────────────

  assignSpecialty: (id: string, payload: AssignSpecialtyPayload) =>
    apiService.post<EducatorSpecialtyDto>(`/educators/${id}/specialties`, payload),

  removeSpecialty: (id: string, assignmentId: string) =>
    apiService.delete(`/educators/${id}/specialties/${assignmentId}`),

  // ── Campus Commands ─────────────────────────────────────────────────────────

  assignCampus: (id: string, payload: AssignCampusPayload) =>
    apiService.post<EducatorCampusDto>(`/educators/${id}/campuses`, payload),

  endCampusAssignment: (id: string, assignmentId: string, payload: EndCampusAssignmentPayload) =>
    apiService.patch<EducatorCampusDto>(`/educators/${id}/campuses/${assignmentId}/end`, payload),

  // ── Certification Commands ──────────────────────────────────────────────────

  addCertification: (id: string, payload: AddCertificationPayload) =>
    apiService.post<EducatorCertificationDto>(`/educators/${id}/certifications`, payload),

  updateCertification: (id: string, certId: string, payload: UpdateCertificationPayload) =>
    apiService.put<EducatorCertificationDto>(`/educators/${id}/certifications/${certId}`, payload),

  deleteCertification: (id: string, certId: string) =>
    apiService.delete(`/educators/${id}/certifications/${certId}`),

  // ── Hierarchy Commands ──────────────────────────────────────────────────────

  linkHierarchy: (id: string, payload: LinkHierarchyPayload) =>
    apiService.post<EducatorHierarchyDto>(`/educators/${id}/hierarchy`, payload),

  endHierarchy: (id: string, edgeId: string, payload: EndHierarchyPayload) =>
    apiService.patch<EducatorHierarchyDto>(`/educators/${id}/hierarchy/${edgeId}/end`, payload),

  unlinkHierarchy: (id: string, edgeId: string) =>
    apiService.delete(`/educators/${id}/hierarchy/${edgeId}`),
}
