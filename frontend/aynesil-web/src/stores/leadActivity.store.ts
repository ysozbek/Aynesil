/**
 * Lead Activity store — activity timeline, follow-ups, interviews.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { leadService } from '@/services/lead.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  LeadActivityDto,
  LeadStatusHistoryDto,
  InterviewDto,
  LogActivityPayload,
  ScheduleInterviewPayload,
  CompleteInterviewPayload,
  RescheduleInterviewPayload,
  FollowUpsQuery,
} from '@/types/crm.types'

const emptyPage = (): PaginatedResult<LeadActivityDto> => ({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
})

export const useLeadActivityStore = defineStore('leadActivity', () => {
  const activities = ref<PaginatedResult<LeadActivityDto>>(emptyPage())
  const history = ref<LeadStatusHistoryDto[]>([])
  const interviews = ref<InterviewDto[]>([])
  const followUps = ref<PaginatedResult<LeadActivityDto>>(emptyPage())
  const loading = ref(false)
  const saving = ref(false)

  async function fetchActivities(leadId: string, page = 1, pageSize = 20) {
    loading.value = true
    try {
      const res = await leadService.getActivities(leadId, page, pageSize)
      if (res.success && res.data) activities.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchHistory(leadId: string) {
    loading.value = true
    try {
      const res = await leadService.getStatusHistory(leadId)
      if (res.success && res.data) history.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchInterviews(leadId: string) {
    loading.value = true
    try {
      const res = await leadService.getInterviews(leadId)
      if (res.success && res.data) interviews.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchFollowUps(query: FollowUpsQuery) {
    loading.value = true
    try {
      const res = await leadService.getFollowUpsDue(query)
      if (res.success && res.data) followUps.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function logActivity(leadId: string, payload: LogActivityPayload): Promise<LeadActivityDto> {
    saving.value = true
    try {
      const res = await leadService.logActivity(leadId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Aktivite kaydedilemedi.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function scheduleInterview(leadId: string, payload: ScheduleInterviewPayload): Promise<InterviewDto> {
    saving.value = true
    try {
      const res = await leadService.scheduleInterview(leadId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Görüşme planlanamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function completeInterview(id: string, payload: CompleteInterviewPayload): Promise<InterviewDto> {
    saving.value = true
    try {
      const res = await leadService.completeInterview(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Görüşme tamamlanamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function cancelInterview(id: string, rowVersion: number) {
    saving.value = true
    try {
      await leadService.cancelInterview(id, rowVersion)
    } finally {
      saving.value = false
    }
  }

  async function rescheduleInterview(id: string, payload: RescheduleInterviewPayload): Promise<InterviewDto> {
    saving.value = true
    try {
      const res = await leadService.rescheduleInterview(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Görüşme yeniden planlanamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function markNoShow(id: string, rowVersion: number) {
    saving.value = true
    try {
      await leadService.markInterviewNoShow(id, rowVersion)
    } finally {
      saving.value = false
    }
  }

  function clearActivities() {
    activities.value = emptyPage()
    history.value = []
    interviews.value = []
  }

  return {
    activities,
    history,
    interviews,
    followUps,
    loading,
    saving,
    fetchActivities,
    fetchHistory,
    fetchInterviews,
    fetchFollowUps,
    logActivity,
    scheduleInterview,
    completeInterview,
    cancelInterview,
    rescheduleInterview,
    markNoShow,
    clearActivities,
  }
})
