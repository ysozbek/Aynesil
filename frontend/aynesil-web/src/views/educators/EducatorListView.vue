<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useEducatorStore } from '@/stores/educator.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { EducatorListItemDto } from '@/types/educator.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useEducatorStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const titles = ref<RefValueItem[]>([])
const employmentTypes = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  campusId: '',
  titleId: '',
  employmentType: '',
  isActive: undefined as boolean | undefined,
  page: 1,
  pageSize: 20,
  search: '',
  sortBy: 'createdAt',
  sortDirection: 'desc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(
  () => [query.campusId, query.titleId, query.employmentType, query.isActive, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('EDUCATOR_TITLE').then(v => { titles.value = v }),
    refData.getValues('EMPLOYMENT_TYPE').then(v => { employmentTypes.value = v }),
  ])
})

async function loadList() {
  await store.fetchList({
    ...query,
    corporationId: corporationId.value,
    campusId: query.campusId || undefined,
    titleId: query.titleId || undefined,
    employmentType: query.employmentType || undefined,
    isActive: query.isActive,
  })
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<EducatorListItemDto>[] = [
  { key: 'fullName', label: t('educator.fullName'), sortable: true },
  { key: 'titleLabel', label: t('educator.titleLabel'), width: '130px' },
  { key: 'email', label: t('educator.email') },
  { key: 'phone', label: t('educator.phone'), width: '130px' },
  { key: 'employmentType', label: t('educator.employmentType'), width: '130px' },
  { key: 'isActive', label: t('educator.isActive'), width: '80px', align: 'center' },
  { key: 'primaryCampusName', label: t('educator.primaryCampus'), width: '150px' },
  { key: 'specialtyCount', label: t('educator.specialtyCount'), width: '100px', align: 'center' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '120px' },
]

function goDetail(row: EducatorListItemDto) {
  router.push({ name: 'educator-detail', params: { id: row.id } })
}

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

// ── Activate / Deactivate ─────────────────────────────────────────────────
const actionLoading = ref(false)

async function toggleActive(row: EducatorListItemDto, e: Event) {
  e.stopPropagation()
  actionLoading.value = true
  try {
    if (row.isActive) {
      await store.deactivate(row.id)
    } else {
      await store.activate(row.id)
    }
    await loadList()
  } finally {
    actionLoading.value = false
  }
}

// ── Delete ─────────────────────────────────────────────────────────────────
const deleteTarget = ref<EducatorListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: EducatorListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.remove(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('educator.title')" :description="t('educator.description')">
      <button
        v-if="can('educator:create')"
        @click="router.push({ name: 'educator-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('educator.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
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

      <select v-model="query.campusId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allCampuses') }}</option>
        <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>

      <select v-model="query.titleId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('educator.titleLabel') }}: {{ t('common.all') }}</option>
        <option v-for="title in titles" :key="title.id" :value="title.id">{{ title.label }}</option>
      </select>

      <select v-model="query.employmentType" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('educator.employmentType') }}: {{ t('common.all') }}</option>
        <option v-for="et in employmentTypes" :key="et.id" :value="et.code">{{ et.label }}</option>
      </select>

      <select v-model="query.isActive" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option :value="undefined">{{ t('educator.isActive') }}: {{ t('common.all') }}</option>
        <option :value="true">{{ t('common.active') }}</option>
        <option :value="false">{{ t('common.inactive') }}</option>
      </select>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="store.list.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="goDetail"
    >
      <template #cell-titleLabel="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-email="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-phone="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-employmentType="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.inactive') }}
        </span>
      </template>
      <template #cell-primaryCampusName="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-specialtyCount="{ value }">
        <span class="font-mono text-xs">{{ value }}</span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('educator:read')"
            @click="goDetail(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('educator:update')"
            @click.stop="router.push({ name: 'educator-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('educator:update')"
            @click="toggleActive(row, $event)"
            :disabled="actionLoading"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors disabled:opacity-50"
            :title="row.isActive ? t('common.deactivate') : t('common.activate')"
          >
            <svg v-if="row.isActive" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </button>
          <button
            v-if="can('educator:delete')"
            @click="confirmDelete(row, $event)"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors"
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
        :page="store.list.page"
        :page-size="store.list.pageSize"
        :total-count="store.list.totalCount"
        :total-pages="store.list.totalPages"
        :has-previous-page="store.list.hasPreviousPage"
        :has-next-page="store.list.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.deleteConfirmMessage', { name: deleteTarget?.fullName })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
