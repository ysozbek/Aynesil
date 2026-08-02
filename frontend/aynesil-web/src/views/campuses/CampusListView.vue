<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useBranchStore } from '@/stores/branch.store'
import { useCorporationStore } from '@/stores/corporation.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { CampusListItemDto } from '@/types/campus.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useBranchStore()
const corpStore = useCorporationStore()
const { can } = usePermission()

const query = reactive({
  page: 1,
  pageSize: 20,
  search: '',
  corporationId: (route.query.corporationId as string) || '',
  isActive: undefined as boolean | undefined,
  sortBy: 'name',
  sortDirection: 'asc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(query, () => store.fetchList(query), { immediate: false })
onMounted(async () => {
  await store.fetchList(query)
  // Load corporation list for filter dropdown
  if (!corpStore.list.items.length) await corpStore.fetchList({ pageSize: 200 })
})

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<CampusListItemDto>[] = [
  { key: 'code', label: t('campus.code'), width: '80px' },
  { key: 'name', label: t('campus.name'), sortable: true },
  { key: 'corporationDisplayName', label: t('corporation.title') },
  { key: 'city', label: t('campus.city') },
  { key: 'district', label: t('campus.district') },
  { key: 'phone', label: t('campus.phone') },
  { key: 'isActive', label: t('common.status'), width: '90px' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '140px' },
]

// ── Create / Edit ──────────────────────────────────────────────────────────────
const showForm = ref(false)
const editTarget = ref<CampusListItemDto | null>(null)

const form = reactive({
  corporationId: '',
  code: '',
  name: '',
  city: '',
  addressLine: '',
  district: '',
  phone: '',
  email: '',
  timezone: 'Europe/Istanbul',
  rowVersion: 0,
})

const formErrors = reactive<Record<string, string>>({})

function openCreate() {
  editTarget.value = null
  Object.assign(form, {
    corporationId: query.corporationId || '',
    code: '', name: '', city: '', addressLine: '', district: '',
    phone: '', email: '', timezone: 'Europe/Istanbul', rowVersion: 0,
  })
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showForm.value = true
}

async function openEdit(row: CampusListItemDto, e: Event) {
  e.stopPropagation()
  await store.fetchOne(row.id)
  if (!store.current) return
  editTarget.value = row
  Object.assign(form, {
    corporationId: store.current.corporationId,
    code: store.current.code,
    name: store.current.name,
    city: store.current.city ?? '',
    addressLine: store.current.addressLine ?? '',
    district: store.current.district ?? '',
    phone: store.current.phone ?? '',
    email: store.current.email ?? '',
    timezone: store.current.timezone ?? 'Europe/Istanbul',
    rowVersion: store.current.rowVersion,
  })
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showForm.value = true
}

function validateForm(): boolean {
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  let valid = true
  if (!form.corporationId) { formErrors.corporationId = t('validation.required', { field: t('corporation.title') }); valid = false }
  if (!form.code.trim()) { formErrors.code = t('validation.required', { field: t('campus.code') }); valid = false }
  if (!form.name.trim()) { formErrors.name = t('validation.required', { field: t('campus.name') }); valid = false }
  return valid
}

async function submitForm() {
  if (!validateForm()) return
  try {
    if (editTarget.value) {
      await store.update(editTarget.value.id, {
        name: form.name,
        city: form.city || undefined,
        addressLine: form.addressLine || undefined,
        district: form.district || undefined,
        phone: form.phone || undefined,
        email: form.email || undefined,
        timezone: form.timezone || undefined,
        rowVersion: form.rowVersion,
      })
    } else {
      await store.create({
        corporationId: form.corporationId,
        code: form.code,
        name: form.name,
        city: form.city || undefined,
        addressLine: form.addressLine || undefined,
        district: form.district || undefined,
        phone: form.phone || undefined,
        email: form.email || undefined,
        timezone: form.timezone || undefined,
      })
    }
    showForm.value = false
    store.fetchList(query)
  } catch (err: unknown) {
    formErrors.general = (err as Error).message
  }
}

// ── Toggle status ──────────────────────────────────────────────────────────────
async function toggleStatus(row: CampusListItemDto, e: Event) {
  e.stopPropagation()
  if (row.isActive) await store.deactivate(row.id)
  else await store.activate(row.id)
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<CampusListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: CampusListItemDto, e: Event) {
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
    <PageHeader :title="t('campus.title')" :description="t('campus.description')">
      <button
        v-if="can('campus:create')"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('campus.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="relative flex-1 min-w-[200px] max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input type="search" :placeholder="t('common.search')" @input="onSearchInput(($event.target as HTMLInputElement).value)"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <select v-model="query.corporationId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('corporation.all') }}</option>
        <option v-for="c in corpStore.list.items" :key="c.id" :value="c.id">{{ c.displayName }}</option>
      </select>

      <select v-model="query.isActive" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option :value="undefined">{{ t('common.allStatuses') }}</option>
        <option :value="true">{{ t('common.active') }}</option>
        <option :value="false">{{ t('common.passive') }}</option>
      </select>
    </div>

    <!-- Table -->
    <DataTable :columns="columns" :rows="store.list.items" :loading="store.loading"
      :sort-by="query.sortBy" :sort-direction="query.sortDirection" @sort="onSort">
      <template #cell-isActive="{ value }">
        <StatusBadge :value="!!value" />
      </template>
      <template #cell-createdAt="{ value }">
        {{ value ? new Date(String(value)).toLocaleDateString('tr-TR') : '' }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button v-if="can('campus:update')" @click="(e) => openEdit(row, e)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors" :title="t('common.edit')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button v-if="can('campus:update')" @click="(e) => toggleStatus(row, e)"
            :class="['p-1.5 rounded-lg hover:bg-accent transition-colors', row.isActive ? 'text-amber-600 hover:text-amber-700' : 'text-emerald-600 hover:text-emerald-700']"
            :title="row.isActive ? t('common.deactivate') : t('common.activate')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.636 5.636a9 9 0 1012.728 12.728M9 9l6 6" />
            </svg>
          </button>
          <button v-if="can('campus:delete')" @click="(e) => confirmDelete(row, e)"
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

    <!-- Form modal -->
    <FormModal :open="showForm" :title="editTarget ? t('campus.edit') : t('campus.create')" :saving="store.saving"
      @submit="submitForm" @close="showForm = false">
      <div class="space-y-4">
        <p v-if="formErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ formErrors.general }}</p>

        <div v-if="!editTarget">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.title') }} *</label>
          <select v-model="form.corporationId" class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="formErrors.corporationId ? 'border-red-400' : 'border-border'">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in corpStore.list.items" :key="c.id" :value="c.id">{{ c.displayName }}</option>
          </select>
          <p v-if="formErrors.corporationId" class="mt-1 text-xs text-red-600">{{ formErrors.corporationId }}</p>
        </div>

        <div v-if="!editTarget">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.code') }} *</label>
          <input v-model="form.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="formErrors.code ? 'border-red-400' : 'border-border'" />
          <p v-if="formErrors.code" class="mt-1 text-xs text-red-600">{{ formErrors.code }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.name') }} *</label>
          <input v-model="form.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="formErrors.name ? 'border-red-400' : 'border-border'" />
          <p v-if="formErrors.name" class="mt-1 text-xs text-red-600">{{ formErrors.name }}</p>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.city') }}</label>
            <input v-model="form.city" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.district') }}</label>
            <input v-model="form.district" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.address') }}</label>
          <input v-model="form.addressLine" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.phone') }}</label>
            <input v-model="form.phone" type="tel" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.email') }}</label>
            <input v-model="form.email" type="email" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Delete confirm -->
    <ConfirmModal :open="!!deleteTarget" :title="t('campus.deleteTitle')" :message="t('campus.deleteMessage', { name: deleteTarget?.name })"
      :confirm-label="t('common.delete')" :loading="deleteLoading" @confirm="doDelete" @cancel="deleteTarget = null" />
  </div>
</template>
