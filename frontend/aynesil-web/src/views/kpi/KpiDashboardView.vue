<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useKpiStore } from '@/stores/kpi.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { EducatorSummaryDto, RankingItemDto } from '@/types/kpi.types'

const { t } = useI18n()
const kpiStore = useKpiStore()
const authStore = useAuthStore()

const tab = ref<'manager' | 'executive' | 'ranking'>('manager')
const periodType = ref<'Monthly' | 'Quarterly' | 'Annual'>('Monthly')

const tabs = [
  { key: 'manager' as const, label: () => t('kpi.dashboard.managerView') },
  { key: 'executive' as const, label: () => t('kpi.dashboard.executiveView') },
  { key: 'ranking' as const, label: () => t('kpi.dashboard.rankingView') },
]

const performerColumns: Column<EducatorSummaryDto>[] = [
  { key: 'rank', label: '#', width: '48px', align: 'center' },
  { key: 'fullName', label: t('kpi.fields.educator') },
  { key: 'sessionCount', label: t('kpi.metrics.sessions'), width: '100px', align: 'right' },
  { key: 'attendanceRate', label: t('kpi.metrics.attendanceRate'), width: '120px', align: 'right' },
  { key: 'goalAchievementRate', label: t('kpi.metrics.goalAchievementRate'), width: '120px', align: 'right' },
  { key: 'parentFeedbackAvg', label: t('kpi.metrics.parentSatisfaction'), width: '120px', align: 'right' },
]

const rankingColumns: Column<RankingItemDto>[] = [
  { key: 'rank', label: '#', width: '60px', align: 'center' },
  { key: 'fullName', label: t('kpi.fields.educator') },
  { key: 'kpiName', label: t('kpi.fields.kpi') },
  { key: 'kpiValue', label: t('kpi.fields.value'), width: '120px', align: 'right' },
]

function pct(v?: number | null) {
  if (v == null) return '—'
  return `%${(v * 100).toFixed(1)}`
}

function rankClass(rank: number) {
  if (rank === 1) return 'bg-amber-100 text-amber-700'
  if (rank === 2) return 'bg-gray-200 text-gray-700'
  if (rank === 3) return 'bg-orange-100 text-orange-700'
  return 'bg-gray-100 text-gray-600'
}

async function loadDashboard() {
  const q = { corporationId: authStore.user?.corporationId, periodType: periodType.value }
  await Promise.all([
    kpiStore.fetchManagerDashboard(q),
    kpiStore.fetchExecutiveDashboard(q),
    kpiStore.fetchRanking({ corporationId: authStore.user?.corporationId }),
  ])
}

onMounted(loadDashboard)
</script>

<template>
  <div>
    <PageHeader :title="t('kpi.dashboard.title')" :description="t('kpi.dashboard.subtitle')">
      <select
        v-model="periodType"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        @change="loadDashboard"
      >
        <option value="Monthly">{{ t('kpi.period.monthly') }}</option>
        <option value="Quarterly">{{ t('kpi.period.quarterly') }}</option>
        <option value="Annual">{{ t('kpi.period.annual') }}</option>
      </select>
    </PageHeader>

    <div class="flex gap-1 mb-6 border-b border-border">
      <button
        v-for="item in tabs"
        :key="item.key"
        @click="tab = item.key"
        :class="[
          'px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors',
          tab === item.key
            ? 'border-primary text-primary'
            : 'border-transparent text-muted-foreground hover:text-foreground',
        ]"
      >
        {{ item.label() }}
      </button>
    </div>

    <div v-if="kpiStore.loading" class="space-y-4">
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="h-24 rounded-xl bg-accent animate-pulse" />
      </div>
      <div class="h-64 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Manager Dashboard -->
    <div v-else-if="tab === 'manager' && kpiStore.managerDashboard">
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-primary">{{ kpiStore.managerDashboard.totalEducators }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.dashboard.totalEducators') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-green-600">{{ pct(kpiStore.managerDashboard.avgAttendanceRate) }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.metrics.attendanceRate') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-amber-600">{{ pct(kpiStore.managerDashboard.avgGoalAchievementRate) }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.metrics.goalAchievementRate') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-sky-600">{{ pct(kpiStore.managerDashboard.avgParentSatisfaction) }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.metrics.parentSatisfaction') }}</p>
        </div>
      </div>

      <h3 class="font-semibold text-foreground mb-3">{{ t('kpi.dashboard.topPerformers') }}</h3>
      <DataTable
          :columns="performerColumns"
          :rows="kpiStore.managerDashboard.topPerformers.map((e, idx) => ({ ...e, rank: idx + 1 }))"
          :empty-text="t('kpi.dashboard.noRanking')"
        >
          <template #cell-rank="{ value }">
            <span class="font-medium text-muted-foreground">{{ value }}</span>
          </template>
          <template #cell-fullName="{ value }">
            <span class="font-medium text-foreground">{{ value }}</span>
          </template>
          <template #cell-sessionCount="{ value }">{{ value ?? '—' }}</template>
          <template #cell-attendanceRate="{ value }">{{ pct(value as number) }}</template>
          <template #cell-goalAchievementRate="{ value }">{{ pct(value as number) }}</template>
          <template #cell-parentFeedbackAvg="{ value }">
            {{ value != null ? Number(value).toFixed(1) : '—' }}
          </template>
        </DataTable>
    </div>

    <!-- Executive Dashboard -->
    <div v-else-if="tab === 'executive' && kpiStore.executiveDashboard">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-primary">{{ kpiStore.executiveDashboard.totalActiveEducators }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.dashboard.activeEducators') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-green-600">{{ kpiStore.executiveDashboard.totalCompletedSessions }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.dashboard.completedSessions') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-2xl font-bold text-amber-600">{{ pct(kpiStore.executiveDashboard.corpAvgParentSatisfaction) }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('kpi.metrics.parentSatisfaction') }}</p>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('kpi.dashboard.topPerformers') }}</h3>
        </div>
        <div v-if="kpiStore.executiveDashboard.topPerformers.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('kpi.dashboard.noRanking') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="e in kpiStore.executiveDashboard.topPerformers"
            :key="e.educatorId"
            class="flex items-center gap-4 px-4 py-3"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ e.fullName }}</p>
              <p class="text-xs text-muted-foreground">{{ e.titleCode ?? '—' }}</p>
            </div>
            <div class="text-right shrink-0">
              <p class="text-sm font-semibold text-green-600">{{ pct(e.attendanceRate) }}</p>
              <p class="text-xs text-muted-foreground">{{ t('kpi.metrics.attendanceRate') }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Ranking -->
    <div v-else-if="tab === 'ranking'">
      <h3 class="font-semibold text-foreground mb-3">{{ t('kpi.dashboard.ranking') }}</h3>
      <DataTable
          :columns="rankingColumns"
          :rows="kpiStore.ranking"
          :empty-text="t('kpi.dashboard.noRanking')"
        >
          <template #cell-rank="{ value }">
            <span :class="['inline-flex items-center justify-center w-7 h-7 rounded-full text-xs font-bold', rankClass(Number(value))]">
              {{ value }}
            </span>
          </template>
          <template #cell-fullName="{ value }">
            <span class="font-medium text-foreground">{{ value }}</span>
          </template>
          <template #cell-kpiName="{ value }">
            <span class="text-muted-foreground">{{ value }}</span>
          </template>
          <template #cell-kpiValue="{ row }">
            <span class="font-semibold text-foreground">
              {{ row.kpiValue?.toFixed(2) ?? '—' }} {{ row.unit ?? '' }}
            </span>
          </template>
        </DataTable>
    </div>
  </div>
</template>
