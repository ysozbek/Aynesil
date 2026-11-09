/**
 * Performance store.
 * Provides educator-level performance dashboard view.
 * Delegates KPI computation to useKpiStore.
 *
 * NOTE: Backend does not expose `GET /performance-kpi/snapshots/{id}`; use
 * `useKpiStore.fetchSnapshots({ educatorId })` for the paginated list instead.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { kpiService } from '@/services/kpi.service'
import type { EducatorDashboardDto, DashboardQuery } from '@/types/kpi.types'

export const usePerformanceStore = defineStore('performance', () => {
  const educatorDashboard = ref<EducatorDashboardDto | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchEducatorDashboard(educatorId: string, query: DashboardQuery) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.getEducatorDashboard(educatorId, query)
      if (res.success && res.data) educatorDashboard.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  function clearCurrent() { educatorDashboard.value = null }

  return { educatorDashboard, loading, error, fetchEducatorDashboard, clearCurrent }
})
