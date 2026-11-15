<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSchedulingStore } from '@/stores/scheduling.store'
import { campusService } from '@/services/campus.service'
import { schedulingService } from '@/services/scheduling.service'
import { usePermission } from '@/composables/usePermission'
import { Permissions } from '@/constants/permissions'
import { startOfDayIso, localInputToIso, toLocalDateString } from '@/utils/datetime'
import DataTable from '@/components/shared/DataTable.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { CalendarEntryDto } from '@/types/scheduling.types'
import type { CampusListItemDto } from '@/types/campus.types'

const { t } = useI18n()
const store = useSchedulingStore()
const { can } = usePermission()

const ENTRY_TYPES = ['holiday', 'closure', 'term_break', 'event'] as const

const year = ref(new Date().getFullYear())
const campusFilter = ref('')
const campuses = ref<CampusListItemDto[]>([])
const loadError = ref('')

async function load() {
  loadError.value = ''
  try {
    await store.fetchCalendarEntries(
      campusFilter.value || undefined,
      startOfDayIso(`${year.value}-01-01`),
      startOfDayIso(`${year.value + 1}-01-01`),
    )
  } catch (e: unknown) {
    loadError.value = (e as Error).message
  }
}

watch([year, campusFilter], load)
onMounted(async () => {
  load()
  const res = await campusService.list({ page: 1, pageSize: 200 })
  if (res.success && res.data) campuses.value = res.data.items
})

const campusName = (id: string | null) => (id ? campuses.value.find((c) => c.id === id)?.name ?? '—' : t('scheduling.calendarEntries.wholeCorporation'))

const columns: Column<CalendarEntryDto>[] = [
  { key: 'title', label: t('scheduling.calendarEntries.titleField') },
  { key: 'entryType', label: t('scheduling.calendarEntries.type'), width: '140px' },
  { key: 'startsAt', label: t('scheduling.calendarEntries.dates'), width: '230px' },
  { key: 'campusId', label: t('campus.name'), width: '160px' },
]

/** Tüm gün kayıtlarında bitiş, sonraki günün 00:00'ıdır; gösterimde bir gün geri alınır. */
function describeRange(e: CalendarEntryDto): string {
  const s = new Date(e.startsAt)
  const en = new Date(e.endsAt)
  if (e.isAllDay) {
    const last = new Date(en.getTime() - 1)
    const a = s.toLocaleDateString('tr-TR')
    const b = last.toLocaleDateString('tr-TR')
    return a === b ? a : `${a} – ${b}`
  }
  const fmt = (d: Date) => d.toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' })
  return `${fmt(s)} – ${fmt(en)}`
}

// ── Create ──────────────────────────────────────────────────────────────────
const showForm = ref(false)
const form = reactive({
  title: '', entryType: 'holiday', campusId: '', isAllDay: true,
  startDate: '', endDate: '', startsAt: '', endsAt: '',
})
const errors = reactive<Record<string, string>>({})

function openCreate() {
  notice.value = ''
  cancelPlanned.value = false
  const today = toLocalDateString()
  Object.assign(form, {
    title: '', entryType: 'holiday', campusId: campusFilter.value, isAllDay: true,
    startDate: today, endDate: today, startsAt: `${today}T09:00`, endsAt: `${today}T18:00`,
  })
  Object.keys(errors).forEach((k) => delete errors[k])
  showForm.value = true
}

const blocksSessions = computed(() => form.entryType !== 'event')

function validate(): boolean {
  Object.keys(errors).forEach((k) => delete errors[k])
  if (!form.title.trim()) errors.title = t('validation.required', { field: t('scheduling.calendarEntries.titleField') })
  if (form.isAllDay) {
    if (!form.startDate) errors.startDate = t('validation.required', { field: t('scheduling.calendarEntries.startDate') })
    if (!form.endDate) errors.endDate = t('validation.required', { field: t('scheduling.calendarEntries.endDate') })
    if (form.startDate && form.endDate && form.endDate < form.startDate) errors.endDate = t('scheduling.calendarEntries.endBeforeStart')
  } else {
    if (!form.startsAt) errors.startsAt = t('validation.required', { field: t('scheduling.session.startsAt') })
    if (!form.endsAt) errors.endsAt = t('validation.required', { field: t('scheduling.session.endsAt') })
    if (form.startsAt && form.endsAt && form.endsAt <= form.startsAt) errors.endsAt = t('scheduling.calendarEntries.endBeforeStart')
  }
  return Object.keys(errors).length === 0
}

