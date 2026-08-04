<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore, type RefValueItem } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { LeaveUsageReportItemDto, LeaveTrendItemDto } from '@/types/leave.types'

const { t } = useI18n()
const leaveStore = useLeaveStore()
const auth = useAuthStore()
const refData = useRefDataStore()
const tab = ref<'usage' | 'trend'>('usage')
const leaveTypes = ref<RefValueItem[]>([])

const filters = reactive({
  periodYear: new Date().getFullYear(),
  leaveTypeId: '',
})

const MONTHS = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık']
function monthName(m: number) {
  return MONTHS[m - 1] ?? String(m)
}

const usageColumns: Column<LeaveUsageReportItemDto>[] = [
  { key: 'educatorFullName', label: t('leave.fields.educator') },
  { key: 'leaveTypeCode', label: t('leave.fields.leaveType'), width: '120px' },
  { key: 'periodYear', label: t('leave.fields.periodYear'), width: '90px' },
  { key: 'entitled', label: t('leave.balance.entitled'), width: '90px', align: 'right' },
  { key: 'used', label: t('leave.balance.used'), width: '90px', align: 'right' },
  { key: 'remaining', label: t('leave.balance.remaining'), width: '90px', align: 'right' },
  { key: 'requestCount', label: t('leave.reports.requestCount'), width: '100px', align: 'right' },
]

const trendColumns: Column<LeaveTrendItemDto>[] = [
  { key: 'year', label: t('leave.reports.year'), width: '80px' },
  { key: 'month', label: t('leave.reports.month'), width: '100px' },
  { key: 'requestCount', label: t('leave.reports.requestCount'), width: '100px', align: 'right' },
  { key: 'approvedCount', label: t('leave.status.approved'), width: '100px', align: 'right' },
  { key: 'rejectedCount', label: t('leave.status.rejected'), width: '100px', align: 'right' },
  { key: 'cancelledCount', label: t('leave.status.cancelled'), width: '100px', align: 'right' },
  { key: 'totalDaysApproved', label: t('leave.reports.totalDaysApproved'), width: '120px', align: 'right' },
]

async function doFetch() {
  const q = {
    corporationId: auth.user?.corporationId,
    periodYear: filters.periodYear || undefined,
    leaveTypeId: filters.leaveTypeId || undefined,
  }
  await Promise.all([leaveStore.fetchUsageReport(q), leaveStore.fetchTrendReport(q)])
}

onMounted(async () => {
  leaveTypes.value = await refData.getValues('leave_type')
  await doFetch()
})
</script>

<template>
  <div>
    <PageHeader :title="t('leave.reports.title')" :description="t('leave.reports.subtitle')" />

    <div class="flex gap-1 mb-4 border-b border-border">
      <button
        type="button"
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          tab === 'usage' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground',
        ]"
        @click="tab = 'usage'"
      >
        {{ t('leave.reports.usageTab') }}
      </button>
      <button
        type="button"
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          tab === 'trend' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground',
        ]"
        @click="tab = 'trend'"
      >
        {{ t('leave.reports.trendTab') }}
      </button>
    </div>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('leave.fields.periodYear') }}</label>
        <input v-model.number="filters.periodYear" type="number" class="h-9 w-28 px-3 text-sm rounded-lg border border-border bg-transparent" />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('leave.fields.leaveType') }}</label>
        <select v-model="filters.leaveTypeId" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option v-for="lt in leaveTypes" :key="lt.id" :value="lt.id">{{ lt.label || lt.code }}</option>
        </select>
      </div>
      <button
        @click="doFetch"
        class="h-9 px-4 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90"
      >
        {{ t('common.filter') }}
      </button>
    </div>

    <DataTable
      v-if="tab === 'usage'"
      :columns="usageColumns"
      :rows="leaveStore.usageReport"
      :loading="leaveStore.loading"
      :empty-text="t('leave.reports.noData')"
      row-key="educatorId"
    >
      <template #cell-educatorFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-used="{ value }">
        <span class="text-amber-600">{{ value }}</span>
      </template>
      <template #cell-remaining="{ value }">
        <span class="font-semibold text-green-600">{{ value }}</span>
      </template>
    </DataTable>

    <DataTable
      v-else
      :columns="trendColumns"
      :rows="leaveStore.trendReport"
      :loading="leaveStore.loading"
      :empty-text="t('leave.reports.noData')"
      row-key="year"
    >
      <template #cell-month="{ value }">{{ monthName(Number(value)) }}</template>
      <template #cell-approvedCount="{ value }">
        <span class="text-green-600">{{ value }}</span>
      </template>
      <template #cell-rejectedCount="{ value }">
        <span class="text-red-600">{{ value }}</span>
      </template>
      <template #cell-totalDaysApproved="{ value }">
        <span class="font-semibold">{{ value }}</span>
      </template>
    </DataTable>
  </div>
</template>
