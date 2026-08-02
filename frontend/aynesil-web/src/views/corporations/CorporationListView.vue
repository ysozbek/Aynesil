<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCorporationStore } from '@/stores/corporation.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { CorporationListItemDto } from '@/types/corporation.types'

const { t } = useI18n()
const router = useRouter()
const store = useCorporationStore()
const { can } = usePermission()

// ── Query state ──────────────────────────────────────────────────────────────
const query = reactive({
  page: 1,
  pageSize: 20,
  search: '',
  status: '',
  sortBy: 'createdAt',
  sortDirection: 'desc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    query.search = val
    query.page = 1
  }, 350)
}

watch(query, () => store.fetchList(query), { immediate: false })
onMounted(() => store.fetchList(query))

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

// ── Columns ──────────────────────────────────────────────────────────────────
const columns: Column<CorporationListItemDto>[] = [
  { key: 'code', label: t('corporation.code'), sortable: true, width: '100px' },
  { key: 'legalName', label: t('corporation.legalName'), sortable: true },
  { key: 'displayName', label: t('corporation.displayName'), sortable: true },
  { key: 'defaultLocale', label: t('corporation.locale'), width: '80px' },
  { key: 'campusCount', label: t('corporation.campusCount'), align: 'right', width: '80px' },
  { key: 'status', label: t('common.status'), width: '100px' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '160px' },
]

// ── Row click → detail ────────────────────────────────────────────────────────
function goDetail(row: CorporationListItemDto) {
  router.push({ name: 'corporation-detail', params: { id: row.id } })
}

// ── Create / Edit modal ───────────────────────────────────────────────────────
const showForm = ref(false)
const editTarget = ref<CorporationListItemDto | null>(null)

const form = reactive({
  code: '',
  legalName: '',
  displayName: '',
  defaultLocale: 'tr',
  defaultCurrency: 'TRY',
  timezone: 'Europe/Istanbul',
  taxOffice: '',
  taxNumber: '',
  rowVersion: 0,
})

const formErrors = reactive<Record<string, string>>({})

function openCreate() {
  editTarget.value = null
  Object.assign(form, {
    code: '', legalName: '', displayName: '',
    defaultLocale: 'tr', defaultCurrency: 'TRY',
    timezone: 'Europe/Istanbul', taxOffice: '', taxNumber: '', rowVersion: 0,
  })
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showForm.value = true
}

async function openEdit(row: CorporationListItemDto, e: Event) {
  e.stopPropagation()
  await store.fetchOne(row.id)
  if (!store.current) return
  editTarget.value = row
  Object.assign(form, {
    code: store.current.code,
    legalName: store.current.legalName,
    displayName: store.current.displayName,
    defaultLocale: store.current.defaultLocale,
    defaultCurrency: store.current.defaultCurrency,
    timezone: store.current.timezone,
    taxOffice: store.current.taxOffice ?? '',
    taxNumber: store.current.taxNumber ?? '',
    rowVersion: store.current.rowVersion,
  })
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showForm.value = true
}

function validateForm(): boolean {
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  let valid = true
  if (!form.code.trim()) { formErrors.code = t('validation.required', { field: t('corporation.code') }); valid = false }
  if (!form.legalName.trim()) { formErrors.legalName = t('validation.required', { field: t('corporation.legalName') }); valid = false }
  if (!form.displayName.trim()) { formErrors.displayName = t('validation.required', { field: t('corporation.displayName') }); valid = false }
  return valid
}

async function submitForm() {
  if (!validateForm()) return
  try {
    if (editTarget.value) {
      await store.update(editTarget.value.id, {
        legalName: form.legalName,
        displayName: form.displayName,
        defaultLocale: form.defaultLocale,
        defaultCurrency: form.defaultCurrency,
        timezone: form.timezone,
        taxOffice: form.taxOffice || undefined,
        taxNumber: form.taxNumber || undefined,
        rowVersion: form.rowVersion,
      })
    } else {
      await store.create({
        code: form.code,
        legalName: form.legalName,
        displayName: form.displayName,
        defaultLocale: form.defaultLocale,
        defaultCurrency: form.defaultCurrency,
        timezone: form.timezone,
        taxOffice: form.taxOffice || undefined,
        taxNumber: form.taxNumber || undefined,
      })
    }
    showForm.value = false
    store.fetchList(query)
  } catch (err: unknown) {
    formErrors.general = (err as Error).message
  }
}

