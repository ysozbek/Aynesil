/**
 * Assessment Template store — list, detail, CRUD, versioning,
 * translations, sections, and items.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { assessmentService } from '@/services/assessment.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  AssessmentTemplateDto,
  AssessmentTemplateListItemDto,
  CreateTemplatePayload,
  UpdateTemplatePayload,
  SetTemplateActivePayload,
  UpsertTranslationPayload,
  AddSectionPayload,
  UpdateSectionPayload,
  AddItemPayload,
  UpdateItemPayload,
  TemplateListQuery,
} from '@/types/assessment.types'

const emptyPage = (): PaginatedResult<AssessmentTemplateListItemDto> => ({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
})

export const useAssessmentTemplateStore = defineStore('assessmentTemplate', () => {
  const list = ref<PaginatedResult<AssessmentTemplateListItemDto>>(emptyPage())
  const current = ref<AssessmentTemplateDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(query: TemplateListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await assessmentService.listTemplates(query)
      if (res.success && res.data) list.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await assessmentService.getTemplate(id)
      if (res.success && res.data) current.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateTemplatePayload): Promise<AssessmentTemplateDto> {
    saving.value = true
    try {
      const res = await assessmentService.createTemplate(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Şablon oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateTemplatePayload): Promise<AssessmentTemplateDto> {
    saving.value = true
    try {
      const res = await assessmentService.updateTemplate(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Güncelleme başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function setActive(id: string, payload: SetTemplateActivePayload): Promise<AssessmentTemplateDto> {
    saving.value = true
    try {
      const res = await assessmentService.setTemplateActive(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Durum değiştirilemedi.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function createVersion(id: string): Promise<AssessmentTemplateDto> {
    saving.value = true
    try {
      const res = await assessmentService.createTemplateVersion(id)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Yeni sürüm oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function upsertTranslation(id: string, locale: string, payload: UpsertTranslationPayload) {
    saving.value = true
    try {
      await assessmentService.upsertTranslation(id, locale, payload)
      await fetchOne(id)
    } finally {
      saving.value = false
    }
  }

  async function addSection(templateId: string, payload: AddSectionPayload) {
    saving.value = true
    try {
      const res = await assessmentService.addSection(templateId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Bölüm eklenemedi.')
      current.value = res.data
    } finally {
      saving.value = false
    }
  }

  async function updateSection(sectionId: string, payload: UpdateSectionPayload) {
    saving.value = true
    try {
      await assessmentService.updateSection(sectionId, payload)
      if (current.value) await fetchOne(current.value.id)
    } finally {
      saving.value = false
    }
  }

  async function deleteSection(sectionId: string) {
    saving.value = true
    try {
      await assessmentService.deleteSection(sectionId)
      if (current.value) await fetchOne(current.value.id)
    } finally {
      saving.value = false
    }
  }

  async function addItem(sectionId: string, payload: AddItemPayload) {
    saving.value = true
    try {
      await assessmentService.addItem(sectionId, payload)
      if (current.value) await fetchOne(current.value.id)
    } finally {
      saving.value = false
    }
  }

  async function updateItem(itemId: string, payload: UpdateItemPayload) {
    saving.value = true
    try {
      await assessmentService.updateItem(itemId, payload)
      if (current.value) await fetchOne(current.value.id)
    } finally {
      saving.value = false
    }
  }

  async function deleteItem(itemId: string) {
    saving.value = true
    try {
      await assessmentService.deleteItem(itemId)
      if (current.value) await fetchOne(current.value.id)
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    current.value = null
  }

  return {
    list,
    current,
    loading,
    saving,
    error,
    fetchList,
    fetchOne,
    create,
    update,
    setActive,
    createVersion,
    upsertTranslation,
    addSection,
    updateSection,
    deleteSection,
    addItem,
    updateItem,
    deleteItem,
    clearCurrent,
  }
})
