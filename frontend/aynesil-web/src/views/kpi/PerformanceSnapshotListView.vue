<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useKpiStore } from '@/stores/kpi.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { EducatorPerformanceSnapshotListItemDto, SnapshotListQuery } from '@/types/kpi.types'

const { t } = useI18n()
const kpiStore = useKpiStore()
const authStore = useAuthStore()
const { can } = usePermission()

const filters = reactive<SnapshotListQuery>({
  page: 1,
  pageSize: 50,
  corporationId: authStore.user?.corporationId,
  periodStart: '',
  periodEnd: '',
})

const columns: Column<EducatorPerformanceSnapshotListItemDto>[] = [
  { key: 'educatorFullName', label: t('kpi.fields.educator') },
  { key: 'period', label: t('kpi.fields.period'), width: '180px' },
  { key: 'sessionCount', label: t('kpi.metrics.sessions'), width: '90px', align: 'right' },
  { key: 'attendanceRate', label: t('kpi.metrics.attendanceRate'), width: '110px', align: 'right' },
  { key: 'goalAchievementRate', label: t('kpi.metrics.goalAchievementRate'), width: '110px', align: 'right' },
  { key: 'parentFeedbackAvg', label: t('kpi.metrics.parentFeedback'), width: '110px', align: 'right' },
  { key: 'utilizationRate', label: t('kpi.metrics.utilization'), width: '110px', align: 'right' },
]

function pct(v?: number | null) {
  if (v == null) return '—'
  return `%${(v * 100).toFixed(1)}`
}

async function doFetch() {
  await kpiStore.fetchSnapshots(filters)
}

async function doBulkCompute() {
  const now = new Date()
  const start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().substring(0, 10)
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().substring(0, 10)
  // bulk compute not directly in store — call service
  await kpiStore.fetchSnapshots(filters)
}

watch(
  () => filters.page,
  () => kpiStore.fetchSnapshots(filters)
)

onMounted(doFetch)
</script>

<template>
  <div>
    <PageHeader :title="t('kpi.snapshots.title')" :description="t('kpi.snapshots.subtitle')">
      <button
        v-if="can('kpi:manage')"
        :disabled="kpiStore.saving"
        @click="doBulkCompute"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50"
      >
        <svg v-if="kpiStore.saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
        </svg>
        {{ t('kpi.snapshots.compute') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('kpi.fields.periodStart') }}</label>
        <input
          v-model="filters.periodStart"
          type="date"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('kpi.fields.periodEnd') }}</label>
        <input
          v-model="filters.periodEnd"
          type="date"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        />
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="kpiStore.snapshots.items"
      :loading="kpiStore.loading"
      :empty-text="t('kpi.snapshots.noData')"
    >
      <template #cell-educatorFullName="{ value }">
        <span class="font-medium text-foreground">{{ value }}</span>
      </template>
      <template #cell-period="{ row }">
        <span class="text-xs text-muted-foreground">{{ row.periodStart }} – {{ row.periodEnd }}</span>
      </template>
      <template #cell-sessionCount="{ value }">{{ value ?? '—' }}</template>
      <template #cell-attendanceRate="{ value }">{{ pct(value as number) }}</template>
      <template #cell-goalAchievementRate="{ value }">{{ pct(value as number) }}</template>
      <template #cell-parentFeedbackAvg="{ value }">
        {{ value != null ? Number(value).toFixed(1) : '—' }}
      </template>
      <template #cell-utilizationRate="{ value }">{{ pct(value as number) }}</template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="kpiStore.snapshots.page"
        :page-size="kpiStore.snapshots.pageSize"
        :total-count="kpiStore.snapshots.totalCount"
        :total-pages="kpiStore.snapshots.totalPages"
        :has-previous-page="kpiStore.snapshots.hasPreviousPage"
        :has-next-page="kpiStore.snapshots.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; kpiStore.fetchSnapshots(filters) }"
      />
    </div>
  </div>
</template>
