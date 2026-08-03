/**
 * Student store — list, detail, CRUD, status, campus enrollment,
 * guardian links, developmental profiles, diagnoses, emergency contacts.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { studentService } from '@/services/student.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  StudentDto,
  StudentListItemDto,
  StudentSummaryDto,
  StudentGuardianDto,
  StudentStatusHistoryDto,
  StudentCampusDto,
  DiagnosisDto,
  DevelopmentalProfileDto,
  EmergencyContactDto,
  StudentListQuery,
  CreateStudentPayload,
  UpdateStudentPayload,
  ChangeStudentStatusPayload,
  EnrollAtCampusPayload,
  TransferStudentPayload,
  LinkGuardianPayload,
  UpdateGuardianLinkPayload,
  ReplaceEmergencyContactsPayload,
  UpsertDevProfilePayload,
  AddDiagnosisPayload,
  UpdateDiagnosisPayload,
} from '@/types/student.types'

const emptyPage = (): PaginatedResult<StudentListItemDto> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useStudentStore = defineStore('student', () => {
  const list = ref<PaginatedResult<StudentListItemDto>>(emptyPage())
  const current = ref<StudentDto | null>(null)
  const summary = ref<StudentSummaryDto | null>(null)
  const statusHistory = ref<StudentStatusHistoryDto[]>([])
  const campuses = ref<StudentCampusDto[]>([])
  const diagnoses = ref<DiagnosisDto[]>([])
  const developmentalProfiles = ref<DevelopmentalProfileDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchList(query: StudentListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await studentService.listStudents(query)
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
      const res = await studentService.getStudent(id)
      if (res.success && res.data) {
        current.value = res.data
        diagnoses.value = res.data.diagnoses
        developmentalProfiles.value = res.data.developmentalProfiles
        campuses.value = res.data.campuses
      }
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchSummary(id: string) {
    loading.value = true
    try {
      const res = await studentService.getSummary(id)
      if (res.success && res.data) summary.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchStatusHistory(id: string) {
    loading.value = true
    try {
      const res = await studentService.getStatusHistory(id)
      if (res.success && res.data) statusHistory.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateStudentPayload): Promise<StudentDto> {
    saving.value = true
    try {
      const res = await studentService.createStudent(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Öğrenci oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateStudentPayload): Promise<StudentDto> {
    saving.value = true
    try {
      const res = await studentService.updateStudent(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Güncelleme başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    saving.value = true
    try {
      await studentService.deleteStudent(id)
      if (current.value?.id === id) current.value = null
    } finally {
      saving.value = false
    }
  }

  async function changeStatus(id: string, payload: ChangeStudentStatusPayload): Promise<StudentDto> {
    saving.value = true
    try {
      const res = await studentService.changeStatus(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Durum değiştirilemedi.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function enrollAtCampus(id: string, payload: EnrollAtCampusPayload): Promise<StudentCampusDto> {
    saving.value = true
    try {
      const res = await studentService.enrollAtCampus(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kampüs kaydı başarısız.')
      campuses.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function transferStudent(id: string, payload: TransferStudentPayload): Promise<StudentDto> {
    saving.value = true
    try {
      const res = await studentService.transferStudent(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Transfer başarısız.')
      current.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function linkGuardian(id: string, payload: LinkGuardianPayload): Promise<StudentGuardianDto> {
    saving.value = true
    try {
      const res = await studentService.linkGuardian(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Veli bağlanamadı.')
      if (current.value) current.value.guardians.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateGuardianLink(id: string, linkId: string, payload: UpdateGuardianLinkPayload): Promise<StudentGuardianDto> {
    saving.value = true
    try {
      const res = await studentService.updateGuardianLink(id, linkId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Veli bağlantısı güncellenemedi.')
      if (current.value) {
        const idx = current.value.guardians.findIndex(g => g.linkId === linkId)
        if (idx >= 0) current.value.guardians[idx] = res.data
      }
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function unlinkGuardian(id: string, linkId: string) {
    saving.value = true
    try {
      await studentService.unlinkGuardian(id, linkId)
      if (current.value) {
        current.value = {
          ...current.value,
          guardians: current.value.guardians.filter(g => g.linkId !== linkId),
        }
      }
    } finally {
      saving.value = false
    }
  }

  async function replaceEmergencyContacts(id: string, payload: ReplaceEmergencyContactsPayload): Promise<EmergencyContactDto[]> {
    saving.value = true
    try {
      const res = await studentService.replaceEmergencyContacts(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Acil kişiler güncellenemedi.')
      if (current.value) current.value.emergencyContacts = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function upsertDevelopmentalProfile(id: string, payload: UpsertDevProfilePayload) {
    saving.value = true
    try {
      const res = await studentService.upsertDevelopmentalProfile(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Gelişim profili güncellenemedi.')
      const idx = developmentalProfiles.value.findIndex(p => p.id === res.data!.id)
      if (idx >= 0) developmentalProfiles.value[idx] = res.data
      else developmentalProfiles.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function addDiagnosis(id: string, payload: AddDiagnosisPayload) {
    saving.value = true
    try {
      const res = await studentService.addDiagnosis(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Tanı eklenemedi.')
      diagnoses.value.push(res.data)
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateDiagnosis(id: string, diagnosisId: string, payload: UpdateDiagnosisPayload) {
    saving.value = true
    try {
      const res = await studentService.updateDiagnosis(id, diagnosisId, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Tanı güncellenemedi.')
      const idx = diagnoses.value.findIndex(d => d.id === diagnosisId)
      if (idx >= 0) diagnoses.value[idx] = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function deleteDiagnosis(id: string, diagnosisId: string) {
    saving.value = true
    try {
      await studentService.deleteDiagnosis(id, diagnosisId)
      diagnoses.value = diagnoses.value.filter(d => d.id !== diagnosisId)
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    current.value = null
    summary.value = null
    statusHistory.value = []
    campuses.value = []
    diagnoses.value = []
    developmentalProfiles.value = []
  }

  return {
    list, current, summary, statusHistory, campuses,
    diagnoses, developmentalProfiles, loading, saving, error,
    fetchList, fetchOne, fetchSummary, fetchStatusHistory,
    create, update, remove, changeStatus, enrollAtCampus,
    transferStudent, linkGuardian, updateGuardianLink, unlinkGuardian,
    replaceEmergencyContacts, upsertDevelopmentalProfile,
    addDiagnosis, updateDiagnosis, deleteDiagnosis,
    clearCurrent,
  }
})
