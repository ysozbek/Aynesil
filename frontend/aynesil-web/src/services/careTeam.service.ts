/**
 * Care Team API service.
 * Wraps /api/students/{id}/care-team, /api/care-team/my-students and /api/students/{id}/session-educators.
 */
import { apiService } from '@/services/api.service'
import type {
  AssignCareTeamPayload,
  CareTeamAssignmentListItemDto,
  CareTeamStudentListItemDto,
  StudentSessionEducatorDto,
  UpdateCareTeamPayload,
} from '@/types/careTeam.types'

export const careTeamService = {
  list: (studentId: string, activeOnly = true) =>
    apiService.get<CareTeamAssignmentListItemDto[]>(`/students/${studentId}/care-team?activeOnly=${activeOnly}`),

  getOne: (studentId: string, assignmentId: string) =>
    apiService.get<CareTeamAssignmentListItemDto>(`/students/${studentId}/care-team/${assignmentId}`),

  assign: (studentId: string, payload: AssignCareTeamPayload) =>
    apiService.post<CareTeamAssignmentListItemDto>(`/students/${studentId}/care-team`, payload),

  update: (studentId: string, assignmentId: string, payload: UpdateCareTeamPayload) =>
    apiService.put<CareTeamAssignmentListItemDto>(`/students/${studentId}/care-team/${assignmentId}`, payload),

  remove: (studentId: string, assignmentId: string, rowVersion: number, reason?: string | null) =>
    apiService.delete(`/students/${studentId}/care-team/${assignmentId}`, { data: { rowVersion, reason: reason || null } }),

  myStudents: () => apiService.get<CareTeamStudentListItemDto[]>('/care-team/my-students'),

  sessionEducators: (studentId: string) =>
    apiService.get<StudentSessionEducatorDto[]>(`/students/${studentId}/session-educators`),
}
