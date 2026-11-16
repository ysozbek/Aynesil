<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCampStore } from '@/stores/camp.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import { campService } from '@/services/camp.service'
import { toLocalDateString, localInputToIso, isoToLocalInput } from '@/utils/datetime'
import {
  CampAttendanceStatuses, CampParticipationStatuses, canRecordForEnrollment, planBulkSave,
  periodPhase,
} from '@/utils/campRules'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type {
  CampPeriodDto, CampEnrollmentListItemDto, CampActivityListItemDto,
  CampAttendanceDto, CampActivityParticipationDto,
} from '@/types/camp.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const campStore = useCampStore()
const { visible } = useModule(Modules.Camps)
const periodId = route.params.periodId as string
const today = toLocalDateString()

const period = ref<CampPeriodDto | null>(null)
const enrollments = ref<CampEnrollmentListItemDto[]>([])
const loaded = ref(false)
const actionError = ref('')
const notice = ref('')
const tab = ref<'activities' | 'attendance' | 'summary'>('activities')

const inputCls = 'w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
const labelCls = 'block text-xs font-medium text-muted-foreground mb-1'

const nameOf = (enrollmentId: string) =>
  enrollments.value.find((e) => e.id === enrollmentId)?.studentName ?? enrollmentId
/** Waitlist satırı yoklama/katılım listesinde gösterilmez; tamamlanmış/iptal satırlar yalnızca okunur. */
const rosterRows = computed(() => enrollments.value.filter((e) => e.status !== 'waitlist'))
const enrolledCount = computed(() => enrollments.value.filter((e) => canRecordForEnrollment(e.status)).length)

async function run(fn: () => Promise<unknown>) {
  actionError.value = ''; notice.value = ''
  try { await fn(); return true } catch (e: unknown) { actionError.value = (e as Error).message; return false }
}

async function loadEnrollments() {
  const res = await campService.listEnrollments(periodId, { pageSize: 200 })
  enrollments.value = res.data?.items ?? []
}

// ── Etkinlik: oluştur / düzenle / sil ─────────────────────────────────────────
const activityModal = ref(false)
const activityError = ref('')
const editingActivity = ref<{ id: string; rowVersion: number } | null>(null)
const activityForm = reactive({
  name: '', description: '', startsAt: '', endsAt: '', location: '', capacity: undefined as number | undefined,
})

async function openActivity(a?: CampActivityListItemDto) {
  activityError.value = ''
  editingActivity.value = null
  Object.assign(activityForm, { name: '', description: '', startsAt: '', endsAt: '', location: '', capacity: undefined })
  activityModal.value = true
  if (!a) return
  try {
    const res = await campService.getActivity(a.id)
    const d = res.data
    if (!d) return
    editingActivity.value = { id: d.id, rowVersion: d.rowVersion }
    Object.assign(activityForm, {
      name: d.name, description: d.description ?? '', startsAt: isoToLocalInput(d.startsAt), endsAt: isoToLocalInput(d.endsAt),
      location: d.location ?? '', capacity: d.capacity ?? undefined,
    })
  } catch (e: unknown) { activityError.value = (e as Error).message }
}

async function saveActivity() {
  activityError.value = ''
  if (!activityForm.name.trim()) { activityError.value = t('validation.required', { field: t('camp.activity.fields.name') }); return }
  const payload = {
    name: activityForm.name.trim(),
    description: activityForm.description.trim() || undefined,
    startsAt: activityForm.startsAt ? localInputToIso(activityForm.startsAt) : undefined,
    endsAt: activityForm.endsAt ? localInputToIso(activityForm.endsAt) : undefined,
    location: activityForm.location.trim() || undefined,
    capacity: activityForm.capacity || undefined,
  }
  try {
    if (editingActivity.value) {
      await campStore.updateActivity(periodId, editingActivity.value.id, { ...payload, rowVersion: editingActivity.value.rowVersion })
    } else {
      await campStore.createActivity(periodId, payload)
    }
    activityModal.value = false
  } catch (e: unknown) { activityError.value = (e as Error).message }
}

const activityToDelete = ref<CampActivityListItemDto | null>(null)
async function confirmDeleteActivity() {
  const a = activityToDelete.value
  activityToDelete.value = null
  if (a) await run(() => campStore.deleteActivity(periodId, a.id))
}

// ── Katılım: tekil + toplu + düzeltme ─────────────────────────────────────────
const participationActivity = ref<CampActivityListItemDto | null>(null)
const participationError = ref('')
const participationDraft = reactive<Record<string, string>>({})
const participationExisting = ref(new Map<string, { id: string; status: string }>())

