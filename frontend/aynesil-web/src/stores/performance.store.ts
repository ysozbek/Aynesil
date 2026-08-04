/**
 * Performance store.
 * Provides educator-level performance dashboard and snapshot views.
 * Delegates KPI computation to useKpiStore.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { kpiService } from '@/services/kpi.service'
import type { EducatorDashboardDto, EducatorPerformanceSnapshotDto, DashboardQuery } from '@/types/kpi.types'

export const usePerformanceStore = defineStore('performance', () => {
  const educatorDashboard = ref<EducatorDashboardDto | null>(null)
  const currentSnapshot = ref<EducatorPerformanceSnapshotDto | null>(null)
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

  async function fetchSnapshot(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await kpiService.getSnapshot(id)
      if (res.success && res.data) currentSnapshot.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  function clearCurrent() { educatorDashboard.value = null; currentSnapshot.value = null }

  return { educatorDashboard, currentSnapshot, loading, error, fetchEducatorDashboard, fetchSnapshot, clearCurrent }
})
