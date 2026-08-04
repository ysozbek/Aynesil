/**
 * Camp Enrollment store.
 * Enrollment CRUD is delegated to useCampStore.
 * This store provides standalone enrollment-centric views (e.g., cross-camp
 * enrollment search and waitlist management dashboard).
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { campService } from '@/services/camp.service'
import type { PaginatedResult } from '@/types/api.types'
import type { CampEnrollmentListItemDto, CampEnrollmentQuery } from '@/types/camp.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useCampEnrollmentStore = defineStore('campEnrollment', () => {
  const enrollments = ref<PaginatedResult<CampEnrollmentListItemDto>>(emptyPage<CampEnrollmentListItemDto>())
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchEnrollments(periodId: string, query?: CampEnrollmentQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await campService.listEnrollments(periodId, query)
      if (res.success && res.data) enrollments.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function promoteFromWaitlist(enrollmentId: string) {
    const res = await campService.promoteFromWaitlist(enrollmentId)
    if (!res.success) throw new Error(res.message ?? 'Bekleme listesinden alma başarısız.')
  }

  async function withdraw(enrollmentId: string) {
    await campService.withdraw(enrollmentId)
  }

  return { enrollments, loading, error, fetchEnrollments, promoteFromWaitlist, withdraw }
})