async function loadParticipations(activityId: string) {
  const res = await campService.listParticipations(activityId, { pageSize: 200 })
  const map = new Map<string, { id: string; status: string }>()
  for (const p of (res.data?.items ?? []) as CampActivityParticipationDto[]) map.set(p.campEnrollmentId, { id: p.id, status: p.status })
  participationExisting.value = map
  for (const k of Object.keys(participationDraft)) delete participationDraft[k]
  for (const e of rosterRows.value) participationDraft[e.id] = map.get(e.id)?.status ?? ''
}

async function openParticipation(a: CampActivityListItemDto) {
  participationError.value = ''
  participationActivity.value = a
  try { await loadParticipations(a.id) } catch (e: unknown) { participationError.value = (e as Error).message }
}

function markAllAttended() {
  for (const e of rosterRows.value)
    if (canRecordForEnrollment(e.status) && !participationExisting.value.has(e.id)) participationDraft[e.id] = 'attended'
}

/** Kayıtlı öğrenciler için taslağın mevcut kayıtlardan farklı olan satırları. */
function participationPlan(onlyId?: string) {
  const rows = rosterRows.value
    .filter((e) => canRecordForEnrollment(e.status) && (!onlyId || e.id === onlyId))
    .map((e) => ({ enrollmentId: e.id, status: participationDraft[e.id] ?? '' }))
  return planBulkSave(rows, participationExisting.value)
}
const participationDirty = computed(() => {
  const p = participationPlan()
  return p.create.length + p.update.length > 0
})
const rowDirty = (id: string) => {
  const p = participationPlan(id)
  return p.create.length + p.update.length > 0
}

async function saveParticipation(onlyId?: string) {
  const a = participationActivity.value
  if (!a) return
  participationError.value = ''
  const plan = participationPlan(onlyId)
  if (plan.create.length + plan.update.length === 0) { participationError.value = t('camp.participation.nothingToSave'); return }
  try {
    let done = 0
    // Tek satır: tekil uç; birden çok satır: toplu uç. Düzeltmeler tekil PATCH.
    if (plan.create.length === 1 && onlyId) {
      await campStore.registerParticipation(a.id, plan.create[0]!)
      done += 1
    } else if (plan.create.length > 0) {
      done += await campStore.bulkRegisterParticipation(a.id, plan.create)
    }
    for (const u of plan.update) { await campStore.updateParticipation(u.id, u.status); done += 1 }
    notice.value = t('camp.participation.saved', { count: done })
    await Promise.all([loadParticipations(a.id), campStore.fetchActivities(periodId)])
  } catch (e: unknown) {
    participationError.value = (e as Error).message
    try { await loadParticipations(a.id) } catch { /* ilk hata gösterilir */ }
  }
}

// ── Günlük yoklama: toplu + düzeltme ──────────────────────────────────────────
const attendanceDate = ref(today)
const attendanceDraft = reactive<Record<string, string>>({})
const attendanceExisting = ref(new Map<string, { id: string; status: string }>())
const attendanceLoading = ref(false)

function clampDate(d: string, p: CampPeriodDto) {
  return d < p.startDate ? p.startDate : d > p.endDate ? p.endDate : d
}

async function loadAttendance() {
  attendanceLoading.value = true
  try {
    const res = await campService.listAttendance({
      campPeriodId: periodId, dateFrom: attendanceDate.value, dateTo: attendanceDate.value, pageSize: 200,
    })
    const map = new Map<string, { id: string; status: string }>()
    for (const a of (res.data?.items ?? []) as CampAttendanceDto[]) map.set(a.campEnrollmentId, { id: a.id, status: a.status })
    attendanceExisting.value = map
    for (const k of Object.keys(attendanceDraft)) delete attendanceDraft[k]
    for (const e of rosterRows.value) attendanceDraft[e.id] = map.get(e.id)?.status ?? ''
  } catch (e: unknown) { actionError.value = (e as Error).message }
  finally { attendanceLoading.value = false }
}

function markAllPresent() {
  for (const e of rosterRows.value)
    if (canRecordForEnrollment(e.status) && !attendanceExisting.value.has(e.id)) attendanceDraft[e.id] = 'present'
}

const attendancePlan = computed(() => planBulkSave(
  rosterRows.value
    .filter((e) => canRecordForEnrollment(e.status))
    .map((e) => ({ enrollmentId: e.id, status: attendanceDraft[e.id] ?? '' })),
  attendanceExisting.value,
))
const attendanceDirty = computed(() => attendancePlan.value.create.length + attendancePlan.value.update.length > 0)

