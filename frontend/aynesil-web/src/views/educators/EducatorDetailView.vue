<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEducatorStore } from '@/stores/educator.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type {
  EducatorCertificationDto,
  AssignSpecialtyPayload,
  AssignCampusPayload,
  AddCertificationPayload,
  UpdateCertificationPayload,
  LinkHierarchyPayload,
} from '@/types/educator.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEducatorStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const educator = computed(() => store.current)
const activeTab = ref<'overview' | 'specialties' | 'certifications' | 'campuses' | 'hierarchy'>('overview')

const specialties = ref<RefValueItem[]>([])
const certificationTypes = ref<RefValueItem[]>([])
const relationshipTypes = ref<RefValueItem[]>([])

onMounted(async () => {
  await store.fetchOne(id.value)
  await Promise.all([
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('EDUCATOR_SPECIALTY').then(v => { specialties.value = v }),
    refData.getValues('CERTIFICATION_TYPE').then(v => { certificationTypes.value = v }),
    refData.getValues('EDUCATOR_RELATIONSHIP').then(v => { relationshipTypes.value = v }),
  ])
})

onUnmounted(() => {
  store.clearCurrent()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

// ── Activate / Deactivate ─────────────────────────────────────────────────
const toggleLoading = ref(false)

async function toggleActive() {
  if (!educator.value) return
  toggleLoading.value = true
  try {
    if (educator.value.isActive) {
      await store.deactivate(id.value)
    } else {
      await store.activate(id.value)
    }
  } finally {
    toggleLoading.value = false
  }
}

// ── Specialty ─────────────────────────────────────────────────────────────
const showSpecialtyModal = ref(false)
const specialtyForm = reactive<AssignSpecialtyPayload>({ specialtyId: '' })
const specialtyError = ref('')

function openSpecialtyModal() {
  specialtyForm.specialtyId = ''
  specialtyError.value = ''
  showSpecialtyModal.value = true
}

async function submitSpecialty() {
  if (!specialtyForm.specialtyId) { specialtyError.value = t('validation.required', { field: t('educator.tab.specialties') }); return }
  specialtyError.value = ''
  try {
    await store.assignSpecialty(id.value, { specialtyId: specialtyForm.specialtyId })
    showSpecialtyModal.value = false
  } catch (e: unknown) {
    specialtyError.value = (e as Error).message
  }
}

const removeSpecialtyTarget = ref<string | null>(null)
const removeSpecialtyLoading = ref(false)

async function doRemoveSpecialty() {
  if (!removeSpecialtyTarget.value) return
  removeSpecialtyLoading.value = true
  try {
    await store.removeSpecialty(id.value, removeSpecialtyTarget.value)
    removeSpecialtyTarget.value = null
  } finally {
    removeSpecialtyLoading.value = false
  }
}

// ── Certification ─────────────────────────────────────────────────────────
const showCertModal = ref(false)
const certTarget = ref<EducatorCertificationDto | null>(null)
const certForm = reactive<AddCertificationPayload & { rowVersion?: number }>({
  name: '',
  certificationTypeId: null,
  issuer: null,
  issuedOn: null,
  expiresOn: null,
  fileId: null,
})
const certError = ref('')

function openAddCert() {
  certTarget.value = null
  certForm.name = ''
  certForm.certificationTypeId = null
  certForm.issuer = null
  certForm.issuedOn = null
  certForm.expiresOn = null
  certForm.fileId = null
  certError.value = ''
  showCertModal.value = true
}

function openEditCert(cert: EducatorCertificationDto) {
  certTarget.value = cert
  certForm.name = cert.name
  certForm.certificationTypeId = cert.certificationTypeId
  certForm.issuer = cert.issuer
  certForm.issuedOn = cert.issuedOn
  certForm.expiresOn = cert.expiresOn
  certForm.fileId = cert.fileId
  certForm.rowVersion = cert.rowVersion
  certError.value = ''
  showCertModal.value = true
}

async function submitCert() {
  if (!certForm.name.trim()) { certError.value = t('validation.required', { field: t('educator.certification.name') }); return }
  certError.value = ''
  try {
    if (certTarget.value) {
      const payload: UpdateCertificationPayload = {
        name: certForm.name,
        certificationTypeId: certForm.certificationTypeId,
        issuer: certForm.issuer,
        issuedOn: certForm.issuedOn,
        expiresOn: certForm.expiresOn,
        fileId: certForm.fileId,
        rowVersion: certForm.rowVersion ?? 0,
      }
      await store.updateCertification(id.value, certTarget.value.id, payload)
    } else {
      await store.addCertification(id.value, {
        name: certForm.name,
        certificationTypeId: certForm.certificationTypeId,
        issuer: certForm.issuer,
        issuedOn: certForm.issuedOn,
        expiresOn: certForm.expiresOn,
        fileId: certForm.fileId,
      })
    }
    showCertModal.value = false
  } catch (e: unknown) {
    certError.value = (e as Error).message
  }
}

const deleteCertTarget = ref<string | null>(null)
const deleteCertLoading = ref(false)

async function doDeleteCert() {
  if (!deleteCertTarget.value) return
  deleteCertLoading.value = true
  try {
    await store.deleteCertification(id.value, deleteCertTarget.value)
    deleteCertTarget.value = null
  } finally {
    deleteCertLoading.value = false
  }
}

// ── Campus ────────────────────────────────────────────────────────────────
const showCampusModal = ref(false)
const campusForm = reactive<AssignCampusPayload>({ campusId: '', isPrimary: false, activeFrom: null })
const campusError = ref('')

function openCampusModal() {
  campusForm.campusId = ''
  campusForm.isPrimary = false
  campusForm.activeFrom = null
  campusError.value = ''
  showCampusModal.value = true
}

async function submitCampus() {
  if (!campusForm.campusId) { campusError.value = t('validation.required', { field: t('educator.primaryCampus') }); return }
  campusError.value = ''
  try {
    await store.assignCampus(id.value, {
      campusId: campusForm.campusId,
      isPrimary: campusForm.isPrimary,
      activeFrom: campusForm.activeFrom,
    })
    showCampusModal.value = false
  } catch (e: unknown) {
    campusError.value = (e as Error).message
  }
}

const endCampusTarget = ref<string | null>(null)
const endCampusLoading = ref(false)

async function doEndCampus() {
  if (!endCampusTarget.value) return
  endCampusLoading.value = true
  try {
    await store.endCampusAssignment(id.value, endCampusTarget.value, { endDate: null })
    endCampusTarget.value = null
  } finally {
    endCampusLoading.value = false
  }
}

// ── Hierarchy ─────────────────────────────────────────────────────────────
const showHierarchyModal = ref(false)
const hierarchyForm = reactive<LinkHierarchyPayload>({ supervisorId: '', relationshipId: null, campusId: null, activeFrom: null })
const hierarchyError = ref('')

function openHierarchyModal() {
  hierarchyForm.supervisorId = ''
  hierarchyForm.relationshipId = null
  hierarchyForm.campusId = null
  hierarchyForm.activeFrom = null
  hierarchyError.value = ''
  showHierarchyModal.value = true
}

async function submitHierarchy() {
  if (!hierarchyForm.supervisorId.trim()) { hierarchyError.value = t('validation.required', { field: t('educator.hierarchy.supervisor') }); return }
  hierarchyError.value = ''
  try {
    await store.linkHierarchy(id.value, {
      supervisorId: hierarchyForm.supervisorId,
      relationshipId: hierarchyForm.relationshipId,
      campusId: hierarchyForm.campusId || null,
      activeFrom: hierarchyForm.activeFrom,
    })
    showHierarchyModal.value = false
  } catch (e: unknown) {
    hierarchyError.value = (e as Error).message
  }
}

const unlinkHierarchyTarget = ref<string | null>(null)
const unlinkHierarchyLoading = ref(false)

async function doUnlinkHierarchy() {
  if (!unlinkHierarchyTarget.value) return
  unlinkHierarchyLoading.value = true
  try {
    await store.unlinkHierarchy(id.value, unlinkHierarchyTarget.value)
    unlinkHierarchyTarget.value = null
  } finally {
    unlinkHierarchyLoading.value = false
  }
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="store.loading && !educator" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Error / Not found -->
    <div v-else-if="store.error && !educator" class="text-center py-24">
      <p class="text-muted-foreground">{{ store.error }}</p>
      <button @click="router.push({ name: 'educators' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('educator.backToList') }}
      </button>
    </div>
    <div v-else-if="!educator && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'educators' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('educator.backToList') }}
      </button>
    </div>

    <template v-else-if="educator">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'educators' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('educator.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ educator.fullName }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span v-if="educator.titleLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
              {{ educator.titleLabel }}
            </span>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', educator.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
              {{ educator.isActive ? t('common.active') : t('common.inactive') }}
            </span>
          </div>
        </div>
        <div v-if="can('educator:update')" class="flex items-center gap-2 flex-wrap">
          <button
            @click="toggleActive"
            :disabled="toggleLoading"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors disabled:opacity-50"
          >
            {{ educator.isActive ? t('common.deactivate') : t('common.activate') }}
          </button>
          <button
            @click="router.push({ name: 'educator-edit', params: { id: educator.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            {{ t('common.edit') }}
          </button>
          <button
            @click="router.push({ name: 'educator-availability', params: { id: educator.id } })"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('common.availability') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6 overflow-x-auto">
          <button
            v-for="tab in ['overview', 'specialties', 'certifications', 'campuses', 'hierarchy']"
            :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ t(`educator.tab.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 space-y-3 shadow-sm">
          <h3 class="font-semibold text-foreground">{{ t('educator.title') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.fullName') }}</dt>
              <dd class="font-medium text-foreground">{{ educator.fullName }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.titleLabel') }}</dt>
              <dd class="font-medium text-foreground">{{ educator.titleLabel ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.email') }}</dt>
              <dd class="font-medium text-foreground">{{ educator.email ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.phone') }}</dt>
              <dd class="font-medium text-foreground">{{ educator.phone ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.employmentType') }}</dt>
              <dd class="font-medium text-foreground">{{ educator.employmentType ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.hireDate') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(educator.hireDate) }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.primaryCampus') }}</dt>
              <dd class="font-medium text-foreground">{{ educator.primaryCampusName ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('educator.isActive') }}</dt>
              <dd>
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', educator.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
                  {{ educator.isActive ? t('common.active') : t('common.inactive') }}
                </span>
              </dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Specialties Tab -->
      <div v-else-if="activeTab === 'specialties'">
        <div class="flex justify-end mb-3">
          <button
            v-if="can('educator:manage_specialties')"
            @click="openSpecialtyModal"
            class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('educator.specialty.assign') }}
          </button>
        </div>
        <div v-if="educator.specialties.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-2">
          <div
            v-for="spec in educator.specialties"
            :key="spec.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm flex items-center justify-between"
          >
            <p class="text-sm font-medium text-foreground">{{ spec.specialtyLabel ?? '—' }}</p>
            <button
              v-if="can('educator:manage_specialties')"
              @click="removeSpecialtyTarget = spec.id"
              class="px-2 py-1 text-xs rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors"
            >
              {{ t('educator.specialty.remove') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Certifications Tab -->
      <div v-else-if="activeTab === 'certifications'">
        <div class="flex justify-end mb-3">
          <button
            v-if="can('educator:manage_certifications')"
            @click="openAddCert"
            class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('educator.certification.add') }}
          </button>
        </div>
        <div v-if="educator.certifications.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="cert in educator.certifications"
            :key="cert.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between">
              <div class="space-y-1">
                <div class="flex items-center gap-2">
                  <p class="text-sm font-semibold text-foreground">{{ cert.name }}</p>
                  <span v-if="cert.isExpired" class="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">
                    {{ t('educator.certification.isExpired') }}
                  </span>
                </div>
                <p class="text-xs text-muted-foreground">{{ cert.certificationTypeLabel ?? '—' }}</p>
                <div class="flex gap-4 text-xs text-muted-foreground">
                  <span>{{ t('educator.certification.issuer') }}: {{ cert.issuer ?? '—' }}</span>
                  <span>{{ t('educator.certification.issuedOn') }}: {{ formatDate(cert.issuedOn) }}</span>
                  <span v-if="cert.expiresOn">{{ t('educator.certification.expiresOn') }}: {{ formatDate(cert.expiresOn) }}</span>
                </div>
              </div>
              <div v-if="can('educator:manage_certifications')" class="flex gap-2">
                <button @click="openEditCert(cert)"
                  class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent transition-colors">
                  {{ t('educator.certification.edit') }}
                </button>
                <button @click="deleteCertTarget = cert.id"
                  class="px-2 py-1 text-xs rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors">
                  {{ t('common.delete') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Campuses Tab -->
      <div v-else-if="activeTab === 'campuses'">
        <div class="flex justify-end mb-3">
          <button
            v-if="can('educator:manage_campuses')"
            @click="openCampusModal"
            class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('educator.campus.assign') }}
          </button>
        </div>
        <div v-if="educator.campuses.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="campus in educator.campuses"
            :key="campus.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm flex items-center justify-between"
          >
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <p class="text-sm font-semibold text-foreground">{{ campus.campusName ?? '—' }}</p>
                <span v-if="campus.isPrimary" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                  {{ t('common.primary') }}
                </span>
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', campus.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
                  {{ campus.isActive ? t('common.active') : t('common.inactive') }}
                </span>
              </div>
              <p class="text-xs text-muted-foreground">
                {{ formatDate(campus.activeFrom) }}{{ campus.activeTo ? ` — ${formatDate(campus.activeTo)}` : '' }}
              </p>
            </div>
            <button
              v-if="can('educator:manage_campuses') && campus.isActive"
              @click="endCampusTarget = campus.id"
              class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent transition-colors"
            >
              {{ t('educator.campus.end') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Hierarchy Tab -->
      <div v-else-if="activeTab === 'hierarchy'">
        <div class="flex justify-end mb-3">
          <button
            v-if="can('educator:manage_hierarchy')"
            @click="openHierarchyModal"
            class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('educator.hierarchy.link') }}
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Supervisors -->
          <div>
            <h3 class="text-sm font-semibold text-foreground mb-3">{{ t('educator.hierarchy.supervisor') }}</h3>
            <div v-if="educator.supervisors.length === 0" class="text-center py-8 text-muted-foreground text-sm">
              {{ t('common.noData') }}
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="edge in educator.supervisors"
                :key="edge.id"
                class="rounded-xl border border-border bg-[--color-card] p-3 shadow-sm flex items-center justify-between"
              >
                <div>
                  <p class="text-sm font-medium text-foreground">{{ edge.supervisorFullName }}</p>
                  <p class="text-xs text-muted-foreground">{{ edge.relationshipLabel ?? '—' }}</p>
                </div>
                <button
                  v-if="can('educator:manage_hierarchy') && edge.isActive"
                  @click="unlinkHierarchyTarget = edge.id"
                  class="px-2 py-1 text-xs rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors"
                >
                  {{ t('common.unlink') }}
                </button>
              </div>
            </div>
          </div>

          <!-- Subordinates -->
          <div>
            <h3 class="text-sm font-semibold text-foreground mb-3">{{ t('educator.hierarchy.subordinates') }}</h3>
            <div v-if="educator.subordinates.length === 0" class="text-center py-8 text-muted-foreground text-sm">
              {{ t('common.noData') }}
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="edge in educator.subordinates"
                :key="edge.id"
                class="rounded-xl border border-border bg-[--color-card] p-3 shadow-sm"
              >
                <p class="text-sm font-medium text-foreground">{{ edge.educatorFullName }}</p>
                <p class="text-xs text-muted-foreground">{{ edge.relationshipLabel ?? '—' }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Assign Specialty Modal -->
    <FormModal :open="showSpecialtyModal" :title="t('educator.specialty.assign')" :saving="store.saving" @submit="submitSpecialty" @close="showSpecialtyModal = false">
      <div class="space-y-4">
        <p v-if="specialtyError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ specialtyError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.tab.specialties') }} *</label>
          <select v-model="specialtyForm.specialtyId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="s in specialties" :key="s.id" :value="s.id">{{ s.label }}</option>
          </select>
        </div>
      </div>
    </FormModal>

    <!-- Certification Modal -->
    <FormModal :open="showCertModal" :title="certTarget ? t('educator.certification.edit') : t('educator.certification.add')" :saving="store.saving" @submit="submitCert" @close="showCertModal = false">
      <div class="space-y-4">
        <p v-if="certError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ certError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.certification.name') }} *</label>
          <input v-model="certForm.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.type') }}</label>
          <select v-model="certForm.certificationTypeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option :value="null">{{ t('common.select') }}</option>
            <option v-for="ct in certificationTypes" :key="ct.id" :value="ct.id">{{ ct.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.certification.issuer') }}</label>
          <input v-model="certForm.issuer" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.certification.issuedOn') }}</label>
            <input v-model="certForm.issuedOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.certification.expiresOn') }}</label>
            <input v-model="certForm.expiresOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Assign Campus Modal -->
    <FormModal :open="showCampusModal" :title="t('educator.campus.assign')" :saving="store.saving" @submit="submitCampus" @close="showCampusModal = false">
      <div class="space-y-4">
        <p v-if="campusError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ campusError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.primaryCampus') }} *</label>
          <select v-model="campusForm.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.startDate') }}</label>
          <input v-model="campusForm.activeFrom" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <label class="flex items-center gap-2 text-sm cursor-pointer">
          <input type="checkbox" v-model="campusForm.isPrimary" class="rounded border-border" />
          {{ t('common.primary') }}
        </label>
      </div>
    </FormModal>

    <!-- Link Hierarchy Modal -->
    <FormModal :open="showHierarchyModal" :title="t('educator.hierarchy.link')" :saving="store.saving" @submit="submitHierarchy" @close="showHierarchyModal = false">
      <div class="space-y-4">
        <p v-if="hierarchyError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ hierarchyError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.hierarchy.supervisor') }} ID *</label>
          <input v-model="hierarchyForm.supervisorId" type="text" placeholder="UUID" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.relationship') }}</label>
          <select v-model="hierarchyForm.relationshipId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option :value="null">{{ t('common.select') }}</option>
            <option v-for="r in relationshipTypes" :key="r.id" :value="r.id">{{ r.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.primaryCampus') }}</label>
          <select v-model="hierarchyForm.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option :value="null">{{ t('common.select') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
      </div>
    </FormModal>

    <!-- Remove Specialty Confirm -->
    <ConfirmModal
      :open="!!removeSpecialtyTarget"
      :title="t('educator.specialty.remove')"
      :message="t('common.confirmAction')"
      :confirm-label="t('educator.specialty.remove')"
      :loading="removeSpecialtyLoading"
      @confirm="doRemoveSpecialty"
      @cancel="removeSpecialtyTarget = null"
    />

    <!-- Delete Certification Confirm -->
    <ConfirmModal
      :open="!!deleteCertTarget"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.confirmAction')"
      :confirm-label="t('common.delete')"
      :loading="deleteCertLoading"
      @confirm="doDeleteCert"
      @cancel="deleteCertTarget = null"
    />

    <!-- End Campus Confirm -->
    <ConfirmModal
      :open="!!endCampusTarget"
      :title="t('educator.campus.end')"
      :message="t('common.confirmAction')"
      :confirm-label="t('educator.campus.end')"
      :loading="endCampusLoading"
      @confirm="doEndCampus"
      @cancel="endCampusTarget = null"
    />

    <!-- Unlink Hierarchy Confirm -->
    <ConfirmModal
      :open="!!unlinkHierarchyTarget"
      :title="t('common.unlink')"
      :message="t('common.confirmAction')"
      :confirm-label="t('common.unlink')"
      :loading="unlinkHierarchyLoading"
      @confirm="doUnlinkHierarchy"
      @cancel="unlinkHierarchyTarget = null"
    />
  </div>
</template>
