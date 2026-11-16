<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCampStore } from '@/stores/camp.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import { CampEnrollmentStatus } from '@/constants/statuses'
import { campService } from '@/services/camp.service'
import { studentService } from '@/services/student.service'
import { toLocalDateString } from '@/utils/datetime'
import { canBulkComplete, canWriteReport } from '@/utils/campRules'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { CampPeriodDto, CampReportDto, CampEnrollmentListItemDto } from '@/types/camp.types'
import type { StudentListItemDto } from '@/types/student.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const campStore = useCampStore()
const { visible } = useModule(Modules.Camps)
const periodId = route.params.periodId as string
const today = toLocalDateString()

const period = ref<CampPeriodDto | null>(null)
const actionError = ref('')
const notice = ref('')

const summary = computed(() => campStore.enrollmentSummary)
// Toplu tamamlama: başlamış dönemde ve kayıtlı öğrenci varken.
const bulkEnabled = computed(() => {
  const p = period.value
  const enrolled = summary.value?.totalEnrolled ?? p?.enrolledCount ?? 0
  return !!p && canBulkComplete({ startDate: p.startDate, enrolledCount: enrolled }, today)
})

function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }

async function run(fn: () => Promise<unknown>) {
  actionError.value = ''; notice.value = ''
  try { await fn() } catch (e: unknown) { actionError.value = (e as Error).message }
}

async function refreshPeriod() {
  const res = await campService.getPeriod(periodId)
  if (res.success && res.data) period.value = res.data
}

// ── Satır işlemleri ───────────────────────────────────────────────────────────
type RowAction = 'waitlist' | 'complete' | 'withdraw'
const rowAction = ref<{ action: RowAction; id: string; name: string } | null>(null)

async function confirmRowAction() {
  const a = rowAction.value
  rowAction.value = null
  if (!a) return
  await run(() => {
    if (a.action === 'waitlist') return campStore.moveToWaitlist(a.id, periodId)
    if (a.action === 'complete') return campStore.completeEnrollment(a.id, periodId)
    return campStore.withdraw(a.id, periodId)
  })
}

// ── Dönem sonu toplu tamamlama ────────────────────────────────────────────────
const bulkOpen = ref(false)
async function confirmBulk() {
  bulkOpen.value = false
  await run(async () => {
    const n = await campStore.bulkComplete(periodId)
    notice.value = n > 0 ? t('camp.enrollment.bulkCompleteDone', { count: n }) : t('camp.enrollment.bulkCompleteNone')
  })
}

// ── Kayıt ─────────────────────────────────────────────────────────────────────
const enrollOpen = ref(false)
const enrollError = ref('')
const students = ref<StudentListItemDto[]>([])
const enrollForm = reactive({ studentId: '', status: 'enrolled' })

async function openEnroll() {
  enrollError.value = ''
  Object.assign(enrollForm, { studentId: '', status: 'enrolled' })
  enrollOpen.value = true
  try {
    const res = await studentService.listStudents({ page: 1, pageSize: 100 })
    students.value = res.data?.items ?? []
  } catch (e: unknown) { enrollError.value = (e as Error).message }
}

async function doEnroll() {
  if (!enrollForm.studentId) return
  enrollError.value = ''
  try {
    await campStore.enrollStudent(periodId, { studentId: enrollForm.studentId, status: enrollForm.status })
    await Promise.all([campStore.fetchEnrollmentSummary(periodId), refreshPeriod()])
    enrollOpen.value = false
  } catch (e: unknown) { enrollError.value = (e as Error).message }
}

// ── Kamp sonu raporu (yalnızca tamamlanmış kayıt) ─────────────────────────────
const reportFor = ref<CampEnrollmentListItemDto | null>(null)
const reports = ref<CampReportDto[]>([])
const reportsLoading = ref(false)
const reportError = ref('')
const reportSummary = ref('')

async function openReports(e: CampEnrollmentListItemDto) {
  reportFor.value = e
  reportError.value = ''; reportSummary.value = ''; reports.value = []
  reportsLoading.value = true
  try {
    const res = await campService.listCampReports(e.id)
    reports.value = res.data ?? []
  } catch (err: unknown) { reportError.value = (err as Error).message }
  finally { reportsLoading.value = false }
}

