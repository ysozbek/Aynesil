<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSchedulingStore } from '@/stores/scheduling.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import RoomPicker from '@/components/pickers/RoomPicker.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { RecurringScheduleListItemDto } from '@/types/scheduling.types'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'
import { toLocalDateString, localInputToIso } from '@/utils/datetime'

/** Default window = today → +30 days, in ISO YYYY-MM-DD. */
function defaultWindow() {
  const today = new Date()
  const to = new Date(today.getTime() + 30 * 86400000)
  return {
    windowStart: today.toISOString().slice(0, 10),
    windowEnd: to.toISOString().slice(0, 10),
  }
}

const { t } = useI18n()
const router = useRouter()
const store = useSchedulingStore()
const refData = useRefDataStore()
const { can } = usePermission()

const query = reactive({
  campusId: '',
  isActive: '' as '' | 'true' | 'false',
  page: 1,
  pageSize: 20,
})

watch(() => [query.campusId, query.isActive, query.page], () => loadList())
onMounted(() => loadList())

async function loadList() {
  await store.fetchRecurringSchedules({
    ...query,
    campusId: query.campusId || undefined,
    isActive: query.isActive === 'true' ? true : query.isActive === 'false' ? false : undefined,
  })
}

const columns: Column<RecurringScheduleListItemDto>[] = [
  { key: 'studentFullName', label: t('scheduling.session.student') },
  { key: 'leadEducatorName', label: t('scheduling.recurring.leadEducator'), width: '150px' },
  { key: 'startTime', label: t('scheduling.recurring.when'), width: '170px' },
  { key: 'roomName', label: t('scheduling.recurring.place'), width: '140px' },
  { key: 'sessionTypeId', label: t('scheduling.session.type'), width: '120px' },
  { key: 'rangeStart', label: t('scheduling.recurring.dateRange'), width: '170px' },
  { key: 'generatedCount', label: t('scheduling.recurring.generated'), width: '90px', align: 'center' },
  { key: 'isActive', label: t('common.status'), width: '90px' },
]

const sessionTypeLabels = ref<Record<string, string>>({})

onMounted(async () => {
  const values = await refData.getValues('session_type', false)
  sessionTypeLabels.value = Object.fromEntries(values.map((v) => [v.id, v.label]))
})

// Short Turkish-style day labels, indexed by the API convention (0 = Sunday … 6 = Saturday).
const WEEKDAY_KEYS = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']

function describeWhen(row: RecurringScheduleListItemDto): string {
  const time = row.startTime.slice(0, 5)
  const frequency = t(`scheduling.recurring.frequency.${row.frequency}`)
  if (!row.byWeekday?.length) return `${frequency} · ${time}`
  // Monday-first ordering for display.
  const days = [...row.byWeekday]
    .sort((a, b) => ((a + 6) % 7) - ((b + 6) % 7))
    .map((d) => t(`scheduling.recurring.weekdayShort.${WEEKDAY_KEYS[d]}`))
    .join(', ')
  return `${days} · ${time}`
}

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

const deactivateTarget = ref<RecurringScheduleListItemDto | null>(null)
const deactivateLoading = ref(false)

async function doDeactivate() {
  if (!deactivateTarget.value) return
  deactivateLoading.value = true
  try {
    await store.deactivateRecurringSchedule(deactivateTarget.value.id)
    deactivateTarget.value = null
    await loadList()
  } finally {
    deactivateLoading.value = false
  }
}

const generateTarget = ref<RecurringScheduleListItemDto | null>(null)
const generateLoading = ref(false)
const generateResult = ref<string | null>(null)
const generateWindow = reactive(defaultWindow())

function openGenerate(row: RecurringScheduleListItemDto) {
  Object.assign(generateWindow, defaultWindow())
  generateResult.value = null
  generateTarget.value = row
}

async function doGenerate() {
  if (!generateTarget.value) return
  if (!generateWindow.windowStart || !generateWindow.windowEnd) return
  generateLoading.value = true
  try {
    // Backend `BulkGenerateRequest` requires body { windowStart, windowEnd } (DateOnly).
    const result = await store.generateSessions(generateTarget.value.id, {
      windowStart: generateWindow.windowStart,
      windowEnd: generateWindow.windowEnd,
    })
    const lines = [t('scheduling.recurring.generateResult', { count: result.affectedCount })]
    if (result.roomConflictSkipped > 0) {
      lines.push(t('scheduling.recurring.generateSkippedRoom', { count: result.roomConflictSkipped }))
    }
    if (result.educatorConflictSkipped > 0) {
      lines.push(t('scheduling.recurring.generateSkippedEducator', { count: result.educatorConflictSkipped }))
    }
    if (result.closureSkipped > 0) {
      lines.push(t('scheduling.recurring.generateSkippedClosure', { count: result.closureSkipped }))
    }
    generateResult.value = lines.join(' ')
    loadList()
    if (result.roomConflictSkipped === 0 && result.educatorConflictSkipped === 0 && result.closureSkipped === 0) {
      setTimeout(() => { generateTarget.value = null; generateResult.value = null }, 2000)
    }
  } finally {
    generateLoading.value = false
  }
}