// ── Activate / Deactivate ─────────────────────────────────────────────────────
async function toggleStatus(row: CorporationListItemDto, e: Event) {
  e.stopPropagation()
  if (row.status === 'Active') {
    await store.deactivate(row.id)
  } else {
    await store.activate(row.id)
  }
}

// ── Delete ────────────────────────────────────────────────────────────────────
const deleteTarget = ref<CorporationListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: CorporationListItemDto, e: Event) {
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
    <PageHeader :title="t('corporation.title')" :description="t('corporation.description')">
      <button
        v-if="can('corporation:create')"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('corporation.create') }}
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
        v-model="query.status"
        @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="Active">{{ t('common.active') }}</option>
        <option value="Inactive">{{ t('common.passive') }}</option>
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
      <template #cell-status="{ value }">
        <StatusBadge :value="String(value)" />
      </template>
      <template #cell-createdAt="{ value }">
        {{ value ? new Date(String(value)).toLocaleDateString('tr-TR') : '' }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('corporation:read')"
            @click="router.push({ name: 'corporation-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('corporation:update')"
            @click="(e) => openEdit(row, e)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('corporation:update')"
            @click="(e) => toggleStatus(row, e)"
            :title="row.status === 'Active' ? t('common.deactivate') : t('common.activate')"
            :class="[
              'p-1.5 rounded-lg hover:bg-accent transition-colors',
              row.status === 'Active' ? 'text-amber-600 hover:text-amber-700' : 'text-emerald-600 hover:text-emerald-700',
            ]"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.636 5.636a9 9 0 1012.728 12.728M9 9l6 6" />
            </svg>
          </button>
          <button
            v-if="can('corporation:delete')"
            @click="(e) => confirmDelete(row, e)"
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

    <!-- Pagination -->
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

    <!-- Create / Edit modal -->
    <FormModal
      :open="showForm"
      :title="editTarget ? t('corporation.edit') : t('corporation.create')"
      :subtitle="editTarget ? editTarget.displayName : undefined"
      :saving="store.saving"
      @submit="submitForm"
      @close="showForm = false"
    >
      <div class="space-y-4">
        <p v-if="formErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ formErrors.general }}</p>

        <div v-if="!editTarget">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.code') }} *</label>
          <input
            v-model="form.code"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="formErrors.code ? 'border-red-400' : 'border-border'"
            :placeholder="t('corporation.codePlaceholder')"
          />
          <p v-if="formErrors.code" class="mt-1 text-xs text-red-600">{{ formErrors.code }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.legalName') }} *</label>
          <input
            v-model="form.legalName"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="formErrors.legalName ? 'border-red-400' : 'border-border'"
          />
          <p v-if="formErrors.legalName" class="mt-1 text-xs text-red-600">{{ formErrors.legalName }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.displayName') }} *</label>
          <input
            v-model="form.displayName"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="formErrors.displayName ? 'border-red-400' : 'border-border'"
          />
          <p v-if="formErrors.displayName" class="mt-1 text-xs text-red-600">{{ formErrors.displayName }}</p>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.locale') }}</label>
            <select v-model="form.defaultLocale" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="tr">Türkçe (tr)</option>
              <option value="en">English (en)</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.currency') }}</label>
            <select v-model="form.defaultCurrency" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="TRY">TRY</option>
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.timezone') }}</label>
          <select v-model="form.timezone" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="Europe/Istanbul">Europe/Istanbul</option>
            <option value="UTC">UTC</option>
          </select>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.taxOffice') }}</label>
            <input v-model="form.taxOffice" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.taxNumber') }}</label>
            <input v-model="form.taxNumber" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Delete confirm -->
    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('corporation.deleteTitle')"
      :message="t('corporation.deleteMessage', { name: deleteTarget?.displayName })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
