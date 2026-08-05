<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useStudentStore } from '@/stores/student.store'
import { useCaseStore } from '@/stores/case.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import { useBranchStore } from '@/stores/branch.store'
import Pagination from '@/components/shared/Pagination.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type {
  StudentGuardianDto,
  DiagnosisDto,
  DevelopmentalProfileDto,
  CaseNoteDto,
  MedicalReportDto,
  EmergencyContactInput,
} from '@/types/student.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useStudentStore()
const caseStore = useCaseStore()
const refData = useRefDataStore()
const branchStore = useBranchStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const student = computed(() => store.current)

type MainTab = 'overview' | 'guardians' | 'programs' | 'case-management' | 'history'
type CaseTab = 'case-notes' | 'medical-reports' | 'development-reports' | 'external-reports'

const activeTab = ref<MainTab>('overview')
const caseTab = ref<CaseTab>('case-notes')
const caseManagementLoaded = ref(false)

// ── Reference data ───────────────────────────────────────────────────────────
const statuses = ref<RefValueItem[]>([])
const relationships = ref<RefValueItem[]>([])
const diagnosisCategories = ref<RefValueItem[]>([])
const developmentAreas = ref<RefValueItem[]>([])
const institutionTypes = ref<RefValueItem[]>([])

// ── Status change ─────────────────────────────────────────────────────────────
const showStatusModal = ref(false)
const statusForm = reactive({ newStatusId: '', reason: '' })
const statusError = ref('')

function openStatusModal() {
  statusForm.newStatusId = student.value?.statusId ?? ''
  statusForm.reason = ''
  statusError.value = ''
  showStatusModal.value = true
}

async function submitStatus() {
  if (!statusForm.newStatusId) { statusError.value = t('validation.required', { field: t('student.newStatus') }); return }
  try {
    await store.changeStatus(id.value, {
      newStatusId: statusForm.newStatusId,
      reason: statusForm.reason.trim() || null,
      rowVersion: student.value!.rowVersion,
    })
    showStatusModal.value = false
    await store.fetchStatusHistory(id.value)
  } catch (e: unknown) {
    statusError.value = (e as Error).message
  }
}

// ── Guardian link ─────────────────────────────────────────────────────────────
const showLinkModal = ref(false)
const linkForm = reactive({ guardianId: '', relationshipId: '', isPrimary: false, hasCustody: false, portalAccess: false, financialResponsible: false })
const linkError = ref('')
const editLinkTarget = ref<StudentGuardianDto | null>(null)
const editLinkForm = reactive({ relationshipId: '', isPrimary: false, hasCustody: false, portalAccess: false, financialResponsible: false })
const editLinkError = ref('')
const unlinkTarget = ref<string | null>(null)
const unlinkLoading = ref(false)

function openLinkModal() {
  linkForm.guardianId = ''
  linkForm.relationshipId = ''
  linkForm.isPrimary = false
  linkForm.hasCustody = false
  linkForm.portalAccess = false
  linkForm.financialResponsible = false
  linkError.value = ''
  showLinkModal.value = true
}

async function submitLink() {
  if (!linkForm.guardianId.trim()) { linkError.value = t('validation.required', { field: t('student.guardian.guardianId') }); return }
  try {
    await store.linkGuardian(id.value, {
      guardianId: linkForm.guardianId.trim(),
      relationshipId: linkForm.relationshipId || null,
      isPrimary: linkForm.isPrimary,
      hasCustody: linkForm.hasCustody,
      portalAccess: linkForm.portalAccess,
      financialResponsible: linkForm.financialResponsible,
    })
    showLinkModal.value = false
  } catch (e: unknown) {
    linkError.value = (e as Error).message
  }
}

function openEditLink(g: StudentGuardianDto) {
  editLinkTarget.value = g
  editLinkForm.relationshipId = g.relationshipId ?? ''
  editLinkForm.isPrimary = g.isPrimary
  editLinkForm.hasCustody = g.hasCustody
  editLinkForm.portalAccess = g.portalAccess
  editLinkForm.financialResponsible = g.financialResponsible
  editLinkError.value = ''
}

async function submitEditLink() {
  if (!editLinkTarget.value) return
  try {
    await store.updateGuardianLink(id.value, editLinkTarget.value.linkId, {
      relationshipId: editLinkForm.relationshipId || null,
      isPrimary: editLinkForm.isPrimary,
      hasCustody: editLinkForm.hasCustody,
      portalAccess: editLinkForm.portalAccess,
      financialResponsible: editLinkForm.financialResponsible,
    })
    editLinkTarget.value = null
  } catch (e: unknown) {
    editLinkError.value = (e as Error).message
  }
}

async function doUnlink() {
  if (!unlinkTarget.value) return
  unlinkLoading.value = true
  try {
    await store.unlinkGuardian(id.value, unlinkTarget.value)
    unlinkTarget.value = null
  } finally {
    unlinkLoading.value = false
  }
}

// ── Case notes ────────────────────────────────────────────────────────────────
const noteQuery = reactive({ page: 1, pageSize: 10 })
const showNoteModal = ref(false)
const editNoteTarget = ref<CaseNoteDto | null>(null)
const noteForm = reactive({ noteType: '', body: '', isConfidential: false, rowVersion: 0 })
const noteError = ref('')
const deleteNoteTarget = ref<string | null>(null)
const deleteNoteLoading = ref(false)

function openAddNote() {
  editNoteTarget.value = null
  noteForm.noteType = ''
  noteForm.body = ''
  noteForm.isConfidential = false
  noteForm.rowVersion = 0
  noteError.value = ''
  showNoteModal.value = true
}

function openEditNote(n: CaseNoteDto) {
  editNoteTarget.value = n
  noteForm.noteType = n.noteType ?? ''
  noteForm.body = n.body
  noteForm.isConfidential = n.isConfidential
  noteForm.rowVersion = n.rowVersion
  noteError.value = ''
  showNoteModal.value = true
}

async function submitNote() {
  if (!noteForm.body.trim()) { noteError.value = t('validation.required', { field: t('student.caseNote.body') }); return }
  try {
    if (editNoteTarget.value) {
      await caseStore.updateCaseNote(id.value, editNoteTarget.value.id, {
        noteType: noteForm.noteType.trim() || null,
        body: noteForm.body.trim(),
        isConfidential: noteForm.isConfidential,
        rowVersion: noteForm.rowVersion,
      })
    } else {
      await caseStore.addCaseNote(id.value, {
        noteType: noteForm.noteType.trim() || null,
        body: noteForm.body.trim(),
        isConfidential: noteForm.isConfidential,
        authoredBy: null,
      })
    }
    showNoteModal.value = false
  } catch (e: unknown) {
    noteError.value = (e as Error).message
  }
}