async function saveReport() {
  const e = reportFor.value
  if (!e) return
  reportError.value = ''
  if (!reportSummary.value.trim()) {
    reportError.value = t('validation.required', { field: t('camp.report.summary') })
    return
  }
  try {
    await campStore.createReport(e.id, { summary: reportSummary.value.trim() })
    reportSummary.value = ''
    notice.value = t('camp.report.saved')
    reports.value = (await campService.listCampReports(e.id)).data ?? []
  } catch (err: unknown) { reportError.value = (err as Error).message }
}

function formatDateTime(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

onMounted(async () => {
  await Promise.all([
    campStore.fetchEnrollments(periodId),
    campStore.fetchEnrollmentSummary(periodId),
    refreshPeriod().catch((e: unknown) => { actionError.value = (e as Error).message }),
  ])
})

const btn = 'text-xs hover:underline'
</script>

<template>
  <div>
    <PageHeader
      :title="period ? `${t('camp.enrollment.title')} · ${period.name}` : t('camp.enrollment.title')"
      :description="period ? `${period.startDate} – ${period.endDate}` : t('camp.enrollment.subtitle')"
    >
      <button type="button" class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="period ? router.push({ name: 'camp-detail', params: { id: period.campId } }) : router.back()">
        {{ t('common.back') }}
      </button>
      <button
        v-if="visible(Permissions.CampEnrollments.Complete)" type="button"
        class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent disabled:opacity-50"
        :disabled="!bulkEnabled"
        @click="bulkOpen = true"
      >{{ t('camp.enrollment.bulkComplete') }}</button>
      <button
        v-if="visible(Permissions.CampEnrollments.Enroll)" type="button"
        class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
        @click="openEnroll"
      >{{ t('camp.enrollment.enroll') }}</button>
    </PageHeader>

    <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ actionError }}</p>
    <p v-if="notice" class="mb-4 text-sm text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2">{{ notice }}</p>

    <div v-if="summary" class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold text-primary">{{ summary.totalEnrolled }}</div>
        <div class="text-xs text-muted-foreground">{{ t('statusBadge.campEnrollment.enrolled') }}</div>
      </div>
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold text-amber-600">{{ summary.totalWaitlist }}</div>
        <div class="text-xs text-muted-foreground">{{ t('statusBadge.campEnrollment.waitlist') }}</div>
      </div>
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold text-emerald-700">{{ summary.totalCompleted }}</div>
        <div class="text-xs text-muted-foreground">{{ t('statusBadge.campEnrollment.completed') }}</div>
      </div>
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold text-red-600">{{ summary.totalWithdrawn }}</div>
        <div class="text-xs text-muted-foreground">{{ t('statusBadge.campEnrollment.withdrawn') }}</div>
      </div>
    </div>

    <div class="rounded-xl border border-border bg-card overflow-x-auto">
      <div v-if="campStore.loading && campStore.enrollments.items.length === 0" class="py-12 text-center text-muted-foreground">{{ t('common.loading') }}</div>
      <p v-else-if="campStore.enrollments.items.length === 0" class="py-12 text-center text-sm text-muted-foreground">{{ t('camp.enrollment.noData') }}</p>
      <table v-else class="w-full text-sm">
        <thead>
          <tr class="text-left text-xs text-muted-foreground border-b border-border">
            <th class="py-3 px-4">{{ t('camp.fields.student') }}</th>
            <th class="px-4">{{ t('common.status') }}</th>
            <th class="px-4">{{ t('camp.enrollment.enrolledAt') }}</th>
            <th class="px-4 text-right">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in campStore.enrollments.items" :key="e.id" class="border-b border-border/60">
            <td class="py-3 px-4 font-medium">{{ e.studentName ?? e.studentId }}</td>
            <td class="px-4"><StatusBadge entity="campEnrollment" :status="e.status" /></td>
            <td class="px-4 text-muted-foreground">{{ formatDate(e.enrolledAt) }}</td>
            <td class="px-4">
              <div class="flex items-center justify-end gap-3">
                <button
                  v-if="canWriteReport(e.status) && visible(Permissions.CampReports.Read)"
                  type="button" :class="[btn, 'text-primary']"
                  @click="openReports(e)"
                >{{ t('camp.enrollment.report') }}</button>
                <button
                  v-if="e.status === CampEnrollmentStatus.Waitlist && visible(Permissions.CampEnrollments.Manage)"
                  type="button" :class="[btn, 'text-emerald-700']"
                  @click="run(() => campStore.promoteFromWaitlist(e.id, periodId))"
                >{{ t('camp.enrollment.promote') }}</button>
                <button
                  v-if="e.status === CampEnrollmentStatus.Enrolled && visible(Permissions.CampEnrollments.Manage)"
                  type="button" :class="[btn, 'text-amber-600']"
                  @click="rowAction = { action: 'waitlist', id: e.id, name: e.studentName ?? '' }"
                >{{ t('camp.enrollment.toWaitlist') }}</button>
                <button
                  v-if="e.status === CampEnrollmentStatus.Enrolled && visible(Permissions.CampEnrollments.Complete)"
                  type="button" :class="[btn, 'text-primary']"
                  @click="rowAction = { action: 'complete', id: e.id, name: e.studentName ?? '' }"
                >{{ t('camp.enrollment.complete') }}</button>
                <button
                  v-if="(e.status === CampEnrollmentStatus.Enrolled || e.status === CampEnrollmentStatus.Waitlist) && visible(Permissions.CampEnrollments.Withdraw)"
                  type="button" :class="[btn, 'text-red-600']"
                  @click="rowAction = { action: 'withdraw', id: e.id, name: e.studentName ?? '' }"
                >{{ t('camp.enrollment.withdraw') }}</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <FormModal
      :open="enrollOpen"
      :title="t('camp.enrollment.enroll')"
      :saving="campStore.saving"
      @submit="doEnroll"
      @close="enrollOpen = false"
    >
      <p v-if="enrollError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ enrollError }}</p>
      <div class="space-y-3">
        <div>
          <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('camp.enrollment.selectStudent') }} *</label>
          <select v-model="enrollForm.studentId" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">—</option>
            <option v-for="s in students" :key="s.id" :value="s.id">{{ s.fullName }}</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('camp.enrollment.enrollAs') }}</label>
          <select v-model="enrollForm.status" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="enrolled">{{ t('camp.enrollment.status.enrolled') }}</option>
            <option value="waitlist">{{ t('camp.enrollment.status.waitlist') }}</option>
          </select>
        </div>
      </div>
    </FormModal>

    <FormModal
      :open="!!reportFor"
      :title="t('camp.report.title')"
      :subtitle="reportFor ? t('camp.report.forStudent', { name: reportFor.studentName ?? '' }) : ''"
      :saving="campStore.saving"
      wide
      @submit="saveReport"
      @close="reportFor = null"
    >
      <p v-if="reportError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ reportError }}</p>
      <div v-if="reportsLoading" class="py-6 text-center text-muted-foreground">{{ t('common.loading') }}</div>
      <p v-else-if="reports.length === 0" class="py-4 text-center text-sm text-muted-foreground">{{ t('camp.report.noData') }}</p>
      <ul v-else class="space-y-3 mb-4">
        <li v-for="r in reports" :key="r.id" class="rounded-lg border border-border p-3">
          <div class="text-xs text-muted-foreground mb-1">{{ t('camp.report.authoredAt') }}: {{ formatDateTime(r.createdAt) }}</div>
          <p class="text-sm whitespace-pre-line">{{ r.summary ?? '—' }}</p>
        </li>
      </ul>
      <div v-if="reportFor && canWriteReport(reportFor.status) && visible(Permissions.CampReports.Create)">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('camp.report.summary') }} *</label>
        <textarea
          v-model="reportSummary" rows="5" :placeholder="t('camp.report.summaryHint')"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!rowAction"
      :title="rowAction ? t(`camp.enrollment.confirm.${rowAction.action}Title`) : ''"
      :message="rowAction ? t(`camp.enrollment.confirm.${rowAction.action}Message`, { name: rowAction.name }) : ''"
      :confirm-label="rowAction ? t(rowAction.action === 'waitlist' ? 'camp.enrollment.toWaitlist' : rowAction.action === 'complete' ? 'camp.enrollment.complete' : 'camp.enrollment.withdraw') : ''"
      :confirm-class="rowAction?.action === 'withdraw' ? undefined : 'bg-primary text-primary-foreground hover:opacity-90'"
      :loading="campStore.saving"
      @confirm="confirmRowAction"
      @cancel="rowAction = null"
    />
    <ConfirmModal
      :open="bulkOpen"
      :title="t('camp.enrollment.bulkCompleteTitle')"
      :message="t('camp.enrollment.bulkCompleteMessage')"
      :confirm-label="t('camp.enrollment.bulkComplete')"
      confirm-class="bg-primary text-primary-foreground hover:opacity-90"
      :loading="campStore.saving"
      @confirm="confirmBulk"
      @cancel="bulkOpen = false"
    />
  </div>
</template>
