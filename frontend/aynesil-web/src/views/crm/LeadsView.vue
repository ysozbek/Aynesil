<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useLeadStore } from '@/stores/lead.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { LeadListItemDto } from '@/types/crm.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useLeadStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const statuses = ref<RefValueItem[]>([])
const sources = ref<RefValueItem[]>([])
const stages = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  campusId: '',
  statusId: '',
  pipelineStageId: '',
  sourceId: '',
  isConverted: undefined as boolean | undefined,
  hasPendingFollowUp: false,
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
  () => [query.campusId, query.statusId, query.pipelineStageId, query.sourceId, query.isConverted, query.hasPendingFollowUp, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('LEAD_STATUS').then(v => { statuses.value = v }),
    refData.getValues('LEAD_SOURCE').then(v => { sources.value = v }),
    refData.getValues('pipeline_stage').then(v => { stages.value = v }),
  ])
})

function buildQuery() {
  return {
    ...query,
    corporationId: corporationId.value,
    campusId: query.campusId || undefined,
    statusId: query.statusId || undefined,
    pipelineStageId: query.pipelineStageId || undefined,
    sourceId: query.sourceId || undefined,
  }
}

async function loadList() {
  await store.fetchList(buildQuery())
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<LeadListItemDto>[] = [
  { key: 'contactName', label: t('crm.lead.contactName'), sortable: true },
  { key: 'childName', label: t('crm.lead.childName') },
  { key: 'contactPhone', label: t('crm.lead.phone'), width: '120px' },
  { key: 'sourceName', label: t('crm.lead.source'), width: '110px' },
  { key: 'pipelineStageName', label: t('crm.lead.pipelineStage'), width: '130px' },
  { key: 'statusName', label: t('common.status'), width: '110px' },
  { key: 'assignedToName', label: t('crm.lead.assignedTo'), width: '130px' },
  { key: 'score', label: t('crm.lead.score'), width: '70px', align: 'center' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '120px' },
]

function goDetail(row: LeadListItemDto) {
  router.push({ name: 'lead-detail', params: { id: row.id } })
}

function formatDate(val: unknown): string {
  if (!val) return '-'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<LeadListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: LeadListItemDto, e: Event) {
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

// Convert badge color
function stageColor(code: string | null): string {
  if (!code) return 'bg-gray-100 text-gray-700'
  const map: Record<string, string> = {
    new: 'bg-blue-100 text-blue-700',
    contacted: 'bg-indigo-100 text-indigo-700',
    meeting: 'bg-violet-100 text-violet-700',
    proposal: 'bg-amber-100 text-amber-700',
    negotiation: 'bg-orange-100 text-orange-700',
    won: 'bg-emerald-100 text-emerald-700',
    lost: 'bg-red-100 text-red-700',
  }
  return map[code.toLowerCase()] ?? 'bg-gray-100 text-gray-700'
}
</script>

<template>
  <div>
    <PageHeader :title="t('crm.lead.title')" :description="t('crm.lead.description')">
      <button
        v-if="can('lead:create')"
        @click="router.push({ name: 'leads-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('crm.lead.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="relative flex-1 min-w-[200px] max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input type="search" :placeholder="t('crm.lead.searchPlaceholder')"
          @input="onSearchInput(($event.target as HTMLInputElement).value)"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <select v-model="query.campusId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allCampuses') }}</option>
        <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>

      <select v-model="query.statusId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('crm.lead.allStatuses') }}</option>
        <option v-for="s in statuses" :key="s.id" :value="s.id">{{ s.label }}</option>
      </select>

      <select v-model="query.pipelineStageId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('crm.lead.allStages') }}</option>
        <option v-for="s in stages" :key="s.id" :value="s.id">{{ s.label }}</option>
      </select>

      <select v-model="query.sourceId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('crm.lead.allSources') }}</option>
        <option v-for="s in sources" :key="s.id" :value="s.id">{{ s.label }}</option>
      </select>

      <label class="flex items-center gap-2 text-sm text-foreground cursor-pointer">
        <input type="checkbox" v-model="query.hasPendingFollowUp" @change="query.page = 1"
          class="rounded border-border" />
        {{ t('crm.lead.hasPendingFollowUp') }}
      </label>
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
      <template #cell-childName="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-contactPhone="{ value }">
        {{ value ?? '—' }}
      </template>
      <template #cell-pipelineStageName="{ row }">
        <span
          v-if="row.pipelineStageName"
          :class="['px-2 py-0.5 rounded-full text-xs font-medium', stageColor(row.pipelineStageCode)]"
        >{{ row.pipelineStageName }}</span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #cell-statusName="{ row }">
        <span
          v-if="row.statusName"
          :class="['px-2 py-0.5 rounded-full text-xs font-medium', row.isConverted ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-700']"
        >{{ row.statusName }}</span>
      </template>
      <template #cell-score="{ value }">
        <span v-if="value !== null && value !== undefined" class="font-mono text-xs">{{ value }}</span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('lead:read')"
            @click="goDetail(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('lead:update')"
            @click.stop="router.push({ name: 'lead-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('lead:delete') && !row.isConverted"
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
      :title="t('crm.lead.deleteTitle')"
      :message="t('crm.lead.deleteMessage', { name: deleteTarget?.contactName })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
