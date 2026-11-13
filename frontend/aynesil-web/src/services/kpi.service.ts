/**
 * Educator Performance & KPI API service.
 * Wraps all /api/performance-kpi endpoints.
 */
import { apiService } from '@/services/api.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  SubmitParentFeedbackPayload, CreateKpiDefinitionPayload, UpdateKpiDefinitionPayload,
} from '@/types/kpi.types'
import type {
  KpiCategoryDto, KpiDefinitionListItemDto, KpiDefinitionDto,
  KpiValueDto, EducatorPerformanceSnapshotListItemDto, EducatorPerformanceSnapshotDto,
  ParentFeedbackDto, EducatorDashboardDto, ManagerDashboardDto, ExecutiveDashboardDto,
  RankingItemDto, KpiReportRowDto,
  KpiDefinitionListQuery, SnapshotListQuery, KpiReportQuery, DashboardQuery,
  KpiValueListQuery, EducatorKpiValueQuery, KpiTrendQuery, KpiTrendDto,
} from '@/types/kpi.types'

function qs(params: object): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const kpiService = {
  // ── Categories ─────────────────────────────────────────────────────────────
  listCategories: () =>
    apiService.get<KpiCategoryDto[]>('/performance-kpi/categories'),

  // ── Definitions ────────────────────────────────────────────────────────────
  listDefinitions: (query: KpiDefinitionListQuery) =>
    apiService.get<PaginatedResult<KpiDefinitionListItemDto>>(
      `/performance-kpi/definitions${qs(query)}`
    ),

  getDefinition: (id: string) =>
    apiService.get<KpiDefinitionDto>(`/performance-kpi/definitions/${id}`),

  createDefinition: (payload: CreateKpiDefinitionPayload) =>
    apiService.post<KpiDefinitionDto>('/performance-kpi/definitions', payload),

  updateDefinition: (id: string, payload: UpdateKpiDefinitionPayload) =>
    apiService.put<KpiDefinitionDto>(`/performance-kpi/definitions/${id}`, payload),

  activateDefinition: (id: string) =>
    apiService.post(`/performance-kpi/definitions/${id}/activate`),

  deactivateDefinition: (id: string) =>
    apiService.post(`/performance-kpi/definitions/${id}/deactivate`),

  // ── KPI Values ─────────────────────────────────────────────────────────────
  // Backend returns PaginatedResult<KpiValueDto> (see KpiValueQueries.cs).
  listKpiValues: (query: KpiValueListQuery) =>
    apiService.get<PaginatedResult<KpiValueDto>>(`/performance-kpi/kpi-values${qs(query)}`),

  listEducatorKpiValues: (
    educatorId: string,
    query: EducatorKpiValueQuery = {},
  ) =>
    apiService.get<KpiValueDto[]>(
      `/performance-kpi/educators/${educatorId}/kpi-values${qs(query)}`,
    ),

  // Backend route: /performance-kpi/educators/{educatorId}/compute
  // Body: { CorporationId, PeriodStart, PeriodEnd } (CorporationId required).
  computeEducator: (
    educatorId: string,
    payload: { periodStart: string; periodEnd: string },
  ) =>
    apiService.post<EducatorPerformanceSnapshotDto>(
      `/performance-kpi/educators/${educatorId}/compute`,
      payload,
    ),

  // Backend route: /performance-kpi/compute/bulk
  bulkCompute: (payload: { periodStart: string; periodEnd: string }) =>
    apiService.post('/performance-kpi/compute/bulk', payload),

  // ── Snapshots ──────────────────────────────────────────────────────────────
  // Backend only exposes a paginated list; there is no `GET /snapshots/{id}`.
  listSnapshots: (query: SnapshotListQuery) =>
    apiService.get<PaginatedResult<EducatorPerformanceSnapshotListItemDto>>(
      `/performance-kpi/snapshots${qs(query)}`
    ),

  // ── Parent Feedback ────────────────────────────────────────────────────────
  // Backend returns PaginatedResult<ParentFeedbackDto>.
  listParentFeedback: (query: { educatorId?: string } & Record<string, unknown>) =>
    apiService.get<PaginatedResult<ParentFeedbackDto>>(`/performance-kpi/parent-feedback${qs(query)}`),

  submitFeedback: (payload: SubmitParentFeedbackPayload) =>
    apiService.post<ParentFeedbackDto>('/performance-kpi/parent-feedback', payload),

  // ── Dashboards ─────────────────────────────────────────────────────────────
  getEducatorDashboard: (educatorId: string, query: DashboardQuery) =>
    apiService.get<EducatorDashboardDto>(
      `/performance-kpi/dashboards/educator/${educatorId}${qs(query)}`
    ),

  getManagerDashboard: (query: DashboardQuery) =>
    apiService.get<ManagerDashboardDto>(
      `/performance-kpi/dashboards/manager${qs(query)}`
    ),

  getExecutiveDashboard: (query: DashboardQuery) =>
    apiService.get<ExecutiveDashboardDto>(
      `/performance-kpi/dashboards/executive${qs(query)}`
    ),

  // ── Trends & Ranking ──────────────────────────────────────────────────────
  // Backend splits trends into `trends/educator/{id}` and `trends/corporation`.
  getEducatorTrend: (educatorId: string, query: KpiTrendQuery) =>
    apiService.get<KpiTrendDto>(
      `/performance-kpi/trends/educator/${educatorId}${qs(query)}`,
    ),

  getCorporationTrend: (query: KpiTrendQuery) =>
    apiService.get<KpiTrendDto>(
      `/performance-kpi/trends/corporation${qs(query)}`,
    ),

  getRanking: (query: { kpiCode?: string; periodStart?: string; periodEnd?: string } & Record<string, unknown>) =>
    apiService.get<RankingItemDto[]>(`/performance-kpi/ranking${qs(query)}`),

  // ── Reports ────────────────────────────────────────────────────────────────
  getKpiReport: (query: KpiReportQuery) =>
    apiService.get<KpiReportRowDto[]>(`/performance-kpi/reports/kpi${qs(query)}`),
}
