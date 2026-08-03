/**
 * Assessment Session store — list, detail, CRUD, workflow transitions,
 * response submission, reports, and recommendations.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { assessmentService } from '@/services/assessment.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  AssessmentSessionDto,
  AssessmentSessionListItemDto,
  AssessmentReportDto,
  ProgramRecommendationDto,
  CreateSessionPayload,
  UpdateSessionPayload,
  SubmitResponsesPayload,
  CreateReportPayload,
  UpdateReportPayload,
  FinalizeReportPayload,
  CreateRecommendationPayload,
  UpdateRecommendationPayload,
  SessionListQuery,
} from '@/types/assessment.types'

const emptyPage = (): PaginatedResult<AssessmentSessionListItemDto> => ({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
})

export const useAssessmentStore = defineStore('assessment', () => {
  const list = ref<PaginatedResult<AssessmentSessionListItemDto>>(emptyPage())
  const current = ref<AssessmentSessionDto | null>(null)
  const history = ref<AssessmentSessionListItemDto[]>([])
  const currentReport = ref<AssessmentReportDto | null>(null)
  const recommendations = ref<ProgramRecommendationDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(query: SessionListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await assessmentService.listSessions(query)
      if (res.success && res.data) list.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await assessmentService.getSession(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchHistory(leadId?: string, studentId?: string) {
    loading.value = true
    try {
      const res = await assessmentService.getHistory(leadId, studentId)
      if (res.success && res.data) history.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateSessionPayload): Promise<AssessmentSessionDto> {
    saving.value = true
    try {
      const res = await assessmentService.createSession(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Değerlendirme oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateSessionPayload): Promise<AssessmentSessionDto> {
    saving.value = true
    try {
      const res = await assessmentService.updateSession(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Güncelleme başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    saving.value = true
    try {
      await assessmentService.deleteSession(id)
      if (current.value?.id === id) current.value = null
    } finally {
      saving.value = false
    }
  }

  async function start(id: string, rowVersion: number): Promise<AssessmentSessionDto> {
    saving.value = true
    try {
      const res = await assessmentService.startSession(id, rowVersion)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Değerlendirme başlatılamadı.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function complete(id: string, rowVersion: number): Promise<AssessmentSessionDto> {
    saving.value = true
    try {
      const res = await assessmentService.completeSession(id, rowVersion)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Değerlendirme tamamlanamadı.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function cancel(id: string, rowVersion: number): Promise<AssessmentSessionDto> {
    saving.value = true
    try {
      const res = await assessmentService.cancelSession(id, rowVersion)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Değerlendirme iptal edilemedi.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function submitResponses(sessionId: string, payload: SubmitResponsesPayload) {
    saving.value = true
    try {
      const res = await assessmentService.submitResponses(sessionId, payload)
      if (!res.success) throw new Error(res.message ?? 'Yanıtlar kaydedilemedi.')
      if (current.value) {
        await fetchOne(sessionId)
      }
    } finally {
      saving.value = false
    }
  }

  async function fetchReport(sessionId: string) {
    loading.value = true
    try {
      const res = await assessmentService.getReport(sessionId)
      if (res.success && res.data) currentReport.value = res.data
      else currentReport.value = null
    } finally {
      loading.value = false
    }
  }

  async function createReport(sessionId: string, payload: CreateReportPayload): Promise<AssessmentReportDto> {
    saving.value = true
    try {
      const res = await assessmentService.createReport(sessionId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rapor oluşturulamadı.')
      currentReport.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateReport(sessionId: string, payload: UpdateReportPayload): Promise<AssessmentReportDto> {
    saving.value = true
    try {
      const res = await assessmentService.updateReport(sessionId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rapor güncellenemedi.')
      currentReport.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function finalizeReport(sessionId: string, payload: FinalizeReportPayload): Promise<AssessmentReportDto> {
    saving.value = true
    try {
      const res = await assessmentService.finalizeReport(sessionId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rapor sonuçlandırılamadı.')
      currentReport.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function fetchRecommendations(sessionId: string) {
    loading.value = true
    try {
      const res = await assessmentService.getRecommendations(sessionId)
      if (res.success && res.data) recommendations.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createRecommendation(sessionId: string, payload: CreateRecommendationPayload) {
    saving.value = true
    try {
      const res = await assessmentService.createRecommendation(sessionId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Öneri oluşturulamadı.')
      recommendations.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateRecommendation(sessionId: string, recId: string, payload: UpdateRecommendationPayload) {
    saving.value = true
    try {
      const res = await assessmentService.updateRecommendation(sessionId, recId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Öneri güncellenemedi.')
      const idx = recommendations.value.findIndex((r) => r.id === recId)
      if (idx >= 0) recommendations.value[idx] = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    current.value = null
    currentReport.value = null
    recommendations.value = []
  }

  return {
    list,
    current,
    history,
    currentReport,
    recommendations,
    loading,
    saving,
    error,
    fetchList,
    fetchOne,
    fetchHistory,
    create,
    update,
    remove,
    start,
    complete,
    cancel,
    submitResponses,
    fetchReport,
    createReport,
    updateReport,
    finalizeReport,
    fetchRecommendations,
    createRecommendation,
    updateRecommendation,
    clearCurrent,
  }
})
