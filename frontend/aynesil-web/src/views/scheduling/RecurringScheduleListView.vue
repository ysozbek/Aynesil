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
import type { Column } from '@/components/shared/DataTable.vue'
import type { RecurringScheduleListItemDto } from '@/types/scheduling.types'

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
    generateResult.value = lines.join(' ')
    loadList()
    if (result.roomConflictSkipped === 0 && result.educatorConflictSkipped === 0) {
      setTimeout(() => { generateTarget.value = null; generateResult.value = null }, 2000)
    }
  } finally {
    generateLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.recurring.title')" :description="t('scheduling.recurring.description')">
      <button
        v-if="can('session:create')"
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
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('session:bulk_generate') && row.isActive"
            @click="openGenerate(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-primary"
            :title="t('scheduling.recurring.generate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </button>
          <button
            v-if="can('session:update') && row.isActive"
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
