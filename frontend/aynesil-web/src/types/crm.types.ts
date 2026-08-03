// CRM & Lead Management types — mirrors backend DTOs exactly

export interface LeadListItemDto {
  id: string
  childName: string | null
  childBirthDate: string | null
  contactName: string
  contactPhone: string | null
  contactEmail: string | null
  sourceName: string | null
  sourceCode: string | null
  statusName: string | null
  statusCode: string | null
  pipelineStageName: string | null
  pipelineStageCode: string | null
  campusName: string | null
  assignedToName: string | null
  score: number | null
  isConverted: boolean
  presentingNeed: string | null
  createdAt: string
  updatedAt: string
}

export interface LeadDto extends LeadListItemDto {
  corporationId: string
  campusId: string | null
  sourceId: string | null
  statusId: string | null
  pipelineStageId: string | null
  assignedToId: string | null
  referralDetail: string | null
  convertedStudentId: string | null
  convertedAt: string | null
  rowVersion: number
}

export interface LeadActivityDto {
  id: string
  leadId: string
  activityTypeId: string | null
  activityTypeName: string | null
  subject: string | null
  body: string | null
  direction: string | null
  occurredAt: string | null
  followUpAt: string | null
  performedByName: string | null
  createdAt: string
}

export interface LeadStatusHistoryDto {
  id: string
  leadId: string
  previousStatusCode: string | null
  newStatusCode: string | null
  previousPipelineStageCode: string | null
  newPipelineStageCode: string | null
  changedByName: string | null
  changedAt: string
}

export interface InterviewDto {
  id: string
  leadId: string
  campusId: string | null
  campusName: string | null
  status: string
  scheduledAt: string | null
  completedAt: string | null
  outcome: string | null
  recommendation: string | null
  conductedByName: string | null
  rowVersion: number
  createdAt: string
}

export interface PipelineSummaryDto {
  totalLeads: number
  convertedLeads: number
  lostLeads: number
  stages: PipelineStageCountDto[]
}

export interface PipelineStageCountDto {
  stageId: string
  stageCode: string
  stageName: string
  count: number
  sortOrder: number
}

export interface ConversionReportDto {
  corporationId: string
  from: string
  to: string
  totalLeads: number
  convertedLeads: number
  conversionRate: number
  bySource: ConversionBySourceDto[]
}

export interface ConversionBySourceDto {
  sourceId: string
  sourceName: string
  total: number
  converted: number
  rate: number
}

// ── Request payloads ───────────────────────────────────────────────────────────

export interface CreateLeadPayload {
  corporationId: string
  contactName: string
  campusId?: string
  sourceId?: string
  statusId?: string
  pipelineStageId?: string
  childName?: string
  childBirthDate?: string
  contactPhone?: string
  contactEmail?: string
  presentingNeed?: string
  referralDetail?: string
  assignedToId?: string
  score?: number
}

export interface UpdateLeadPayload {
  contactName: string
  campusId?: string
  sourceId?: string
  childName?: string
  childBirthDate?: string
  contactPhone?: string
  contactEmail?: string
  presentingNeed?: string
  referralDetail?: string
  assignedToId?: string
  score?: number
  rowVersion: number
}

export interface ChangeLeadStatusPayload {
  newStatusId: string
  newPipelineStageId?: string
  rowVersion: number
}

export interface AssignLeadPayload {
  userId: string
  rowVersion: number
}

export interface ConvertLeadPayload {
  studentId: string
  rowVersion: number
}

export interface LogActivityPayload {
  activityTypeId?: string
  subject?: string
  body?: string
  direction?: string
  occurredAt?: string
  followUpAt?: string
  performedBy?: string
}

export interface ScheduleInterviewPayload {
  campusId?: string
  scheduledAt?: string
}

export interface CompleteInterviewPayload {
  outcome?: string
  recommendation?: string
  conductedBy?: string
  rowVersion: number
}

export interface RescheduleInterviewPayload {
  newScheduledAt: string
  rowVersion: number
}

// ── Query types ────────────────────────────────────────────────────────────────

export interface LeadListQuery {
  corporationId?: string
  campusId?: string
  statusId?: string
  pipelineStageId?: string
  sourceId?: string
  assignedToId?: string
  isConverted?: boolean
  hasPendingFollowUp?: boolean
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export interface FollowUpsQuery {
  corporationId: string
  campusId?: string
  dueBy?: string
  page?: number
  pageSize?: number
}

export interface ConversionReportQuery {
  corporationId: string
  from: string
  to: string
  campusId?: string
}
