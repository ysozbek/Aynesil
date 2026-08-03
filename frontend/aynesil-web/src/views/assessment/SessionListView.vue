<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useAssessmentStore } from '@/stores/assessment.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { AssessmentSessionListItemDto } from '@/types/assessment.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useAssessmentStore()
const branchStore = useBranchStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
  campusId: '',
  status: '',
  page: 1,
  pageSize: 20,
  sortBy: 'createdAt',
  sortDirection: 'desc' as 'asc' | 'desc',
})

watch(
  () => [query.campusId, query.status, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
  ])
})

async function loadList() {
  await store.fetchList({
    ...query,
    corporationId: query.corporationId || undefined,
    campusId: query.campusId || undefined,
    status: query.status || undefined,
  })
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<AssessmentSessionListItemDto>[] = [
  { key: 'templateName', label: t('assessment.session.template'), sortable: false },
  { key: 'leadContactName', label: t('assessment.session.subject') },
  { key: 'assessorName', label: t('assessment.session.assessor'), width: '140px' },
  { key: 'status', label: t('common.status'), width: '120px' },
  { key: 'scheduledAt', label: t('assessment.session.scheduledAt'), sortable: true, width: '140px' },
  { key: 'totalScore', label: t('assessment.session.score'), width: '80px', align: 'center' },
]

function goDetail(row: AssessmentSessionListItemDto) {
  router.push({ name: 'assessment-session-detail', params: { id: row.id } })
}

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

const statusColor = (status: string): string => {
  const map: Record<string, string> = {
    planned: 'bg-blue-100 text-blue-700',
    in_progress: 'bg-amber-100 text-amber-700',
    completed: 'bg-emerald-100 text-emerald-700',
    cancelled: 'bg-red-100 text-red-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<AssessmentSessionListItemDto | null>(null)
const deleteLoading = ref(false)

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
    <PageHeader :title="t('assessment.session.title')" :description="t('assessment.session.description')">
      <button
        v-if="can('assessment_session:create')"
        @click="router.push({ name: 'assessment-sessions-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('assessment.session.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <select v-model="query.campusId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allCampuses') }}</option>
        <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>

      <select v-model="query.status" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="planned">{{ t('assessment.session.status.planned') }}</option>
        <option value="in_progress">{{ t('assessment.session.status.in_progress') }}</option>
        <option value="completed">{{ t('assessment.session.status.completed') }}</option>
        <option value="cancelled">{{ t('assessment.session.status.cancelled') }}</option>
      </select>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.list.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="goDetail"
    >
      <template #cell-leadContactName="{ row }">
        {{ row.leadContactName ?? row.studentName ?? '—' }}
      </template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ t(`assessment.session.status.${row.status}`) }}
        </span>
      </template>
      <template #cell-scheduledAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #cell-totalScore="{ value }">
        <span v-if="value !== null && value !== undefined" class="font-mono text-xs">{{ value }}</span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button v-if="can('assessment_session:read')" @click="goDetail(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors" :title="t('common.view')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button v-if="can('assessment_session:delete') && row.status === 'planned'" @click.stop="deleteTarget = row"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors" :title="t('common.delete')">
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
      :title="t('assessment.session.deleteTitle')"
      :message="t('assessment.session.deleteMessage')"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
