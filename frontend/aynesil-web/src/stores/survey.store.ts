/**
 * Survey & parent-feedback store.
 * Staff screens (list/form/results/feedback) and the parent-portal screens (pending surveys, own feedback).
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { surveyService } from '@/services/survey.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  SurveyListItemDto, SurveyDto, SurveyListQuery, CreateSurveyPayload, UpdateSurveyPayload,
  SurveyResultsDto, SubmitSurveyResponsePayload, PendingSurveyDto,
  ParentFeedbackListItemDto, ParentFeedbackListQuery, CreateParentFeedbackPayload,
} from '@/types/survey.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useSurveyStore = defineStore('survey', () => {
  const surveys = ref<PaginatedResult<SurveyListItemDto>>(emptyPage<SurveyListItemDto>())
  const current = ref<SurveyDto | null>(null)
  const results = ref<SurveyResultsDto | null>(null)
  const feedback = ref<PaginatedResult<ParentFeedbackListItemDto>>(emptyPage<ParentFeedbackListItemDto>())
  const pending = ref<PendingSurveyDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function load<T>(run: () => Promise<{ success: boolean; data?: T }>, assign: (d: T) => void) {
    loading.value = true; error.value = null
    try {
      const res = await run()
      if (res.success && res.data !== undefined) assign(res.data)
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  const fetchSurveys = (q: SurveyListQuery) => load(() => surveyService.list(q), (d) => { surveys.value = d })
  const fetchSurvey = (id: string) => load(() => surveyService.get(id), (d) => { current.value = d })
  const fetchResults = (id: string) => load(() => surveyService.results(id), (d) => { results.value = d })
  const fetchFeedback = (q: ParentFeedbackListQuery) => load(() => surveyService.listFeedback(q), (d) => { feedback.value = d })
  const fetchMyFeedback = (q: ParentFeedbackListQuery) => load(() => surveyService.myFeedback(q), (d) => { feedback.value = d })
  const fetchPending = () => load(() => surveyService.pending(), (d) => { pending.value = d })

  async function create(payload: CreateSurveyPayload): Promise<string> {
    saving.value = true
    try {
      const res = await surveyService.create(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Anket oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function update(id: string, payload: UpdateSurveyPayload) {
    saving.value = true
    try {
      const res = await surveyService.update(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Anket güncellenemedi.')
    } finally { saving.value = false }
  }

  async function remove(id: string) {
    saving.value = true
    try {
      const res = await surveyService.remove(id)
      if (!res.success) throw new Error(res.message ?? 'Anket silinemedi.')
    } finally { saving.value = false }
  }

  async function submitResponse(payload: SubmitSurveyResponsePayload) {
    saving.value = true
    try {
      const res = await surveyService.submitResponse(payload)
      if (!res.success) throw new Error(res.message ?? 'Yanıt gönderilemedi.')
    } finally { saving.value = false }
  }

  async function createFeedback(payload: CreateParentFeedbackPayload) {
    saving.value = true
    try {
      const res = await surveyService.createFeedback(payload)
      if (!res.success) throw new Error(res.message ?? 'Geri bildirim gönderilemedi.')
    } finally { saving.value = false }
  }

  return {
    surveys, current, results, feedback, pending, loading, saving, error,
    fetchSurveys, fetchSurvey, fetchResults, fetchFeedback, fetchMyFeedback, fetchPending,
    create, update, remove, submitResponse, createFeedback,
  }
})
