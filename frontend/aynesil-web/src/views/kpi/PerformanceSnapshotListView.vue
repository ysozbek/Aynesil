<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('kpi.snapshots.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('kpi.snapshots.subtitle') }}</p>
      </div>
      <button
        v-if="hasPermission('kpi:manage')"
        class="btn btn-light-primary"
        :disabled="kpiStore.saving"
        @click="doBulkCompute"
      >
        <span v-if="kpiStore.saving" class="spinner-border spinner-border-sm me-2"></span>
        <i v-else class="ki-outline ki-calculator fs-2 me-1"></i>
        {{ $t('kpi.snapshots.compute') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('kpi.fields.periodStart') }}</label>
            <input v-model="filters.periodStart" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('kpi.fields.periodEnd') }}</label>
            <input v-model="filters.periodEnd" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="kpiStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="kpiStore.snapshots.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('kpi.snapshots.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('kpi.fields.educator') }}</th>
                <th>{{ $t('kpi.fields.period') }}</th>
                <th class="text-end">{{ $t('kpi.metrics.sessions') }}</th>
                <th class="text-end">{{ $t('kpi.metrics.attendanceRate') }}</th>
                <th class="text-end">{{ $t('kpi.metrics.goalAchievementRate') }}</th>
                <th class="text-end">{{ $t('kpi.metrics.parentFeedback') }}</th>
                <th class="text-end pe-4">{{ $t('kpi.metrics.utilization') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in kpiStore.snapshots.items" :key="s.id">
                <td class="ps-4 fw-semibold">{{ s.educatorFullName }}</td>
                <td class="text-muted fs-7">{{ s.periodStart }} – {{ s.periodEnd }}</td>
                <td class="text-end">{{ s.sessionCount ?? '—' }}</td>
                <td class="text-end">{{ pct(s.attendanceRate) }}</td>
                <td class="text-end">{{ pct(s.goalAchievementRate) }}</td>
                <td class="text-end">{{ s.parentFeedbackAvg?.toFixed(1) ?? '—' }}</td>
                <td class="text-end pe-4">{{ pct(s.utilizationRate) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useKpiStore } from '@/stores/kpi.store'
import { useAuthStore } from '@/stores/auth.store'
import type { SnapshotListQuery } from '@/types/kpi.types'

const kpiStore = useKpiStore()
const authStore = useAuthStore()

const filters = reactive<SnapshotListQuery>({
  page: 1, pageSize: 50,
  corporationId: authStore.user?.corporationId,
  periodStart: '', periodEnd: '',
})

function hasPermission(p: string) { return authStore.hasPermission(p) }
function pct(v?: number | null) {
  if (v == null) return '—'
  return `%${(v * 100).toFixed(1)}`
}

async function doFetch() { await kpiStore.fetchSnapshots(filters) }

async function doBulkCompute() {
  const now = new Date()
  const start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().substring(0, 10)
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().substring(0, 10)
  // bulk compute not directly in store — call service
  await kpiStore.fetchSnapshots(filters)
}

onMounted(doFetch)
</script>
