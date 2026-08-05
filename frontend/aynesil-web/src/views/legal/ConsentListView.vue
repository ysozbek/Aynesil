<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsentStore } from '@/stores/consent.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ConsentListQuery, StudentConsentListItemDto } from '@/types/legal.types'

const { t } = useI18n()
const router = useRouter()
const consentStore = useConsentStore()
const auth = useAuthStore()

const filters = reactive<ConsentListQuery>({
  page: 1,
  pageSize: 20,
  state: '',
  corporationId: auth.user?.corporationId,
})

const columns: Column<StudentConsentListItemDto>[] = [
  { key: 'studentFullName', label: t('legal.consent.fields.student') },
  { key: 'consentTypeCode', label: t('legal.consent.fields.consentType') },
  { key: 'templateCode', label: t('legal.consent.fields.template') },
  { key: 'grantedAt', label: t('legal.consent.fields.grantedAt'), width: '120px' },
  { key: 'validUntil', label: t('legal.consent.fields.validUntil'), width: '110px' },
  { key: 'state', label: t('common.status'), width: '120px' },
]

function formatDate(dt: unknown) {
  if (!dt) return '—'
  return new Date(String(dt)).toLocaleDateString('tr-TR')
}

function stateClass(s: string) {
  const map: Record<string, string> = {
    Granted: 'bg-green-100 text-green-700',
    Withdrawn: 'bg-red-100 text-red-700',
    Pending: 'bg-amber-100 text-amber-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function stateLabel(s: string) {
  return t(`legal.consent.state.${s.toLowerCase()}`, s)
}

function doFetch() {
  filters.page = 1
  consentStore.fetchConsents(filters)
}

function resetFilters() {
  filters.state = ''
  filters.page = 1
  consentStore.fetchConsents(filters)
}

watch(
  () => filters.page,
  () => consentStore.fetchConsents(filters)
)

onMounted(() => consentStore.fetchConsents(filters))
</script>

<template>
  <div>
    <PageHeader :title="t('legal.consent.list.title')" :description="t('legal.consent.list.subtitle')" />

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.state"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="Granted">{{ t('legal.consent.state.granted') }}</option>
          <option value="Withdrawn">{{ t('legal.consent.state.withdrawn') }}</option>
          <option value="Pending">{{ t('legal.consent.state.pending') }}</option>
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
      :rows="consentStore.consents.items"
      :loading="consentStore.loading"
      :empty-text="t('legal.consent.list.noData')"
      @row-click="(row) => router.push({ name: 'consent-detail', params: { id: row.id } })"
    >
      <template #cell-studentFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-consentTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-templateCode="{ row }">
        <span class="text-muted-foreground">
          {{ row.templateCode ? `${row.templateCode} v${row.templateVersion}` : '—' }}
        </span>
      </template>
      <template #cell-grantedAt="{ value }">{{ formatDate(value) }}</template>
      <template #cell-validUntil="{ value }">{{ value ?? '—' }}</template>
      <template #cell-state="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', stateClass(String(value))]">
          {{ stateLabel(String(value)) }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'consent-detail', params: { id: row.id } })"
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
        :page="consentStore.consents.page"
        :page-size="consentStore.consents.pageSize"
        :total-count="consentStore.consents.totalCount"
        :total-pages="consentStore.consents.totalPages"
        :has-previous-page="consentStore.consents.hasPreviousPage"
        :has-next-page="consentStore.consents.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; consentStore.fetchConsents(filters) }"
      />
    </div>
  </div>
</template>