/** Tüm gün: yerel gün başı → bitiş gününü kapsayacak şekilde ertesi günün 00:00'ı. Geçersizse null. */
function computeRange(): { startsAt: string; endsAt: string } | null {
  try {
    if (form.isAllDay) {
      if (!form.startDate || !form.endDate || form.endDate < form.startDate) return null
      const next = new Date(`${form.endDate}T00:00:00`)
      next.setDate(next.getDate() + 1)
      return { startsAt: startOfDayIso(form.startDate), endsAt: next.toISOString() }
    }
    if (!form.startsAt || !form.endsAt || form.endsAt <= form.startsAt) return null
    return { startsAt: localInputToIso(form.startsAt), endsAt: localInputToIso(form.endsAt) }
  } catch {
    return null
  }
}

// Kayıt eklenmeden önce: aralığa denk gelen planlı seans sayısı ve "iptal et" seçeneği (yalnızca seans engelleyen türlerde).
const plannedCount = ref(0)
const cancelPlanned = ref(false)
let impactSeq = 0

watch(
  () => [showForm.value, form.entryType, form.campusId, form.isAllDay, form.startDate, form.endDate, form.startsAt, form.endsAt],
  async () => {
    plannedCount.value = 0
    const range = showForm.value && blocksSessions.value ? computeRange() : null
    if (!range) { cancelPlanned.value = false; return }
    const seq = ++impactSeq
    try {
      const res = await schedulingService.getCalendarEntryImpact(range.startsAt, range.endsAt, form.campusId || undefined)
      if (seq === impactSeq && res.success && res.data) plannedCount.value = res.data.plannedSessionCount
    } catch { /* sayı gösterilemezse seçenek sunulmaz */ }
    if (plannedCount.value === 0) cancelPlanned.value = false
  },
)

const notice = ref('')

async function submit() {
  if (!validate()) return
  const range = computeRange()
  if (!range) return
  try {
    const created = await store.createCalendarEntry({
      title: form.title.trim(),
      entryType: form.entryType,
      campusId: form.campusId || undefined,
      startsAt: range.startsAt, endsAt: range.endsAt,
      isAllDay: form.isAllDay,
      cancelPlannedSessions: cancelPlanned.value && plannedCount.value > 0,
    })
    notice.value = created.cancelledSessionCount
      ? t('scheduling.calendarEntries.createdCancelled', { count: created.cancelledSessionCount })
      : t('scheduling.calendarEntries.created')
    showForm.value = false
    await load()
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}

// ── Delete ──────────────────────────────────────────────────────────────────
const deleteTarget = ref<CalendarEntryDto | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await store.deleteCalendarEntry(deleteTarget.value.id)
    deleteTarget.value = null
    await load()
  } catch (e: unknown) {
    deleteError.value = (e as Error).message
  } finally {
    deleteLoading.value = false
  }
}