// ── Toplu iptal / toplu oda değişikliği / tek tarih istisnası ───────────────
type OpKind = 'cancel' | 'room' | 'exception'
const opKind = ref<OpKind | null>(null)
const opTarget = ref<RecurringScheduleListItemDto | null>(null)
const opResult = ref<string | null>(null)
const opError = ref('')
const opErrors = reactive<Record<string, string>>({})
const opForm = reactive({
  from: '', to: '', reason: '',
  roomId: '',
  exceptionDate: '', action: 'skip' as 'skip' | 'cancel' | 'reschedule', newStartAt: '',
})

function openOp(kind: OpKind, row: RecurringScheduleListItemDto) {
  const today = toLocalDateString()
  Object.assign(opForm, { from: today, to: today, reason: '', roomId: '', exceptionDate: today, action: 'skip', newStartAt: '' })
  Object.keys(opErrors).forEach((k) => delete opErrors[k])
  opError.value = ''
  opResult.value = null
  opTarget.value = row
  opKind.value = kind
}

function closeOp() {
  opKind.value = null
  opTarget.value = null
  opResult.value = null
}

function requiredMsg(field: string) { return t('validation.required', { field }) }

function validateOp(): boolean {
  Object.keys(opErrors).forEach((k) => delete opErrors[k])
  if (opKind.value === 'cancel') {
    if (!opForm.from) opErrors.from = requiredMsg(t('scheduling.recurring.from'))
    if (!opForm.to) opErrors.to = requiredMsg(t('scheduling.recurring.to'))
    if (opForm.from && opForm.to && opForm.to < opForm.from) opErrors.to = t('scheduling.recurring.ops.rangeInverted')
    if (!opForm.reason.trim()) opErrors.reason = requiredMsg(t('scheduling.recurring.ops.reason'))
  } else if (opKind.value === 'room') {
    if (!opForm.from) opErrors.from = requiredMsg(t('scheduling.recurring.ops.fromDate'))
    if (!opForm.roomId) opErrors.roomId = requiredMsg(t('scheduling.room.title'))
  } else if (opKind.value === 'exception') {
    if (!opForm.exceptionDate) opErrors.exceptionDate = requiredMsg(t('scheduling.recurring.ops.exceptionDate'))
    if (opForm.action === 'reschedule' && !opForm.newStartAt) opErrors.newStartAt = requiredMsg(t('scheduling.recurring.ops.newStart'))
  }
  return Object.keys(opErrors).length === 0
}

async function submitOp() {
  if (!opTarget.value || !validateOp()) return
  opError.value = ''
  const id = opTarget.value.id
  try {
    if (opKind.value === 'cancel') {
      const r = await store.bulkCancelSessions(id, { from: opForm.from, to: opForm.to, reason: opForm.reason.trim() })
      opResult.value = t('scheduling.recurring.ops.cancelResult', { count: r.affectedCount })
    } else if (opKind.value === 'room') {
      const r = await store.bulkReassignRoom(id, { newRoomId: opForm.roomId, from: opForm.from })
      const lines = [t('scheduling.recurring.ops.roomResult', { count: r.affectedCount })]
      if (r.roomConflictSkipped > 0) lines.push(t('scheduling.recurring.generateSkippedRoom', { count: r.roomConflictSkipped }))
      opResult.value = lines.join(' ')
    } else if (opKind.value === 'exception') {
      const r = await store.addRecurringException(id, {
        exceptionDate: opForm.exceptionDate,
        action: opForm.action,
        newStartAt: opForm.action === 'reschedule' ? localInputToIso(opForm.newStartAt) : undefined,
        reason: opForm.reason.trim() || undefined,
      })
      opResult.value = r.affectedCount > 0
        ? t('scheduling.recurring.ops.exceptionResultApplied', { count: r.affectedCount })
        : t('scheduling.recurring.ops.exceptionResult')
    }
    loadList()
  } catch (e: unknown) {
    opError.value = (e as Error).message
  }
}

