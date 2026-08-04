<template>
  <div class="container-xxl py-6">
    <div class="mb-6">
      <h1 class="text-gray-900 fw-bold fs-2">{{ $t('legal.reports.title') }}</h1>
      <p class="text-muted mb-0">{{ $t('legal.reports.subtitle') }}</p>
    </div>

    <!-- Tabs -->
    <ul class="nav nav-tabs nav-line-tabs mb-6">
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'contracts' }" href="#" @click.prevent="tab = 'contracts'">{{ $t('legal.reports.contractsTab') }}</a>
      </li>
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'consents' }" href="#" @click.prevent="tab = 'consents'">{{ $t('legal.reports.consentsTab') }}</a>
      </li>
      <li class="nav-item">
        <a class="nav-link" :class="{ active: tab === 'signatures' }" href="#" @click.prevent="tab = 'signatures'">{{ $t('legal.reports.signaturesTab') }}</a>
      </li>
    </ul>

    <!-- Contract Report -->
    <div v-if="tab === 'contracts'" class="card">
      <div class="card-body py-3">
        <div v-if="contractStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="contractStore.contractReport.length === 0" class="text-center py-15 text-muted">{{ $t('legal.reports.noData') }}</div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('legal.contract.fields.student') }}</th>
                <th class="text-center">{{ $t('legal.reports.total') }}</th>
                <th class="text-center">Draft</th>
                <th class="text-center">{{ $t('legal.contract.status.active') }}</th>
                <th class="text-center">{{ $t('legal.contract.status.expired') }}</th>
                <th class="text-center pe-4">{{ $t('legal.contract.status.terminated') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in contractStore.contractReport" :key="r.studentId">
                <td class="ps-4 fw-semibold">{{ r.studentFullName }}</td>
                <td class="text-center">{{ r.totalContracts }}</td>
                <td class="text-center text-muted">{{ r.draftContracts }}</td>
                <td class="text-center text-success fw-bold">{{ r.activeContracts }}</td>
                <td class="text-center text-warning">{{ r.expiredContracts }}</td>
                <td class="text-center text-danger pe-4">{{ r.terminatedContracts }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Consent Report -->
    <div v-if="tab === 'consents'" class="card">
      <div class="card-body py-3">
        <div v-if="consentStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="consentStore.consentReport.length === 0" class="text-center py-15 text-muted">{{ $t('legal.reports.noData') }}</div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('legal.consent.fields.student') }}</th>
                <th>{{ $t('legal.consent.fields.consentType') }}</th>
                <th>{{ $t('legal.consent.fields.mandatory') }}</th>
                <th>{{ $t('legal.consent.state.granted') }}</th>
                <th>{{ $t('legal.consent.fields.grantedAt') }}</th>
                <th class="text-end pe-4">{{ $t('legal.consent.fields.validUntil') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in consentStore.consentReport" :key="r.studentId + r.consentTypeCode">
                <td class="ps-4 fw-semibold">{{ r.studentFullName }}</td>
                <td>{{ r.consentTypeCode ?? '—' }}</td>
                <td>
                  <span v-if="r.isMandatory" class="badge badge-light-danger">{{ $t('legal.consent.mandatory') }}</span>
                  <span v-else class="badge badge-light">{{ $t('legal.consent.optional') }}</span>
                </td>
                <td>
                  <i v-if="r.hasGrantedConsent" class="ki-outline ki-check-circle fs-2 text-success"></i>
                  <i v-else class="ki-outline ki-cross-circle fs-2 text-danger"></i>
                </td>
                <td class="text-muted fs-7">{{ r.grantedAt ? formatDate(r.grantedAt) : '—' }}</td>
                <td class="text-end pe-4 text-muted fs-7">{{ r.validUntil ?? '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Signature Report -->
    <div v-if="tab === 'signatures'" class="card">
      <div class="card-body py-3">
        <div v-if="contractStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="contractStore.signatureReport.length === 0" class="text-center py-15 text-muted">{{ $t('legal.reports.noData') }}</div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('legal.contract.fields.student') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th>{{ $t('legal.contract.fields.signatureMethod') }}</th>
                <th>{{ $t('legal.contract.fields.signedBy') }}</th>
                <th>{{ $t('legal.contract.fields.signedAt') }}</th>
                <th class="text-end pe-4">{{ $t('legal.signature.hasFile') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in contractStore.signatureReport" :key="r.contractId">
                <td class="ps-4 fw-semibold">{{ r.studentFullName }}</td>
                <td><span :class="statusBadge(r.status)">{{ r.status }}</span></td>
                <td class="text-muted">{{ r.signatureMethod ?? '—' }}</td>
                <td class="text-muted">{{ r.signedByName ?? '—' }}</td>
                <td class="text-muted fs-7">{{ r.signedAt ? formatDate(r.signedAt) : '—' }}</td>
                <td class="text-end pe-4">
                  <i v-if="r.hasSignedFile" class="ki-outline ki-check-circle fs-2 text-success"></i>
                  <i v-else class="ki-outline ki-cross-circle fs-2 text-muted"></i>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useContractStore } from '@/stores/contract.store'
import { useConsentStore } from '@/stores/consent.store'
import { useAuthStore } from '@/stores/auth.store'

const contractStore = useContractStore()
const consentStore = useConsentStore()
const authStore = useAuthStore()
const tab = ref<'contracts' | 'consents' | 'signatures'>('contracts')

function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }

function statusBadge(s: string) {
  const map: Record<string, string> = {
    Draft: 'badge badge-light-secondary', Sent: 'badge badge-light-warning',
    Active: 'badge badge-light-success', Expired: 'badge badge-light-dark',
    Terminated: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

onMounted(async () => {
  const corp = authStore.user?.corporationId
  const q = { corporationId: corp }
  await Promise.all([
    contractStore.fetchContractReport(q),
    consentStore.fetchConsentReport(q),
    contractStore.fetchSignatureReport(q),
  ])
})
</script>
