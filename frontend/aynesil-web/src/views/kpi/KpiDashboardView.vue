<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('kpi.dashboard.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('kpi.dashboard.subtitle') }}</p>
      </div>
      <!-- Period Selector -->
      <div class="d-flex gap-2">
        <select v-model="periodType" class="form-select form-select-sm w-auto" @change="loadDashboard">
          <option value="Monthly">{{ $t('kpi.period.monthly') }}</option>
          <option value="Quarterly">{{ $t('kpi.period.quarterly') }}</option>
          <option value="Annual">{{ $t('kpi.period.annual') }}</option>
        </select>
      </div>
    </div>

    <!-- Tabs -->
    <ul class="nav nav-tabs nav-line-tabs mb-6">
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'manager' }" href="#" @click.prevent="tab = 'manager'">
          {{ $t('kpi.dashboard.managerView') }}
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'executive' }" href="#" @click.prevent="tab = 'executive'">
          {{ $t('kpi.dashboard.executiveView') }}
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'ranking' }" href="#" @click.prevent="tab = 'ranking'">
          {{ $t('kpi.dashboard.rankingView') }}
        </a>
      </li>
    </ul>

    <div v-if="kpiStore.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <!-- Manager Dashboard -->
    <div v-else-if="tab === 'manager' && kpiStore.managerDashboard">
      <div class="row g-5 mb-6">
        <div class="col-sm-6 col-xl-3">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-primary">{{ kpiStore.managerDashboard.totalEducators }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.dashboard.totalEducators') }}</div>
            </div>
          </div>
        </div>
        <div class="col-sm-6 col-xl-3">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-success">{{ pct(kpiStore.managerDashboard.avgAttendanceRate) }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.metrics.attendanceRate') }}</div>
            </div>
          </div>
        </div>
        <div class="col-sm-6 col-xl-3">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-warning">{{ pct(kpiStore.managerDashboard.avgGoalAchievementRate) }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.metrics.goalAchievementRate') }}</div>
            </div>
          </div>
        </div>
        <div class="col-sm-6 col-xl-3">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-info">{{ pct(kpiStore.managerDashboard.avgParentSatisfaction) }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.metrics.parentSatisfaction') }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Top Performers -->
      <div class="card">
        <div class="card-header border-0">
          <h3 class="card-title fw-bold">{{ $t('kpi.dashboard.topPerformers') }}</h3>
        </div>
        <div class="card-body py-3">
          <div class="table-responsive">
            <table class="table table-row-dashed align-middle gs-0 gy-4">
              <thead>
                <tr class="fw-bold text-muted bg-light">
                  <th class="ps-4">#</th>
                  <th>{{ $t('kpi.fields.educator') }}</th>
                  <th class="text-end">{{ $t('kpi.metrics.sessions') }}</th>
                  <th class="text-end">{{ $t('kpi.metrics.attendanceRate') }}</th>
                  <th class="text-end">{{ $t('kpi.metrics.goalAchievementRate') }}</th>
                  <th class="text-end pe-4">{{ $t('kpi.metrics.parentSatisfaction') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(e, idx) in kpiStore.managerDashboard.topPerformers" :key="e.educatorId">
                  <td class="ps-4 fw-bold text-gray-500">{{ idx + 1 }}</td>
                  <td class="fw-semibold">{{ e.fullName }}</td>
                  <td class="text-end">{{ e.sessionCount ?? '—' }}</td>
                  <td class="text-end">{{ pct(e.attendanceRate) }}</td>
                  <td class="text-end">{{ pct(e.goalAchievementRate) }}</td>
                  <td class="text-end pe-4">{{ e.parentFeedbackAvg?.toFixed(1) ?? '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- Executive Dashboard -->
    <div v-else-if="tab === 'executive' && kpiStore.executiveDashboard">
      <div class="row g-5 mb-6">
        <div class="col-sm-6 col-xl-4">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-primary">{{ kpiStore.executiveDashboard.totalActiveEducators }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.dashboard.activeEducators') }}</div>
            </div>
          </div>
        </div>
        <div class="col-sm-6 col-xl-4">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-success">{{ kpiStore.executiveDashboard.totalCompletedSessions }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.dashboard.completedSessions') }}</div>
            </div>
          </div>
        </div>
        <div class="col-sm-6 col-xl-4">
          <div class="card card-flush h-100">
            <div class="card-body text-center py-6">
              <div class="fs-2 fw-bold text-warning">{{ pct(kpiStore.executiveDashboard.corpAvgParentSatisfaction) }}</div>
              <div class="text-muted fs-7">{{ $t('kpi.metrics.parentSatisfaction') }}</div>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('kpi.dashboard.topPerformers') }}</h3></div>
        <div class="card-body py-3">
          <div v-for="e in kpiStore.executiveDashboard.topPerformers" :key="e.educatorId" class="d-flex align-items-center mb-4 p-3 rounded bg-light">
            <div class="flex-grow-1">
              <div class="fw-semibold">{{ e.fullName }}</div>
              <div class="text-muted fs-7">{{ e.titleCode ?? '—' }}</div>
            </div>
            <div class="text-end">
              <div class="fw-bold text-success">{{ pct(e.attendanceRate) }}</div>
              <div class="text-muted fs-8">{{ $t('kpi.metrics.attendanceRate') }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Ranking -->
    <div v-else-if="tab === 'ranking'">
      <div class="card">
        <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('kpi.dashboard.ranking') }}</h3></div>
        <div class="card-body py-3">
          <div v-if="kpiStore.ranking.length === 0" class="text-center py-10 text-muted">{{ $t('kpi.dashboard.noRanking') }}</div>
          <div v-else class="table-responsive">
            <table class="table table-row-dashed align-middle gs-0 gy-4">
              <thead>
                <tr class="fw-bold text-muted bg-light">
                  <th class="ps-4 w-60px">#</th>
                  <th>{{ $t('kpi.fields.educator') }}</th>
                  <th>{{ $t('kpi.fields.kpi') }}</th>
                  <th class="text-end pe-4">{{ $t('kpi.fields.value') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="r in kpiStore.ranking" :key="r.rank + r.educatorId">
                  <td class="ps-4">
                    <span :class="`badge badge-${r.rank === 1 ? 'warning' : r.rank === 2 ? 'secondary' : r.rank === 3 ? 'danger' : 'light'} fs-7`">{{ r.rank }}</span>
                  </td>
                  <td class="fw-semibold">{{ r.fullName }}</td>
                  <td class="text-muted">{{ r.kpiName }}</td>
                  <td class="text-end pe-4 fw-bold">{{ r.kpiValue?.toFixed(2) ?? '—' }} {{ r.unit }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useKpiStore } from '@/stores/kpi.store'
import { useAuthStore } from '@/stores/auth.store'

const kpiStore = useKpiStore()
const authStore = useAuthStore()
const tab = ref<'manager' | 'executive' | 'ranking'>('manager')
const periodType = ref<'Monthly' | 'Quarterly' | 'Annual'>('Monthly')

function pct(v?: number | null) {
  if (v == null) return '—'
  return `%${(v * 100).toFixed(1)}`
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
