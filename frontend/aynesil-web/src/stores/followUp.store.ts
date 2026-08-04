/**
 * Follow-Up store — cross-meeting follow-up tracking.
 * Provides a filtered view of follow-ups sourced from meetings.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { meetingService } from '@/services/meeting.service'
import type { MeetingFollowUpDto, UpdateFollowUpStatusPayload } from '@/types/meeting.types'

export const useFollowUpStore = defineStore('followUp', () => {
  // All follow-ups collected across meetings
  const allFollowUps = ref<(MeetingFollowUpDto & { meetingTitle?: string })[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // Computed filtered views
  const pendingFollowUps = computed(() =>
    allFollowUps.value.filter(f => f.status === 'pending' || f.status === 'in_progress')
  )
  const overdueFollowUps = computed(() => {
    const today = new Date().toISOString().split('T')[0]
    return allFollowUps.value.filter(f =>
      f.dueDate && f.dueDate < today && f.status !== 'completed' && f.status !== 'cancelled'
    )
  })

  function setFollowUps(items: (MeetingFollowUpDto & { meetingTitle?: string })[]) {
    allFollowUps.value = items
  }

  async function updateStatus(meetingId: string, followUpId: string, payload: UpdateFollowUpStatusPayload) {
    saving.value = true
    try {
      const res = await meetingService.updateFollowUpStatus(followUpId, payload)
      if (!res.success) throw new Error(res.message ?? 'Durum güncellenemedi.')
      const item = allFollowUps.value.find(f => f.id === followUpId)
      if (item) item.status = payload.status
    } finally {
      saving.value = false
    }
  }

  return {
    allFollowUps, pendingFollowUps, overdueFollowUps,
    loading, saving, error,
    setFollowUps, updateStatus,
  }
})
