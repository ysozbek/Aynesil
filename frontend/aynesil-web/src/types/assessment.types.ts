// Assessment & Evaluation types — mirrors backend DTOs exactly

export interface AssessmentTemplateListItemDto {
  id: string
  code: string
  name: string
  typeName: string | null
  typeCode: string | null
  categoryName: string | null
  categoryCode: string | null
  scoringModel: string | null
  version: number
  isActive: boolean
  corporationId: string | null
  sectionCount: number
  createdAt: string
  updatedAt: string
}

export interface AssessmentTemplateDto extends AssessmentTemplateListItemDto {
  sections: AssessmentSectionDto[]
  translations: AssessmentTranslationDto[]
  rowVersion: number
}

export interface AssessmentTranslationDto {
  locale: string
  name: string
  description: string | null
}

export interface AssessmentSectionDto {
  id: string
  templateId: string
  code: string
  sortOrder: number
  developmentAreaId: string | null
  developmentAreaName: string | null
  items: AssessmentItemDto[]
}

export interface AssessmentItemDto {
  id: string
  sectionId: string
  code: string
  prompt: string
  responseType: 'numeric' | 'scale' | 'boolean' | 'text' | 'choice'
  choices: string | null
  weight: number
  sortOrder: number
}

// ── Session DTOs ───────────────────────────────────────────────────────────────

export interface AssessmentSessionListItemDto {
  id: string
  templateId: string
  templateName: string
  templateCode: string
  corporationId: string
  campusId: string | null
  campusName: string | null
  leadId: string | null
  leadContactName: string | null
  studentId: string | null
  studentName: string | null
  assessorId: string | null
  assessorName: string | null
  status: 'planned' | 'in_progress' | 'completed' | 'cancelled'
  scheduledAt: string | null
  startedAt: string | null
  completedAt: string | null
  totalScore: number | null
  createdAt: string
}

export interface AssessmentSessionDto extends AssessmentSessionListItemDto {
  responses: AssessmentResponseDto[]
  rowVersion: number
}

export interface AssessmentResponseDto {
  id: string
  sessionId: string
  itemId: string
  itemCode: string
  itemPrompt: string
  numericValue: number | null
  textValue: string | null
  choiceValue: string | null
  note: string | null
  respondedAt: string
}

// ── Report & Recommendation DTOs ──────────────────────────────────────────────

export interface AssessmentReportDto {
  id: string
  sessionId: string
  corporationId: string
  summary: string | null
  findings: string | null
  fileId: string | null
  isFinalized: boolean
  finalizedAt: string | null
  finalizedByName: string | null
  rowVersion: number
  createdAt: string
  updatedAt: string
}

export interface ProgramRecommendationDto {
  id: string
  sessionId: string
  corporationId: string
  leadId: string | null
  studentId: string | null
  recommendedProgramId: string | null
  recommendedProgramName: string | null
  recommendedIntensity: string | null
  rationale: string | null
  recommendedByName: string | null
  rowVersion: number
  createdAt: string
}

// ── Request payloads ───────────────────────────────────────────────────────────

export interface CreateTemplatePayload {
  corporationId?: string
  code: string
  name: string
  typeId?: string
  categoryId?: string
  scoringModel?: string
  translations?: Array<{ locale: string; name: string; description?: string }>
}

export interface UpdateTemplatePayload {
  name: string
  typeId?: string
  categoryId?: string
  scoringModel?: string
  rowVersion: number
}

export interface SetTemplateActivePayload {
  isActive: boolean
  rowVersion: number
}

export interface UpsertTranslationPayload {
  name: string
  description?: string
}

export interface AddSectionPayload {
  code: string
  sortOrder: number
  developmentAreaId?: string
}

export interface UpdateSectionPayload {
  code: string
  sortOrder: number
  developmentAreaId?: string
}

export interface AddItemPayload {
  code: string
  prompt: string
  responseType: string
  choices?: string
  weight: number
  sortOrder: number
}

export interface UpdateItemPayload extends AddItemPayload {}

export interface CreateSessionPayload {
  corporationId: string
  templateId: string
  leadId?: string
  studentId?: string
  campusId?: string
  assessorId?: string
  scheduledAt?: string
}

export interface UpdateSessionPayload {
  scheduledAt?: string
  assessorId?: string
  campusId?: string
  rowVersion: number
}

export interface SubmitResponsesPayload {
  responses: ResponseItemPayload[]
}

export interface ResponseItemPayload {
  itemId: string
  numericValue?: number
  textValue?: string
  choiceValue?: string
  note?: string
}

export interface CreateReportPayload {
  corporationId: string
  summary?: string
  findings?: string
  fileId?: string
}

export interface UpdateReportPayload {
  reportId: string
  summary?: string
  findings?: string
  fileId?: string
  rowVersion: number
}

export interface FinalizeReportPayload {
  reportId: string
  rowVersion: number
}

export interface CreateRecommendationPayload {
  corporationId: string
  leadId?: string
  studentId?: string
  recommendedProgramId?: string
  recommendedIntensity?: string
  rationale?: string
  recommendedBy?: string
}

export interface UpdateRecommendationPayload {
  recommendedProgramId?: string
  recommendedIntensity?: string
  rationale?: string
  recommendedBy?: string
  rowVersion: number
}

// ── Query types ────────────────────────────────────────────────────────────────

export interface TemplateListQuery {
  corporationId?: string
  typeId?: string
  categoryId?: string
  isActive?: boolean
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export interface SessionListQuery {
  corporationId?: string
  campusId?: string
  templateId?: string
  status?: string
  leadId?: string
  studentId?: string
  assessorId?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}
