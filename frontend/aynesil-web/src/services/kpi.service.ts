/**
 * Educator Performance & KPI API service.
 * Wraps all /api/performance-kpi endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  KpiCategoryDto, KpiDefinitionListItemDto, KpiDefinitionDto,
  KpiValueDto, EducatorPerformanceSnapshotListItemDto, EducatorPerformanceSnapshotDto,
  ParentFeedbackDto, EducatorDashboardDto, ManagerDashboardDto, ExecutiveDashboardDto,
  RankingItemDto, KpiReportRowDto,
  KpiDefinitionListQuery, SnapshotListQuery, KpiReportQuery, DashboardQuery,
} from '@/types/kpi.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const kpiService = {
  // ── Categories ─────────────────────────────────────────────────────────────
  listCategories: (corporationId?: string) =>
    apiService.get<KpiCategoryDto[]>(`/performance-kpi/categories${corporationId ? `?corporationId=${corporationId}` : ''}`),

  // ── Definitions ────────────────────────────────────────────────────────────
  listDefinitions: (query: KpiDefinitionListQuery) =>
    apiService.get<PaginatedResult<KpiDefinitionListItemDto>>(
      `/performance-kpi/definitions${qs(query as Record<string, unknown>)}`
    ),

  getDefinition: (id: string) =>
    apiService.get<KpiDefinitionDto>(`/performance-kpi/definitions/${id}`),

  activateDefinition: (id: string) =>
    apiService.post(`/performance-kpi/definitions/${id}/activate`),

  deactivateDefinition: (id: string) =>
    apiService.post(`/performance-kpi/definitions/${id}/deactivate`),

  // ── KPI Values ─────────────────────────────────────────────────────────────
  listKpiValues: (query: { corporationId?: string; kpiId?: string; educatorId?: string } & Record<string, unknown>) =>
    apiService.get<KpiValueDto[]>(`/performance-kpi/kpi-values${qs(query)}`),

  computeEducator: (educatorId: string, periodStart: string, periodEnd: string) =>
    apiService.post(`/performance-kpi/educator/${educatorId}/compute`, { periodStart, periodEnd }),

  bulkCompute: (payload: { corporationId: string; periodStart: string; periodEnd: string }) =>
    apiService.post('/performance-kpi/bulk-compute', payload),

  // ── Snapshots ──────────────────────────────────────────────────────────────
  listSnapshots: (query: SnapshotListQuery) =>
    apiService.get<PaginatedResult<EducatorPerformanceSnapshotListItemDto>>(
      `/performance-kpi/snapshots${qs(query as Record<string, unknown>)}`
    ),

  getSnapshot: (id: string) =>
    apiService.get<EducatorPerformanceSnapshotDto>(`/performance-kpi/snapshots/${id}`),

  // ── Parent Feedback ────────────────────────────────────────────────────────
  listParentFeedback: (query: { corporationId?: string; educatorId?: string } & Record<string, unknown>) =>
    apiService.get<ParentFeedbackDto[]>(`/performance-kpi/parent-feedback${qs(query)}`),

  submitFeedback: (payload: Partial<ParentFeedbackDto>) =>
    apiService.post<ParentFeedbackDto>('/performance-kpi/parent-feedback', payload),

  // ── Dashboards ─────────────────────────────────────────────────────────────
  getEducatorDashboard: (educatorId: string, query: DashboardQuery) =>
    apiService.get<EducatorDashboardDto>(
      `/performance-kpi/dashboards/educator/${educatorId}${qs(query as Record<string, unknown>)}`
    ),

  getManagerDashboard: (query: DashboardQuery) =>
    apiService.get<ManagerDashboardDto>(
      `/performance-kpi/dashboards/manager${qs(query as Record<string, unknown>)}`
    ),

  getExecutiveDashboard: (query: DashboardQuery) =>
    apiService.get<ExecutiveDashboardDto>(
      `/performance-kpi/dashboards/executive${qs(query as Record<string, unknown>)}`
    ),

  // ── Trends & Ranking ──────────────────────────────────────────────────────
  getTrends: (query: DashboardQuery) =>
    apiService.get(`/performance-kpi/trends${qs(query as Record<string, unknown>)}`),

  getRanking: (query: { corporationId?: string; kpiId?: string; periodStart?: string; periodEnd?: string } & Record<string, unknown>) =>
    apiService.get<RankingItemDto[]>(`/performance-kpi/ranking${qs(query)}`),

  // ── Reports ────────────────────────────────────────────────────────────────
  getKpiReport: (query: KpiReportQuery) =>
    apiService.get<KpiReportRowDto[]>(`/performance-kpi/reports/kpi${qs(query as Record<string, unknown>)}`),
}
