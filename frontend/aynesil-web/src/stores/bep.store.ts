/**
 * BEP/IEP (Education Plan) store — academic periods, plans lifecycle,
 * plan goals, plan reviews, approvals, revisions, and reports.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { bepService } from '@/services/bep.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  AcademicPeriodDto,
  AcademicPeriodListItemDto,
  EducationPlanDto,
  EducationPlanListItemDto,
  StudentGoalSummaryReportDto,
  TrendReportRowDto,
  AcademicPeriodListQuery,
  EducationPlanListQuery,
  CreateAcademicPeriodPayload,
  UpdateAcademicPeriodPayload,
  CreateEducationPlanPayload,
  UpdateEducationPlanPayload,
  ApproveRejectPayload,
  RevisePayload,
  GuardianVisibilityPayload,
  AddGoalToPlanPayload,
  ReorderGoalsPayload,
  AddPlanReviewPayload,
} from '@/types/bep.types'

const emptyPeriodPage = (): PaginatedResult<AcademicPeriodListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

const emptyPlanPage = (): PaginatedResult<EducationPlanListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useBepStore = defineStore('bep', () => {
  const periodList = ref<PaginatedResult<AcademicPeriodListItemDto>>(emptyPeriodPage())
  const currentPeriod = ref<AcademicPeriodDto | null>(null)
  const planList = ref<PaginatedResult<EducationPlanListItemDto>>(emptyPlanPage())
  const currentPlan = ref<EducationPlanDto | null>(null)
  const summaryReport = ref<StudentGoalSummaryReportDto | null>(null)
  const trendReport = ref<TrendReportRowDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Academic Periods ────────────────────────────────────────────────────────

  async function fetchPeriods(query: AcademicPeriodListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await bepService.listAcademicPeriods(query)
      if (res.success && res.data) periodList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchPeriod(id: string) {
    loading.value = true
    try {
      const res = await bepService.getAcademicPeriod(id)
      if (res.success && res.data) currentPeriod.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createPeriod(payload: CreateAcademicPeriodPayload): Promise<AcademicPeriodDto> {
    saving.value = true
    try {
      const res = await bepService.createAcademicPeriod(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Dönem oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updatePeriod(id: string, payload: UpdateAcademicPeriodPayload): Promise<AcademicPeriodDto> {
    saving.value = true
    try {
      const res = await bepService.updateAcademicPeriod(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Dönem güncellenemedi.')
      currentPeriod.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function setCurrentPeriod(id: string): Promise<AcademicPeriodDto> {
    saving.value = true
    try {
      const res = await bepService.setCurrentPeriod(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Aktif dönem belirlenemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  // ── Education Plans ─────────────────────────────────────────────────────────

  async function fetchPlans(query: EducationPlanListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await bepService.listPlans(query)
      if (res.success && res.data) planList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchPlan(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await bepService.getPlan(id)
      if (res.success && res.data) currentPlan.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createPlan(payload: CreateEducationPlanPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.createPlan(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updatePlan(id: string, payload: UpdateEducationPlanPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.updatePlan(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan güncellenemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deletePlan(id: string) {
    saving.value = true
    try {
      await bepService.deletePlan(id)
      if (currentPlan.value?.id === id) currentPlan.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Workflow ────────────────────────────────────────────────────────────────

  async function submitPlan(id: string): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.submitPlan(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan gönderilemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function approvePlan(id: string, payload: ApproveRejectPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.approvePlan(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan onaylanamadı.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function rejectPlan(id: string, payload: ApproveRejectPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.rejectPlan(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan reddedilemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function activatePlan(id: string): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.activatePlan(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan aktifleştirilemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function closePlan(id: string): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.closePlan(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan kapatılamadı.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function revisePlan(id: string, payload: RevisePayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.revisePlan(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan revize edilemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function setGuardianVisibility(id: string, payload: GuardianVisibilityPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.setGuardianVisibility(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Görünürlük güncellenemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function addGoal(id: string, payload: AddGoalToPlanPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.addGoal(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hedef eklenemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function removeGoal(id: string, planGoalId: string): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.removeGoal(id, planGoalId)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hedef kaldırılamadı.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function reorderGoals(id: string, payload: ReorderGoalsPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.reorderGoals(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Hedef sıralaması güncellenemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function addReview(id: string, payload: AddPlanReviewPayload): Promise<EducationPlanDto> {
    saving.value = true
    try {
      const res = await bepService.addReview(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İnceleme eklenemedi.')
      currentPlan.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function fetchSummaryReport(corporationId: string, studentId: string) {
    loading.value = true
    try {
      const res = await bepService.getStudentGoalSummaryReport(corporationId, studentId)
      if (res.success && res.data) summaryReport.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchTrendReport(corporationId: string, studentId: string, from?: string, to?: string) {
    loading.value = true
    try {
      const res = await bepService.getTrendReport(corporationId, studentId, from, to)
      if (res.success && res.data) trendReport.value = res.data
    } finally {
      loading.value = false
    }
  }

  function clearCurrent() {
    currentPeriod.value = null
    currentPlan.value = null
    summaryReport.value = null
    trendReport.value = []
  }

  return {
    periodList, currentPeriod, planList, currentPlan,
    summaryReport, trendReport, loading, saving, error,
    fetchPeriods, fetchPeriod, createPeriod, updatePeriod, setCurrentPeriod,
    fetchPlans, fetchPlan, createPlan, updatePlan, deletePlan,
    submitPlan, approvePlan, rejectPlan, activatePlan, closePlan,
    revisePlan, setGuardianVisibility, addGoal, removeGoal, reorderGoals,
    addReview, fetchSummaryReport, fetchTrendReport,
    clearCurrent,
  }
})
