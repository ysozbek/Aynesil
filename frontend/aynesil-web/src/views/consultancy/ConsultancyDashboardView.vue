<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('consultancy.dashboard.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('consultancy.dashboard.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('institution:create')" to="/consultancy/institutions/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('consultancy.institution.new') }}
      </RouterLink>
    </div>

    <!-- Stats -->
    <div class="row g-5 mb-6">
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-primary">
                <i class="ki-outline ki-home fs-1 text-primary"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-primary">{{ consultancyStore.institutions.totalCount }}</div>
              <div class="text-muted fs-7">{{ $t('consultancy.dashboard.totalInstitutions') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-success">
                <i class="ki-outline ki-document fs-1 text-success"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-success">{{ consultancyStore.plans.totalCount }}</div>
              <div class="text-muted fs-7">{{ $t('consultancy.dashboard.activePlans') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-warning">
                <i class="ki-outline ki-calendar fs-1 text-warning"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-warning">{{ consultancyStore.visits.totalCount }}</div>
              <div class="text-muted fs-7">{{ $t('consultancy.dashboard.totalVisits') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-danger">
                <i class="ki-outline ki-time fs-1 text-danger"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-danger">{{ pendingFollowUps }}</div>
              <div class="text-muted fs-7">{{ $t('consultancy.dashboard.openFollowUps') }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-5">
      <!-- Upcoming Visits -->
      <div class="col-xl-6">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title fw-bold">{{ $t('consultancy.dashboard.upcomingVisits') }}</h3>
            <div class="card-toolbar">
              <RouterLink to="/consultancy/visits" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="consultancyStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="consultancyStore.visits.items.length === 0" class="text-center py-10 text-muted">
              {{ $t('consultancy.dashboard.noVisits') }}
            </div>
            <div v-else>
              <div v-for="v in upcomingVisits" :key="v.id" class="d-flex align-items-center mb-4 p-3 rounded bg-light">
                <div class="symbol symbol-45px me-4">
                  <span class="symbol-label bg-light-primary">
                    <i class="ki-outline ki-calendar fs-2 text-primary"></i>
                  </span>
                </div>
                <div class="flex-grow-1">
                  <div class="fw-semibold">{{ v.institutionName }}</div>
                  <div class="text-muted fs-7">{{ v.planName ?? '—' }} · {{ v.visitDate }}</div>
                </div>
                <span :class="visitStatusBadge(v.status)">{{ v.status }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Open Follow-Ups -->
      <div class="col-xl-6">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title fw-bold">{{ $t('consultancy.dashboard.openFollowUpsList') }}</h3>
            <div class="card-toolbar">
              <RouterLink to="/consultancy/follow-ups" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="consultancyStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="consultancyStore.followUps.items.length === 0" class="text-center py-10 text-muted">
              {{ $t('consultancy.dashboard.noFollowUps') }}
            </div>
            <div v-else>
              <div v-for="f in consultancyStore.followUps.items.slice(0, 6)" :key="f.id" class="d-flex align-items-center mb-3 p-3 rounded bg-light">
                <div class="flex-grow-1">
                  <div class="fw-semibold">{{ f.title }}</div>
                  <div class="text-muted fs-7">{{ f.planName ?? '—' }} · {{ f.dueDate ?? '—' }}</div>
                </div>
                <span :class="followUpStatusBadge(f.status)">{{ f.status }}</span>
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
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'

const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()

function hasPermission(p: string) { return authStore.hasPermission(p) }

const upcomingVisits = computed(() =>
  consultancyStore.visits.items.filter(v => v.status !== 'Completed' && v.status !== 'Cancelled').slice(0, 5)
)
const pendingFollowUps = computed(() =>
  consultancyStore.followUps.items.filter(f => f.status === 'Open' || f.status === 'InProgress').length
)

function visitStatusBadge(s: string) {
  const map: Record<string, string> = {
    Scheduled: 'badge badge-light-primary', Completed: 'badge badge-light-success',
    Cancelled: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

function followUpStatusBadge(s: string) {
  const map: Record<string, string> = {
    Open: 'badge badge-light-warning', InProgress: 'badge badge-light-info',
    Completed: 'badge badge-light-success', Cancelled: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

onMounted(async () => {
  const corp = authStore.user?.corporationId
  await Promise.all([
    consultancyStore.fetchInstitutions({ corporationId: corp }),
    consultancyStore.fetchPlans({ corporationId: corp, status: 'Active' }),
    consultancyStore.fetchVisits({ corporationId: corp, pageSize: 10 }),
    consultancyStore.fetchFollowUps({ corporationId: corp, status: 'Open', pageSize: 10 }),
  ])
})
</script>
