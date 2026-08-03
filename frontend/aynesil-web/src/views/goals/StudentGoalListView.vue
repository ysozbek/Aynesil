<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useGoalStore } from '@/stores/goal.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { StudentGoalListItemDto } from '@/types/goal.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useGoalStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const categories = ref<RefValueItem[]>([])
const developmentAreas = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  studentId: '',
  horizon: '',
  status: '',
  categoryId: '',
  developmentAreaId: '',
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
  () => [query.studentId, query.horizon, query.status, query.categoryId, query.developmentAreaId, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('GOAL_CATEGORY').then(v => { categories.value = v }),
    refData.getValues('DEVELOPMENT_AREA').then(v => { developmentAreas.value = v }),
  ])
})

function buildQuery() {
  return {
    ...query,
    corporationId: corporationId.value,
    studentId: query.studentId || undefined,
    horizon: query.horizon || undefined,
    status: query.status || undefined,
    categoryId: query.categoryId || undefined,
    developmentAreaId: query.developmentAreaId || undefined,
    search: query.search || undefined,
  }
}

async function loadList() {
  await store.fetchStudentGoals(buildQuery())
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<StudentGoalListItemDto>[] = [
  { key: 'statement', label: t('goal.studentGoal.statement') },
  { key: 'horizon', label: t('goal.studentGoal.horizon'), width: '110px' },
  { key: 'status', label: t('goal.studentGoal.status'), width: '110px' },
  { key: 'categoryLabel', label: t('goal.studentGoal.category'), width: '120px' },
  { key: 'developmentAreaLabel', label: t('goal.studentGoal.developmentArea'), width: '140px' },
  { key: 'targetDate', label: t('goal.studentGoal.targetDate'), width: '110px' },
  { key: 'latestPercentComplete', label: t('goal.progress.percentComplete'), width: '130px' },
  { key: 'latestTrend', label: t('goal.progress.trend'), width: '90px', align: 'center' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '120px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function truncate(str: string, max = 80): string {
  return str.length > max ? str.slice(0, max) + '…' : str
}

function horizonColor(horizon: string): string {
  return horizon === 'long_term' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'
}

function horizonLabel(horizon: string): string {
  return horizon === 'long_term' ? t('goal.studentGoal.horizon.longTerm') : t('goal.studentGoal.horizon.shortTerm')
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-green-100 text-green-700',
    achieved: 'bg-teal-100 text-teal-700',
    discontinued: 'bg-red-100 text-red-700',
    on_hold: 'bg-orange-100 text-orange-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

function statusLabel(status: string): string {
  const map: Record<string, string> = {
    active: t('goal.studentGoal.status.active'),
    achieved: t('goal.studentGoal.status.achieved'),
    discontinued: t('goal.studentGoal.status.discontinued'),
    on_hold: t('goal.studentGoal.status.on_hold'),
  }
  return map[status] ?? status
}

function trendDisplay(trend: string | null): string {
  if (trend === 'improving') return '↑'
  if (trend === 'declining') return '↓'
  if (trend === 'stable') return '→'
  return '—'
}

function trendColor(trend: string | null): string {
  if (trend === 'improving') return 'text-emerald-600'
  if (trend === 'declining') return 'text-red-600'
  return 'text-gray-500'
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<StudentGoalListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: StudentGoalListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteStudentGoal(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('goal.studentGoal.title')" :description="t('goal.description')">
      <button
        v-if="can('student_goal:create')"
        @click="router.push({ name: 'student-goal-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('goal.studentGoal.create') }}
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

      <input
        v-model="query.studentId"
        type="text"
        :placeholder="t('bep.studentName') + ' ID'"
        @input="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />

      <select v-model="query.horizon" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('goal.studentGoal.horizon') }}</option>
        <option value="long_term">{{ t('goal.studentGoal.horizon.longTerm') }}</option>
        <option value="short_term">{{ t('goal.studentGoal.horizon.shortTerm') }}</option>
      </select>

      <select v-model="query.status" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('goal.studentGoal.status') }}</option>
        <option value="active">{{ t('goal.studentGoal.status.active') }}</option>
        <option value="achieved">{{ t('goal.studentGoal.status.achieved') }}</option>
        <option value="discontinued">{{ t('goal.studentGoal.status.discontinued') }}</option>
        <option value="on_hold">{{ t('goal.studentGoal.status.on_hold') }}</option>
      </select>

      <select v-model="query.categoryId" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('goal.studentGoal.category') }}</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.label }}</option>
      </select>

      <select v-model="query.developmentAreaId" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('goal.studentGoal.developmentArea') }}</option>
        <option v-for="a in developmentAreas" :key="a.id" :value="a.id">{{ a.label }}</option>
      </select>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="store.studentGoalList.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="(row) => router.push({ name: 'student-goal-detail', params: { id: row.id } })"
    >
      <template #cell-statement="{ value }">
        <span :title="String(value)">{{ truncate(String(value)) }}</span>
      </template>
      <template #cell-horizon="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', horizonColor(String(value))]">
          {{ horizonLabel(String(value)) }}
        </span>
      </template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ statusLabel(row.status) }}
        </span>
      </template>
      <template #cell-categoryLabel="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-developmentAreaLabel="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-targetDate="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #cell-latestPercentComplete="{ value }">
        <div v-if="value !== null && value !== undefined" class="flex items-center gap-2">
          <div class="flex-1 bg-gray-200 rounded-full h-1.5 w-20">
            <div class="bg-primary h-1.5 rounded-full" :style="{ width: `${value}%` }" />
          </div>
          <span class="text-xs font-mono text-muted-foreground">{{ value }}%</span>
        </div>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #cell-latestTrend="{ value }">
        <span :class="['text-base font-bold', trendColor(value as string | null)]">
          {{ trendDisplay(value as string | null) }}
        </span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('student_goal:read')"
            @click="router.push({ name: 'student-goal-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('student_goal:update')"
            @click.stop="router.push({ name: 'student-goal-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('student_goal:delete')"
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
        :page="store.studentGoalList.page"
        :page-size="store.studentGoalList.pageSize"
        :total-count="store.studentGoalList.totalCount"
        :total-pages="store.studentGoalList.totalPages"
        :has-previous-page="store.studentGoalList.hasPreviousPage"
        :has-next-page="store.studentGoalList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.deleteConfirmMessage', { name: deleteTarget?.statement })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
