/**
 * Calendar store — school, campus, room, educator, student calendar views.
 *
 * The backend returns:
 *   - `CalendarViewDto { sessions, entries }` for school + campus calendars.
 *   - `SessionListItemDto[]`                  for room / educator / student calendars.
 *
 * We normalise everything into a single `events` list the Vue calendar UI
 * can render uniformly, while also exposing the raw session + entry arrays
 * for callers that need them.
 */
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { schedulingService } from '@/services/scheduling.service'
import type {
  CalendarEntryDto,
  CalendarEventDto,
  CalendarQuery,
  SessionListItemDto,
} from '@/types/scheduling.types'

export const useCalendarStore = defineStore('calendar', () => {
  const sessions = ref<SessionListItemDto[]>([])
  const entries = ref<CalendarEntryDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)
  const activeView = ref<'school' | 'campus' | 'room' | 'educator' | 'student'>('school')

  /** Flat, normalised event list for calendar rendering. */
  const events = computed<CalendarEventDto[]>(() => [
    ...sessions.value.map(sessionToEvent),
    ...entries.value.map(entryToEvent),
  ])

  function reset() {
    sessions.value = []
    entries.value = []
  }

  async function fetchSchoolCalendar(query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getSchoolCalendar(query)
      if (res.success && res.data) {
        sessions.value = res.data.sessions
        entries.value = res.data.entries
      } else {
        reset()
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
      reset()
    } finally {
      loading.value = false
    }
  }

  async function fetchCampusCalendar(
    campusId: string,
    query: CalendarQuery
  ) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getCampusCalendar(campusId, query)
      if (res.success && res.data) {
        sessions.value = res.data.sessions
        entries.value = res.data.entries
      } else {
        reset()
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
      reset()
    } finally {
      loading.value = false
    }
  }

  async function fetchRoomCalendar(roomId: string, query: CalendarQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getRoomCalendar(roomId, query)
      if (res.success && res.data) {
        sessions.value = res.data
        entries.value = []
      } else {
        reset()
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
      reset()
    } finally {
      loading.value = false
    }
  }

  async function fetchEducatorCalendar(
    educatorId: string,
    query: CalendarQuery
  ) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getEducatorCalendar(educatorId, query)
      if (res.success && res.data) {
        sessions.value = res.data
        entries.value = []
      } else {
        reset()
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
      reset()
    } finally {
      loading.value = false
    }
  }

  async function fetchStudentCalendar(
    studentId: string,
    query: CalendarQuery
  ) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getStudentCalendar(studentId, query)
      if (res.success && res.data) {
        sessions.value = res.data
        entries.value = []
      } else {
        reset()
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
      reset()
    } finally {
      loading.value = false
    }
  }

  function clearEvents() {
    reset()
  }

  return {
    sessions, entries, events,
    loading, error, activeView,
    fetchSchoolCalendar, fetchCampusCalendar, fetchRoomCalendar,
    fetchEducatorCalendar, fetchStudentCalendar,
    clearEvents,
  }
})

// ── helpers ───────────────────────────────────────────────────────────────────

function sessionToEvent(s: SessionListItemDto): CalendarEventDto {
  return {
    id: s.id,
    title: s.title ?? s.sessionTypeName ?? '—',
    start: s.startsAt,
    end: s.endsAt,
    type: 'session',
    status: s.status,
    roomName: s.roomName,
    participantCount: s.participantCount,
    isMakeup: s.isMakeup,
  }
}

function entryToEvent(e: CalendarEntryDto): CalendarEventDto {
  return {
    id: e.id,
    title: e.title,
    start: e.startsAt,
    end: e.endsAt,
    type: 'calendar_entry',
  }
}
