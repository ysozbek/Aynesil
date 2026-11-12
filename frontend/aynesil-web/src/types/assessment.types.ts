// Assessment & Evaluation types — mirrors
// `src/Aynesil.Application/Features/Assessment/Dtos/*.cs`.
//
// NOTE: Backend does NOT return human-readable names for type/category/assessor
// /student/lead. UI must enrich from refdata/student/lead stores (see
// `AssessmentProjection.cs`).

export interface AssessmentTemplateListItemDto {
  id: string
  corporationId: string | null
  code: string
  name: string
  typeId: string | null
  typeCode: string | null
  categoryId: string | null
  categoryCode: string | null
  scoringModel: string | null
  version: number
  isActive: boolean
  sectionCount: number
  createdAt: string
}

/** Detail DTO has no sectionCount (use sections.length). */
export interface AssessmentTemplateDto extends Omit<AssessmentTemplateListItemDto, 'sectionCount'> {
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
  developmentAreaCode: string | null
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

/** Status tokens come from `assessment.assessment_session.status` (DB check). */
export type AssessmentSessionStatus =
  | 'planned'
  | 'in_progress'
  | 'completed'
  | 'cancelled'

export interface AssessmentSessionListItemDto {
  id: string
  corporationId: string
  templateId: string
  templateName: string | null
  templateVersion: number
  leadId: string | null
  studentId: string | null
  campusId: string | null
  campusName: string | null
  assessorId: string | null
  scheduledAt: string | null
  status: AssessmentSessionStatus | string
  totalScore: number | null
  createdAt: string
}

export interface AssessmentSessionDto extends AssessmentSessionListItemDto {
  /** Backend uses `performedAt`, not `completedAt`. */
  performedAt: string | null
  responses: AssessmentResponseDto[]
  updatedAt: string
  rowVersion: number
}

/** Mirrors `AssessmentResponseDto` on the API. */
export interface AssessmentResponseDto {
  id: string
  /** Backend property is `AssessmentSessionId`; JSON key is `assessmentSessionId`. */
  assessmentSessionId: string
  itemId: string
  itemCode: string | null
  numericValue: number | null
  textValue: string | null
  choiceValue: string | null
  note: string | null
}

// ── Report & Recommendation DTOs ──────────────────────────────────────────────

export interface AssessmentReportDto {
  id: string
  corporationId: string
  assessmentSessionId: string
  summary: string | null
  findings: string | null
  fileId: string | null
  finalizedAt: string | null
  finalizedBy: string | null
  isFinalized: boolean
  rowVersion: number
  createdAt: string
  updatedAt: string
}

export interface ProgramRecommendationDto {
  id: string
  corporationId: string
  assessmentSessionId: string | null
  leadId: string | null
  studentId: string | null
  recommendedProgramId: string | null
  recommendedIntensity: string | null
  rationale: string | null
  recommendedBy: string | null
  rowVersion: number
  createdAt: string
  updatedAt: string
}

// ── Request payloads ───────────────────────────────────────────────────────────

export interface CreateTemplatePayload {
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
