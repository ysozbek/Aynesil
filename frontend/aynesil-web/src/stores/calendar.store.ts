/**
 * Calendar store — school, campus, room, educator, student calendar views.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { schedulingService } from '@/services/scheduling.service'
import type { CalendarEventDto, CalendarQuery } from '@/types/scheduling.types'

export const useCalendarStore = defineStore('calendar', () => {
  const events = ref<CalendarEventDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)
  const activeView = ref<'school' | 'campus' | 'room' | 'educator' | 'student'>('school')

  async function fetchSchoolCalendar(corporationId: string, query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getSchoolCalendar(corporationId, query)
      if (res.success && res.data) events.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchCampusCalendar(campusId: string, query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getCampusCalendar(campusId, query)
      if (res.success && res.data) events.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchRoomCalendar(roomId: string, query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getRoomCalendar(roomId, query)
      if (res.success && res.data) events.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchEducatorCalendar(educatorId: string, query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getEducatorCalendar(educatorId, query)
      if (res.success && res.data) events.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchStudentCalendar(studentId: string, query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getStudentCalendar(studentId, query)
      if (res.success && res.data) events.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  function clearEvents() {
    events.value = []
  }

  return {
    events, loading, error, activeView,
    fetchSchoolCalendar, fetchCampusCalendar, fetchRoomCalendar,
    fetchEducatorCalendar, fetchStudentCalendar,
    clearEvents,
  }
})
