<script setup lang="ts">
import { ContractStatus } from '@/constants/statuses'
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { ContractListQuery, StudentContractListItemDto } from '@/types/legal.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const contractStore = useContractStore()
const { can } = usePermission()

const filters = reactive<ContractListQuery>({
  page: 1,
  pageSize: 20,
  status: '',
})

const columns: Column<StudentContractListItemDto>[] = [
  { key: 'studentFullName', label: t('legal.contract.fields.student') },
  { key: 'templateCode', label: t('legal.contract.fields.template') },
  { key: 'startsOn', label: t('legal.contract.fields.startsOn'), width: '110px' },
  { key: 'endsOn', label: t('legal.contract.fields.endsOn'), width: '110px' },
  { key: 'signedAt', label: t('legal.contract.fields.signedAt'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '120px' },
]

function formatDate(dt: unknown) {
  if (!dt) return '—'
  return new Date(String(dt)).toLocaleDateString('tr-TR')
}

function doFetch() {
  filters.page = 1
  contractStore.fetchContracts(filters)
}

function resetFilters() {
  filters.status = ''
  filters.page = 1
  contractStore.fetchContracts(filters)
}

watch(
  () => filters.page,
  () => contractStore.fetchContracts(filters)
)

onMounted(() => contractStore.fetchContracts(filters))
</script>

<template>
  <div>
    <PageHeader :title="t('legal.contract.list.title')" :description="t('legal.contract.list.subtitle')">
      <button
        v-if="can(Permissions.StudentContracts.Generate)"
        @click="router.push({ name: 'contract-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('legal.contract.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.status"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option :value="ContractStatus.Draft">{{ t('statusBadge.contract.draft') }}</option>
          <option :value="ContractStatus.Sent">{{ t('statusBadge.contract.sent') }}</option>
          <option :value="ContractStatus.Signed">{{ t('statusBadge.contract.signed') }}</option>
          <option :value="ContractStatus.Active">{{ t('statusBadge.contract.active') }}</option>
          <option :value="ContractStatus.Expired">{{ t('statusBadge.contract.expired') }}</option>
          <option :value="ContractStatus.Terminated">{{ t('statusBadge.contract.terminated') }}</option>
        </select>
      </div>
      <button
        @click="resetFilters"
        class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="contractStore.contracts.items"
      :loading="contractStore.loading"
      :empty-text="t('legal.contract.list.noData')"
      @row-click="(row) => router.push({ name: 'contract-detail', params: { id: row.id } })"
    >
      <template #cell-studentFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-templateCode="{ row }">
        <span class="text-muted-foreground">
          {{ row.templateCode ? `${row.templateCode} v${row.templateVersion}` : '—' }}
        </span>
      </template>
      <template #cell-startsOn="{ value }">{{ value ?? '—' }}</template>
      <template #cell-endsOn="{ value }">{{ value ?? '—' }}</template>
      <template #cell-signedAt="{ value }">{{ formatDate(value) }}</template>
      <template #cell-status="{ value }">
        <StatusBadge entity="contract" :status="String(value)" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'contract-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="contractStore.contracts.page"
        :page-size="contractStore.contracts.pageSize"
        :total-count="contractStore.contracts.totalCount"
        :total-pages="contractStore.contracts.totalPages"
        :has-previous-page="contractStore.contracts.hasPreviousPage"
        :has-next-page="contractStore.contracts.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; contractStore.fetchContracts(filters) }"
      />
    </div>
  </div>
</template>
