<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useKpiStore } from '@/stores/kpi.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { KpiDefinitionListItemDto, KpiDefinitionListQuery } from '@/types/kpi.types'

const { t } = useI18n()
const router = useRouter()
const kpiStore = useKpiStore()
const authStore = useAuthStore()
const { can } = usePermission()

const filters = reactive<KpiDefinitionListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  isActive: undefined,
  corporationId: authStore.user?.corporationId,
})

const columns: Column<KpiDefinitionListItemDto>[] = [
  { key: 'code', label: t('kpi.fields.code'), width: '120px' },
  { key: 'name', label: t('kpi.fields.name') },
  { key: 'categoryCode', label: t('kpi.fields.category'), width: '140px' },
  { key: 'unit', label: t('kpi.fields.unit'), width: '100px' },
  { key: 'isActive', label: t('common.status'), width: '110px' },
]

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(doFetch, 400)
}

async function doFetch() {
  filters.page = 1
  await kpiStore.fetchDefinitions(filters)
}

watch(
  () => filters.page,
  () => kpiStore.fetchDefinitions(filters)
)

onMounted(async () => {
  await kpiStore.fetchCategories(authStore.user?.corporationId)
  await kpiStore.fetchDefinitions(filters)
})
</script>

<template>
  <div>
    <PageHeader :title="t('kpi.definitions.title')" :description="t('kpi.definitions.subtitle')" />

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          @input="debouncedFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('kpi.fields.category') }}</label>
        <select
          v-model="filters.categoryId"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option v-for="c in kpiStore.categories" :key="c.id" :value="c.id">{{ c.label || c.code }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.isActive"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option :value="undefined">{{ t('common.allStatuses') }}</option>
          <option :value="true">{{ t('common.active') }}</option>
          <option :value="false">{{ t('common.passive') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="kpiStore.definitions.items"
      :loading="kpiStore.loading"
      :empty-text="t('kpi.definitions.noData')"
      @row-click="(row) => router.push({ name: 'kpi-definition-detail', params: { id: row.id } })"
    >
      <template #cell-code="{ value }">
        <span class="font-mono text-xs font-semibold text-foreground">{{ value }}</span>
      </template>
      <template #cell-name="{ value }">
        <span class="text-foreground">{{ value }}</span>
      </template>
      <template #cell-categoryCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-unit="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-isActive="{ value }">
        <span
          :class="[
            'px-2 py-0.5 rounded-full text-xs font-medium',
            value ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700',
          ]"
        >
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'kpi-definition-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('kpi:manage')"
            @click="kpiStore.toggleDefinition(row.id, !row.isActive)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="row.isActive ? t('common.passive') : t('common.active')"
          >
            <svg v-if="row.isActive" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="kpiStore.definitions.page"
        :page-size="kpiStore.definitions.pageSize"
        :total-count="kpiStore.definitions.totalCount"
        :total-pages="kpiStore.definitions.totalPages"
        :has-previous-page="kpiStore.definitions.hasPreviousPage"
        :has-next-page="kpiStore.definitions.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; kpiStore.fetchDefinitions(filters) }"
      />
    </div>
  </div>
</template>
