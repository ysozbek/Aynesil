/**
 * Leave Approval store.
 * Approval workflow actions are delegated to useLeaveStore.
 * This store provides a scoped view of pending approval items for the
 * approval workflow screen.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { leaveService } from '@/services/leave.service'
import type { PaginatedResult } from '@/types/api.types'
import type { LeaveRequestListItemDto } from '@/types/leave.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useLeaveApprovalStore = defineStore('leaveApproval', () => {
  const pendingRequests = ref<PaginatedResult<LeaveRequestListItemDto>>(emptyPage<LeaveRequestListItemDto>())
  const loading = ref(false)
  const error = ref<string | null>(null)

  const pendingCount = computed(() => pendingRequests.value.totalCount)

  async function fetchPending(corporationId?: string) {
    loading.value = true
    error.value = null
    try {
      const res = await leaveService.list({ corporationId, status: 'Pending', pageSize: 50 })
      if (res.success && res.data) pendingRequests.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  return { pendingRequests, pendingCount, loading, error, fetchPending }
})
