<template>
  <div class="container-xxl py-6">
    <!-- Header -->
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('leave.dashboard.title') }}</h1>
        <p class="text-muted fs-6 mb-0">{{ $t('leave.dashboard.subtitle') }}</p>
      </div>
      <RouterLink
        v-if="hasPermission('leave_request:submit')"
        to="/leave/requests/new"
        class="btn btn-primary"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>
        {{ $t('leave.request.new') }}
      </RouterLink>
    </div>

    <!-- Stats Row -->
    <div class="row g-5 mb-6">
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-warning">
                <i class="ki-outline ki-time fs-1 text-warning"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-warning">{{ pendingCount }}</div>
              <div class="text-muted fs-7">{{ $t('leave.dashboard.pendingApprovals') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-success">
                <i class="ki-outline ki-check fs-1 text-success"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-success">{{ approvedCount }}</div>
              <div class="text-muted fs-7">{{ $t('leave.dashboard.approvedThisMonth') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-primary">
                <i class="ki-outline ki-calendar fs-1 text-primary"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-primary">{{ upcomingCount }}</div>
              <div class="text-muted fs-7">{{ $t('leave.dashboard.upcomingLeaves') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-info">
                <i class="ki-outline ki-people fs-1 text-info"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-info">{{ leaveStore.balances.length }}</div>
              <div class="text-muted fs-7">{{ $t('leave.dashboard.balanceRecords') }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-5">
      <!-- Pending Requests -->
      <div class="col-xl-8">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title align-items-start flex-column">
              <span class="card-label fw-bold text-gray-900">{{ $t('leave.dashboard.pendingRequests') }}</span>
            </h3>
            <div class="card-toolbar">
              <RouterLink to="/leave/requests?status=Pending" class="btn btn-sm btn-light">
                {{ $t('common.viewAll') }}
              </RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="leaveStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="pendingLeaves.length === 0" class="text-center py-10 text-muted">
              {{ $t('leave.dashboard.noPending') }}
            </div>
            <div v-else class="table-responsive">
              <table class="table table-row-dashed align-middle gs-0 gy-4">
                <thead>
                  <tr class="fw-bold text-muted bg-light">
                    <th class="ps-4">{{ $t('leave.fields.educator') }}</th>
                    <th>{{ $t('leave.fields.leaveType') }}</th>
                    <th>{{ $t('leave.fields.period') }}</th>
                    <th>{{ $t('common.status') }}</th>
                    <th class="text-end pe-4">{{ $t('common.actions') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in pendingLeaves" :key="item.id">
                    <td class="ps-4 fw-semibold">{{ item.educatorFullName ?? '—' }}</td>
                    <td>{{ item.leaveTypeCode ?? '—' }}</td>
                    <td class="text-muted fs-7">
                      {{ formatDate(item.startsAt) }} – {{ formatDate(item.endsAt) }}
                    </td>
                    <td>
                      <span class="badge badge-light-warning fw-bold">{{ $t('leave.status.pending') }}</span>
                    </td>
                    <td class="text-end pe-4">
                      <RouterLink :to="`/leave/requests/${item.id}`" class="btn btn-sm btn-light-primary">
                        {{ $t('common.view') }}
                      </RouterLink>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- Leave Balances Summary -->
      <div class="col-xl-4">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title">
              <span class="card-label fw-bold text-gray-900">{{ $t('leave.dashboard.balanceSummary') }}</span>
            </h3>
            <div class="card-toolbar">
              <RouterLink to="/leave/balances" class="btn btn-sm btn-light">
                {{ $t('common.viewAll') }}
              </RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="leaveStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="leaveStore.balances.length === 0" class="text-center py-10 text-muted">
              {{ $t('leave.dashboard.noBalances') }}
            </div>
            <div v-else>
              <div
                v-for="bal in leaveStore.balances.slice(0, 6)"
                :key="bal.id"
                class="d-flex align-items-center mb-5"
              >
                <div class="flex-grow-1">
                  <div class="fw-semibold text-gray-800 fs-7">{{ bal.educatorFullName }}</div>
                  <div class="text-muted fs-8">{{ bal.leaveTypeCode }} · {{ bal.periodYear }}</div>
                </div>
                <div class="text-end">
                  <div class="fw-bold text-gray-900">{{ bal.remaining }} / {{ bal.entitled }}</div>
                  <div class="text-muted fs-8">{{ bal.unit }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'

const leaveStore = useLeaveStore()
const authStore = useAuthStore()

function hasPermission(p: string) {
  return authStore.hasPermission(p)
}

function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('tr-TR')
}

const pendingLeaves = computed(() =>
  leaveStore.leaveList.items.filter(l => l.status === 'Pending')
)
const pendingCount = computed(() => pendingLeaves.value.length)
const approvedCount = computed(() =>
  leaveStore.leaveList.items.filter(l => l.status === 'Approved').length
)
const upcomingCount = computed(() => {
  const now = new Date()
  return leaveStore.leaveList.items.filter(l => {
    return l.status === 'Approved' && new Date(l.startsAt) > now
  }).length
})

onMounted(async () => {
  const corp = authStore.user?.corporationId
  await Promise.all([
    leaveStore.fetchLeaves({ corporationId: corp, pageSize: 50 }),
    leaveStore.fetchBalances({ corporationId: corp }),
  ])
})
</script>
