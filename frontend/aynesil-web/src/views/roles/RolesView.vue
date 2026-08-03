<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useRoleStore } from '@/stores/role.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { RoleListItemDto } from '@/types/role.types'

const { t } = useI18n()
const router = useRouter()
const store = useRoleStore()
const { can } = usePermission()

const query = reactive({
  page: 1, pageSize: 20, search: '',
  includeSystem: true,
  sortBy: 'name', sortDirection: 'asc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(query, () => store.fetchList(query), { immediate: false })
onMounted(() => store.fetchList(query))

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<RoleListItemDto>[] = [
  { key: 'code', label: t('role.code'), sortable: true, width: '140px' },
  { key: 'name', label: t('role.name'), sortable: true },
  { key: 'description', label: t('role.description') },
  { key: 'permissionCount', label: t('role.permissionCount'), align: 'right', width: '120px' },
  { key: 'isSystem', label: t('role.isSystem'), width: '80px' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '130px' },
]

function goDetail(row: RoleListItemDto) {
  router.push({ name: 'role-detail', params: { id: row.id } })
}

// ── Create modal ──────────────────────────────────────────────────────────────
const showCreate = ref(false)
const createForm = reactive({ code: '', name: '', description: '' })
const createErrors = reactive<Record<string, string>>({})

function openCreate() {
  Object.assign(createForm, { code: '', name: '', description: '' })
  Object.keys(createErrors).forEach((k) => delete createErrors[k])
  showCreate.value = true
}

function validateCreate(): boolean {
  Object.keys(createErrors).forEach((k) => delete createErrors[k])
  let valid = true
  if (!createForm.code.trim()) { createErrors.code = t('validation.required', { field: t('role.code') }); valid = false }
  if (!createForm.name.trim()) { createErrors.name = t('validation.required', { field: t('role.name') }); valid = false }
  return valid
}

async function submitCreate() {
  if (!validateCreate()) return
  try {
    await store.create({ code: createForm.code, name: createForm.name, description: createForm.description || undefined })
    showCreate.value = false
    store.fetchList(query)
  } catch (err: unknown) {
    createErrors.general = (err as Error).message
  }
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<RoleListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: RoleListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.remove(deleteTarget.value.id)
    deleteTarget.value = null
    store.fetchList(query)
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('role.title')" :description="t('role.subtitle')">
      <div class="flex items-center gap-2">
        <label class="flex items-center gap-1.5 text-sm text-muted-foreground cursor-pointer select-none">
          <input type="checkbox" v-model="query.includeSystem" class="rounded border-border" @change="query.page = 1" />
          {{ t('role.showSystem') }}
        </label>
        <button v-if="can('role:create')" @click="openCreate"
          class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('role.create') }}
        </button>
      </div>
    </PageHeader>

    <!-- Search -->
    <div class="mb-4">
      <div class="relative max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input type="search" :placeholder="t('common.search')" @input="onSearchInput(($event.target as HTMLInputElement).value)"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>
    </div>

    <!-- Table -->
    <DataTable :columns="columns" :rows="store.list.items" :loading="store.loading"
      :sort-by="query.sortBy" :sort-direction="query.sortDirection"
      @sort="onSort" @row-click="goDetail">
      <template #cell-isSystem="{ value }">
        <span v-if="value" class="inline-flex items-center rounded-md bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-600/20">
          {{ t('role.system') }}
        </span>
      </template>
      <template #cell-createdAt="{ value }">
        {{ value ? new Date(String(value)).toLocaleDateString('tr-TR') : '' }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button v-if="can('role:read')" @click="router.push({ name: 'role-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors" :title="t('common.view')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button v-if="can('role:delete') && !row.isSystem" @click="(e) => confirmDelete(row, e)"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors" :title="t('common.delete')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination :page="store.list.page" :page-size="store.list.pageSize" :total-count="store.list.totalCount"
        :total-pages="store.list.totalPages" :has-previous-page="store.list.hasPreviousPage" :has-next-page="store.list.hasNextPage"
        @update:page="(p) => { query.page = p }" @update:page-size="(s) => { query.pageSize = s; query.page = 1 }" />
    </div>

    <!-- Create modal -->
    <FormModal :open="showCreate" :title="t('role.create')" :saving="store.saving" @submit="submitCreate" @close="showCreate = false">
      <div class="space-y-4">
        <p v-if="createErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ createErrors.general }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('role.code') }} *</label>
          <input v-model="createForm.code" type="text" placeholder="admin, teacher, counselor..."
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="createErrors.code ? 'border-red-400' : 'border-border'" />
          <p v-if="createErrors.code" class="mt-1 text-xs text-red-600">{{ createErrors.code }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('role.name') }} *</label>
          <input v-model="createForm.name" type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="createErrors.name ? 'border-red-400' : 'border-border'" />
          <p v-if="createErrors.name" class="mt-1 text-xs text-red-600">{{ createErrors.name }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('role.description') }}</label>
          <textarea v-model="createForm.description" rows="3"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete confirm -->
    <ConfirmModal :open="!!deleteTarget" :title="t('role.deleteTitle')"
      :message="t('role.deleteMessage', { name: deleteTarget?.name })"
      :confirm-label="t('common.delete')" :loading="deleteLoading" @confirm="doDelete" @cancel="deleteTarget = null" />
  </div>
</template>
