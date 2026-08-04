/**
 * Meeting Management store — full lifecycle, participants, outcomes, follow-ups.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { meetingService } from '@/services/meeting.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  MeetingDto,
  MeetingListItemDto,
  MeetingListQuery,
  ScheduleMeetingPayload,
  UpdateMeetingPayload,
  AddParticipantPayload,
  UpdateAttendancePayload,
  AddOutcomePayload,
  UpdateOutcomePayload,
  AddFollowUpPayload,
  UpdateFollowUpPayload,
  UpdateFollowUpStatusPayload,
} from '@/types/meeting.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useMeetingStore = defineStore('meeting', () => {
  // ── State ──────────────────────────────────────────────────────────────────
  const meetingList = ref<PaginatedResult<MeetingListItemDto>>(emptyPage<MeetingListItemDto>())
  const currentMeeting = ref<MeetingDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Meeting CRUD ───────────────────────────────────────────────────────────

  async function fetchMeetings(query: MeetingListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await meetingService.list(query)
      if (res.success && res.data) meetingList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchMeeting(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await meetingService.get(id)
      if (res.success && res.data) currentMeeting.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function scheduleMeeting(payload: ScheduleMeetingPayload): Promise<MeetingDto> {
    saving.value = true
    try {
      const res = await meetingService.schedule(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplantı oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateMeeting(id: string, payload: UpdateMeetingPayload): Promise<MeetingDto> {
    saving.value = true
    try {
      const res = await meetingService.update(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplantı güncellenemedi.')
      currentMeeting.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteMeeting(id: string) {
    saving.value = true
    try {
      const res = await meetingService.delete(id)
      if (!res.success) throw new Error(res.message ?? 'Toplantı silinemedi.')
      if (currentMeeting.value?.id === id) currentMeeting.value = null
    } finally {
      saving.value = false
    }
  }

  async function completeMeeting(id: string) {
    saving.value = true
    try {
      const res = await meetingService.complete(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplantı tamamlanamadı.')
      if (currentMeeting.value?.id === id) currentMeeting.value = res.data
    } finally {
      saving.value = false
    }
  }

  async function cancelMeeting(id: string) {
    saving.value = true
    try {
      const res = await meetingService.cancel(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Toplantı iptal edilemedi.')
      if (currentMeeting.value?.id === id) currentMeeting.value = res.data
    } finally {
      saving.value = false
    }
  }

  // ── Participants ───────────────────────────────────────────────────────────

  async function addParticipant(meetingId: string, payload: AddParticipantPayload) {
    saving.value = true
    try {
      const res = await meetingService.addParticipant(meetingId, payload)
      if (!res.success) throw new Error(res.message ?? 'Katılımcı eklenemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  async function updateParticipantAttendance(meetingId: string, participantId: string, payload: UpdateAttendancePayload) {
    saving.value = true
    try {
      const res = await meetingService.updateParticipantAttendance(participantId, payload)
      if (!res.success) throw new Error(res.message ?? 'Katılım güncellenemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  async function removeParticipant(meetingId: string, participantId: string) {
    saving.value = true
    try {
      await meetingService.removeParticipant(participantId)
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  // ── Outcomes ───────────────────────────────────────────────────────────────

  async function addOutcome(meetingId: string, payload: AddOutcomePayload) {
    saving.value = true
    try {
      const res = await meetingService.addOutcome(meetingId, payload)
      if (!res.success) throw new Error(res.message ?? 'Sonuç kaydedilemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  async function updateOutcome(meetingId: string, outcomeId: string, payload: UpdateOutcomePayload) {
    saving.value = true
    try {
      const res = await meetingService.updateOutcome(outcomeId, payload)
      if (!res.success) throw new Error(res.message ?? 'Sonuç güncellenemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  // ── Follow-Ups ─────────────────────────────────────────────────────────────

  async function addFollowUp(meetingId: string, payload: AddFollowUpPayload) {
    saving.value = true
    try {
      const res = await meetingService.addFollowUp(meetingId, payload)
      if (!res.success) throw new Error(res.message ?? 'Takip görevi eklenemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  async function updateFollowUp(meetingId: string, followUpId: string, payload: UpdateFollowUpPayload) {
    saving.value = true
    try {
      const res = await meetingService.updateFollowUp(followUpId, payload)
      if (!res.success) throw new Error(res.message ?? 'Takip görevi güncellenemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  async function updateFollowUpStatus(meetingId: string, followUpId: string, payload: UpdateFollowUpStatusPayload) {
    saving.value = true
    try {
      const res = await meetingService.updateFollowUpStatus(followUpId, payload)
      if (!res.success) throw new Error(res.message ?? 'Durum güncellenemedi.')
      await fetchMeeting(meetingId)
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentMeeting.value = null
  }

  return {
    meetingList, currentMeeting,
    loading, saving, error,
    fetchMeetings, fetchMeeting,
    scheduleMeeting, updateMeeting, deleteMeeting,
    completeMeeting, cancelMeeting,
    addParticipant, updateParticipantAttendance, removeParticipant,
    addOutcome, updateOutcome,
    addFollowUp, updateFollowUp, updateFollowUpStatus,
    clearCurrent,
  }
})