async function saveAttendance() {
  const plan = attendancePlan.value
  if (!attendanceDirty.value) { actionError.value = t('camp.attendance.nothingToSave'); return }
  const ok = await run(async () => {
    let added = 0
    if (plan.create.length > 0) {
      added = await campStore.bulkRecordAttendance(
        periodId, attendanceDate.value,
        plan.create.map((r) => ({ enrollmentId: r.enrollmentId, status: r.status })),
      )
    }
    for (const u of plan.update) await campStore.updateAttendance(u.id, u.status)
    notice.value = t('camp.attendance.saved', { added, corrected: plan.update.length })
  })
  await loadAttendance()
  if (ok) await campStore.fetchAttendanceSummary(periodId)
}

async function openTab(next: 'activities' | 'attendance' | 'summary') {
  tab.value = next
  if (next === 'attendance') await loadAttendance()
  if (next === 'summary') await campStore.fetchAttendanceSummary(periodId)
}

onMounted(async () => {
  const ok = await run(async () => {
    const res = await campService.getPeriod(periodId)
    if (res.data) {
      period.value = res.data
      attendanceDate.value = clampDate(today, res.data)
    }
    await Promise.all([loadEnrollments(), campStore.fetchActivities(periodId)])
  })
  loaded.value = ok || !!period.value
})
</script>

