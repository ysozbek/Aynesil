<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { InstitutionListItemDto, InstitutionListQuery } from '@/types/consultancy.types'

const { t } = useI18n()
const router = useRouter()
const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()
const { can } = usePermission()

const filters = reactive<InstitutionListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  city: '',
  corporationId: authStore.user?.corporationId,
})

const columns: Column<InstitutionListItemDto>[] = [
  { key: 'name', label: t('consultancy.institution.fields.name') },
  { key: 'institutionTypeCode', label: t('consultancy.institution.fields.type'), width: '120px' },
  { key: 'city', label: t('consultancy.institution.fields.city'), width: '120px' },
  { key: 'planCount', label: t('consultancy.institution.fields.planCount'), width: '90px' },
  { key: 'visitCount', label: t('consultancy.institution.fields.visitCount'), width: '90px' },
]

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(doFetch, 400)
}

async function doFetch() {
  filters.page = 1
  await consultancyStore.fetchInstitutions(filters)
}

function resetFilters() {
  filters.search = ''
  filters.city = ''
  filters.page = 1
  consultancyStore.fetchInstitutions(filters)
}

watch(
  () => filters.page,
  () => consultancyStore.fetchInstitutions(filters)
)

onMounted(doFetch)
</script>

<template>
  <div>
    <PageHeader :title="t('consultancy.institution.list.title')" :description="t('consultancy.institution.list.subtitle')">
      <button
        v-if="can('institution:create')"
        @click="router.push('/consultancy/institutions/new')"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('consultancy.institution.new') }}
      </button>
    </PageHeader>

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
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('consultancy.institution.fields.city') }}</label>
        <input
          v-model="filters.city"
          type="text"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @input="debouncedFetch"
        />
      </div>
      <button @click="resetFilters" class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent">
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="consultancyStore.institutions.items"
      :loading="consultancyStore.loading"
      :empty-text="t('consultancy.institution.list.noData')"
      @row-click="(row) => router.push(`/consultancy/institutions/${row.id}`)"
    >
      <template #cell-name="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-institutionTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-city="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-planCount="{ value }">
        <span class="text-center block">{{ value }}</span>
      </template>
      <template #cell-visitCount="{ value }">
        <span class="text-center block">{{ value }}</span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('institution:update')"
            @click="router.push(`/consultancy/institutions/${row.id}/edit`)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="consultancyStore.institutions.page"
        :page-size="consultancyStore.institutions.pageSize"
        :total-count="consultancyStore.institutions.totalCount"
        :total-pages="consultancyStore.institutions.totalPages"
        :has-previous-page="consultancyStore.institutions.hasPreviousPage"
        :has-next-page="consultancyStore.institutions.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; consultancyStore.fetchInstitutions(filters) }"
      />
    </div>
  </div>
</template>
