/**
 * Lead Pipeline store — Kanban summary, optimistic stage moves.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { leadService } from '@/services/lead.service'
import type { PipelineSummaryDto } from '@/types/crm.types'

export const useLeadPipelineStore = defineStore('leadPipeline', () => {
  const summary = ref<PipelineSummaryDto | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchSummary(corporationId: string, campusId?: string) {
    loading.value = true
    error.value = null
    try {
      const res = await leadService.getPipelineSummary(corporationId, campusId)
      if (res.success && res.data) summary.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  function clear() {
    summary.value = null
  }

  return { summary, loading, error, fetchSummary, clear }
})
