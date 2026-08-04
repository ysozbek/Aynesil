/**
 * Session store — CRUD, participants, educators, goals, notes.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { schedulingService } from '@/services/scheduling.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  SessionDto,
  SessionListItemDto,
  SessionListQuery,
  CreateSessionPayload,
  RescheduleSessionPayload,
  CompleteSessionPayload,
  CancelSessionPayload,
  AddParticipantPayload,
  AddEducatorPayload,
  UpdateSessionGoalPayload,
  CreateSessionNotePayload,
  UpdateSessionNotePayload,
} from '@/types/scheduling.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useSessionStore = defineStore('session', () => {
  const sessionList = ref<PaginatedResult<SessionListItemDto>>(emptyPage<SessionListItemDto>())
  const currentSession = ref<SessionDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchSessions(query: SessionListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.listSessions(query)
      if (res.success && res.data) sessionList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchSession(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await schedulingService.getSession(id)
      if (res.success && res.data) currentSession.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createSession(payload: CreateSessionPayload): Promise<SessionDto> {
    saving.value = true
    try {
      const res = await schedulingService.createSession(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Seans oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function rescheduleSession(id: string, payload: RescheduleSessionPayload): Promise<SessionDto> {
    saving.value = true
    try {
      const res = await schedulingService.rescheduleSession(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Seans yeniden planlanamadı.')
      currentSession.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function completeSession(id: string, payload: CompleteSessionPayload): Promise<SessionDto> {
    saving.value = true
    try {
      const res = await schedulingService.completeSession(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Seans tamamlanamadı.')
      currentSession.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function cancelSession(id: string, payload: CancelSessionPayload): Promise<SessionDto> {
    saving.value = true
    try {
      const res = await schedulingService.cancelSession(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Seans iptal edilemedi.')
      currentSession.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function noShowSession(id: string, rowVersion: string): Promise<SessionDto> {
    saving.value = true
    try {
      const res = await schedulingService.noShowSession(id, { rowVersion })
      if (!res.success || !res.data) throw new Error(res.message ?? 'Gelmedi işareti yapılamadı.')
      currentSession.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteSession(id: string) {
    saving.value = true
    try {
      await schedulingService.deleteSession(id)
      if (currentSession.value?.id === id) currentSession.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Participants ─────────────────────────────────────────────────────────────

  async function addParticipant(sessionId: string, payload: AddParticipantPayload) {
    saving.value = true
    try {
      await schedulingService.addParticipant(sessionId, payload)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  async function removeParticipant(sessionId: string, studentId: string) {
    saving.value = true
    try {
      await schedulingService.removeParticipant(sessionId, studentId)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  // ── Educators ────────────────────────────────────────────────────────────────

  async function addEducator(sessionId: string, payload: AddEducatorPayload) {
    saving.value = true
    try {
      await schedulingService.addEducator(sessionId, payload)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  async function removeEducator(sessionId: string, educatorId: string) {
    saving.value = true
    try {
      await schedulingService.removeEducator(sessionId, educatorId)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  // ── Goals ────────────────────────────────────────────────────────────────────

  async function updateSessionGoal(sessionId: string, studentGoalId: string, payload: UpdateSessionGoalPayload) {
    saving.value = true
    try {
      await schedulingService.updateSessionGoal(sessionId, studentGoalId, payload)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  async function removeSessionGoal(sessionId: string, studentGoalId: string) {
    saving.value = true
    try {
      await schedulingService.removeSessionGoal(sessionId, studentGoalId)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  // ── Notes ────────────────────────────────────────────────────────────────────

  async function addNote(sessionId: string, payload: CreateSessionNotePayload) {
    saving.value = true
    try {
      await schedulingService.addSessionNote(sessionId, payload)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  async function updateNote(sessionId: string, noteId: string, payload: UpdateSessionNotePayload) {
    saving.value = true
    try {
      await schedulingService.updateSessionNote(sessionId, noteId, payload)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  async function deleteNote(sessionId: string, noteId: string) {
    saving.value = true
    try {
      await schedulingService.deleteSessionNote(sessionId, noteId)
      if (currentSession.value?.id === sessionId) await fetchSession(sessionId)
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentSession.value = null
  }

  return {
    sessionList, currentSession, loading, saving, error,
    fetchSessions, fetchSession, createSession, rescheduleSession,
    completeSession, cancelSession, noShowSession, deleteSession,
    addParticipant, removeParticipant,
    addEducator, removeEducator,
    updateSessionGoal, removeSessionGoal,
    addNote, updateNote, deleteNote,
    clearCurrent,
  }
})
