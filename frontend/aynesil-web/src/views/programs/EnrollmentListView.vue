<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useProgramStore } from '@/stores/program.store'
import { useStudentStore } from '@/stores/student.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { campusService } from '@/services/campus.service'
import { enrollmentService } from '@/services/program.service'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { EnrollmentListItemDto, ProgramListItemDto } from '@/types/program.types'
import type { RefValueItem } from '@/stores/refdata.store'
import type { CampusListItemDto } from '@/types/campus.types'
import type { StudentListItemDto } from '@/types/student.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useProgramStore()
const studentStore = useStudentStore()
const refData = useRefDataStore()
const { can } = usePermission()

const statuses = ref<RefValueItem[]>([])
const campuses = ref<CampusListItemDto[]>([])
const students = ref<StudentListItemDto[]>([])
const programs = ref<ProgramListItemDto[]>([])

const query = reactive({
  campusId: '',
  statusId: '',
  studentId: (route.query.studentId as string) || '',
  isActive: undefined as boolean | undefined,
  page: 1,
  pageSize: 20,
  sortBy: 'enrolledOn',
  sortDirection: 'desc' as 'asc' | 'desc',
})

watch(
  () => [query.campusId, query.statusId, query.studentId, query.isActive, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('enrollment_status').then((v) => { statuses.value = v }),
    campusService.list({ page: 1, pageSize: 200 }).then((res) => {
      if (res.success && res.data) campuses.value = res.data.items
    }),
    studentStore.fetchList({ page: 1, pageSize: 200 }).then(() => {
      students.value = studentStore.list.items
    }),
    store.fetchPrograms({ isActive: true, page: 1, pageSize: 200 }).then(() => {
      programs.value = store.programList.items
    }),
  ])
})

