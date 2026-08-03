<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useProgramStore } from '@/stores/program.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ProgramListItemDto } from '@/types/program.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useProgramStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const programTypes = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  programTypeId: '',
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
  () => [query.programTypeId, query.isActive, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('PROGRAM_TYPE').then(v => { programTypes.value = v }),
  ])
})

async function loadList() {
  await store.fetchPrograms({
    ...query,
    corporationId: corporationId.value,
    programTypeId: query.programTypeId || undefined,
    isActive: query.isActive,
  })
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<ProgramListItemDto>[] = [
  { key: 'code', label: t('program.code'), sortable: true, width: '100px' },
  { key: 'name', label: t('program.name'), sortable: true },
  { key: 'programTypeLabel', label: t('program.programType'), width: '150px' },
  { key: 'isActive', label: t('program.isActive'), width: '80px', align: 'center' },
  { key: 'serviceCount', label: t('program.serviceCount'), width: '110px', align: 'center' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '120px' },
]

function goDetail(row: ProgramListItemDto) {
  router.push({ name: 'program-detail', params: { id: row.id } })
}

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

// ── Delete ─────────────────────────────────────────────────────────────────
const deleteTarget = ref<ProgramListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: ProgramListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteProgram(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('program.title')" :description="t('program.description')">
      <button
        v-if="can('program:create')"
        @click="router.push({ name: 'program-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('program.create') }}
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

      <select v-model="query.programTypeId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('program.programType') }}: {{ t('common.all') }}</option>
        <option v-for="pt in programTypes" :key="pt.id" :value="pt.id">{{ pt.label }}</option>
      </select>

      <select v-model="query.isActive" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option :value="undefined">{{ t('program.isActive') }}: {{ t('common.all') }}</option>
        <option :value="true">{{ t('common.active') }}</option>
        <option :value="false">{{ t('common.inactive') }}</option>
      </select>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="store.programList.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="goDetail"
    >
      <template #cell-programTypeLabel="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.inactive') }}
        </span>
      </template>
      <template #cell-serviceCount="{ value }">
        <span class="font-mono text-xs">{{ value }}</span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('program:read')"
            @click="goDetail(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('program:update')"
            @click.stop="router.push({ name: 'program-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('program:delete')"
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
        :page="store.programList.page"
        :page-size="store.programList.pageSize"
        :total-count="store.programList.totalCount"
        :total-pages="store.programList.totalPages"
        :has-previous-page="store.programList.hasPreviousPage"
        :has-next-page="store.programList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.deleteConfirmMessage', { name: deleteTarget?.name })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
