/**
 * KPI & Performance store.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { kpiService } from '@/services/kpi.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  KpiCategoryDto, KpiDefinitionListItemDto, KpiDefinitionDto,
  KpiValueDto, EducatorPerformanceSnapshotListItemDto,
  ParentFeedbackDto, EducatorDashboardDto, ManagerDashboardDto, ExecutiveDashboardDto,
  RankingItemDto, KpiReportRowDto,
  KpiDefinitionListQuery, SnapshotListQuery, DashboardQuery,
} from '@/types/kpi.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useKpiStore = defineStore('kpi', () => {
  const categories = ref<KpiCategoryDto[]>([])
  const definitions = ref<PaginatedResult<KpiDefinitionListItemDto>>(emptyPage<KpiDefinitionListItemDto>())
  const currentDefinition = ref<KpiDefinitionDto | null>(null)
  const kpiValues = ref<KpiValueDto[]>([])
  const snapshots = ref<PaginatedResult<EducatorPerformanceSnapshotListItemDto>>(emptyPage<EducatorPerformanceSnapshotListItemDto>())
  const parentFeedback = ref<ParentFeedbackDto[]>([])
  const educatorDashboard = ref<EducatorDashboardDto | null>(null)
  const managerDashboard = ref<ManagerDashboardDto | null>(null)
  const executiveDashboard = ref<ExecutiveDashboardDto | null>(null)
  const ranking = ref<RankingItemDto[]>([])
  const kpiReport = ref<KpiReportRowDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchCategories(corporationId?: string) {
    try {
      const res = await kpiService.listCategories(corporationId)
      if (res.success && res.data) categories.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
  }

  async function fetchDefinitions(query: KpiDefinitionListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.listDefinitions(query)
      if (res.success && res.data) definitions.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchDefinition(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.getDefinition(id)
      if (res.success && res.data) currentDefinition.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function toggleDefinition(id: string, activate: boolean) {
    saving.value = true
    try {
      if (activate) await kpiService.activateDefinition(id)
      else await kpiService.deactivateDefinition(id)
    } finally { saving.value = false }
  }

  async function fetchSnapshots(query: SnapshotListQuery) {
    loading.value = true
    try {
      const res = await kpiService.listSnapshots(query)
      if (res.success && res.data) snapshots.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchKpiValues(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await kpiService.listKpiValues(query)
      if (res.success && res.data) kpiValues.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function computeEducator(educatorId: string, periodStart: string, periodEnd: string) {
    saving.value = true
    try {
      await kpiService.computeEducator(educatorId, periodStart, periodEnd)
    } finally { saving.value = false }
  }

  async function fetchParentFeedback(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await kpiService.listParentFeedback(query)
      if (res.success && res.data) parentFeedback.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchEducatorDashboard(educatorId: string, query: DashboardQuery) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.getEducatorDashboard(educatorId, query)
      if (res.success && res.data) educatorDashboard.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchManagerDashboard(query: DashboardQuery) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.getManagerDashboard(query)
      if (res.success && res.data) managerDashboard.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchExecutiveDashboard(query: DashboardQuery) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.getExecutiveDashboard(query)
      if (res.success && res.data) executiveDashboard.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchRanking(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await kpiService.getRanking(query)
      if (res.success && res.data) ranking.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchKpiReport(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await kpiService.getKpiReport(query)
      if (res.success && res.data) kpiReport.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  return {
    categories, definitions, currentDefinition, kpiValues, snapshots,
    parentFeedback, educatorDashboard, managerDashboard, executiveDashboard,
    ranking, kpiReport, loading, saving, error,
    fetchCategories, fetchDefinitions, fetchDefinition, toggleDefinition,
    fetchSnapshots, fetchKpiValues, computeEducator, fetchParentFeedback,
    fetchEducatorDashboard, fetchManagerDashboard, fetchExecutiveDashboard,
    fetchRanking, fetchKpiReport,
  }
})