async function loadList() {
  await store.fetchEnrollments({
    campusId: query.campusId || undefined,
    statusId: query.statusId || undefined,
    studentId: query.studentId || undefined,
    isActive: query.isActive,
    page: query.page,
    pageSize: query.pageSize,
    sortBy: query.sortBy,
    sortDirection: query.sortDirection,
  })
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<EnrollmentListItemDto>[] = [
  { key: 'studentFullName', label: t('enrollment.student'), sortable: true },
  { key: 'campusName', label: t('enrollment.campus'), width: '160px' },
  { key: 'statusLabel', label: t('enrollment.status'), width: '120px' },
  { key: 'enrolledOn', label: t('enrollment.enrolledOn'), sortable: true, width: '120px' },
  { key: 'endedOn', label: t('enrollment.endedOn'), width: '120px' },
  { key: 'programCount', label: t('enrollment.programCount'), width: '100px', align: 'center' },
]

function goDetail(row: EnrollmentListItemDto) {
  router.push({ name: 'enrollment-detail', params: { id: row.id } })
}

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

// ── Create modal ──────────────────────────────────────────────────────────────
const showCreate = ref(false)
const createForm = reactive({
  studentId: '',
  programId: '',
  campusId: '',
  statusId: '',
  enrolledOn: new Date().toISOString().slice(0, 10),
  startDate: new Date().toISOString().slice(0, 10),
})
const createError = ref('')

// A student normally has ONE open enrollment (the file at the institution).
// If the selected student already has one, the program is added to it
// instead of opening a second enrollment.
const existingEnrollment = ref<EnrollmentListItemDto | null>(null)
watch(() => createForm.studentId, async (studentId) => {
  existingEnrollment.value = null
  if (!studentId) return
  try {
    const res = await enrollmentService.listEnrollments({
      studentId,
      isActive: true,
      page: 1,
      pageSize: 1,
      sortBy: 'enrolledOn',
      sortDirection: 'desc',
    })
    if (createForm.studentId !== studentId) return
    existingEnrollment.value = res.success && res.data ? (res.data.items[0] ?? null) : null
  } catch {
    existingEnrollment.value = null
  }
})

function openCreate() {
  createForm.studentId = query.studentId || ''
  createForm.programId = ''
  createForm.campusId = ''
  createForm.statusId = statuses.value.find((s) => s.code === 'active')?.id
    ?? statuses.value.find((s) => s.isDefault)?.id
    ?? ''
  createForm.enrolledOn = new Date().toISOString().slice(0, 10)
  createForm.startDate = new Date().toISOString().slice(0, 10)
  createError.value = ''
  showCreate.value = true
}

async function submitCreate() {
  if (!createForm.studentId) {
    createError.value = t('validation.required', { field: t('enrollment.student') })
    return
  }
  if (!createForm.programId) {
    createError.value = t('validation.required', { field: t('enrollment.program') })
    return
  }
  createError.value = ''

  if (existingEnrollment.value) {
    const target = existingEnrollment.value
    try {
      await store.assignStudentToProgram({
        studentId: createForm.studentId,
        programId: createForm.programId,
        enrollmentId: target.id,
        campusId: target.campusId,
        startDate: createForm.startDate || null,
        endDate: null,
      })
      showCreate.value = false
      router.push({ name: 'enrollment-detail', params: { id: target.id } })
    } catch (e: unknown) {
      createError.value = (e as Error).message
    }
    return
  }

  try {
    const created = await store.createEnrollment({
      studentId: createForm.studentId,
      campusId: createForm.campusId || null,
      statusId: createForm.statusId || null,
      enrolledOn: createForm.enrolledOn || null,
    })
    // Enrollment is created in a separate request. If the program assign fails
    // the enrollment row still exists, so we surface the error and push the
    // user to the detail where they can retry the assignment instead of
    // silently swallowing the failure.
    let assignFailure: string | null = null
    try {
      await store.assignStudentToProgram({
        studentId: createForm.studentId,
        programId: createForm.programId,
        enrollmentId: created.id,
        campusId: createForm.campusId || null,
        startDate: createForm.startDate || createForm.enrolledOn || null,
        endDate: null,
      })
    } catch (assignErr: unknown) {
      assignFailure = (assignErr as Error).message
    }
    showCreate.value = false
    if (assignFailure) {
      // Keep the user on the list long enough to see the error. The detail is
      // still reachable from the row link if they want to retry.
      createError.value = t('enrollment.assignFailedAfterCreate', {
        error: assignFailure,
      })
      await loadList()
    } else {
      router.push({ name: 'enrollment-detail', params: { id: created.id } })
    }
  } catch (e: unknown) {
    createError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('enrollment.title')" :description="t('enrollment.description')">
      <button
        v-if="can('enrollment:create')"
        type="button"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
        @click="openCreate"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('enrollment.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <select
        v-model="query.studentId"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary min-w-[200px]"
        @change="query.page = 1"
      >
        <option value="">{{ t('enrollment.allStudents') }}</option>
        <option v-for="s in students" :key="s.id" :value="s.id">{{ s.fullName }}</option>
      </select>

      <select
        v-model="query.campusId"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        @change="query.page = 1"
      >
        <option value="">{{ t('common.allCampuses') }}</option>
        <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>

      <select
        v-model="query.statusId"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        @change="query.page = 1"
      >
        <option value="">{{ t('common.allStatuses') }}</option>
        <option v-for="st in statuses" :key="st.id" :value="st.id">{{ st.label }}</option>
      </select>

      <select
        v-model="query.isActive"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        @change="query.page = 1"
      >
        <option :value="undefined">{{ t('enrollment.activeFilter') }}: {{ t('common.allStatuses') }}</option>
        <option :value="true">{{ t('common.active') }}</option>
        <option :value="false">{{ t('enrollment.ended') }}</option>
      </select>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.enrollmentList.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="goDetail"
    >
      <template #cell-studentFullName="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-campusName="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-statusLabel="{ value }">
        <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700">
          {{ value ?? '—' }}
        </span>
      </template>
      <template #cell-enrolledOn="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #cell-endedOn="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #cell-programCount="{ value }">
        <span class="font-mono text-xs">{{ value }}</span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            type="button"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
            @click="goDetail(row)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.enrollmentList.page"
        :page-size="store.enrollmentList.pageSize"
        :total-count="store.enrollmentList.totalCount"
        :total-pages="store.enrollmentList.totalPages"
        :has-previous-page="store.enrollmentList.hasPreviousPage"
        :has-next-page="store.enrollmentList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <FormModal
      :open="showCreate"
      :title="t('enrollment.create')"
      :saving="store.saving"
      @submit="submitCreate"
      @close="showCreate = false"
    >
      <div class="space-y-4">
        <p v-if="createError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ createError }}</p>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('enrollment.student') }} *</label>
          <select
            v-model="createForm.studentId"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option value="">{{ t('common.select') }}</option>
            <option v-for="s in students" :key="s.id" :value="s.id">{{ s.fullName }}</option>
          </select>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('enrollment.program') }} *</label>
          <select
            v-model="createForm.programId"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option value="">{{ t('common.select') }}</option>
            <option v-for="p in programs" :key="p.id" :value="p.id">{{ p.name }} ({{ p.code }})</option>
          </select>
        </div>

        <p v-if="existingEnrollment" class="text-sm text-blue-700 bg-blue-50 rounded-lg px-3 py-2">
          {{ t('enrollment.existingEnrollmentNotice', { date: formatDate(existingEnrollment.enrolledOn) }) }}
        </p>

        <template v-if="!existingEnrollment">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('enrollment.campus') }}</label>
          <select
            v-model="createForm.campusId"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option value="">{{ t('common.none') }}</option>
            <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('enrollment.status') }}</label>
            <select
              v-model="createForm.statusId"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="st in statuses" :key="st.id" :value="st.id">{{ st.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('enrollment.enrolledOn') }}</label>
            <input
              v-model="createForm.enrolledOn"
              type="date"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>
        </template>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('enrollment.startDate') }}</label>
          <input
            v-model="createForm.startDate"
            type="date"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
      </div>
    </FormModal>
  </div>
</template>
