/**
 * Meeting Calendar store — calendar view data for meetings.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { meetingService } from '@/services/meeting.service'
import type { MeetingCalendarItemDto, MeetingCalendarQuery } from '@/types/meeting.types'

export const useMeetingCalendarStore = defineStore('meetingCalendar', () => {
  const calendarItems = ref<MeetingCalendarItemDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchCalendar(query: MeetingCalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await meetingService.getCalendar(query)
      if (res.success && res.data) calendarItems.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  function clearCalendar() {
    calendarItems.value = []
  }

  return {
    calendarItems,
    loading, error,
    fetchCalendar, clearCalendar,
  }
})