async function doDeleteNote() {
  if (!deleteNoteTarget.value) return
  deleteNoteLoading.value = true
  try {
    await caseStore.deleteCaseNote(id.value, deleteNoteTarget.value)
    deleteNoteTarget.value = null
  } finally {
    deleteNoteLoading.value = false
  }
}

watch(() => noteQuery.page, () => {
  caseStore.fetchCaseNotes({ studentId: id.value, page: noteQuery.page, pageSize: noteQuery.pageSize })
})

// ── Medical reports ───────────────────────────────────────────────────────────
const showMedicalModal = ref(false)
const editMedicalTarget = ref<MedicalReportDto | null>(null)
const medicalForm = reactive({ title: '', reportDate: '', issuer: '', summary: '', rowVersion: 0 })
const medicalError = ref('')
const deleteMedicalTarget = ref<string | null>(null)
const deleteMedicalLoading = ref(false)

function openAddMedical() {
  editMedicalTarget.value = null
  medicalForm.title = ''
  medicalForm.reportDate = ''
  medicalForm.issuer = ''
  medicalForm.summary = ''
  medicalForm.rowVersion = 0
  medicalError.value = ''
  showMedicalModal.value = true
}

function openEditMedical(r: MedicalReportDto) {
  editMedicalTarget.value = r
  medicalForm.title = r.title
  medicalForm.reportDate = r.reportDate ?? ''
  medicalForm.issuer = r.issuer ?? ''
  medicalForm.summary = r.summary ?? ''
  medicalForm.rowVersion = r.rowVersion
  medicalError.value = ''
  showMedicalModal.value = true
}

async function submitMedical() {
  if (!medicalForm.title.trim()) { medicalError.value = t('validation.required', { field: t('student.medicalReport.titleField') }); return }
  try {
    if (editMedicalTarget.value) {
      await caseStore.updateMedicalReport(id.value, editMedicalTarget.value.id, {
        title: medicalForm.title.trim(),
        reportDate: medicalForm.reportDate || null,
        issuer: medicalForm.issuer.trim() || null,
        summary: medicalForm.summary.trim() || null,
        fileId: null,
        rowVersion: medicalForm.rowVersion,
      })
    } else {
      await caseStore.addMedicalReport(id.value, {
        title: medicalForm.title.trim(),
        reportDate: medicalForm.reportDate || null,
        issuer: medicalForm.issuer.trim() || null,
        summary: medicalForm.summary.trim() || null,
        fileId: null,
      })
    }
    showMedicalModal.value = false
  } catch (e: unknown) {
    medicalError.value = (e as Error).message
  }
}

async function doDeleteMedical() {
  if (!deleteMedicalTarget.value) return
  deleteMedicalLoading.value = true
  try {
    await caseStore.deleteMedicalReport(id.value, deleteMedicalTarget.value)
    deleteMedicalTarget.value = null
  } finally {
    deleteMedicalLoading.value = false
  }
}

// ── Development reports ───────────────────────────────────────────────────────
const showDevReportModal = ref(false)
const devReportForm = reactive({ periodLabel: '', reportDate: '', authoredBy: '', content: '' })
const devReportError = ref('')
const deleteDevReportTarget = ref<string | null>(null)
const deleteDevReportLoading = ref(false)

function openAddDevReport() {
  devReportForm.periodLabel = ''
  devReportForm.reportDate = ''
  devReportForm.authoredBy = ''
  devReportForm.content = ''
  devReportError.value = ''
  showDevReportModal.value = true
}

async function submitDevReport() {
  try {
    await caseStore.addDevelopmentReport(id.value, {
      periodLabel: devReportForm.periodLabel.trim() || null,
      reportDate: devReportForm.reportDate || null,
      authoredBy: devReportForm.authoredBy.trim() || null,
      content: devReportForm.content.trim() || null,
      fileId: null,
    })
    showDevReportModal.value = false
  } catch (e: unknown) {
    devReportError.value = (e as Error).message
  }
}

async function doDeleteDevReport() {
  if (!deleteDevReportTarget.value) return
  deleteDevReportLoading.value = true
  try {
    await caseStore.deleteDevelopmentReport(id.value, deleteDevReportTarget.value)
    deleteDevReportTarget.value = null
  } finally {
    deleteDevReportLoading.value = false
  }
}

// ── External reports ──────────────────────────────────────────────────────────
const showExtReportModal = ref(false)
const extReportForm = reactive({ institutionName: '', institutionTypeId: '', reportDate: '', summary: '' })
const extReportError = ref('')
const deleteExtReportTarget = ref<string | null>(null)
const deleteExtReportLoading = ref(false)

function openAddExtReport() {
  extReportForm.institutionName = ''
  extReportForm.institutionTypeId = ''
  extReportForm.reportDate = ''
  extReportForm.summary = ''
  extReportError.value = ''
  showExtReportModal.value = true
}

async function submitExtReport() {
  if (!extReportForm.institutionName.trim()) { extReportError.value = t('validation.required', { field: t('student.externalReport.institutionName') }); return }
  try {
    await caseStore.addExternalReport(id.value, {
      institutionName: extReportForm.institutionName.trim(),
      institutionTypeId: extReportForm.institutionTypeId || null,
      reportDate: extReportForm.reportDate || null,
      summary: extReportForm.summary.trim() || null,
      fileId: null,
    })
    showExtReportModal.value = false
  } catch (e: unknown) {
    extReportError.value = (e as Error).message
  }
}

async function doDeleteExtReport() {
  if (!deleteExtReportTarget.value) return
  deleteExtReportLoading.value = true
  try {
    await caseStore.deleteExternalReport(id.value, deleteExtReportTarget.value)
    deleteExtReportTarget.value = null
  } finally {
    deleteExtReportLoading.value = false
  }
}

// ── Diagnoses ─────────────────────────────────────────────────────────────────
const showDiagnosisModal = ref(false)
const editDiagnosisTarget = ref<DiagnosisDto | null>(null)
const diagnosisForm = reactive({ categoryId: '', icdCode: '', description: '', diagnosedOn: '', diagnosedBy: '', rowVersion: 0 })
const diagnosisError = ref('')
const deleteDiagnosisTarget = ref<string | null>(null)
const deleteDiagnosisLoading = ref(false)

function openAddDiagnosis() {
  editDiagnosisTarget.value = null
  diagnosisForm.categoryId = ''
  diagnosisForm.icdCode = ''
  diagnosisForm.description = ''
  diagnosisForm.diagnosedOn = ''
  diagnosisForm.diagnosedBy = ''
  diagnosisForm.rowVersion = 0
  diagnosisError.value = ''
  showDiagnosisModal.value = true
}

