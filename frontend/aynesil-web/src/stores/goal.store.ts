/**
 * Goal store — libraries, templates, student goals, progress tracking, analytics.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { goalService } from '@/services/goal.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  GoalLibraryDto,
  GoalLibraryListItemDto,
  GoalTemplateDto,
  GoalTemplateListItemDto,
  StudentGoalDto,
  StudentGoalListItemDto,
  GoalProgressDto,
  GoalTrendDto,
  StudentGoalSummaryDto,
  GoalSuccessRateDto,
  GoalLibraryListQuery,
  GoalTemplateListQuery,
  StudentGoalListQuery,
  GoalProgressQuery,
  SuccessRatesQuery,
  CreateGoalLibraryPayload,
  UpdateGoalLibraryPayload,
  CreateGoalTemplatePayload,
  UpdateGoalTemplatePayload,
  SetGoalTemplateTranslationPayload,
  CreateStudentGoalPayload,
  UpdateStudentGoalPayload,
  ChangeGoalStatusPayload,
  RecordProgressPayload,
} from '@/types/goal.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useGoalStore = defineStore('goal', () => {
  const libraryList = ref<PaginatedResult<GoalLibraryListItemDto>>(emptyPage<GoalLibraryListItemDto>())
  const currentLibrary = ref<GoalLibraryDto | null>(null)
  const templateList = ref<PaginatedResult<GoalTemplateListItemDto>>(emptyPage<GoalTemplateListItemDto>())
  const currentTemplate = ref<GoalTemplateDto | null>(null)
  const studentGoalList = ref<PaginatedResult<StudentGoalListItemDto>>(emptyPage<StudentGoalListItemDto>())
  const currentStudentGoal = ref<StudentGoalDto | null>(null)
  const progressList = ref<GoalProgressDto[]>([])
  const trend = ref<GoalTrendDto | null>(null)
  const summary = ref<StudentGoalSummaryDto | null>(null)
  const successRates = ref<GoalSuccessRateDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Libraries ───────────────────────────────────────────────────────────────

  async function fetchLibraries(query: GoalLibraryListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await goalService.listLibraries(query)
      if (res.success && res.data) libraryList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchLibrary(id: string) {
    loading.value = true
    try {
      const res = await goalService.getLibrary(id)
      if (res.success && res.data) currentLibrary.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createLibrary(payload: CreateGoalLibraryPayload): Promise<GoalLibraryDto> {
    saving.value = true
    try {
      const res = await goalService.createLibrary(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kütüphane oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateLibrary(id: string, payload: UpdateGoalLibraryPayload): Promise<GoalLibraryDto> {
    saving.value = true
    try {
      const res = await goalService.updateLibrary(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kütüphane güncellenemedi.')
      currentLibrary.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteLibrary(id: string) {
    saving.value = true
    try {
      await goalService.deleteLibrary(id)
      if (currentLibrary.value?.id === id) currentLibrary.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Templates ───────────────────────────────────────────────────────────────

  async function fetchTemplates(query: GoalTemplateListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await goalService.listTemplates(query)
      if (res.success && res.data) templateList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchTemplate(id: string) {
    loading.value = true
    try {
      const res = await goalService.getTemplate(id)
      if (res.success && res.data) currentTemplate.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createTemplate(payload: CreateGoalTemplatePayload): Promise<GoalTemplateDto> {
    saving.value = true
    try {
      const res = await goalService.createTemplate(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateTemplate(id: string, payload: UpdateGoalTemplatePayload): Promise<GoalTemplateDto> {
    saving.value = true
    try {
      const res = await goalService.updateTemplate(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon güncellenemedi.')
      currentTemplate.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function setTemplateTranslation(id: string, locale: string, payload: SetGoalTemplateTranslationPayload) {
    saving.value = true
    try {
      await goalService.setTemplateTranslation(id, locale, payload)
      if (currentTemplate.value) await fetchTemplate(id)
    } finally {
      saving.value = false
    }
  }

  async function deleteTemplate(id: string) {
    saving.value = true
    try {
      await goalService.deleteTemplate(id)
      if (currentTemplate.value?.id === id) currentTemplate.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Student Goals ───────────────────────────────────────────────────────────

  async function fetchStudentGoals(query: StudentGoalListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await goalService.listStudentGoals(query)
      if (res.success && res.data) studentGoalList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchStudentGoal(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await goalService.getStudentGoal(id)
      if (res.success && res.data) {
        currentStudentGoal.value = res.data
        progressList.value = res.data.recentProgress
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createStudentGoal(payload: CreateStudentGoalPayload): Promise<StudentGoalDto> {
    saving.value = true
    try {
      const res = await goalService.createStudentGoal(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hedef oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateStudentGoal(id: string, payload: UpdateStudentGoalPayload): Promise<StudentGoalDto> {
    saving.value = true
    try {
      const res = await goalService.updateStudentGoal(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hedef güncellenemedi.')
      currentStudentGoal.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function changeGoalStatus(id: string, payload: ChangeGoalStatusPayload): Promise<StudentGoalDto> {
    saving.value = true
    try {
      const res = await goalService.changeGoalStatus(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hedef durumu değiştirilemedi.')
      currentStudentGoal.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteStudentGoal(id: string) {
    saving.value = true
    try {
      await goalService.deleteStudentGoal(id)
      if (currentStudentGoal.value?.id === id) currentStudentGoal.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Progress ────────────────────────────────────────────────────────────────

  async function fetchProgress(goalId: string, query?: GoalProgressQuery) {
    loading.value = true
    try {
      const res = await goalService.getProgress(goalId, query)
      if (res.success && res.data) progressList.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchTrend(goalId: string) {
    loading.value = true
    try {
      const res = await goalService.getTrend(goalId)
      if (res.success && res.data) trend.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function recordProgress(goalId: string, payload: RecordProgressPayload): Promise<GoalProgressDto> {
    saving.value = true
    try {
      const res = await goalService.recordProgress(goalId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İlerleme kaydedilemedi.')
      progressList.value.unshift(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  // ── Analytics ───────────────────────────────────────────────────────────────

  async function fetchStudentSummary(corporationId: string, studentId: string) {
    loading.value = true
    try {
      const res = await goalService.getStudentSummary(corporationId, studentId)
      if (res.success && res.data) summary.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchSuccessRates(query: SuccessRatesQuery) {
    loading.value = true
    try {
      const res = await goalService.getSuccessRates(query)
      if (res.success && res.data) successRates.value = res.data
    } finally {
      loading.value = false
    }
  }

  function clearCurrent() {
    currentLibrary.value = null
    currentTemplate.value = null
    currentStudentGoal.value = null
    progressList.value = []
    trend.value = null
    summary.value = null
  }

  return {
    libraryList, currentLibrary, templateList, currentTemplate,
    studentGoalList, currentStudentGoal, progressList, trend, summary,
    successRates, loading, saving, error,
    fetchLibraries, fetchLibrary, createLibrary, updateLibrary, deleteLibrary,
    fetchTemplates, fetchTemplate, createTemplate, updateTemplate,
    setTemplateTranslation, deleteTemplate,
    fetchStudentGoals, fetchStudentGoal, createStudentGoal, updateStudentGoal,
    changeGoalStatus, deleteStudentGoal,
    fetchProgress, fetchTrend, recordProgress,
    fetchStudentSummary, fetchSuccessRates,
    clearCurrent,
  }
})