<template>
  <div>
    <div v-if="!loaded" class="py-20 text-center text-muted-foreground">{{ t('common.loading') }}</div>
    <div v-else-if="!period" class="py-20 text-center text-muted-foreground">{{ t('camp.periodView.notFound') }}</div>

    <template v-else>
      <PageHeader
        :title="`${t('camp.periodView.title')} · ${period.name}`"
        :description="`${period.startDate} – ${period.endDate} · ${t(`camp.period.phase.${periodPhase(period, today)}`)}`"
      >
        <button type="button" class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="router.push({ name: 'camp-detail', params: { id: period.campId } })">
          {{ t('common.back') }}
        </button>
        <button
          v-if="visible(Permissions.CampEnrollments.Read)" type="button"
          class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          @click="router.push({ name: 'camp-enrollment', params: { periodId } })"
        >{{ t('camp.detail.enrollments') }}</button>
      </PageHeader>

      <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ actionError }}</p>
      <p v-if="notice" class="mb-4 text-sm text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2">{{ notice }}</p>
      <p v-if="enrolledCount === 0" class="mb-4 text-sm text-amber-700 bg-amber-50 rounded-lg px-3 py-2">{{ t('camp.periodView.noEnrolled') }}</p>

      <div class="flex gap-1 border-b border-border mb-5">
        <button
          type="button" :class="['px-4 py-2 text-sm -mb-px border-b-2', tab === 'activities' ? 'border-primary text-primary font-medium' : 'border-transparent text-muted-foreground']"
          @click="openTab('activities')"
        >{{ t('camp.periodView.tabs.activities') }}</button>
        <button
          v-if="visible(Permissions.CampAttendance.Read)" type="button"
          :class="['px-4 py-2 text-sm -mb-px border-b-2', tab === 'attendance' ? 'border-primary text-primary font-medium' : 'border-transparent text-muted-foreground']"
          @click="openTab('attendance')"
        >{{ t('camp.periodView.tabs.attendance') }}</button>
        <button
          v-if="visible(Permissions.CampAttendance.Read)" type="button"
          :class="['px-4 py-2 text-sm -mb-px border-b-2', tab === 'summary' ? 'border-primary text-primary font-medium' : 'border-transparent text-muted-foreground']"
          @click="openTab('summary')"
        >{{ t('camp.periodView.tabs.summary') }}</button>
      </div>

      <!-- Etkinlikler -->
      <div v-if="tab === 'activities'" class="rounded-xl border border-border bg-card p-5">
        <div class="flex items-center justify-between mb-3">
          <h3 class="font-semibold">{{ t('camp.periodView.tabs.activities') }}</h3>
          <button
            v-if="visible(Permissions.CampActivities.Create)" type="button"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
            @click="openActivity()"
          >{{ t('camp.activity.add') }}</button>
        </div>
        <p v-if="campStore.activities.length === 0" class="py-8 text-center text-sm text-muted-foreground">{{ t('camp.activity.noData') }}</p>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-left text-xs text-muted-foreground border-b border-border">
                <th class="py-2 pr-3">{{ t('camp.activity.fields.name') }}</th>
                <th class="pr-3">{{ t('camp.activity.fields.startsAt') }}</th>
                <th class="pr-3">{{ t('camp.activity.fields.location') }}</th>
                <th class="pr-3 text-center">{{ t('camp.activity.participants') }}</th>
                <th class="text-right">{{ t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="a in campStore.activities" :key="a.id" class="border-b border-border/60">
                <td class="py-2 pr-3 font-medium">{{ a.name }}</td>
                <td class="pr-3 text-muted-foreground">{{ a.startsAt ? new Date(a.startsAt).toLocaleString('tr-TR') : '—' }}</td>
                <td class="pr-3 text-muted-foreground">{{ a.location ?? '—' }}</td>
                <td class="pr-3 text-center">{{ a.participationCount }}<span v-if="a.capacity"> / {{ a.capacity }}</span></td>
                <td class="text-right whitespace-nowrap">
                  <div class="flex items-center justify-end gap-3">
                    <button v-if="visible(Permissions.CampParticipation.Read)" type="button" class="text-xs text-primary hover:underline" @click="openParticipation(a)">{{ t('camp.activity.participation') }}</button>
                    <button v-if="visible(Permissions.CampActivities.Update)" type="button" class="text-xs text-primary hover:underline" @click="openActivity(a)">{{ t('common.edit') }}</button>
                    <button v-if="visible(Permissions.CampActivities.Delete)" type="button" class="text-xs text-red-600 hover:underline" @click="activityToDelete = a">{{ t('common.delete') }}</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Günlük yoklama -->
      <div v-else-if="tab === 'attendance'" class="rounded-xl border border-border bg-card p-5">
        <div class="flex flex-wrap items-end gap-3 mb-4">
          <div>
            <label :class="labelCls">{{ t('camp.attendance.date') }}</label>
            <input v-model="attendanceDate" type="date" :min="period.startDate" :max="period.endDate" :class="inputCls" @change="loadAttendance" />
          </div>
          <button
            v-if="visible(Permissions.CampAttendance.Record)" type="button"
            class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent"
            @click="markAllPresent"
          >{{ t('camp.attendance.markAllPresent') }}</button>
          <button
            v-if="visible(Permissions.CampAttendance.Record)" type="button"
            class="h-9 px-3 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50"
            :disabled="!attendanceDirty || campStore.saving"
            @click="saveAttendance"
          >{{ t('camp.attendance.save') }}</button>
        </div>
        <div v-if="attendanceLoading" class="py-8 text-center text-muted-foreground">{{ t('common.loading') }}</div>
        <p v-else-if="rosterRows.length === 0" class="py-8 text-center text-sm text-muted-foreground">{{ t('camp.enrollment.noData') }}</p>
        <table v-else class="w-full text-sm">
          <thead>
            <tr class="text-left text-xs text-muted-foreground border-b border-border">
              <th class="py-2 pr-3">{{ t('camp.fields.student') }}</th>
              <th class="pr-3">{{ t('common.status') }}</th>
              <th class="text-right">{{ t('camp.periodView.tabs.attendance') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="e in rosterRows" :key="e.id" class="border-b border-border/60">
              <td class="py-2 pr-3 font-medium">{{ e.studentName ?? e.studentId }}</td>
              <td class="pr-3"><StatusBadge entity="campEnrollment" :status="e.status" /></td>
              <td class="text-right">
                <select
                  v-model="attendanceDraft[e.id]"
                  :disabled="!canRecordForEnrollment(e.status) || !visible(Permissions.CampAttendance.Record)"
                  class="h-8 px-2 text-sm rounded-lg border border-border bg-transparent disabled:opacity-60"
                >
                  <option value="">{{ t('camp.attendance.notRecorded') }}</option>
                  <option v-for="s in CampAttendanceStatuses" :key="s" :value="s">{{ t(`camp.attendance.status.${s}`) }}</option>
                </select>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Devam özeti -->
      <div v-else class="rounded-xl border border-border bg-card p-5 overflow-x-auto">
        <p v-if="campStore.attendanceSummary.length === 0" class="py-8 text-center text-sm text-muted-foreground">{{ t('camp.attendance.summary.noData') }}</p>
        <table v-else class="w-full text-sm">
          <thead>
            <tr class="text-left text-xs text-muted-foreground border-b border-border">
              <th class="py-2 pr-3">{{ t('camp.fields.student') }}</th>
              <th class="pr-3 text-center">{{ t('camp.attendance.summary.days') }}</th>
              <th class="pr-3 text-center">{{ t('camp.attendance.summary.present') }}</th>
              <th class="pr-3 text-center">{{ t('camp.attendance.summary.absent') }}</th>
              <th class="pr-3 text-center">{{ t('camp.attendance.summary.late') }}</th>
              <th class="pr-3 text-center">{{ t('camp.attendance.summary.excused') }}</th>
              <th class="text-right">{{ t('camp.attendance.summary.rate') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in campStore.attendanceSummary" :key="r.enrollmentId" class="border-b border-border/60">
              <td class="py-2 pr-3 font-medium">{{ nameOf(r.enrollmentId) }}</td>
              <td class="pr-3 text-center">{{ r.totalDays }}</td>
              <td class="pr-3 text-center text-emerald-700">{{ r.present }}</td>
              <td class="pr-3 text-center text-red-600">{{ r.absent }}</td>
              <td class="pr-3 text-center text-amber-600">{{ r.late }}</td>
              <td class="pr-3 text-center">{{ r.excused }}</td>
              <td class="text-right font-semibold">{{ r.attendanceRatePct }}%</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- Etkinlik ekle / düzenle -->
    <FormModal
      :open="activityModal"
      :title="editingActivity ? t('camp.activity.edit') : t('camp.activity.add')"
      :saving="campStore.saving"
      @submit="saveActivity"
      @close="activityModal = false"
    >
      <p v-if="activityError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ activityError }}</p>
      <div class="grid grid-cols-2 gap-3">
        <div class="col-span-2">
          <label :class="labelCls">{{ t('camp.activity.fields.name') }} *</label>
          <input v-model="activityForm.name" type="text" maxlength="200" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.activity.fields.startsAt') }}</label>
          <input v-model="activityForm.startsAt" type="datetime-local" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.activity.fields.endsAt') }}</label>
          <input v-model="activityForm.endsAt" type="datetime-local" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.activity.fields.location') }}</label>
          <input v-model="activityForm.location" type="text" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.activity.fields.capacity') }}</label>
          <input v-model.number="activityForm.capacity" type="number" min="1" :class="inputCls" />
        </div>
        <div class="col-span-2">
          <label :class="labelCls">{{ t('camp.activity.fields.description') }}</label>
          <textarea v-model="activityForm.description" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <!-- Katılım -->
    <FormModal
      :open="!!participationActivity"
      :title="`${t('camp.participation.title')} · ${participationActivity?.name ?? ''}`"
      :saving="campStore.saving"
      wide
      @submit="saveParticipation()"
      @close="participationActivity = null"
    >
      <p v-if="participationError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ participationError }}</p>
      <div v-if="visible(Permissions.CampParticipation.Record)" class="flex flex-wrap gap-2 mb-3">
        <button type="button" class="h-8 px-3 text-xs rounded-lg border border-border hover:bg-accent" @click="markAllAttended">{{ t('camp.participation.markAllAttended') }}</button>
        <button type="button" class="h-8 px-3 text-xs rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50" :disabled="!participationDirty || campStore.saving" @click="saveParticipation()">{{ t('camp.participation.saveAll') }}</button>
      </div>
      <p v-if="rosterRows.length === 0" class="py-6 text-center text-sm text-muted-foreground">{{ t('camp.enrollment.noData') }}</p>
      <table v-else class="w-full text-sm">
        <tbody>
          <tr v-for="e in rosterRows" :key="e.id" class="border-b border-border/60">
            <td class="py-2 pr-3 font-medium">{{ e.studentName ?? e.studentId }}</td>
            <td class="pr-3"><StatusBadge v-if="!canRecordForEnrollment(e.status)" entity="campEnrollment" :status="e.status" /></td>
            <td class="text-right whitespace-nowrap">
              <select
                v-model="participationDraft[e.id]"
                :disabled="!canRecordForEnrollment(e.status) || !visible(Permissions.CampParticipation.Record)"
                class="h-8 px-2 text-sm rounded-lg border border-border bg-transparent disabled:opacity-60"
              >
                <option value="">{{ t('camp.participation.notRecorded') }}</option>
                <option v-for="s in CampParticipationStatuses" :key="s" :value="s">{{ t(`camp.participation.status.${s}`) }}</option>
              </select>
              <button
                v-if="visible(Permissions.CampParticipation.Record) && canRecordForEnrollment(e.status)" type="button"
                class="ml-2 text-xs text-primary hover:underline disabled:opacity-40 disabled:no-underline"
                :disabled="!rowDirty(e.id) || campStore.saving"
                @click="saveParticipation(e.id)"
              >{{ t('camp.participation.save') }}</button>
            </td>
          </tr>
        </tbody>
      </table>
    </FormModal>

    <ConfirmModal
      :open="!!activityToDelete"
      :title="t('camp.activity.deleteTitle')"
      :message="t('camp.activity.deleteMessage', { name: activityToDelete?.name ?? '' })"
      :confirm-label="t('common.delete')"
      :loading="campStore.saving"
      @confirm="confirmDeleteActivity"
      @cancel="activityToDelete = null"
    />
  </div>
</template>