function openEditDiagnosis(d: DiagnosisDto) {
  editDiagnosisTarget.value = d
  diagnosisForm.categoryId = d.categoryId ?? ''
  diagnosisForm.icdCode = d.icdCode ?? ''
  diagnosisForm.description = d.description ?? ''
  diagnosisForm.diagnosedOn = d.diagnosedOn ?? ''
  diagnosisForm.diagnosedBy = d.diagnosedBy ?? ''
  diagnosisForm.rowVersion = d.rowVersion
  diagnosisError.value = ''
  showDiagnosisModal.value = true
}

async function submitDiagnosis() {
  try {
    if (editDiagnosisTarget.value) {
      await store.updateDiagnosis(id.value, editDiagnosisTarget.value.id, {
        categoryId: diagnosisForm.categoryId || null,
        icdCode: diagnosisForm.icdCode.trim() || null,
        description: diagnosisForm.description.trim() || null,
        diagnosedOn: diagnosisForm.diagnosedOn || null,
        diagnosedBy: diagnosisForm.diagnosedBy.trim() || null,
        sourceFileId: null,
        rowVersion: diagnosisForm.rowVersion,
      })
    } else {
      await store.addDiagnosis(id.value, {
        categoryId: diagnosisForm.categoryId || null,
        icdCode: diagnosisForm.icdCode.trim() || null,
        description: diagnosisForm.description.trim() || null,
        diagnosedOn: diagnosisForm.diagnosedOn || null,
        diagnosedBy: diagnosisForm.diagnosedBy.trim() || null,
        sourceFileId: null,
      })
    }
    showDiagnosisModal.value = false
  } catch (e: unknown) {
    diagnosisError.value = (e as Error).message
  }
}

async function doDeleteDiagnosis() {
  if (!deleteDiagnosisTarget.value) return
  deleteDiagnosisLoading.value = true
  try {
    await store.deleteDiagnosis(id.value, deleteDiagnosisTarget.value)
    deleteDiagnosisTarget.value = null
  } finally {
    deleteDiagnosisLoading.value = false
  }
}

// ── Developmental profiles ────────────────────────────────────────────────────
const showProfileModal = ref(false)
const profileTarget = ref<DevelopmentalProfileDto | null>(null)
const profileForm = reactive({ developmentAreaId: '', summary: '', strengths: '', needs: '', assessedOn: '' })
const profileError = ref('')

function openUpsertProfile(profile?: DevelopmentalProfileDto) {
  profileTarget.value = profile ?? null
  profileForm.developmentAreaId = profile?.developmentAreaId ?? ''
  profileForm.summary = profile?.summary ?? ''
  profileForm.strengths = profile?.strengths ?? ''
  profileForm.needs = profile?.needs ?? ''
  profileForm.assessedOn = profile?.assessedOn ?? ''
  profileError.value = ''
  showProfileModal.value = true
}

async function submitProfile() {
  try {
    await store.upsertDevelopmentalProfile(id.value, {
      developmentAreaId: profileForm.developmentAreaId || null,
      summary: profileForm.summary.trim() || null,
      strengths: profileForm.strengths.trim() || null,
      needs: profileForm.needs.trim() || null,
      assessedOn: profileForm.assessedOn || null,
    })
    showProfileModal.value = false
  } catch (e: unknown) {
    profileError.value = (e as Error).message
  }
}

// ── Campus enrollment ─────────────────────────────────────────────────────────
const showEnrollModal = ref(false)
const enrollForm = reactive({ campusId: '', isPrimary: false, activeFrom: '' })
const enrollError = ref('')
const showTransferModal = ref(false)
const transferForm = reactive({ newCampusId: '', transferDate: '' })
const transferError = ref('')

const activeCampusIds = computed(() =>
  new Set(store.campuses.filter(c => !c.activeTo).map(c => c.campusId))
)

const enrollableCampuses = computed(() =>
  branchStore.list.items.filter(c => !activeCampusIds.value.has(c.id))
)

function apiErrorMessage(e: unknown, fallback: string) {
  const ax = e as { response?: { data?: { message?: string } }; message?: string }
  return ax.response?.data?.message || ax.message || fallback
}

function openEnrollModal() {
  enrollForm.campusId = ''
  enrollForm.isPrimary = false
  enrollForm.activeFrom = new Date().toISOString().split('T')[0]
  enrollError.value = ''
  showEnrollModal.value = true
}

async function submitEnroll() {
  if (!enrollForm.campusId) { enrollError.value = t('validation.required', { field: t('student.primaryCampus') }); return }
  try {
    await store.enrollAtCampus(id.value, {
      campusId: enrollForm.campusId,
      isPrimary: enrollForm.isPrimary,
      activeFrom: enrollForm.activeFrom || null,
    })
    showEnrollModal.value = false
  } catch (e: unknown) {
    enrollError.value = apiErrorMessage(e, t('student.campus.alreadyEnrolled'))
  }
}

function openTransferModal() {
  transferForm.newCampusId = ''
  transferForm.transferDate = new Date().toISOString().split('T')[0]
  transferError.value = ''
  showTransferModal.value = true
}

async function submitTransfer() {
  if (!transferForm.newCampusId) { transferError.value = t('validation.required', { field: t('student.primaryCampus') }); return }
  try {
    await store.transferStudent(id.value, {
      newCampusId: transferForm.newCampusId,
      transferDate: transferForm.transferDate || null,
      rowVersion: student.value!.rowVersion,
    })
    showTransferModal.value = false
  } catch (e: unknown) {
    transferError.value = (e as Error).message
  }
}

// ── Emergency contacts edit ────────────────────────────────────────────────────
const showContactsModal = ref(false)
const contactsForm = ref<EmergencyContactInput[]>([])
const contactsError = ref('')

function openContactsModal() {
  contactsForm.value = (student.value?.emergencyContacts ?? []).map(c => ({
    fullName: c.fullName,
    relationship: c.relationship,
    phone: c.phone,
    priority: c.priority,
  }))
  if (contactsForm.value.length === 0) addContact()
  contactsError.value = ''
  showContactsModal.value = true
}

function addContact() {
  contactsForm.value.push({ fullName: '', relationship: null, phone: '', priority: contactsForm.value.length + 1 })
}

function removeContact(i: number) {
  contactsForm.value.splice(i, 1)
  contactsForm.value.forEach((c, idx) => { c.priority = idx + 1 })
}

async function submitContacts() {
  try {
    await store.replaceEmergencyContacts(id.value, { contacts: contactsForm.value })
    showContactsModal.value = false
  } catch (e: unknown) {
    contactsError.value = (e as Error).message
  }
}

