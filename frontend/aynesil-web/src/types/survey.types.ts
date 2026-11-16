/**
 * Survey & parent-feedback type definitions.
 * Mirrors Aynesil.Application.Features.Surveys.Dtos / Commands.
 */
import type { PagedQuery, PageQuery } from './api.types'

export type SurveyTarget = 'guardian' | 'educator' | 'student'
export type SurveyQuestionType = 'text' | 'rating' | 'yes_no' | 'multiple_choice' | 'scale'

// ── Definitions ───────────────────────────────────────────────────────────────

export interface SurveyListItemDto {
  id: string
  typeId?: string
  typeCode?: string
  title: string
  target: SurveyTarget
  isActive: boolean
  questionCount: number
  responseCount: number
  updatedAt: string
}

export interface SurveyAnswerOptionDto {
  id: string
  optionText: string
  optionValue?: string
  sortOrder: number
}

export interface SurveyQuestionDto {
  id: string
  questionText: string
  questionType: SurveyQuestionType
  isRequired: boolean
  sortOrder: number
  answerOptions: SurveyAnswerOptionDto[]
}

export interface SurveyDto {
  id: string
  corporationId: string
  typeId?: string
  typeCode?: string
  title: string
  description?: string
  target: SurveyTarget
  isActive: boolean
  createdAt: string
  updatedAt: string
  rowVersion: number
  responseCount: number
  questions: SurveyQuestionDto[]
}

export interface SurveyListQuery extends PagedQuery {
  target?: SurveyTarget | ''
  isActive?: boolean | ''
  typeId?: string
}

export interface AnswerOptionPayload {
  id?: string
  optionText: string
  optionValue?: string | null
  sortOrder: number
}

export interface QuestionPayload {
  id?: string
  questionText: string
  questionType: SurveyQuestionType
  isRequired: boolean
  sortOrder: number
  answerOptions: AnswerOptionPayload[]
}

export interface CreateSurveyPayload {
  title: string
  target: SurveyTarget
  typeId?: string | null
  description?: string | null
  questions: QuestionPayload[]
}

export interface UpdateSurveyPayload {
  title: string
  description?: string | null
  target: SurveyTarget
  typeId?: string | null
  isActive: boolean
  rowVersion: number
  /** Gönderilmezse sorulara dokunulmaz (yanıt alınmış ankette gönderme: 422). */
  questions?: QuestionPayload[]
}

// ── Responses & results ───────────────────────────────────────────────────────

export interface SurveyResponseListItemDto {
  id: string
  surveyId: string
  surveyTitle: string
  guardianId?: string
  guardianName?: string
  studentId?: string
  studentName?: string
  submittedAt?: string
  isComplete: boolean
}

export interface SurveyResponseListQuery extends PageQuery {
  surveyId?: string
  guardianId?: string
  studentId?: string
  isSubmitted?: boolean
}

export interface SurveyResultBucketDto {
  label: string
  value?: string
  count: number
}

export interface SurveyQuestionResultDto {
  questionId: string
  questionText: string
  questionType: SurveyQuestionType
  isRequired: boolean
  answeredCount: number
  average?: number
  distribution: SurveyResultBucketDto[]
  textAnswers: string[]
}

export interface SurveyResultsDto {
  surveyId: string
  title: string
  target: SurveyTarget
  responseCount: number
  lastSubmittedAt?: string
  questions: SurveyQuestionResultDto[]
}

export interface QuestionAnswerPayload {
  questionId: string
  answerText?: string | null
  answerOptionId?: string | null
  numericValue?: number | null
}

export interface SubmitSurveyResponsePayload {
  surveyId: string
  guardianId?: string | null
  studentId?: string | null
  sessionId?: string | null
  answers: QuestionAnswerPayload[]
}

// ── Pending (parent portal) ───────────────────────────────────────────────────

export interface PendingSurveyStudentDto {
  studentId: string
  studentName: string
}

export interface PendingSurveyDto {
  surveyId: string
  title: string
  description?: string
  typeCode?: string
  questions: SurveyQuestionDto[]
  students: PendingSurveyStudentDto[]
}

// ── Parent feedback ───────────────────────────────────────────────────────────

export interface ParentFeedbackListItemDto {
  id: string
  guardianId?: string
  guardianName?: string
  educatorId?: string
  educatorName?: string
  sessionId?: string
  rating?: number
  comment?: string
  createdAt: string
}

export interface ParentFeedbackListQuery extends PageQuery {
  educatorId?: string
  guardianId?: string
  sessionId?: string
  sortDirection?: 'asc' | 'desc'
}

export interface CreateParentFeedbackPayload {
  guardianId?: string | null
  educatorId?: string | null
  sessionId?: string | null
  rating?: number | null
  comment?: string | null
}
