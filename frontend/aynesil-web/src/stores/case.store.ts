/**
 * Case Management store — case notes, medical reports, development reports,
 * external institution reports. All scoped to a specific student.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { studentService } from '@/services/student.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CaseNoteDto,
  MedicalReportDto,
  DevelopmentReportDto,
  ExternalInstitutionReportDto,
  CaseNoteListQuery,
  AddCaseNotePayload,
  UpdateCaseNotePayload,
  AddMedicalReportPayload,
  UpdateMedicalReportPayload,
  AddDevelopmentReportPayload,
  UpdateDevelopmentReportPayload,
  AddExternalReportPayload,
} from '@/types/student.types'

const emptyNotesPage = (): PaginatedResult<CaseNoteDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useCaseStore = defineStore('case', () => {
  const caseNotes = ref<PaginatedResult<CaseNoteDto>>(emptyNotesPage())
  const medicalReports = ref<MedicalReportDto[]>([])
  const developmentReports = ref<DevelopmentReportDto[]>([])
  const externalReports = ref<ExternalInstitutionReportDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchCaseNotes(query: CaseNoteListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await studentService.getCaseNotes(query)
      if (res.success && res.data) caseNotes.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchMedicalReports(studentId: string) {
    loading.value = true
    try {
      const res = await studentService.getMedicalReports(studentId)
      if (res.success && res.data) medicalReports.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchDevelopmentReports(studentId: string) {
    loading.value = true
    try {
      const res = await studentService.getDevelopmentReports(studentId)
      if (res.success && res.data) developmentReports.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchExternalReports(studentId: string) {
    loading.value = true
    try {
      const res = await studentService.getExternalReports(studentId)
      if (res.success && res.data) externalReports.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function addCaseNote(studentId: string, payload: AddCaseNotePayload): Promise<CaseNoteDto> {
    saving.value = true
    try {
      const res = await studentService.addCaseNote(studentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Not eklenemedi.')
      caseNotes.value.items.unshift(res.data)
      caseNotes.value.totalCount++
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateCaseNote(studentId: string, noteId: string, payload: UpdateCaseNotePayload) {
    saving.value = true
    try {
      const res = await studentService.updateCaseNote(studentId, noteId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Not güncellenemedi.')
      const idx = caseNotes.value.items.findIndex(n => n.id === noteId)
      if (idx >= 0) caseNotes.value.items[idx] = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteCaseNote(studentId: string, noteId: string) {
    saving.value = true
    try {
      await studentService.deleteCaseNote(studentId, noteId)
      caseNotes.value.items = caseNotes.value.items.filter(n => n.id !== noteId)
      caseNotes.value.totalCount--
    } finally {
      saving.value = false
    }
  }

  async function addMedicalReport(studentId: string, payload: AddMedicalReportPayload): Promise<MedicalReportDto> {
    saving.value = true
    try {
      const res = await studentService.addMedicalReport(studentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Tıbbi rapor eklenemedi.')
      medicalReports.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateMedicalReport(studentId: string, reportId: string, payload: UpdateMedicalReportPayload) {
    saving.value = true
    try {
      const res = await studentService.updateMedicalReport(studentId, reportId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rapor güncellenemedi.')
      const idx = medicalReports.value.findIndex(r => r.id === reportId)
      if (idx >= 0) medicalReports.value[idx] = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteMedicalReport(studentId: string, reportId: string) {
    saving.value = true
    try {
      await studentService.deleteMedicalReport(studentId, reportId)
      medicalReports.value = medicalReports.value.filter(r => r.id !== reportId)
    } finally {
      saving.value = false
    }
  }

  async function addDevelopmentReport(studentId: string, payload: AddDevelopmentReportPayload): Promise<DevelopmentReportDto> {
    saving.value = true
    try {
      const res = await studentService.addDevelopmentReport(studentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Gelişim raporu eklenemedi.')
      developmentReports.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteDevelopmentReport(studentId: string, reportId: string) {
    saving.value = true
    try {
      await studentService.deleteDevelopmentReport(studentId, reportId)
      developmentReports.value = developmentReports.value.filter(r => r.id !== reportId)
    } finally {
      saving.value = false
    }
  }

  async function addExternalReport(studentId: string, payload: AddExternalReportPayload): Promise<ExternalInstitutionReportDto> {
    saving.value = true
    try {
      const res = await studentService.addExternalReport(studentId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kurum raporu eklenemedi.')
      externalReports.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteExternalReport(studentId: string, reportId: string) {
    saving.value = true
    try {
      await studentService.deleteExternalReport(studentId, reportId)
      externalReports.value = externalReports.value.filter(r => r.id !== reportId)
    } finally {
      saving.value = false
    }
  }

  function clearAll() {
    caseNotes.value = emptyNotesPage()
    medicalReports.value = []
    developmentReports.value = []
    externalReports.value = []
  }

  return {
    caseNotes, medicalReports, developmentReports, externalReports,
    loading, saving, error,
    fetchCaseNotes, fetchMedicalReports, fetchDevelopmentReports, fetchExternalReports,
    addCaseNote, updateCaseNote, deleteCaseNote,
    addMedicalReport, updateMedicalReport, deleteMedicalReport,
    addDevelopmentReport, deleteDevelopmentReport,
    addExternalReport, deleteExternalReport,
    clearAll,
  }
})