// ── Lifecycle ─────────────────────────────────────────────────────────────────
onMounted(async () => {
  await store.fetchOne(id.value)
  await Promise.all([
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('STUDENT_STATUS').then(v => { statuses.value = v }),
    refData.getValues('GUARDIAN_RELATIONSHIP').then(v => { relationships.value = v }),
    refData.getValues('DIAGNOSIS_CATEGORY').then(v => { diagnosisCategories.value = v }),
    refData.getValues('DEVELOPMENT_AREA').then(v => { developmentAreas.value = v }),
    refData.getValues('INSTITUTION_TYPE').then(v => { institutionTypes.value = v }),
  ])
})

onUnmounted(() => {
  store.clearCurrent()
  caseStore.clearAll()
})

watch(activeTab, async (tab) => {
  if (tab === 'case-management' && !caseManagementLoaded.value) {
    caseManagementLoaded.value = true
    await Promise.all([
      caseStore.fetchCaseNotes({ studentId: id.value, page: 1, pageSize: noteQuery.pageSize }),
      caseStore.fetchMedicalReports(id.value),
      caseStore.fetchDevelopmentReports(id.value),
      caseStore.fetchExternalReports(id.value),
    ])
  }
  if (tab === 'history') {
    await store.fetchStatusHistory(id.value)
  }
})