const inputCls = 'w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.calendarEntries.title')" :description="t('scheduling.calendarEntries.description')">
      <button
        v-if="can(Permissions.Sessions.ManageCalendar)"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('scheduling.calendarEntries.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="flex items-center gap-1">
        <button @click="year--" class="w-8 h-8 rounded-lg border border-border hover:bg-accent" :aria-label="String(year - 1)">‹</button>
        <span class="px-3 text-sm font-medium min-w-[56px] text-center">{{ year }}</span>
        <button @click="year++" class="w-8 h-8 rounded-lg border border-border hover:bg-accent" :aria-label="String(year + 1)">›</button>
      </div>
      <select v-model="campusFilter" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('scheduling.room.allCampuses') }}</option>
        <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>

    <p class="mb-3 text-xs text-muted-foreground">{{ t('scheduling.calendarEntries.hint') }}</p>
    <div v-if="notice" class="p-3 rounded-lg bg-green-50 border border-green-200 text-sm text-green-800 mb-4">{{ notice }}</div>
    <div v-if="loadError" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700 mb-4">{{ loadError }}</div>

    <DataTable :columns="columns" :rows="store.calendarEntries" :loading="store.loading">
      <template #cell-entryType="{ value }">
        <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-violet-100 text-violet-700">
          {{ t(`scheduling.calendarEntries.types.${value}`) }}
        </span>
      </template>
      <template #cell-startsAt="{ row }">{{ describeRange(row) }}</template>
      <template #cell-campusId="{ value }">{{ campusName((value as string | null) ?? null) }}</template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can(Permissions.Sessions.ManageCalendar)"
            @click="deleteError = ''; deleteTarget = row"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
            :title="t('common.delete')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <FormModal :open="showForm" :title="t('scheduling.calendarEntries.create')" :saving="store.saving" @submit="submit" @close="showForm = false">
      <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700 mb-4">{{ errors.submit }}</div>
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.calendarEntries.titleField') }} *</label>
          <input v-model="form.title" type="text" maxlength="500" :class="[inputCls, errors.title ? 'border-red-400' : 'border-border']" />
          <p v-if="errors.title" class="mt-1 text-xs text-red-600">{{ errors.title }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.calendarEntries.type') }}</label>
          <select v-model="form.entryType" :class="[inputCls, 'border-border']">
            <option v-for="et in ENTRY_TYPES" :key="et" :value="et">{{ t(`scheduling.calendarEntries.types.${et}`) }}</option>
          </select>
          <p class="mt-1 text-xs text-muted-foreground">
            {{ blocksSessions ? t('scheduling.calendarEntries.blocksSessions') : t('scheduling.calendarEntries.noBlock') }}
          </p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.name') }}</label>
          <select v-model="form.campusId" :class="[inputCls, 'border-border']">
            <option value="">{{ t('scheduling.calendarEntries.wholeCorporation') }}</option>
            <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div v-if="blocksSessions && plannedCount > 0" class="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm space-y-2">
          <p class="text-amber-900">{{ t('scheduling.calendarEntries.impact', { count: plannedCount }) }}</p>
          <label class="flex items-start gap-2 text-amber-900">
            <input type="checkbox" v-model="cancelPlanned" class="rounded mt-0.5" />
            <span>{{ t('scheduling.calendarEntries.cancelPlanned') }}</span>
          </label>
          <p class="text-xs text-amber-800">{{ t('scheduling.calendarEntries.cancelPlannedHint') }}</p>
        </div>
        <label class="flex items-center gap-2 text-sm">
          <input type="checkbox" v-model="form.isAllDay" class="rounded" />
          {{ t('scheduling.calendarEntries.allDay') }}
        </label>
        <div v-if="form.isAllDay" class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.calendarEntries.startDate') }} *</label>
            <input v-model="form.startDate" type="date" :class="[inputCls, errors.startDate ? 'border-red-400' : 'border-border']" />
            <p v-if="errors.startDate" class="mt-1 text-xs text-red-600">{{ errors.startDate }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.calendarEntries.endDate') }} *</label>
            <input v-model="form.endDate" type="date" :class="[inputCls, errors.endDate ? 'border-red-400' : 'border-border']" />
            <p v-if="errors.endDate" class="mt-1 text-xs text-red-600">{{ errors.endDate }}</p>
          </div>
        </div>
        <div v-else class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.session.startsAt') }} *</label>
            <input v-model="form.startsAt" type="datetime-local" :class="[inputCls, errors.startsAt ? 'border-red-400' : 'border-border']" />
            <p v-if="errors.startsAt" class="mt-1 text-xs text-red-600">{{ errors.startsAt }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.session.endsAt') }} *</label>
            <input v-model="form.endsAt" type="datetime-local" :class="[inputCls, errors.endsAt ? 'border-red-400' : 'border-border']" />
            <p v-if="errors.endsAt" class="mt-1 text-xs text-red-600">{{ errors.endsAt }}</p>
          </div>
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('scheduling.calendarEntries.deleteTitle')"
      :message="deleteError || t('scheduling.calendarEntries.deleteMessage', { name: deleteTarget?.title })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
