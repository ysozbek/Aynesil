/**
 * Scheduling store — rooms, recurring schedules, bulk operations, dashboard.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { schedulingService } from '@/services/scheduling.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  RoomDto,
  RoomListItemDto,
  RoomListQuery,
  CreateRoomPayload,
  UpdateRoomPayload,
  RecurringScheduleDto,
  RecurringScheduleListItemDto,
  RecurringScheduleListQuery,
  CreateRecurringSchedulePayload,
  AddRecurringExceptionPayload,
  BulkCancelPayload,
  BulkReassignRoomPayload,
  BulkOperationResultDto,
  CalendarEntryDto,
  CreateCalendarEntryPayload,
} from '@/types/scheduling.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useSchedulingStore = defineStore('scheduling', () => {
  // ── Rooms ──────────────────────────────────────────────────────────────────
  const roomList = ref<PaginatedResult<RoomListItemDto>>(emptyPage<RoomListItemDto>())
  const currentRoom = ref<RoomDto | null>(null)

  // ── Recurring Schedules ────────────────────────────────────────────────────
  const recurringList = ref<PaginatedResult<RecurringScheduleListItemDto>>(emptyPage<RecurringScheduleListItemDto>())
  const currentRecurring = ref<RecurringScheduleDto | null>(null)

  // ── Calendar Entries ───────────────────────────────────────────────────────
  const calendarEntries = ref<CalendarEntryDto[]>([])

  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Rooms ──────────────────────────────────────────────────────────────────

  async function fetchRooms(query: RoomListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.listRooms(query)
      if (res.success && res.data) roomList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchRoom(id: string) {
    loading.value = true
    try {
      const res = await schedulingService.getRoom(id)
      if (res.success && res.data) currentRoom.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createRoom(payload: CreateRoomPayload): Promise<RoomDto> {
    saving.value = true
    try {
      const res = await schedulingService.createRoom(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Oda oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateRoom(id: string, payload: UpdateRoomPayload): Promise<RoomDto> {
    saving.value = true
    try {
      const res = await schedulingService.updateRoom(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Oda güncellenemedi.')
      currentRoom.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deactivateRoom(id: string) {
    saving.value = true
    try {
      await schedulingService.deactivateRoom(id)
      if (currentRoom.value?.id === id) await fetchRoom(id)
    } finally {
      saving.value = false
    }
  }

  async function deleteRoom(id: string) {
    saving.value = true
    try {
      await schedulingService.deleteRoom(id)
      if (currentRoom.value?.id === id) currentRoom.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Calendar Entries ───────────────────────────────────────────────────────

  async function fetchCalendarEntries(corporationId: string, campusId?: string, from?: string, to?: string) {
    loading.value = true
    try {
      const res = await schedulingService.listCalendarEntries(corporationId, campusId, from, to)
      if (res.success && res.data) calendarEntries.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createCalendarEntry(payload: CreateCalendarEntryPayload) {
    saving.value = true
    try {
      const res = await schedulingService.createCalendarEntry(payload)
      if (!res.success) throw new Error(res.message ?? 'Takvim kaydı oluşturulamadı.')
    } finally {
      saving.value = false
    }
  }

  async function deleteCalendarEntry(id: string) {
    saving.value = true
    try {
      await schedulingService.deleteCalendarEntry(id)
    } finally {
      saving.value = false
    }
  }

  // ── Recurring Schedules ────────────────────────────────────────────────────

  async function fetchRecurringSchedules(query: RecurringScheduleListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.listRecurringSchedules(query)
      if (res.success && res.data) recurringList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchRecurringSchedule(id: string) {
    loading.value = true
    try {
      const res = await schedulingService.getRecurringSchedule(id)
      if (res.success && res.data) currentRecurring.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createRecurringSchedule(payload: CreateRecurringSchedulePayload): Promise<RecurringScheduleDto> {
    saving.value = true
    try {
      const res = await schedulingService.createRecurringSchedule(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Tekrar eden plan oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deactivateRecurringSchedule(id: string) {
    saving.value = true
    try {
      await schedulingService.deactivateRecurringSchedule(id)
      if (currentRecurring.value?.id === id) await fetchRecurringSchedule(id)
    } finally {
      saving.value = false
    }
  }

  async function addRecurringException(id: string, payload: AddRecurringExceptionPayload) {
    saving.value = true
    try {
      await schedulingService.addRecurringException(id, payload)
    } finally {
      saving.value = false
    }
  }

  async function generateSessions(id: string): Promise<BulkOperationResultDto> {
    saving.value = true
    try {
      const res = await schedulingService.generateSessions(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Seanslar oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function bulkCancelSessions(id: string, payload: BulkCancelPayload): Promise<BulkOperationResultDto> {
    saving.value = true
    try {
      const res = await schedulingService.bulkCancelSessions(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplu iptal başarısız.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function bulkReassignRoom(id: string, payload: BulkReassignRoomPayload): Promise<BulkOperationResultDto> {
    saving.value = true
    try {
      const res = await schedulingService.bulkReassignRoom(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplu oda değişikliği başarısız.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentRoom.value = null
    currentRecurring.value = null
  }

  return {
    roomList, currentRoom,
    recurringList, currentRecurring,
    calendarEntries,
    loading, saving, error,
    fetchRooms, fetchRoom, createRoom, updateRoom, deactivateRoom, deleteRoom,
    fetchCalendarEntries, createCalendarEntry, deleteCalendarEntry,
    fetchRecurringSchedules, fetchRecurringSchedule, createRecurringSchedule,
    deactivateRecurringSchedule, addRecurringException,
    generateSessions, bulkCancelSessions, bulkReassignRoom,
    clearCurrent,
  }
})
