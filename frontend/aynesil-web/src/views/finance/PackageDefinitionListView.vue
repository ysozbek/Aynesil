<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { usePackageStore } from '@/stores/package.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { PackageDefinitionListItemDto } from '@/types/finance.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = usePackageStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const packageTypes = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  isActive: '' as '' | 'true' | 'false',
  packageTypeId: '',
  page: 1,
  pageSize: 20,
  search: '',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(() => [query.isActive, query.packageTypeId, query.page], () => loadList())
onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('PACKAGE_TYPE').then(v => { packageTypes.value = v }),
  ])
})

async function loadList() {
  await store.fetchDefinitions({
    ...query,
    corporationId: corporationId.value,
    isActive: query.isActive === 'true' ? true : query.isActive === 'false' ? false : undefined,
    packageTypeId: query.packageTypeId || undefined,
    search: query.search || undefined,
  })
}

const columns: Column<PackageDefinitionListItemDto>[] = [
  { key: 'code', label: t('finance.package.code'), width: '90px' },
  { key: 'name', label: t('finance.package.name') },
  { key: 'packageTypeLabel', label: t('finance.package.type'), width: '120px' },
  { key: 'programName', label: t('finance.package.program'), width: '140px' },
  { key: 'totalCredits', label: t('finance.package.totalCredits'), width: '90px', align: 'center' },
  { key: 'validityDays', label: t('finance.package.validityDays'), width: '90px', align: 'center' },
  { key: 'listPrice', label: t('finance.package.listPrice'), width: '110px', align: 'right' },
  { key: 'isActive', label: t('common.status'), width: '90px' },
]

function formatCurrency(amount: number, currency = 'TRY'): string {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency }).format(amount)
}

const deleteTarget = ref<PackageDefinitionListItemDto | null>(null)
const deleteLoading = ref(false)

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteDefinition(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.package.title')" :description="t('finance.package.description')">
      <button
        v-if="can('package_definition:create')"
        @click="router.push({ name: 'package-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('finance.package.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="relative flex-1 min-w-[200px] max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          type="search"
          :placeholder="t('common.search')"
          @input="onSearchInput(($event.target as HTMLInputElement).value)"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>
      <select v-model="query.packageTypeId" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('finance.package.allTypes') }}</option>
        <option v-for="pt in packageTypes" :key="pt.id" :value="pt.id">{{ pt.label }}</option>
      </select>
      <select v-model="query.isActive" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="true">{{ t('common.active') }}</option>
        <option value="false">{{ t('common.passive') }}</option>
      </select>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.definitionList.items"
      :loading="store.loading"
      @row-click="(row) => router.push({ name: 'package-detail', params: { id: row.id } })"
    >
      <template #cell-packageTypeLabel="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-programName="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-listPrice="{ row }">
        <span class="font-mono font-medium">{{ formatCurrency(row.listPrice, row.currency) }}</span>
      </template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('package_definition:update')"
            @click="router.push({ name: 'package-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('package_definition:update') && row.isActive"
            @click="store.deactivateDefinition(row.id).then(() => loadList())"
            class="p-1.5 rounded-lg hover:bg-amber-50 text-muted-foreground hover:text-amber-600"
            :title="t('common.deactivate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
          </button>
          <button
            v-if="can('package_definition:update') && !row.isActive"
            @click="store.activateDefinition(row.id).then(() => loadList())"
            class="p-1.5 rounded-lg hover:bg-green-50 text-muted-foreground hover:text-green-600"
            :title="t('common.activate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </button>
          <button
            v-if="can('package_definition:delete')"
            @click="deleteTarget = row"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
            :title="t('common.delete')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.definitionList.page"
        :page-size="store.definitionList.pageSize"
        :total-count="store.definitionList.totalCount"
        :total-pages="store.definitionList.totalPages"
        :has-previous-page="store.definitionList.hasPreviousPage"
        :has-next-page="store.definitionList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('finance.package.deleteTitle')"
      :message="t('finance.package.deleteMessage', { name: deleteTarget?.name })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
