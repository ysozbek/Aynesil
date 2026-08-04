<template>
  <div class="container-xxl py-6">
    <div class="mb-6">
      <h1 class="text-gray-900 fw-bold fs-2">{{ $t('leave.reports.title') }}</h1>
      <p class="text-muted mb-0">{{ $t('leave.reports.subtitle') }}</p>
    </div>

    <!-- Tabs -->
    <ul class="nav nav-tabs nav-line-tabs mb-6">
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'usage' }" href="#" @click.prevent="tab = 'usage'">
          {{ $t('leave.reports.usageTab') }}
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'trend' }" href="#" @click.prevent="tab = 'trend'">
          {{ $t('leave.reports.trendTab') }}
        </a>
      </li>
    </ul>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('leave.fields.periodYear') }}</label>
            <input v-model.number="filters.periodYear" type="number" class="form-control form-control-sm" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('leave.fields.leaveType') }}</label>
            <select v-model="filters.leaveTypeId" class="form-select form-select-sm">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option v-for="lt in leaveTypes" :key="lt.id" :value="lt.id">{{ lt.label || lt.code }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <button class="btn btn-primary btn-sm w-100" @click="doFetch">
              {{ $t('common.filter') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Usage Report -->
    <div v-if="tab === 'usage'" class="card">
      <div class="card-body py-3">
        <div v-if="leaveStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="leaveStore.usageReport.length === 0" class="text-center py-15 text-muted">
          {{ $t('leave.reports.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('leave.fields.educator') }}</th>
                <th>{{ $t('leave.fields.leaveType') }}</th>
                <th>{{ $t('leave.fields.periodYear') }}</th>
                <th class="text-end">{{ $t('leave.balance.entitled') }}</th>
                <th class="text-end">{{ $t('leave.balance.used') }}</th>
                <th class="text-end">{{ $t('leave.balance.remaining') }}</th>
                <th class="text-end pe-4">{{ $t('leave.reports.requestCount') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in leaveStore.usageReport" :key="r.educatorId + r.leaveTypeCode + r.periodYear">
                <td class="ps-4 fw-semibold">{{ r.educatorFullName }}</td>
                <td>{{ r.leaveTypeCode ?? '—' }}</td>
                <td>{{ r.periodYear }}</td>
                <td class="text-end">{{ r.entitled }} {{ r.unit }}</td>
                <td class="text-end text-warning">{{ r.used }}</td>
                <td class="text-end text-success fw-bold">{{ r.remaining }}</td>
                <td class="text-end pe-4">{{ r.requestCount }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Trend Report -->
    <div v-if="tab === 'trend'" class="card">
      <div class="card-body py-3">
        <div v-if="leaveStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="leaveStore.trendReport.length === 0" class="text-center py-15 text-muted">
          {{ $t('leave.reports.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('leave.reports.year') }}</th>
                <th>{{ $t('leave.reports.month') }}</th>
                <th class="text-end">{{ $t('leave.reports.requestCount') }}</th>
                <th class="text-end">{{ $t('leave.status.approved') }}</th>
                <th class="text-end">{{ $t('leave.status.rejected') }}</th>
                <th class="text-end">{{ $t('leave.status.cancelled') }}</th>
                <th class="text-end pe-4">{{ $t('leave.reports.totalDaysApproved') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in leaveStore.trendReport" :key="`${t.year}-${t.month}`">
                <td class="ps-4">{{ t.year }}</td>
                <td>{{ monthName(t.month) }}</td>
                <td class="text-end">{{ t.requestCount }}</td>
                <td class="text-end text-success">{{ t.approvedCount }}</td>
                <td class="text-end text-danger">{{ t.rejectedCount }}</td>
                <td class="text-end text-muted">{{ t.cancelledCount }}</td>
                <td class="text-end pe-4 fw-bold">{{ t.totalDaysApproved }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'

const leaveStore = useLeaveStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()
const tab = ref<'usage' | 'trend'>('usage')

const filters = reactive({
  periodYear: new Date().getFullYear(),
  leaveTypeId: '',
})

const leaveTypes = computed(() => refDataStore.getByCategory?.('leave_type') ?? [])

const MONTHS = ['Ocak','Şubat','Mart','Nisan','Mayıs','Haziran','Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık']
function monthName(m: number) { return MONTHS[m - 1] ?? m }

async function doFetch() {
  const q = {
    corporationId: authStore.user?.corporationId,
    periodYear: filters.periodYear || undefined,
    leaveTypeId: filters.leaveTypeId || undefined,
  }
  await Promise.all([
    leaveStore.fetchUsageReport(q),
    leaveStore.fetchTrendReport(q),
  ])
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('leave_type')
  await doFetch()
})
</script>
