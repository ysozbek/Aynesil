<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { useConsentStore } from '@/stores/consent.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type {
  ContractReportItemDto,
  ConsentReportItemDto,
  SignatureReportItemDto,
} from '@/types/legal.types'

const { t } = useI18n()
const contractStore = useContractStore()
const consentStore = useConsentStore()
const auth = useAuthStore()
const tab = ref<'contracts' | 'consents' | 'signatures'>('contracts')

type ConsentReportRow = ConsentReportItemDto & { id: string }

const consentReportRows = computed<ConsentReportRow[]>(() =>
  consentStore.consentReport.map((r, i) => ({
    ...r,
    id: `${r.studentId}-${r.consentTypeCode ?? i}`,
  }))
)

const contractReportColumns: Column<ContractReportItemDto>[] = [
  { key: 'studentFullName', label: t('legal.contract.fields.student') },
  { key: 'totalContracts', label: t('legal.reports.total'), width: '90px', align: 'center' },
  { key: 'draftContracts', label: t('legal.contract.status.draft'), width: '90px', align: 'center' },
  { key: 'activeContracts', label: t('legal.contract.status.active'), width: '90px', align: 'center' },
  { key: 'expiredContracts', label: t('legal.contract.status.expired'), width: '90px', align: 'center' },
  { key: 'terminatedContracts', label: t('legal.contract.status.terminated'), width: '90px', align: 'center' },
]

const consentReportColumns: Column<ConsentReportItemDto>[] = [
  { key: 'studentFullName', label: t('legal.consent.fields.student') },
  { key: 'consentTypeCode', label: t('legal.consent.fields.consentType') },
  { key: 'isMandatory', label: t('legal.consent.fields.mandatory'), width: '110px' },
  { key: 'hasGrantedConsent', label: t('legal.consent.state.granted'), width: '100px', align: 'center' },
  { key: 'grantedAt', label: t('legal.consent.fields.grantedAt'), width: '120px' },
  { key: 'validUntil', label: t('legal.consent.fields.validUntil'), width: '110px', align: 'right' },
]

const signatureReportColumns: Column<SignatureReportItemDto>[] = [
  { key: 'studentFullName', label: t('legal.contract.fields.student') },
  { key: 'status', label: t('common.status'), width: '120px' },
  { key: 'signatureMethod', label: t('legal.contract.fields.signatureMethod') },
  { key: 'signedByName', label: t('legal.contract.fields.signedBy') },
  { key: 'signedAt', label: t('legal.contract.fields.signedAt'), width: '120px' },
  { key: 'hasSignedFile', label: t('legal.signature.hasFile'), width: '110px', align: 'right' },
]

function formatDate(dt: unknown) {
  if (!dt) return '—'
  return new Date(String(dt)).toLocaleDateString('tr-TR')
}

function statusClass(s: string) {
  const map: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-600',
    Sent: 'bg-amber-100 text-amber-700',
    Active: 'bg-green-100 text-green-700',
    Expired: 'bg-gray-100 text-gray-700',
    Terminated: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function statusLabel(s: string) {
  return t(`legal.contract.status.${s.toLowerCase()}`, s)
}

onMounted(async () => {
  const corp = auth.user?.corporationId
  const q = { corporationId: corp }
  await Promise.all([
    contractStore.fetchContractReport(q),
    consentStore.fetchConsentReport(q),
    contractStore.fetchSignatureReport(q),
  ])
})
</script>

<template>
  <div>
    <PageHeader :title="t('legal.reports.title')" :description="t('legal.reports.subtitle')" />

    <div class="flex gap-1 mb-4 border-b border-border">
      <button
        type="button"
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          tab === 'contracts' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground',
        ]"
        @click="tab = 'contracts'"
      >
        {{ t('legal.reports.contractsTab') }}
      </button>
      <button
        type="button"
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          tab === 'consents' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground',
        ]"
        @click="tab = 'consents'"
      >
        {{ t('legal.reports.consentsTab') }}
      </button>
      <button
        type="button"
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          tab === 'signatures' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground',
        ]"
        @click="tab = 'signatures'"
      >
        {{ t('legal.reports.signaturesTab') }}
      </button>
    </div>

    <DataTable
      v-if="tab === 'contracts'"
      :columns="contractReportColumns"
      :rows="contractStore.contractReport"
      :loading="contractStore.loading"
      :empty-text="t('legal.reports.noData')"
      row-key="studentId"
    >
      <template #cell-studentFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-totalContracts="{ value }">{{ value ?? 0 }}</template>
      <template #cell-draftContracts="{ value }">
        <span class="text-muted-foreground">{{ value ?? 0 }}</span>
      </template>
      <template #cell-activeContracts="{ value }">
        <span class="font-semibold text-green-600">{{ value ?? 0 }}</span>
      </template>
      <template #cell-expiredContracts="{ value }">
        <span class="text-amber-600">{{ value ?? 0 }}</span>
      </template>
      <template #cell-terminatedContracts="{ value }">
        <span class="text-red-600">{{ value ?? 0 }}</span>
      </template>
    </DataTable>

    <DataTable
      v-else-if="tab === 'consents'"
      :columns="consentReportColumns"
      :rows="consentReportRows"
      :loading="consentStore.loading"
      :empty-text="t('legal.reports.noData')"
    >
      <template #cell-studentFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-consentTypeCode="{ value }">{{ value ?? '—' }}</template>
      <template #cell-isMandatory="{ value }">
        <span
          v-if="value"
          class="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700"
        >
          {{ t('legal.consent.mandatory') }}
        </span>
        <span v-else class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
          {{ t('legal.consent.optional') }}
        </span>
      </template>
      <template #cell-hasGrantedConsent="{ value }">
        <svg
          v-if="value"
          class="w-5 h-5 text-green-600 mx-auto"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <svg
          v-else
          class="w-5 h-5 text-red-500 mx-auto"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </template>
      <template #cell-grantedAt="{ value }">
        <span class="text-muted-foreground">{{ formatDate(value) }}</span>
      </template>
      <template #cell-validUntil="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
    </DataTable>

    <DataTable
      v-else
      :columns="signatureReportColumns"
      :rows="contractStore.signatureReport"
      :loading="contractStore.loading"
      :empty-text="t('legal.reports.noData')"
      row-key="contractId"
    >
      <template #cell-studentFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
          {{ statusLabel(String(value)) }}
        </span>
      </template>
      <template #cell-signatureMethod="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-signedByName="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-signedAt="{ value }">
        <span class="text-muted-foreground">{{ formatDate(value) }}</span>
      </template>
      <template #cell-hasSignedFile="{ value }">
        <svg
          v-if="value"
          class="w-5 h-5 text-green-600 ml-auto"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <svg
          v-else
          class="w-5 h-5 text-muted-foreground ml-auto"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </template>
    </DataTable>
  </div>
</template>
