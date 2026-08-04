<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('legal.dashboard.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('legal.dashboard.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('student_contract:generate')" to="/legal/contracts/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('legal.contract.new') }}
      </RouterLink>
    </div>

    <!-- Stats -->
    <div class="row g-5 mb-6">
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-warning">
                <i class="ki-outline ki-pencil fs-1 text-warning"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-warning">{{ pendingSignatures }}</div>
              <div class="text-muted fs-7">{{ $t('legal.dashboard.pendingSignatures') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-success">
                <i class="ki-outline ki-check-circle fs-1 text-success"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-success">{{ activeContracts }}</div>
              <div class="text-muted fs-7">{{ $t('legal.dashboard.activeContracts') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-info">
                <i class="ki-outline ki-shield-tick fs-1 text-info"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-info">{{ grantedConsents }}</div>
              <div class="text-muted fs-7">{{ $t('legal.dashboard.grantedConsents') }}</div>
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
              <div class="fs-2 fw-bold text-danger">{{ expiringContracts }}</div>
              <div class="text-muted fs-7">{{ $t('legal.dashboard.expiringSoon') }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-5">
      <!-- Recent Contracts -->
      <div class="col-xl-6">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title fw-bold">{{ $t('legal.dashboard.recentContracts') }}</h3>
            <div class="card-toolbar">
              <RouterLink to="/legal/contracts" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="contractStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="contractStore.contracts.items.length === 0" class="text-center py-10 text-muted">
              {{ $t('legal.dashboard.noContracts') }}
            </div>
            <div v-else>
              <div v-for="c in contractStore.contracts.items.slice(0, 6)" :key="c.id" class="d-flex align-items-center mb-4 p-3 rounded bg-light">
                <div class="flex-grow-1">
                  <div class="fw-semibold">{{ c.studentFullName ?? '—' }}</div>
                  <div class="text-muted fs-7">{{ c.templateCode ?? '—' }} v{{ c.templateVersion }}</div>
                </div>
                <span :class="contractStatusBadge(c.status)">{{ c.status }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Consent Status -->
      <div class="col-xl-6">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title fw-bold">{{ $t('legal.dashboard.consentStatus') }}</h3>
            <div class="card-toolbar">
              <RouterLink to="/legal/consents" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="consentStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="consentStore.consents.items.length === 0" class="text-center py-10 text-muted">
              {{ $t('legal.dashboard.noConsents') }}
            </div>
            <div v-else>
              <div v-for="c in consentStore.consents.items.slice(0, 6)" :key="c.id" class="d-flex align-items-center mb-4 p-3 rounded bg-light">
                <div class="flex-grow-1">
                  <div class="fw-semibold">{{ c.studentFullName ?? '—' }}</div>
                  <div class="text-muted fs-7">{{ c.consentTypeCode ?? '—' }}</div>
                </div>
                <span :class="consentStateBadge(c.state)">{{ c.state }}</span>
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
import { useContractStore } from '@/stores/contract.store'
import { useConsentStore } from '@/stores/consent.store'
import { useAuthStore } from '@/stores/auth.store'

const contractStore = useContractStore()
const consentStore = useConsentStore()
const authStore = useAuthStore()

function hasPermission(p: string) { return authStore.hasPermission(p) }

const pendingSignatures = computed(() => contractStore.contracts.items.filter(c => c.status === 'Sent').length)
const activeContracts = computed(() => contractStore.contracts.items.filter(c => c.status === 'Active').length)
const grantedConsents = computed(() => consentStore.consents.items.filter(c => c.state === 'Granted').length)
const expiringContracts = computed(() => {
  const thirtyDays = Date.now() + 30 * 24 * 60 * 60 * 1000
  return contractStore.contracts.items.filter(c => c.status === 'Active' && c.endsOn && new Date(c.endsOn).getTime() < thirtyDays).length
})

function contractStatusBadge(s: string) {
  const map: Record<string, string> = {
    Draft: 'badge badge-light-secondary', Sent: 'badge badge-light-warning',
    Active: 'badge badge-light-success', Expired: 'badge badge-light-dark',
    Terminated: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

function consentStateBadge(s: string) {
  const map: Record<string, string> = {
    Granted: 'badge badge-light-success', Withdrawn: 'badge badge-light-danger', Pending: 'badge badge-light-warning',
  }
  return map[s] ?? 'badge badge-light'
}

onMounted(async () => {
  const corp = authStore.user?.corporationId
  await Promise.all([
    contractStore.fetchContracts({ corporationId: corp, pageSize: 20 }),
    consentStore.fetchConsents({ corporationId: corp, pageSize: 20 }),
  ])
})
</script>
