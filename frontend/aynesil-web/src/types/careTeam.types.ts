/** Bakım ekibi — mirrors Aynesil.Application/Features/CareTeam/Dtos. */

/** Mirrors CareTeamAssignmentListItemDto. */
export interface CareTeamAssignmentListItemDto {
  id: string
  studentId: string
  educatorId: string
  educatorName: string
  roleId: string
  roleCode?: string | null
  isPrimary: boolean
  /** active | suspended | ended */
  status: string
  activeFrom: string
  activeTo?: string | null
  createdAt: string
  rowVersion: number
  reason?: string | null
}

/** Mirrors CareTeamStudentListItemDto ("my-students"). */
export interface CareTeamStudentListItemDto {
  studentId: string
  studentFullName: string
  assignmentId: string
  roleId: string
  roleCode?: string | null
  isPrimary: boolean
  status: string
  activeFrom: string
  activeTo?: string | null
}

/** Mirrors StudentSessionEducatorDto (students/{id}/session-educators). */
export interface StudentSessionEducatorDto {
  educatorId: string
  educatorName: string
  sessionCount: number
  lastSessionAt: string
}

/** Mirrors CareTeamController.AssignRequest. Dates are yyyy-MM-dd. */
export interface AssignCareTeamPayload {
  educatorId: string
  roleId: string
  isPrimary: boolean
  activeFrom: string
  activeTo?: string | null
  campusId?: string | null
  grantTypeId?: string | null
  sourceAssignmentId?: string | null
  reason?: string | null
}

/** Mirrors CareTeamController.UpdateRequest. */
export interface UpdateCareTeamPayload {
  roleId?: string | null
  isPrimary?: boolean | null
  activeFrom?: string | null
  activeTo?: string | null
  campusId?: string | null
  reason?: string | null
  rowVersion: number
}
