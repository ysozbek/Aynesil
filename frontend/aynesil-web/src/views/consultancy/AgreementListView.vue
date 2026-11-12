<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { ConsultancyAgreementListItemDto, AgreementListQuery } from '@/types/consultancy.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const store = useConsultancyStore()
const refDataStore = useRefDataStore()
const { can } = usePermission()

const filters = reactive<AgreementListQuery>({
  page: 1,
  pageSize: 20,
  status: '',
  agreementTypeId: '',
})

const agreementTypes = ref<Awaited<ReturnType<typeof refDataStore.getValues>>>([])

const columns: Column<ConsultancyAgreementListItemDto>[] = [
  { key: 'title', label: t('consultancyContract.fields.title') },
  { key: 'institutionName', label: t('consultancyContract.fields.institution') },
  { key: 'planName', label: t('consultancyContract.fields.plan') },
  { key: 'agreementTypeCode', label: t('consultancyContract.fields.type'), width: '100px' },
  { key: 'startDate', label: t('consultancyContract.fields.startDate'), width: '100px' },
  { key: 'endDate', label: t('consultancyContract.fields.endDate'), width: '100px' },
  { key: 'signedDate', label: t('consultancyContract.fields.signedDate'), width: '100px' },
  { key: 'status', label: t('common.status'), width: '100px' },
]

async function doFetch() {
  filters.page = 1
  await store.fetchAgreements(filters)
}

function resetFilters() {
  filters.status = ''
  filters.agreementTypeId = ''
  filters.page = 1
  doFetch()
}

watch(
  () => filters.page,
  () => store.fetchAgreements(filters)
)

onMounted(async () => {
  agreementTypes.value = await refDataStore.getValues('agreement_type')
  await doFetch()
})
</script>

<template>
  <div>
    <PageHeader :title="t('consultancyContract.title')" :description="t('consultancyContract.subtitle')">
      <button
        v-if="can(Permissions.ConsultancyAgreements.Create)"
        @click="router.push('/consultancy/agreements/new')"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('consultancyContract.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="draft">{{ t('statusBadge.consultancyAgreement.draft') }}</option>
          <option value="sent">{{ t('statusBadge.consultancyAgreement.sent') }}</option>
          <option value="signed">{{ t('statusBadge.consultancyAgreement.signed') }}</option>
          <option value="expired">{{ t('statusBadge.consultancyAgreement.expired') }}</option>
          <option value="cancelled">{{ t('statusBadge.consultancyAgreement.cancelled') }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('consultancyContract.agreementType') }}</label>
        <select v-model="filters.agreementTypeId" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option v-for="type in agreementTypes" :key="type.id" :value="type.id">{{ type.label || type.code }}</option>
        </select>
      </div>
      <button @click="resetFilters" class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent">
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.agreements.items"
      :loading="store.loading"
      :empty-text="t('consultancyContract.noData')"
      @row-click="(row) => router.push(`/consultancy/agreements/${row.id}`)"
    >
      <template #cell-title="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-institutionName="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-planName="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-agreementTypeCode="{ value }">{{ value ?? '—' }}</template>
      <template #cell-startDate="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-endDate="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-signedDate="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ value }">
        <StatusBadge entity="consultancyAgreement" :status="String(value)" />
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.agreements.page"
        :page-size="store.agreements.pageSize"
        :total-count="store.agreements.totalCount"
        :total-pages="store.agreements.totalPages"
        :has-previous-page="store.agreements.hasPreviousPage"
        :has-next-page="store.agreements.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; store.fetchAgreements(filters) }"
      />
    </div>
  </div>
</template>
