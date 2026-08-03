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
import type { GoalTemplateListItemDto } from '@/types/goal.types'
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
  libraryId: '',
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
  () => [query.libraryId, query.categoryId, query.developmentAreaId, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    store.libraryList.items.length === 0 ? store.fetchLibraries({ corporationId: corporationId.value, pageSize: 200 }) : Promise.resolve(),
    refData.getValues('GOAL_CATEGORY').then(v => { categories.value = v }),
    refData.getValues('DEVELOPMENT_AREA').then(v => { developmentAreas.value = v }),
  ])
})

function buildQuery() {
  return {
    ...query,
    corporationId: corporationId.value,
    libraryId: query.libraryId || undefined,
    categoryId: query.categoryId || undefined,
    developmentAreaId: query.developmentAreaId || undefined,
    search: query.search || undefined,
  }
}

async function loadList() {
  await store.fetchTemplates(buildQuery())
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<GoalTemplateListItemDto>[] = [
  { key: 'code', label: t('goal.template.code'), width: '100px' },
  { key: 'statement', label: t('goal.template.statement') },
  { key: 'libraryName', label: t('goal.template.library'), width: '140px' },
  { key: 'categoryLabel', label: t('goal.template.category'), width: '130px' },
  { key: 'developmentAreaLabel', label: t('goal.template.developmentArea'), width: '140px' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '120px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function truncate(str: string, max = 80): string {
  return str.length > max ? str.slice(0, max) + '…' : str
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<GoalTemplateListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: GoalTemplateListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteTemplate(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('goal.template.title')" :description="t('goal.description')">
      <button
        v-if="can('goal_template:create')"
        @click="router.push({ name: 'goal-template-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('goal.template.create') }}
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

      <select
        v-model="query.libraryId"
        @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option value="">{{ t('goal.template.library') }}</option>
        <option v-for="lib in store.libraryList.items" :key="lib.id" :value="lib.id">{{ lib.name }}</option>
      </select>

      <select
        v-model="query.categoryId"
        @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option value="">{{ t('goal.template.category') }}</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.label }}</option>
      </select>

      <select
        v-model="query.developmentAreaId"
        @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option value="">{{ t('goal.template.developmentArea') }}</option>
        <option v-for="a in developmentAreas" :key="a.id" :value="a.id">{{ a.label }}</option>
      </select>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="store.templateList.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="(row) => router.push({ name: 'goal-template-detail', params: { id: row.id } })"
    >
      <template #cell-code="{ value }">
        <span class="font-mono text-xs text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-statement="{ value }">
        <span :title="String(value)">{{ truncate(String(value)) }}</span>
      </template>
      <template #cell-libraryName="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-categoryLabel="{ value }">
        <span v-if="value" class="px-2 py-0.5 rounded-full text-xs font-medium bg-accent text-foreground">{{ value }}</span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #cell-developmentAreaLabel="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('goal_template:read')"
            @click="router.push({ name: 'goal-template-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('goal_template:update')"
            @click.stop="router.push({ name: 'goal-template-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('goal_template:delete')"
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
        :page="store.templateList.page"
        :page-size="store.templateList.pageSize"
        :total-count="store.templateList.totalCount"
        :total-pages="store.templateList.totalPages"
        :has-previous-page="store.templateList.hasPreviousPage"
        :has-next-page="store.templateList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.deleteConfirmMessage', { name: deleteTarget?.code ?? deleteTarget?.statement })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
