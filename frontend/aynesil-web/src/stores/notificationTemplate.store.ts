/**
 * Notification Template store — admin CRUD for templates and trigger configs.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { notificationService } from '@/services/notification.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  NotificationTemplateDto,
  NotificationTemplateListItemDto,
  NotificationTemplateListQuery,
  CreateNotificationTemplatePayload,
  UpdateNotificationTemplatePayload,
  NotificationTriggerConfigListItemDto,
  UpsertTriggerConfigPayload,
} from '@/types/notification.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useNotificationTemplateStore = defineStore('notificationTemplate', () => {
  // ── Templates ──────────────────────────────────────────────────────────────
  const templateList = ref<PaginatedResult<NotificationTemplateListItemDto>>(emptyPage<NotificationTemplateListItemDto>())
  const currentTemplate = ref<NotificationTemplateDto | null>(null)

  // ── Trigger Configs ────────────────────────────────────────────────────────
  const triggerList = ref<PaginatedResult<NotificationTriggerConfigListItemDto>>(emptyPage<NotificationTriggerConfigListItemDto>())

  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Template Actions ───────────────────────────────────────────────────────

  async function fetchTemplates(query: NotificationTemplateListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await notificationService.listTemplates(query)
      if (res.success && res.data) templateList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchTemplate(id: string) {
    loading.value = true
    try {
      const res = await notificationService.getTemplate(id)
      if (res.success && res.data) currentTemplate.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createTemplate(payload: CreateNotificationTemplatePayload): Promise<NotificationTemplateDto> {
    saving.value = true
    try {
      const res = await notificationService.createTemplate(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateTemplate(id: string, payload: UpdateNotificationTemplatePayload): Promise<NotificationTemplateDto> {
    saving.value = true
    try {
      const res = await notificationService.updateTemplate(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon güncellenemedi.')
      currentTemplate.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteTemplate(id: string) {
    saving.value = true
    try {
      const res = await notificationService.deleteTemplate(id)
      if (!res.success) throw new Error(res.message ?? 'Şablon silinemedi.')
      if (currentTemplate.value?.id === id) currentTemplate.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Trigger Config Actions ─────────────────────────────────────────────────

  async function fetchTriggers(corporationId?: string) {
    loading.value = true
    error.value = null
    try {
      const res = await notificationService.listTriggers(corporationId)
      if (res.success && res.data) triggerList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function upsertTrigger(payload: UpsertTriggerConfigPayload) {
    saving.value = true
    try {
      const res = await notificationService.upsertTrigger(payload)
      if (!res.success) throw new Error(res.message ?? 'Tetikleyici kaydedilemedi.')
    } finally {
      saving.value = false
    }
  }

  async function deleteTrigger(id: string) {
    saving.value = true
    try {
      const res = await notificationService.deleteTrigger(id)
      if (!res.success) throw new Error(res.message ?? 'Tetikleyici silinemedi.')
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentTemplate.value = null
  }

  return {
    templateList, currentTemplate,
    triggerList,
    loading, saving, error,
    fetchTemplates, fetchTemplate, createTemplate, updateTemplate, deleteTemplate,
    fetchTriggers, upsertTrigger, deleteTrigger,
    clearCurrent,
  }
})
