<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { AssessmentTemplateListItemDto } from '@/types/assessment.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useAssessmentTemplateStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const types = ref<RefValueItem[]>([])
const categories = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  typeId: '',
  categoryId: '',
  isActive: undefined as boolean | undefined,
  page: 1,
  pageSize: 20,
  search: '',
  sortBy: 'name',
  sortDirection: 'asc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(
  () => [query.typeId, query.categoryId, query.isActive, query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('ASSESSMENT_TYPE').then(v => { types.value = v }),
    refData.getValues('ASSESSMENT_CATEGORY').then(v => { categories.value = v }),
  ])
})

async function loadList() {
  await store.fetchList({
    ...query,
    corporationId: query.corporationId || undefined,
    typeId: query.typeId || undefined,
    categoryId: query.categoryId || undefined,
  })
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<AssessmentTemplateListItemDto>[] = [
  { key: 'name', label: t('assessment.template.name'), sortable: true },
  { key: 'code', label: t('assessment.template.code'), width: '120px' },
  { key: 'typeName', label: t('assessment.template.type'), width: '120px' },
  { key: 'categoryName', label: t('assessment.template.category'), width: '130px' },
  { key: 'scoringModel', label: t('assessment.template.scoringModel'), width: '120px' },
  { key: 'version', label: t('assessment.template.version'), width: '70px', align: 'center' },
  { key: 'sectionCount', label: t('assessment.template.sections'), width: '80px', align: 'center' },
  { key: 'isActive', label: t('common.status'), width: '80px' },
]

function goDetail(row: AssessmentTemplateListItemDto) {
  router.push({ name: 'assessment-template-detail', params: { id: row.id } })
}
</script>

<template>
  <div>
    <PageHeader :title="t('assessment.template.title')" :description="t('assessment.template.description')">
      <button
        v-if="can('assessment_template:create')"
        @click="router.push({ name: 'assessment-template-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('assessment.template.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="relative flex-1 min-w-[200px] max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input type="search" :placeholder="t('assessment.template.searchPlaceholder')"
          @input="onSearchInput(($event.target as HTMLInputElement).value)"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <select v-model="query.typeId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('assessment.template.allTypes') }}</option>
        <option v-for="type in types" :key="type.id" :value="type.id">{{ type.label }}</option>
      </select>

      <select v-model="query.categoryId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('assessment.template.allCategories') }}</option>
        <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.label }}</option>
      </select>

      <select :value="query.isActive === undefined ? '' : String(query.isActive)"
        @change="(e) => { const v = (e.target as HTMLSelectElement).value; query.isActive = v === '' ? undefined : v === 'true'; query.page = 1 }"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="true">{{ t('common.active') }}</option>
        <option value="false">{{ t('common.passive') }}</option>
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
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #cell-scoringModel="{ value }">
        <span class="text-xs font-mono text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button v-if="can('assessment_template:read')" @click="goDetail(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors" :title="t('common.view')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button v-if="can('assessment_template:update')"
            @click.stop="router.push({ name: 'assessment-template-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors" :title="t('common.edit')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
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
  </div>
</template>
