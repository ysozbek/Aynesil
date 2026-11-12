<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useProgramStore } from '@/stores/program.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type { ProgramListItemDto, StudentProgramDto } from '@/types/program.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useProgramStore()
const refData = useRefDataStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const enrollment = computed(() => store.currentEnrollment)
const statuses = ref<RefValueItem[]>([])
const programs = ref<ProgramListItemDto[]>([])

onMounted(async () => {
  await Promise.all([
    store.fetchEnrollment(id.value),
    refData.getValues('enrollment_status').then((v) => { statuses.value = v }),
    store.fetchPrograms({ isActive: true, page: 1, pageSize: 200 }).then(() => {
      programs.value = store.programList.items
    }),
  ])
})

onUnmounted(() => {
  store.clearCurrent()
})

function formatDate(val: string | null | undefined): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

function spStatusClass(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-emerald-100 text-emerald-700',
    paused: 'bg-amber-100 text-amber-700',
    completed: 'bg-blue-100 text-blue-700',
    cancelled: 'bg-gray-100 text-gray-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}

// ── Change status ─────────────────────────────────────────────────────────────
const showStatus = ref(false)
const statusForm = reactive({ newStatusId: '' })
const statusError = ref('')

function openStatus() {
  statusForm.newStatusId = enrollment.value?.statusId ?? ''
  statusError.value = ''
  showStatus.value = true
}

async function submitStatus() {
  if (!enrollment.value || !statusForm.newStatusId) {
    statusError.value = t('validation.required', { field: t('enrollment.status') })
    return
  }
  try {
    await store.changeEnrollmentStatus(enrollment.value.id, {
      newStatusId: statusForm.newStatusId,
      rowVersion: enrollment.value.rowVersion,
    })
    showStatus.value = false
  } catch (e: unknown) {
    statusError.value = (e as Error).message
  }
}

// ── End enrollment ────────────────────────────────────────────────────────────
const showEnd = ref(false)
const endForm = reactive({ endedOn: '', terminationReason: '' })
const endError = ref('')

function openEnd() {
  endForm.endedOn = new Date().toISOString().slice(0, 10)
  endForm.terminationReason = ''
  endError.value = ''
  showEnd.value = true
}

async function submitEnd() {
  if (!enrollment.value) return
  try {
    await store.endEnrollment(enrollment.value.id, {
      endedOn: endForm.endedOn || null,
      terminationReason: endForm.terminationReason || null,
      rowVersion: enrollment.value.rowVersion,
    })
    showEnd.value = false
  } catch (e: unknown) {
    endError.value = (e as Error).message
  }
}

// ── Assign program ────────────────────────────────────────────────────────────
const showAssign = ref(false)
const assignForm = reactive({
  programId: '',
  startDate: new Date().toISOString().slice(0, 10),
  endDate: '',
})
const assignError = ref('')

function openAssign() {
  assignForm.programId = ''
  assignForm.startDate = new Date().toISOString().slice(0, 10)
  assignForm.endDate = ''
  assignError.value = ''
  showAssign.value = true
}

async function submitAssign() {
  if (!enrollment.value || !assignForm.programId) {
    assignError.value = t('validation.required', { field: t('enrollment.program') })
    return
  }
  try {
    await store.assignStudentToProgram({
      studentId: enrollment.value.studentId,
      programId: assignForm.programId,
      enrollmentId: enrollment.value.id,
      campusId: enrollment.value.campusId,
      startDate: assignForm.startDate || null,
      endDate: assignForm.endDate || null,
    })
    showAssign.value = false
    await store.fetchEnrollment(id.value)
  } catch (e: unknown) {
    assignError.value = (e as Error).message
  }
}

// ── Remove program ────────────────────────────────────────────────────────────
const removeTarget = ref<StudentProgramDto | null>(null)
const removeLoading = ref(false)

async function doRemove() {
  if (!removeTarget.value || !enrollment.value) return
  removeLoading.value = true
  try {
    await store.removeStudentProgram(enrollment.value.id, removeTarget.value.id)
    removeTarget.value = null
  } finally {
    removeLoading.value = false
  }
}

// ── Edit student-program (status / dates) ─────────────────────────────────────
// Backend accepts status transitions through `PUT /enrollments/{id}/programs/{spId}`
// which maps to `UpdateStudentProgramCommand` (startDate/endDate/status + rowVersion).
// Allowed status values: `active`, `paused`, `completed`, `cancelled`.
const SP_STATUSES = ['active', 'paused', 'completed', 'cancelled'] as const
type SpStatus = typeof SP_STATUSES[number]

const showEditSp = ref(false)
const editSpTarget = ref<StudentProgramDto | null>(null)
const editSpForm = reactive({
  status: 'active' as SpStatus,
  startDate: '',
  endDate: '',
  rowVersion: 0,
})
const editSpError = ref('')

function openEditSp(sp: StudentProgramDto) {
  editSpTarget.value = sp
  editSpForm.status = (SP_STATUSES as readonly string[]).includes(sp.status)
    ? (sp.status as SpStatus)
    : 'active'
  editSpForm.startDate = sp.startDate ?? ''
  editSpForm.endDate = sp.endDate ?? ''
  editSpForm.rowVersion = sp.rowVersion
  editSpError.value = ''
  showEditSp.value = true
}

/** Quick-complete shortcut used by the "End" action on an active row. */
function openEndSp(sp: StudentProgramDto) {
  openEditSp(sp)
  editSpForm.status = 'completed'
  if (!editSpForm.endDate) editSpForm.endDate = new Date().toISOString().slice(0, 10)
}

async function submitEditSp() {
  if (!editSpTarget.value || !enrollment.value) return
  editSpError.value = ''
  try {
    await store.updateStudentProgram(enrollment.value.id, editSpTarget.value.id, {
      startDate: editSpForm.startDate || null,
      endDate: editSpForm.endDate || null,
      status: editSpForm.status,
      rowVersion: editSpForm.rowVersion,
    })
    showEditSp.value = false
    await store.fetchEnrollment(id.value)
  } catch (e: unknown) {
    editSpError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="enrollment?.studentFullName ?? t('enrollment.detail')"
      :description="t('enrollment.detailDescription')"
    >
      <button
        type="button"
        class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
        @click="router.push({ name: 'enrollments' })"
      >
        {{ t('enrollment.backToList') }}
      </button>
      <button
        v-if="can('enrollment:update') && enrollment && !enrollment.endedOn"
        type="button"
        class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
        @click="openStatus"
      >
        {{ t('enrollment.changeStatus') }}
      </button>
      <button
        v-if="can('enrollment:update') && enrollment && !enrollment.endedOn"
        type="button"
        class="px-3 py-2 text-sm rounded-lg border border-amber-300 text-amber-700 hover:bg-amber-50 transition-colors"
        @click="openEnd"
      >
        {{ t('enrollment.end') }}
      </button>
      <button
        v-if="can('enrollment:manage_programs') && enrollment && !enrollment.endedOn"
        type="button"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        @click="openAssign"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('enrollment.assignProgram') }}
      </button>
    </PageHeader>

    <div v-if="store.loading && !enrollment" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-20 rounded-xl border border-border bg-card animate-pulse" />
    </div>

    <template v-else-if="enrollment">
      <div class="mb-6 grid grid-cols-2 md:grid-cols-4 gap-3">
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground mb-1">{{ t('enrollment.status') }}</p>
          <p class="text-sm font-medium">{{ enrollment.statusLabel ?? '—' }}</p>
        </div>
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground mb-1">{{ t('enrollment.campus') }}</p>
          <p class="text-sm font-medium">{{ enrollment.campusName ?? '—' }}</p>
        </div>
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground mb-1">{{ t('enrollment.enrolledOn') }}</p>
          <p class="text-sm font-medium">{{ formatDate(enrollment.enrolledOn) }}</p>
        </div>
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground mb-1">{{ t('enrollment.endedOn') }}</p>
          <p class="text-sm font-medium">{{ formatDate(enrollment.endedOn) }}</p>
        </div>
      </div>

      <p v-if="enrollment.terminationReason" class="mb-4 text-sm text-muted-foreground">
        {{ t('enrollment.terminationReason') }}: {{ enrollment.terminationReason }}
      </p>

      <div class="rounded-xl border border-border bg-card overflow-hidden">
        <div class="px-4 py-3 border-b border-border flex items-center justify-between">
          <h3 class="text-sm font-semibold">{{ t('enrollment.programs') }}</h3>
          <span class="text-xs text-muted-foreground">{{ enrollment.studentPrograms.length }}</span>
        </div>

        <div v-if="enrollment.studentPrograms.length === 0" class="text-center py-12 text-sm text-muted-foreground">
          {{ t('enrollment.noPrograms') }}
        </div>

        <div v-else class="divide-y divide-border">
          <div
            v-for="sp in enrollment.studentPrograms"
            :key="sp.id"
            class="flex items-center gap-3 px-4 py-3"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium truncate">{{ sp.programName ?? '—' }}</p>
              <p class="text-xs text-muted-foreground font-mono">
                {{ sp.programCode }}
                · {{ formatDate(sp.startDate) }}
                <span v-if="sp.endDate"> → {{ formatDate(sp.endDate) }}</span>
              </p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', spStatusClass(sp.status)]">
              {{ t(`enrollment.spStatus.${sp.status}`, sp.status) }}
            </span>
            <div v-if="can('enrollment:manage_programs')" class="flex items-center gap-1 shrink-0">
              <button
                type="button"
                class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
                :title="t('enrollment.editProgram')"
                @click="openEditSp(sp)"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button
                v-if="sp.status === 'active' || sp.status === 'paused'"
                type="button"
                class="p-1.5 rounded-lg hover:bg-blue-50 text-muted-foreground hover:text-blue-600"
                :title="t('enrollment.endProgram')"
                @click="openEndSp(sp)"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </button>
              <button
                type="button"
                class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
                :title="t('common.delete')"
                @click="removeTarget = sp"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>

    <FormModal :open="showStatus" :title="t('enrollment.changeStatus')" :saving="store.saving" @submit="submitStatus" @close="showStatus = false">
      <div class="space-y-3">
        <p v-if="statusError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ statusError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('enrollment.status') }}</label>
          <select v-model="statusForm.newStatusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option v-for="st in statuses" :key="st.id" :value="st.id">{{ st.label }}</option>
          </select>
        </div>
      </div>
    </FormModal>

    <FormModal :open="showEnd" :title="t('enrollment.end')" :saving="store.saving" @submit="submitEnd" @close="showEnd = false">
      <div class="space-y-3">
        <p v-if="endError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ endError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('enrollment.endedOn') }}</label>
          <input v-model="endForm.endedOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('enrollment.terminationReason') }}</label>
          <textarea v-model="endForm.terminationReason" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>

    <FormModal :open="showAssign" :title="t('enrollment.assignProgram')" :saving="store.saving" @submit="submitAssign" @close="showAssign = false">
      <div class="space-y-3">
        <p v-if="assignError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ assignError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('enrollment.program') }} *</label>
          <select v-model="assignForm.programId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="p in programs" :key="p.id" :value="p.id">{{ p.name }} ({{ p.code }})</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('enrollment.startDate') }}</label>
            <input v-model="assignForm.startDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('enrollment.endDate') }}</label>
            <input v-model="assignForm.endDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!removeTarget"
      :title="t('enrollment.removeProgramTitle')"
      :message="t('enrollment.removeProgramMessage', { name: removeTarget?.programName })"
      :confirm-label="t('common.delete')"
      :loading="removeLoading"
      @confirm="doRemove"
      @cancel="removeTarget = null"
    />

    <!-- Edit student-program (status + dates). Backend: UpdateStudentProgramCommand. -->
    <FormModal
      :open="showEditSp"
      :title="t('enrollment.editProgram')"
      :saving="store.saving"
      @submit="submitEditSp"
      @close="showEditSp = false"
    >
      <div class="space-y-3">
        <p v-if="editSpError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">
          {{ editSpError }}
        </p>
        <p v-if="editSpTarget" class="text-sm text-muted-foreground">
          {{ editSpTarget.programName }}
          <span v-if="editSpTarget.programCode" class="font-mono"> · {{ editSpTarget.programCode }}</span>
        </p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('enrollment.status') }}</label>
          <select
            v-model="editSpForm.status"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option v-for="s in SP_STATUSES" :key="s" :value="s">
              {{ t(`enrollment.spStatus.${s}`, s) }}
            </option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('enrollment.startDate') }}</label>
            <input
              v-model="editSpForm.startDate"
              type="date"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('enrollment.endDate') }}</label>
            <input
              v-model="editSpForm.endDate"
              type="date"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>
      </div>
    </FormModal>
  </div>
</template>