// ── Helpers ───────────────────────────────────────────────────────────────────
function formatDate(val: string | null | unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function formatDateTime(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="store.loading && !student" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Not found -->
    <div v-else-if="!student && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'students' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('student.backToList') }}
      </button>
    </div>

    <template v-else-if="student">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4 flex-wrap">
        <div>
          <button @click="router.push({ name: 'students' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('student.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ student.fullName }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span class="text-sm text-muted-foreground font-mono">{{ student.studentNo ?? '—' }}</span>
            <span v-if="student.statusLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
              {{ student.statusLabel }}
            </span>
            <span v-if="student.primaryCampusName" class="text-xs text-muted-foreground">{{ student.primaryCampusName }}</span>
          </div>
        </div>
        <div class="flex items-center gap-2 flex-wrap">
          <button
            v-if="can('student:change_status')"
            @click="openStatusModal"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('student.changeStatus') }}
          </button>
          <button
            v-if="can('student:update')"
            @click="router.push({ name: 'student-edit', params: { id: student.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            {{ t('common.edit') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6 overflow-x-auto">
          <button
            v-for="tab in (['overview', 'guardians', 'programs', 'case-management', 'history'] as MainTab[])"
            :key="tab"
            @click="activeTab = tab"
            :class="['pb-3 text-sm font-medium border-b-2 whitespace-nowrap transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ t(`student.tab.${tab === 'case-management' ? 'caseManagement' : tab}`) }}
          </button>
        </nav>
      </div>

      <!-- ── Overview Tab ── -->
      <div v-if="activeTab === 'overview'" class="space-y-6">

        <!-- Student info + emergency contacts -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

          <!-- Student info -->
          <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
            <h3 class="font-semibold text-foreground mb-3">{{ t('student.fullName') }}</h3>
            <dl class="space-y-2 text-sm">
              <div class="flex justify-between">
                <dt class="text-muted-foreground">{{ t('student.studentNo') }}</dt>
                <dd class="font-mono text-xs text-foreground">{{ student.studentNo ?? '—' }}</dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-muted-foreground">{{ t('student.nationalId') }}</dt>
                <dd class="text-foreground">{{ student.nationalId ?? '—' }}</dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-muted-foreground">{{ t('student.birthDate') }}</dt>
                <dd class="text-foreground">{{ formatDate(student.birthDate) }}</dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-muted-foreground">{{ t('student.gender') }}</dt>
                <dd class="text-foreground">{{ student.gender ?? '—' }}</dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-muted-foreground">{{ t('student.primaryCampus') }}</dt>
                <dd class="text-foreground">{{ student.primaryCampusName ?? '—' }}</dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-muted-foreground">{{ t('student.status') }}</dt>
                <dd>
                  <span v-if="student.statusLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                    {{ student.statusLabel }}
                  </span>
                  <span v-else class="text-foreground">—</span>
                </dd>
              </div>
              <div v-if="student.notes" class="pt-2 border-t border-border">
                <dt class="text-muted-foreground mb-1">{{ t('student.notes') }}</dt>
                <dd class="text-foreground text-xs leading-relaxed">{{ student.notes }}</dd>
              </div>
            </dl>
          </div>

          <!-- Emergency contacts -->
          <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
            <div class="flex items-center justify-between mb-3">
              <h3 class="font-semibold text-foreground">{{ t('student.emergencyContact.title') }}</h3>
              <button
                v-if="can('student:update')"
                @click="openContactsModal"
                class="text-xs text-primary hover:underline"
              >
                {{ t('common.edit') }}
              </button>
            </div>
            <div v-if="student.emergencyContacts.length === 0" class="text-sm text-muted-foreground py-4 text-center">
              {{ t('common.noData') }}
            </div>
            <ul v-else class="space-y-2">
              <li
                v-for="c in student.emergencyContacts"
                :key="c.id"
                class="flex items-center justify-between text-sm py-1 border-b border-border last:border-0"
              >
                <div>
                  <p class="font-medium text-foreground">{{ c.fullName }}</p>
                  <p class="text-xs text-muted-foreground">{{ c.relationship ?? '—' }} · {{ c.phone }}</p>
                </div>
                <span class="text-xs text-muted-foreground">{{ c.priority }}</span>
              </li>
            </ul>
          </div>
        </div>

        <!-- Diagnoses -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('student.diagnosis.title') }}</h3>
            <button
              v-if="can('student:write')"
              @click="openAddDiagnosis"
              class="flex items-center gap-1 text-xs text-primary hover:text-primary/80"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ t('student.diagnosis.add') }}
            </button>
          </div>
          <div v-if="store.diagnoses.length === 0" class="p-5 text-center text-sm text-muted-foreground">
            {{ t('common.noData') }}
          </div>
          <ul v-else class="divide-y divide-border">
            <li v-for="d in store.diagnoses" :key="d.id" class="flex items-start justify-between px-5 py-3 gap-4">
              <div class="text-sm">
                <p class="font-medium text-foreground">{{ d.description ?? d.icdCode ?? '—' }}</p>
                <p class="text-xs text-muted-foreground mt-0.5">
                  {{ d.categoryLabel ?? '—' }}
                  <span v-if="d.icdCode"> · {{ d.icdCode }}</span>
                  <span v-if="d.diagnosedOn"> · {{ formatDate(d.diagnosedOn) }}</span>
                  <span v-if="d.diagnosedBy"> · {{ d.diagnosedBy }}</span>
                </p>
              </div>
              <div v-if="can('student:write')" class="flex gap-1 shrink-0">
                <button @click="openEditDiagnosis(d)" class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button @click="deleteDiagnosisTarget = d.id" class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </li>
          </ul>
        </div>

        <!-- Developmental Profiles -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('student.developmentalProfile.title') }}</h3>
            <button
              v-if="can('student:write')"
              @click="openUpsertProfile()"
              class="flex items-center gap-1 text-xs text-primary hover:text-primary/80"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ t('common.add') }}
            </button>
          </div>
          <div v-if="store.developmentalProfiles.length === 0" class="p-5 text-center text-sm text-muted-foreground">
            {{ t('common.noData') }}
          </div>
          <div v-else class="p-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div
              v-for="profile in store.developmentalProfiles"
              :key="profile.id"
              class="rounded-lg border border-border p-4 space-y-2"
            >
              <div class="flex items-center justify-between">
                <p class="text-sm font-medium text-foreground">{{ profile.developmentAreaLabel ?? t('student.developmentalProfile.area') }}</p>
                <button v-if="can('student:write')" @click="openUpsertProfile(profile)" class="text-xs text-primary hover:underline">
                  {{ t('common.edit') }}
                </button>
              </div>
              <p v-if="profile.summary" class="text-xs text-foreground">{{ profile.summary }}</p>
              <div v-if="profile.strengths" class="text-xs">
                <span class="text-muted-foreground">{{ t('student.developmentalProfile.strengths') }}: </span>{{ profile.strengths }}
              </div>
              <div v-if="profile.needs" class="text-xs">
                <span class="text-muted-foreground">{{ t('student.developmentalProfile.needs') }}: </span>{{ profile.needs }}
              </div>
              <p v-if="profile.assessedOn" class="text-xs text-muted-foreground">{{ formatDate(profile.assessedOn) }}</p>
            </div>
          </div>
        </div>

        <!-- Campus Enrollments -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('student.campus.title') }}</h3>
            <div class="flex gap-2">
              <button
                v-if="can('student:update')"
                @click="openEnrollModal"
                class="text-xs px-2.5 py-1.5 rounded-lg border border-border hover:bg-accent transition-colors"
              >
                {{ t('student.campus.enroll') }}
              </button>
              <button
                v-if="can('student:update')"
                @click="openTransferModal"
                class="text-xs px-2.5 py-1.5 rounded-lg border border-border hover:bg-accent transition-colors"
              >
                {{ t('student.campus.transfer') }}
              </button>
            </div>
          </div>
          <div v-if="store.campuses.length === 0" class="p-5 text-center text-sm text-muted-foreground">
            {{ t('common.noData') }}
          </div>
          <ul v-else class="divide-y divide-border">
            <li v-for="c in store.campuses" :key="c.id" class="flex items-center justify-between px-5 py-3 text-sm">
              <div>
                <p class="font-medium text-foreground">{{ c.campusName ?? '—' }}</p>
                <p class="text-xs text-muted-foreground">
                  {{ formatDate(c.activeFrom) }}
                  <span v-if="c.activeTo"> – {{ formatDate(c.activeTo) }}</span>
                  <span v-if="c.isPrimary" class="ml-2 text-primary">●</span>
                </p>
              </div>
              <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', c.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-700']">
                {{ c.isActive ? t('common.active') : t('common.passive') }}
              </span>
            </li>
          </ul>
        </div>
      </div>

      <!-- ── Guardians Tab ── -->
      <div v-else-if="activeTab === 'guardians'">
        <div class="flex items-center justify-between mb-4">
          <p class="text-sm text-muted-foreground">{{ t('student.guardian.link') }}</p>
          <button
            v-if="can('guardian:create')"
            @click="openLinkModal"
            class="flex items-center gap-1.5 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('student.guardian.link') }}
          </button>
        </div>

        <div v-if="!student.guardians.length" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="g in student.guardians"
            :key="g.linkId"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between gap-4">
              <div>
                <p class="font-medium text-foreground">{{ g.guardianFullName }}</p>
                <p class="text-xs text-muted-foreground mt-0.5">
                  {{ g.relationshipLabel ?? '—' }}
                  <span v-if="g.guardianPhone"> · {{ g.guardianPhone }}</span>
                  <span v-if="g.guardianEmail"> · {{ g.guardianEmail }}</span>
                </p>
                <div class="flex gap-3 mt-2 text-xs">
                  <span v-if="g.isPrimary" class="text-emerald-600 font-medium">{{ t('student.guardian.isPrimary') }}</span>
                  <span v-if="g.hasCustody" class="text-blue-600">{{ t('student.guardian.hasCustody') }}</span>
                  <span v-if="g.portalAccess" class="text-indigo-600">{{ t('student.guardian.portalAccess') }}</span>
                  <span v-if="g.financialResponsible" class="text-amber-600">{{ t('student.guardian.financialResponsible') }}</span>
                </div>
              </div>
              <div class="flex gap-1 shrink-0">
                <button
                  v-if="can('guardian:update')"
                  @click="openEditLink(g)"
                  class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                  :title="t('student.guardian.edit')"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button
                  v-if="can('guardian:update')"
                  @click="unlinkTarget = g.linkId"
                  class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors"
                  :title="t('student.guardian.unlink')"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Programs Tab ── -->
      <div v-else-if="activeTab === 'programs'" class="text-center py-16">
        <svg class="w-10 h-10 mx-auto mb-3 text-muted-foreground opacity-40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        <p class="text-sm text-muted-foreground">Programları yönetmek için Kayıt sayfasına gidin</p>
      </div>

      <!-- ── Case Management Tab ── -->
      <div v-else-if="activeTab === 'case-management'">
        <!-- Sub-tabs -->
        <div class="mb-4 border-b border-border">
          <nav class="-mb-px flex gap-5 overflow-x-auto">
            <button
              v-for="sub in (['case-notes', 'medical-reports', 'development-reports', 'external-reports'] as CaseTab[])"
              :key="sub"
              @click="caseTab = sub"
              :class="['pb-3 text-sm whitespace-nowrap transition-colors', caseTab === sub ? 'border-b-2 border-primary text-primary font-medium' : 'text-muted-foreground hover:text-foreground']"
            >
              <span v-if="sub === 'case-notes'">{{ t('student.caseNote.title') }}</span>
              <span v-else-if="sub === 'medical-reports'">{{ t('student.medicalReport.title') }}</span>
              <span v-else-if="sub === 'development-reports'">{{ t('student.developmentReport.title') }}</span>
              <span v-else>{{ t('student.externalReport.title') }}</span>
            </button>
          </nav>
        </div>

        <!-- Case Notes -->
        <div v-if="caseTab === 'case-notes'">
          <div class="flex justify-between items-center mb-3">
            <p class="text-sm text-muted-foreground">{{ caseStore.caseNotes.totalCount }} {{ t('student.caseNote.title') }}</p>
            <button
              v-if="can('student:write')"
              @click="openAddNote"
              class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ t('student.caseNote.add') }}
            </button>
          </div>

          <div v-if="caseStore.loading" class="space-y-3">
            <div v-for="i in 3" :key="i" class="h-20 rounded-xl bg-accent animate-pulse" />
          </div>
          <div v-else-if="caseStore.caseNotes.items.length === 0" class="text-center py-12 text-muted-foreground text-sm">
            {{ t('common.noData') }}
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="note in caseStore.caseNotes.items"
              :key="note.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-4">
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-2 mb-1">
                    <span v-if="note.noteType" class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700">{{ note.noteType }}</span>
                    <span v-if="note.isConfidential" class="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">{{ t('student.caseNote.isConfidential') }}</span>
                    <span class="text-xs text-muted-foreground ml-auto">{{ formatDateTime(note.createdAt) }}</span>
                  </div>
                  <p class="text-sm text-foreground whitespace-pre-wrap">{{ note.body }}</p>
                  <p v-if="note.authoredBy" class="text-xs text-muted-foreground mt-1">{{ note.authoredBy }}</p>
                </div>
                <div v-if="can('student:write')" class="flex gap-1 shrink-0">
                  <button @click="openEditNote(note)" class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button @click="deleteNoteTarget = note.id" class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
          <div class="mt-4">
            <Pagination
              :page="caseStore.caseNotes.page"
              :page-size="caseStore.caseNotes.pageSize"
              :total-count="caseStore.caseNotes.totalCount"
              :total-pages="caseStore.caseNotes.totalPages"
              :has-previous-page="caseStore.caseNotes.hasPreviousPage"
              :has-next-page="caseStore.caseNotes.hasNextPage"
              @update:page="(p) => { noteQuery.page = p }"
              @update:page-size="(s) => { noteQuery.pageSize = s; noteQuery.page = 1 }"
            />
          </div>
        </div>

        <!-- Medical Reports -->
        <div v-else-if="caseTab === 'medical-reports'">
          <div class="flex justify-between items-center mb-3">
            <p class="text-sm text-muted-foreground">{{ caseStore.medicalReports.length }} {{ t('student.medicalReport.title') }}</p>
            <button
              v-if="can('student:write')"
              @click="openAddMedical"
              class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ t('student.medicalReport.add') }}
            </button>
          </div>
          <div v-if="caseStore.loading" class="space-y-3">
            <div v-for="i in 2" :key="i" class="h-16 rounded-xl bg-accent animate-pulse" />
          </div>
          <div v-else-if="caseStore.medicalReports.length === 0" class="text-center py-12 text-muted-foreground text-sm">
            {{ t('common.noData') }}
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="r in caseStore.medicalReports"
              :key="r.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="font-medium text-foreground text-sm">{{ r.title }}</p>
                  <p class="text-xs text-muted-foreground mt-0.5">
                    {{ formatDate(r.reportDate) }}
                    <span v-if="r.issuer"> · {{ r.issuer }}</span>
                  </p>
                  <p v-if="r.summary" class="text-xs text-foreground mt-1">{{ r.summary }}</p>
                </div>
                <div v-if="can('student:write')" class="flex gap-1 shrink-0">
                  <button @click="openEditMedical(r)" class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button @click="deleteMedicalTarget = r.id" class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Development Reports -->
        <div v-else-if="caseTab === 'development-reports'">
          <div class="flex justify-between items-center mb-3">
            <p class="text-sm text-muted-foreground">{{ caseStore.developmentReports.length }} {{ t('student.developmentReport.title') }}</p>
            <button
              v-if="can('student:write')"
              @click="openAddDevReport"
              class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ t('student.developmentReport.add') }}
            </button>
          </div>
          <div v-if="caseStore.loading" class="space-y-3">
            <div v-for="i in 2" :key="i" class="h-16 rounded-xl bg-accent animate-pulse" />
          </div>
          <div v-else-if="caseStore.developmentReports.length === 0" class="text-center py-12 text-muted-foreground text-sm">
            {{ t('common.noData') }}
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="r in caseStore.developmentReports"
              :key="r.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="font-medium text-foreground text-sm">{{ r.periodLabel ?? '—' }}</p>
                  <p class="text-xs text-muted-foreground mt-0.5">
                    {{ formatDate(r.reportDate) }}
                    <span v-if="r.authoredBy"> · {{ r.authoredBy }}</span>
                  </p>
                  <p v-if="r.content" class="text-xs text-foreground mt-1 line-clamp-2">{{ r.content }}</p>
                </div>
                <button
                  v-if="can('student:write')"
                  @click="deleteDevReportTarget = r.id"
                  class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors shrink-0"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- External Reports -->
        <div v-else-if="caseTab === 'external-reports'">
          <div class="flex justify-between items-center mb-3">
            <p class="text-sm text-muted-foreground">{{ caseStore.externalReports.length }} {{ t('student.externalReport.title') }}</p>
            <button
              v-if="can('student:write')"
              @click="openAddExtReport"
              class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ t('student.externalReport.add') }}
            </button>
          </div>
          <div v-if="caseStore.loading" class="space-y-3">
            <div v-for="i in 2" :key="i" class="h-16 rounded-xl bg-accent animate-pulse" />
          </div>
          <div v-else-if="caseStore.externalReports.length === 0" class="text-center py-12 text-muted-foreground text-sm">
            {{ t('common.noData') }}
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="r in caseStore.externalReports"
              :key="r.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="font-medium text-foreground text-sm">{{ r.institutionName }}</p>
                  <p class="text-xs text-muted-foreground mt-0.5">
                    {{ r.institutionTypeLabel ?? '—' }}
                    <span v-if="r.reportDate"> · {{ formatDate(r.reportDate) }}</span>
                  </p>
                  <p v-if="r.summary" class="text-xs text-foreground mt-1">{{ r.summary }}</p>
                </div>
                <button
                  v-if="can('student:write')"
                  @click="deleteExtReportTarget = r.id"
                  class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors shrink-0"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── History Tab ── -->
      <div v-else-if="activeTab === 'history'">
        <div class="flex items-center justify-between mb-4">
          <h3 class="font-semibold text-foreground">{{ t('student.statusHistory') }}</h3>
          <button
            v-if="can('student:change_status')"
            @click="openStatusModal"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('student.changeStatus') }}
          </button>
        </div>

        <div v-if="store.loading" class="space-y-2">
          <div v-for="i in 3" :key="i" class="h-12 rounded-xl bg-accent animate-pulse" />
        </div>
        <div v-else-if="store.statusHistory.length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <div v-else class="relative pl-4 border-l-2 border-border ml-4 space-y-4">
          <div v-for="h in store.statusHistory" :key="h.id" class="relative">
            <div class="absolute -left-[1.35rem] w-3 h-3 rounded-full bg-primary border-2 border-background" />
            <div class="rounded-xl border border-border bg-[--color-card] p-3 shadow-sm ml-4">
              <p class="text-xs text-muted-foreground mb-1">
                {{ formatDateTime(h.changedAt) }}
                <span v-if="h.changedBy"> · {{ h.changedBy }}</span>
              </p>
              <p class="text-sm font-medium text-foreground">{{ h.statusLabel ?? '—' }}</p>
              <p v-if="h.reason" class="text-xs text-muted-foreground mt-0.5">{{ h.reason }}</p>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══════════════════════════════════════════════════════════════════════ -->
    <!-- MODALS                                                                  -->
    <!-- ═══════════════════════════════════════════════════════════════════════ -->

    <!-- Change Status -->
    <FormModal :open="showStatusModal" :title="t('student.changeStatus')" :saving="store.saving" @submit="submitStatus" @close="showStatusModal = false">
      <div class="space-y-4">
        <p v-if="statusError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ statusError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.newStatus') }} *</label>
          <select v-model="statusForm.newStatusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="s in statuses" :key="s.id" :value="s.id">{{ s.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.reason') }}</label>
          <textarea v-model="statusForm.reason" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Link Guardian -->
    <FormModal :open="showLinkModal" :title="t('student.guardian.link')" :saving="store.saving" @submit="submitLink" @close="showLinkModal = false">
      <div class="space-y-4">
        <p v-if="linkError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ linkError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.guardian.guardianId') }} *</label>
          <input v-model="linkForm.guardianId" type="text" placeholder="UUID" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary font-mono" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.guardian.relationship') }}</label>
          <select v-model="linkForm.relationshipId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="r in relationships" :key="r.id" :value="r.id">{{ r.label }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="linkForm.isPrimary" class="rounded border-border" />
            {{ t('student.guardian.isPrimary') }}
          </label>
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="linkForm.hasCustody" class="rounded border-border" />
            {{ t('student.guardian.hasCustody') }}
          </label>
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="linkForm.portalAccess" class="rounded border-border" />
            {{ t('student.guardian.portalAccess') }}
          </label>
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="linkForm.financialResponsible" class="rounded border-border" />
            {{ t('student.guardian.financialResponsible') }}
          </label>
        </div>
      </div>
    </FormModal>

    <!-- Edit Guardian Link -->
    <FormModal :open="!!editLinkTarget" :title="t('student.guardian.edit')" :saving="store.saving" @submit="submitEditLink" @close="editLinkTarget = null">
      <div class="space-y-4">
        <p v-if="editLinkError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ editLinkError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.guardian.relationship') }}</label>
          <select v-model="editLinkForm.relationshipId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="r in relationships" :key="r.id" :value="r.id">{{ r.label }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="editLinkForm.isPrimary" class="rounded border-border" />
            {{ t('student.guardian.isPrimary') }}
          </label>
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="editLinkForm.hasCustody" class="rounded border-border" />
            {{ t('student.guardian.hasCustody') }}
          </label>
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="editLinkForm.portalAccess" class="rounded border-border" />
            {{ t('student.guardian.portalAccess') }}
          </label>
          <label class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="editLinkForm.financialResponsible" class="rounded border-border" />
            {{ t('student.guardian.financialResponsible') }}
          </label>
        </div>
      </div>
    </FormModal>

    <!-- Unlink Guardian Confirm -->
    <ConfirmModal
      :open="!!unlinkTarget"
      :title="t('student.guardian.unlink')"
      :message="t('student.guardian.unlink')"
      :confirm-label="t('student.guardian.unlink')"
      :loading="unlinkLoading"
      @confirm="doUnlink"
      @cancel="unlinkTarget = null"
    />

    <!-- Add / Edit Case Note -->
    <FormModal
      :open="showNoteModal"
      :title="editNoteTarget ? t('common.edit') : t('student.caseNote.add')"
      :saving="caseStore.saving"
      @submit="submitNote"
      @close="showNoteModal = false"
    >
      <div class="space-y-4">
        <p v-if="noteError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ noteError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.caseNote.noteType') }}</label>
          <input v-model="noteForm.noteType" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.caseNote.body') }} *</label>
          <textarea v-model="noteForm.body" rows="5" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <label class="flex items-center gap-2 text-sm cursor-pointer">
          <input type="checkbox" v-model="noteForm.isConfidential" class="rounded border-border" />
          {{ t('student.caseNote.isConfidential') }}
        </label>
      </div>
    </FormModal>

    <!-- Delete Case Note Confirm -->
    <ConfirmModal
      :open="!!deleteNoteTarget"
      :title="t('student.caseNote.deleteTitle')"
      :message="t('student.caseNote.deleteMessage')"
      :confirm-label="t('common.delete')"
      :loading="deleteNoteLoading"
      @confirm="doDeleteNote"
      @cancel="deleteNoteTarget = null"
    />

    <!-- Add / Edit Medical Report -->
    <FormModal
      :open="showMedicalModal"
      :title="editMedicalTarget ? t('common.edit') : t('student.medicalReport.add')"
      :saving="caseStore.saving"
      @submit="submitMedical"
      @close="showMedicalModal = false"
    >
      <div class="space-y-4">
        <p v-if="medicalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ medicalError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.medicalReport.titleField') }} *</label>
          <input v-model="medicalForm.title" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.medicalReport.reportDate') }}</label>
            <input v-model="medicalForm.reportDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.medicalReport.issuer') }}</label>
            <input v-model="medicalForm.issuer" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.medicalReport.summary') }}</label>
          <textarea v-model="medicalForm.summary" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete Medical Report Confirm -->
    <ConfirmModal
      :open="!!deleteMedicalTarget"
      :title="t('common.delete')"
      :message="t('student.medicalReport.title')"
      :confirm-label="t('common.delete')"
      :loading="deleteMedicalLoading"
      @confirm="doDeleteMedical"
      @cancel="deleteMedicalTarget = null"
    />

    <!-- Add Development Report -->
    <FormModal :open="showDevReportModal" :title="t('student.developmentReport.add')" :saving="caseStore.saving" @submit="submitDevReport" @close="showDevReportModal = false">
      <div class="space-y-4">
        <p v-if="devReportError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ devReportError }}</p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentReport.periodLabel') }}</label>
            <input v-model="devReportForm.periodLabel" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentReport.reportDate') }}</label>
            <input v-model="devReportForm.reportDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentReport.content') }}</label>
          <textarea v-model="devReportForm.content" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete Development Report Confirm -->
    <ConfirmModal
      :open="!!deleteDevReportTarget"
      :title="t('common.delete')"
      :message="t('student.developmentReport.title')"
      :confirm-label="t('common.delete')"
      :loading="deleteDevReportLoading"
      @confirm="doDeleteDevReport"
      @cancel="deleteDevReportTarget = null"
    />

    <!-- Add External Report -->
    <FormModal :open="showExtReportModal" :title="t('student.externalReport.add')" :saving="caseStore.saving" @submit="submitExtReport" @close="showExtReportModal = false">
      <div class="space-y-4">
        <p v-if="extReportError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ extReportError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.externalReport.institutionName') }} *</label>
          <input v-model="extReportForm.institutionName" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.externalReport.institutionType') }}</label>
            <select v-model="extReportForm.institutionTypeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="it in institutionTypes" :key="it.id" :value="it.id">{{ it.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.medicalReport.reportDate') }}</label>
            <input v-model="extReportForm.reportDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.medicalReport.summary') }}</label>
          <textarea v-model="extReportForm.summary" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete External Report Confirm -->
    <ConfirmModal
      :open="!!deleteExtReportTarget"
      :title="t('common.delete')"
      :message="t('student.externalReport.title')"
      :confirm-label="t('common.delete')"
      :loading="deleteExtReportLoading"
      @confirm="doDeleteExtReport"
      @cancel="deleteExtReportTarget = null"
    />

    <!-- Add / Edit Diagnosis -->
    <FormModal
      :open="showDiagnosisModal"
      :title="editDiagnosisTarget ? t('common.edit') : t('student.diagnosis.add')"
      :saving="store.saving"
      @submit="submitDiagnosis"
      @close="showDiagnosisModal = false"
    >
      <div class="space-y-4">
        <p v-if="diagnosisError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ diagnosisError }}</p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.diagnosis.category') }}</label>
            <select v-model="diagnosisForm.categoryId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="c in diagnosisCategories" :key="c.id" :value="c.id">{{ c.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.diagnosis.icdCode') }}</label>
            <input v-model="diagnosisForm.icdCode" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.diagnosis.description') }}</label>
          <textarea v-model="diagnosisForm.description" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.diagnosis.diagnosedOn') }}</label>
            <input v-model="diagnosisForm.diagnosedOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.diagnosis.diagnosedBy') }}</label>
            <input v-model="diagnosisForm.diagnosedBy" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Delete Diagnosis Confirm -->
    <ConfirmModal
      :open="!!deleteDiagnosisTarget"
      :title="t('common.delete')"
      :message="t('student.diagnosis.title')"
      :confirm-label="t('common.delete')"
      :loading="deleteDiagnosisLoading"
      @confirm="doDeleteDiagnosis"
      @cancel="deleteDiagnosisTarget = null"
    />

    <!-- Upsert Developmental Profile -->
    <FormModal
      :open="showProfileModal"
      :title="t('student.developmentalProfile.title')"
      :saving="store.saving"
      @submit="submitProfile"
      @close="showProfileModal = false"
    >
      <div class="space-y-4">
        <p v-if="profileError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ profileError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentalProfile.area') }}</label>
          <select v-model="profileForm.developmentAreaId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="a in developmentAreas" :key="a.id" :value="a.id">{{ a.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentalProfile.summary') }}</label>
          <textarea v-model="profileForm.summary" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentalProfile.strengths') }}</label>
          <textarea v-model="profileForm.strengths" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentalProfile.needs') }}</label>
          <textarea v-model="profileForm.needs" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.developmentalProfile.assessedOn') }}</label>
          <input v-model="profileForm.assessedOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>

    <!-- Enroll at Campus -->
    <FormModal :open="showEnrollModal" :title="t('student.campus.enroll')" :saving="store.saving" @submit="submitEnroll" @close="showEnrollModal = false">
      <div class="space-y-4">
        <p v-if="enrollError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ enrollError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.primaryCampus') }} *</label>
          <select v-model="enrollForm.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in enrollableCampuses" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
          <p v-if="enrollableCampuses.length === 0" class="mt-1 text-xs text-muted-foreground">
            {{ t('student.campus.noEnrollableCampuses') }}
          </p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.campus.enroll') }} {{ t('common.from') }}</label>
          <input v-model="enrollForm.activeFrom" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <label class="flex items-center gap-2 text-sm cursor-pointer">
          <input type="checkbox" v-model="enrollForm.isPrimary" class="rounded border-border" />
          {{ t('student.primaryCampus') }}
        </label>
      </div>
    </FormModal>

    <!-- Transfer Student -->
    <FormModal :open="showTransferModal" :title="t('student.campus.transfer')" :saving="store.saving" @submit="submitTransfer" @close="showTransferModal = false">
      <div class="space-y-4">
        <p v-if="transferError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ transferError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.primaryCampus') }} *</label>
          <select v-model="transferForm.newCampusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.campus.transfer') }} {{ t('common.date') }}</label>
          <input v-model="transferForm.transferDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>

    <!-- Edit Emergency Contacts -->
    <FormModal :open="showContactsModal" :title="t('student.emergencyContact.title')" :saving="store.saving" :wide="true" @submit="submitContacts" @close="showContactsModal = false">
      <div class="space-y-4">
        <p v-if="contactsError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ contactsError }}</p>
        <div v-for="(c, i) in contactsForm" :key="i" class="rounded-lg border border-border p-3 space-y-3">
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-muted-foreground">#{{ i + 1 }}</span>
            <button type="button" @click="removeContact(i)" class="text-xs text-red-600 hover:text-red-700">{{ t('common.delete') }}</button>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium text-foreground mb-1">{{ t('student.fullName') }}</label>
              <input v-model="c.fullName" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
            </div>
            <div>
              <label class="block text-xs font-medium text-foreground mb-1">{{ t('student.guardian.relationship') }}</label>
              <input v-model="c.relationship" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
            </div>
            <div>
              <label class="block text-xs font-medium text-foreground mb-1">{{ t('common.phone') }}</label>
              <input v-model="c.phone" type="tel" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
            </div>
          </div>
        </div>
        <button type="button" @click="addContact" class="flex items-center gap-1.5 text-sm text-primary hover:text-primary/80">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('common.add') }}
        </button>
      </div>
    </FormModal>
  </div>
</template>
