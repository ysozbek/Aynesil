<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useSchedulingStore } from '@/stores/scheduling.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { RecurringScheduleListItemDto } from '@/types/scheduling.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useSchedulingStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
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
    corporationId: corporationId.value,
    campusId: query.campusId || undefined,
    isActive: query.isActive === 'true' ? true : query.isActive === 'false' ? false : undefined,
  })
}

const columns: Column<RecurringScheduleListItemDto>[] = [
  { key: 'campusName', label: t('campus.name'), width: '140px' },
  { key: 'roomName', label: t('scheduling.room.name'), width: '120px' },
  { key: 'sessionTypeLabel', label: t('scheduling.session.type'), width: '120px' },
  { key: 'frequency', label: t('scheduling.recurring.frequency'), width: '100px' },
  { key: 'startTime', label: t('scheduling.recurring.startTime'), width: '90px' },
  { key: 'durationMinutes', label: t('scheduling.recurring.duration'), width: '90px' },
  { key: 'rangeStart', label: t('scheduling.recurring.from'), width: '110px' },
  { key: 'rangeEnd', label: t('scheduling.recurring.to'), width: '110px' },
  { key: 'generatedCount', label: t('scheduling.recurring.generated'), width: '90px', align: 'center' },
  { key: 'isActive', label: t('common.status'), width: '90px' },
]

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

async function doGenerate() {
  if (!generateTarget.value) return
  generateLoading.value = true
  try {
    const result = await store.generateSessions(generateTarget.value.id)
    generateResult.value = result.message
    setTimeout(() => { generateTarget.value = null; generateResult.value = null; loadList() }, 2000)
  } finally {
    generateLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.recurring.title')" :description="t('scheduling.recurring.description')">
      <button
        v-if="can('recurring_schedule:create')"
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
      @row-click="(row) => router.push({ name: 'recurring-schedule-detail', params: { id: row.id } })"
    >
      <template #cell-roomName="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-sessionTypeLabel="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-frequency="{ value }">
        <span class="text-xs font-medium">{{ t(`scheduling.recurring.frequency.${value}`) }}</span>
      </template>
      <template #cell-rangeStart="{ value }">{{ formatDate(value) }}</template>
      <template #cell-rangeEnd="{ value }">{{ formatDate(value) }}</template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('recurring_schedule:update') && row.isActive"
            @click="generateTarget = row"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-primary"
            :title="t('scheduling.recurring.generate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </button>
          <button
            v-if="can('recurring_schedule:update') && row.isActive"
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

    <ConfirmModal
      :open="!!generateTarget"
      :title="t('scheduling.recurring.generateTitle')"
      :message="generateResult ?? t('scheduling.recurring.generateMessage')"
      :confirm-label="t('scheduling.recurring.generate')"
      :loading="generateLoading"
      @confirm="doGenerate"
      @cancel="generateTarget = null; generateResult = null"
    />
  </div>
</template>
