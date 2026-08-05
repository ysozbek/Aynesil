<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ContractListQuery, StudentContractListItemDto } from '@/types/legal.types'

const { t } = useI18n()
const router = useRouter()
const contractStore = useContractStore()
const auth = useAuthStore()
const { can } = usePermission()

const filters = reactive<ContractListQuery>({
  page: 1,
  pageSize: 20,
  status: '',
  corporationId: auth.user?.corporationId,
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
        v-if="can('student_contract:generate')"
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
          <option value="Draft">{{ t('legal.contract.status.draft') }}</option>
          <option value="Sent">{{ t('legal.contract.status.sent') }}</option>
          <option value="Active">{{ t('legal.contract.status.active') }}</option>
          <option value="Expired">{{ t('legal.contract.status.expired') }}</option>
          <option value="Terminated">{{ t('legal.contract.status.terminated') }}</option>
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
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
          {{ statusLabel(String(value)) }}
        </span>
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
