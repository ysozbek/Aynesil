<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useGoalStore } from '@/stores/goal.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { GoalLibraryListItemDto } from '@/types/goal.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useGoalStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
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
  () => [query.page, query.pageSize, query.sortBy, query.sortDirection],
  () => loadList(),
)

onMounted(() => loadList())

function buildQuery() {
  return {
    ...query,
    corporationId: corporationId.value,
    search: query.search || undefined,
  }
}

async function loadList() {
  await store.fetchLibraries(buildQuery())
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<GoalLibraryListItemDto>[] = [
  { key: 'name', label: t('goal.library.name'), sortable: true },
  { key: 'description', label: t('goal.library.description2') },
  { key: 'templateCount', label: t('goal.library.templateCount'), width: '110px', align: 'center' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '120px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<GoalLibraryListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: GoalLibraryListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteLibrary(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}

// ── Create / Edit modal ────────────────────────────────────────────────────────
const showFormModal = ref(false)
const editTarget = ref<GoalLibraryListItemDto | null>(null)
const formData = reactive({ name: '', description: '', rowVersion: 0 })
const formError = ref('')

function openCreateModal() {
  editTarget.value = null
  formData.name = ''
  formData.description = ''
  formData.rowVersion = 0
  formError.value = ''
  showFormModal.value = true
}

async function openEditModal(row: GoalLibraryListItemDto, e: Event) {
  e.stopPropagation()
  await store.fetchLibrary(row.id)
  const lib = store.currentLibrary
  if (!lib) return
  editTarget.value = row
  formData.name = lib.name
  formData.description = lib.description ?? ''
  formData.rowVersion = lib.rowVersion
  formError.value = ''
  showFormModal.value = true
}

async function submitForm() {
  if (!formData.name.trim()) {
    formError.value = t('validation.required', { field: t('goal.library.name') })
    return
  }
  try {
    if (editTarget.value) {
      await store.updateLibrary(editTarget.value.id, {
        name: formData.name,
        description: formData.description || null,
        rowVersion: formData.rowVersion,
      })
    } else {
      await store.createLibrary({
        corporationId: corporationId.value,
        name: formData.name,
        description: formData.description || null,
      })
    }
    showFormModal.value = false
    await loadList()
  } catch (e: unknown) {
    formError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('goal.library.title')" :description="t('goal.description')">
      <button
        v-if="can('goal_library:create')"
        @click="openCreateModal"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('goal.library.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3">
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
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="store.libraryList.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
    >
      <template #cell-description="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-templateCount="{ value }">
        <span class="font-mono text-sm">{{ value }}</span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ formatDate(value) }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('goal_library:update')"
            @click="openEditModal(row, $event)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('goal_library:delete')"
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
        :page="store.libraryList.page"
        :page-size="store.libraryList.pageSize"
        :total-count="store.libraryList.totalCount"
        :total-pages="store.libraryList.totalPages"
        :has-previous-page="store.libraryList.hasPreviousPage"
        :has-next-page="store.libraryList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <!-- Create / Edit Modal -->
    <FormModal
      :open="showFormModal"
      :title="editTarget ? t('common.edit') : t('goal.library.create')"
      :saving="store.saving"
      @submit="submitForm"
      @close="showFormModal = false"
    >
      <div class="space-y-4">
        <p v-if="formError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ formError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.library.name') }} *</label>
          <input v-model="formData.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.library.description2') }}</label>
          <textarea v-model="formData.description" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

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
