<script setup lang="ts">
/**
 * Observations are stored per school visit (no top-level list API).
 * This screen lists visits with observation counts as the entry point.
 */
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { SchoolVisitListItemDto, VisitListQuery } from '@/types/consultancy.types'

const { t } = useI18n()
const router = useRouter()
const store = useConsultancyStore()
const auth = useAuthStore()

const filters = reactive<VisitListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  status: '',
  corporationId: auth.user?.corporationId,
})

const columns: Column<SchoolVisitListItemDto>[] = [
  { key: 'institutionName', label: t('consultancy.institution.fields.name') },
  { key: 'planName', label: t('consultancy.visit.fields.plan') },
  { key: 'visitDate', label: t('consultancy.visit.fields.visitDate'), width: '120px' },
  { key: 'observationCount', label: t('consultancy.visit.fields.observations'), width: '110px' },
  { key: 'status', label: t('common.status'), width: '110px' },
]

function formatDate(val: unknown) {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function statusClass(status: string) {
  const map: Record<string, string> = {
    scheduled: 'bg-amber-100 text-amber-700',
    completed: 'bg-green-100 text-green-700',
    cancelled: 'bg-gray-100 text-gray-600',
  }
  return map[status?.toLowerCase()] ?? 'bg-gray-100 text-gray-600'
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { filters.page = 1; store.fetchVisits(filters) }, 400)
}

watch(() => filters.page, () => store.fetchVisits(filters))
onMounted(() => store.fetchVisits(filters))
</script>

<template>
  <div>
    <PageHeader
      :title="t('consultancy.observation.list.title')"
      :description="t('consultancy.observation.list.subtitle')"
    />

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @input="debouncedFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.status"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="() => { filters.page = 1; store.fetchVisits(filters) }"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="scheduled">{{ t('consultancy.visit.status.scheduled') }}</option>
          <option value="completed">{{ t('consultancy.visit.status.completed') }}</option>
          <option value="cancelled">{{ t('consultancy.visit.status.cancelled') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.visits.items"
      :loading="store.loading"
      :empty-text="t('consultancy.observation.list.noData')"
      @row-click="(row) => router.push({ name: 'consultancy-visit-detail', params: { id: row.id } })"
    >
      <template #cell-planName="{ value }">{{ value ?? '—' }}</template>
      <template #cell-visitDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-observationCount="{ value }">
        <span class="font-medium">{{ value ?? 0 }}</span>
      </template>
      <template #cell-status="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
          {{ t(`consultancy.visit.status.${String(value).toLowerCase()}`, String(value)) }}
        </span>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.visits.page"
        :page-size="store.visits.pageSize"
        :total-count="store.visits.totalCount"
        :total-pages="store.visits.totalPages"
        :has-previous-page="store.visits.hasPreviousPage"
        :has-next-page="store.visits.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; store.fetchVisits(filters) }"
      />
    </div>
  </div>
</template>