const opInput = 'w-full h-9 px-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.recurring.title')" :description="t('scheduling.recurring.description')">
      <button
        v-if="can(Permissions.Sessions.Create)"
        @click="router.push({ name: 'recurring-schedule-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('scheduling.recurring.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3">
      <select v-model="query.isActive" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="true">{{ t('common.active') }}</option>
        <option value="false">{{ t('common.passive') }}</option>
      </select>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.recurringList.items"
      :loading="store.loading"
    >
      <template #cell-studentFullName="{ row }">
        <div v-if="row.studentFullName">
          <div class="font-medium text-foreground">{{ row.studentFullName }}</div>
          <div class="text-xs text-muted-foreground">
            {{ [row.programName, row.programServiceName].filter(Boolean).join(' · ') || '—' }}
          </div>
        </div>
        <span v-else class="text-muted-foreground">{{ t('scheduling.recurring.noStudent') }}</span>
      </template>
      <template #cell-leadEducatorName="{ value }">
        <span :class="value ? '' : 'text-muted-foreground'">{{ value ?? '—' }}</span>
      </template>
      <template #cell-startTime="{ row }">
        <div>{{ describeWhen(row) }}</div>
        <div class="text-xs text-muted-foreground">
          {{ row.durationMinutes }} dk<template v-if="row.frequency === 'biweekly'"> · {{ t('scheduling.recurring.frequency.biweekly') }}</template>
        </div>
      </template>
      <template #cell-roomName="{ row }">
        <div>{{ row.roomName ?? '—' }}</div>
        <div v-if="row.campusName" class="text-xs text-muted-foreground">{{ row.campusName }}</div>
      </template>
      <template #cell-sessionTypeId="{ value }">
        <span class="text-muted-foreground">{{ (value && sessionTypeLabels[String(value)]) || '—' }}</span>
      </template>
      <template #cell-rangeStart="{ row }">
        {{ formatDate(row.rangeStart) }} – {{ row.rangeEnd ? formatDate(row.rangeEnd) : t('scheduling.recurring.openEnded') }}
      </template>
      <template #cell-isActive="{ value }">
        <StatusBadge :value="Boolean(value)" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can(Permissions.Sessions.BulkGenerate) && row.isActive"
            @click="openGenerate(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-primary"
            :title="t('scheduling.recurring.generate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </button>
          <button
            v-if="can(Permissions.Sessions.BulkCancel) && row.isActive"
            @click="openOp('cancel', row)"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
            :title="t('scheduling.recurring.ops.cancelTitle')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2zM10 14l4 4m0-4l-4 4" />
            </svg>
          </button>
          <button
            v-if="can(Permissions.Sessions.BulkReassign) && row.isActive"
            @click="openOp('room', row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-primary"
            :title="t('scheduling.recurring.ops.roomTitle')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
            </svg>
          </button>
          <button
            v-if="can(Permissions.Sessions.Update) && row.isActive"
            @click="openOp('exception', row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-primary"
            :title="t('scheduling.recurring.ops.exceptionTitle')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </button>
          <button
            v-if="can(Permissions.Sessions.Update) && row.isActive"
            @click="deactivateTarget = row"
            class="p-1.5 rounded-lg hover:bg-amber-50 text-muted-foreground hover:text-amber-600"
            :title="t('common.deactivate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.recurringList.page"
        :page-size="store.recurringList.pageSize"
        :total-count="store.recurringList.totalCount"
        :total-pages="store.recurringList.totalPages"
        :has-previous-page="store.recurringList.hasPreviousPage"
        :has-next-page="store.recurringList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deactivateTarget"
      :title="t('scheduling.recurring.deactivateTitle')"
      :message="t('scheduling.recurring.deactivateMessage')"
      :confirm-label="t('common.deactivate')"
      :loading="deactivateLoading"
      @confirm="doDeactivate"
      @cancel="deactivateTarget = null"
    />

    <!-- Toplu iptal / oda değişikliği / istisna -->
    <FormModal
      :open="!!opKind"
      :title="opKind === 'cancel' ? t('scheduling.recurring.ops.cancelTitle') : opKind === 'room' ? t('scheduling.recurring.ops.roomTitle') : t('scheduling.recurring.ops.exceptionTitle')"
      :subtitle="opTarget?.studentFullName ?? t('scheduling.recurring.noStudent')"
      :saving="store.saving"
      @submit="opResult ? closeOp() : submitOp()"
      @close="closeOp"
    >
      <template v-if="opResult" #footer>
        <button @click="closeOp" class="px-4 py-2 rounded-lg text-sm font-medium bg-primary text-primary-foreground hover:opacity-90">{{ t('common.close') }}</button>
      </template>
      <div v-if="opError" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700 mb-4">{{ opError }}</div>
      <div v-if="opResult" class="p-3 rounded-lg bg-green-50 border border-green-200 text-sm text-green-800">{{ opResult }}</div>

      <div v-else class="space-y-4">
        <template v-if="opKind === 'cancel'">
          <p class="text-sm text-muted-foreground">{{ t('scheduling.recurring.ops.cancelHint') }}</p>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs text-muted-foreground mb-1">{{ t('scheduling.recurring.from') }} *</label>
              <input v-model="opForm.from" type="date" :class="[opInput, opErrors.from ? 'border-red-400' : 'border-border']" />
              <p v-if="opErrors.from" class="mt-1 text-xs text-red-600">{{ opErrors.from }}</p>
            </div>
            <div>
              <label class="block text-xs text-muted-foreground mb-1">{{ t('scheduling.recurring.to') }} *</label>
              <input v-model="opForm.to" type="date" :class="[opInput, opErrors.to ? 'border-red-400' : 'border-border']" />
              <p v-if="opErrors.to" class="mt-1 text-xs text-red-600">{{ opErrors.to }}</p>
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.recurring.ops.reason') }} *</label>
            <textarea v-model="opForm.reason" rows="2" maxlength="500" :class="['w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary', opErrors.reason ? 'border-red-400' : 'border-border']" />
            <p v-if="opErrors.reason" class="mt-1 text-xs text-red-600">{{ opErrors.reason }}</p>
          </div>
        </template>

        <template v-else-if="opKind === 'room'">
          <p class="text-sm text-muted-foreground">{{ t('scheduling.recurring.ops.roomHint') }}</p>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.recurring.ops.newRoom') }} *</label>
            <RoomPicker v-model="opForm.roomId" :invalid="!!opErrors.roomId" />
            <p v-if="opErrors.roomId" class="mt-1 text-xs text-red-600">{{ opErrors.roomId }}</p>
          </div>
          <div>
            <label class="block text-xs text-muted-foreground mb-1">{{ t('scheduling.recurring.ops.fromDate') }} *</label>
            <input v-model="opForm.from" type="date" :class="[opInput, opErrors.from ? 'border-red-400' : 'border-border']" />
            <p v-if="opErrors.from" class="mt-1 text-xs text-red-600">{{ opErrors.from }}</p>
          </div>
        </template>

        <template v-else-if="opKind === 'exception'">
          <p class="text-sm text-muted-foreground">{{ t('scheduling.recurring.ops.exceptionHint') }}</p>
          <div>
            <label class="block text-xs text-muted-foreground mb-1">{{ t('scheduling.recurring.ops.exceptionDate') }} *</label>
            <input v-model="opForm.exceptionDate" type="date" :class="[opInput, opErrors.exceptionDate ? 'border-red-400' : 'border-border']" />
            <p v-if="opErrors.exceptionDate" class="mt-1 text-xs text-red-600">{{ opErrors.exceptionDate }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.recurring.ops.action') }}</label>
            <select v-model="opForm.action" :class="[opInput, 'border-border']">
              <option value="skip">{{ t('scheduling.recurring.ops.actionSkip') }}</option>
              <option value="cancel">{{ t('scheduling.recurring.ops.actionCancel') }}</option>
              <option value="reschedule">{{ t('scheduling.recurring.ops.actionReschedule') }}</option>
            </select>
          </div>
          <div v-if="opForm.action === 'reschedule'">
            <label class="block text-xs text-muted-foreground mb-1">{{ t('scheduling.recurring.ops.newStart') }} *</label>
            <input v-model="opForm.newStartAt" type="datetime-local" :class="[opInput, opErrors.newStartAt ? 'border-red-400' : 'border-border']" />
            <p v-if="opErrors.newStartAt" class="mt-1 text-xs text-red-600">{{ opErrors.newStartAt }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('scheduling.recurring.ops.reasonOptional') }}</label>
            <input v-model="opForm.reason" type="text" maxlength="500" :class="[opInput, 'border-border']" />
          </div>
        </template>
      </div>
    </FormModal>

    <!-- Generate modal with required window inputs (API requires body). -->
    <div
      v-if="generateTarget"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/30"
    >
      <div class="bg-card rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-2">{{ t('scheduling.recurring.generateTitle') }}</h3>
        <p class="text-sm text-muted-foreground mb-4">
          {{ generateResult ?? t('scheduling.recurring.generateMessage') }}
        </p>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs text-muted-foreground mb-1">
              {{ t('scheduling.recurring.from') }}
            </label>
            <input
              v-model="generateWindow.windowStart"
              type="date"
              class="w-full h-9 px-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-xs text-muted-foreground mb-1">
              {{ t('scheduling.recurring.to') }}
            </label>
            <input
              v-model="generateWindow.windowEnd"
              type="date"
              class="w-full h-9 px-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>

        <div class="flex justify-end gap-2 mt-5">
          <button
            @click="generateTarget = null; generateResult = null"
            class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent"
          >{{ t('common.cancel') }}</button>
          <button
            @click="doGenerate"
            :disabled="generateLoading || !generateWindow.windowStart || !generateWindow.windowEnd"
            class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
          >
            {{ generateLoading ? t('common.saving') : t('scheduling.recurring.generate') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
