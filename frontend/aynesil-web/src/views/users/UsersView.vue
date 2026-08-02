<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user.store'
import { useBranchStore } from '@/stores/branch.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { UserListItemDto } from '@/types/user.types'

const { t } = useI18n()
const router = useRouter()
const store = useUserStore()
const branchStore = useBranchStore()
const { can } = usePermission()

const query = reactive({
  page: 1, pageSize: 20, search: '',
  status: '', campusId: '',
  sortBy: 'fullName', sortDirection: 'asc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(query, () => store.fetchList(query), { immediate: false })
onMounted(async () => {
  await store.fetchList(query)
  if (!branchStore.list.items.length) await branchStore.fetchList({ pageSize: 200 })
})

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<UserListItemDto>[] = [
  { key: 'fullName', label: t('user.fullName'), sortable: true },
  { key: 'username', label: t('user.username'), sortable: true },
  { key: 'email', label: t('user.email') },
  { key: 'status', label: t('common.status'), width: '100px' },
  { key: 'lastLoginAt', label: t('user.lastLogin'), width: '140px' },
  { key: 'createdAt', label: t('common.createdAt'), sortable: true, width: '130px' },
]

function goDetail(row: UserListItemDto) {
  router.push({ name: 'user-detail', params: { id: row.id } })
}

// ── Create modal ──────────────────────────────────────────────────────────────
const showCreate = ref(false)
const createForm = reactive({
  username: '', fullName: '', email: '', phone: '',
  password: '', preferredLocale: 'tr', primaryCampusId: '',
})
const createErrors = reactive<Record<string, string>>({})

function openCreate() {
  Object.assign(createForm, {
    username: '', fullName: '', email: '', phone: '',
    password: '', preferredLocale: 'tr', primaryCampusId: '',
  })
  Object.keys(createErrors).forEach((k) => delete createErrors[k])
  showCreate.value = true
}

function validateCreate(): boolean {
  Object.keys(createErrors).forEach((k) => delete createErrors[k])
  let valid = true
  if (!createForm.username.trim()) { createErrors.username = t('validation.required', { field: t('user.username') }); valid = false }
  if (!createForm.fullName.trim()) { createErrors.fullName = t('validation.required', { field: t('user.fullName') }); valid = false }
  return valid
}

async function submitCreate() {
  if (!validateCreate()) return
  try {
    await store.create({
      username: createForm.username,
      fullName: createForm.fullName,
      email: createForm.email || undefined,
      phone: createForm.phone || undefined,
      password: createForm.password || undefined,
      preferredLocale: createForm.preferredLocale || undefined,
      primaryCampusId: createForm.primaryCampusId || undefined,
    })
    showCreate.value = false
    store.fetchList(query)
  } catch (err: unknown) {
    createErrors.general = (err as Error).message
  }
}

// ── Status ─────────────────────────────────────────────────────────────────────
async function activateUser(row: UserListItemDto, e: Event) {
  e.stopPropagation()
  await store.activate(row.id)
}

async function suspendUser(row: UserListItemDto, e: Event) {
  e.stopPropagation()
  await store.suspend(row.id)
}

// ── Delete ─────────────────────────────────────────────────────────────────────
const deleteTarget = ref<UserListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: UserListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.fetchOne(deleteTarget.value.id)
    if (store.current) {
      await store.remove(deleteTarget.value.id, store.current.rowVersion)
    }
    deleteTarget.value = null
    store.fetchList(query)
  } finally {
    deleteLoading.value = false
  }
}

function statusActions(row: UserListItemDto) {
  if (row.status === 'Active') return [{ label: t('user.suspend'), action: suspendUser, class: 'text-amber-600' }]
  if (row.status === 'Suspended') return [{ label: t('user.activate'), action: activateUser, class: 'text-emerald-600' }]
  return [{ label: t('user.activate'), action: activateUser, class: 'text-emerald-600' }]
}
</script>

<template>
  <div>
    <PageHeader :title="t('user.title')" :description="t('user.description')">
      <button
        v-if="can('user:create')"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('user.create') }}
      </button>
    </PageHeader>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="relative flex-1 min-w-[200px] max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input type="search" :placeholder="t('user.searchPlaceholder')" @input="onSearchInput(($event.target as HTMLInputElement).value)"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <select v-model="query.status" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="Active">{{ t('common.active') }}</option>
        <option value="Suspended">{{ t('user.suspended') }}</option>
        <option value="Inactive">{{ t('common.passive') }}</option>
      </select>

      <select v-model="query.campusId" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('campus.all') }}</option>
        <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>

    <!-- Table -->
    <DataTable :columns="columns" :rows="store.list.items" :loading="store.loading"
      :sort-by="query.sortBy" :sort-direction="query.sortDirection"
      @sort="onSort" @row-click="goDetail">
      <template #cell-status="{ value }">
        <StatusBadge :value="String(value)" />
      </template>
      <template #cell-lastLoginAt="{ value }">
        {{ value ? new Date(String(value)).toLocaleDateString('tr-TR') : '-' }}
      </template>
      <template #cell-createdAt="{ value }">
        {{ value ? new Date(String(value)).toLocaleDateString('tr-TR') : '' }}
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button v-if="can('user:read')" @click="router.push({ name: 'user-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors" :title="t('common.view')">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <template v-if="can('user:update')">
            <button v-for="action in statusActions(row)" :key="action.label"
              @click="(e) => action.action(row, e)"
              :class="['p-1.5 rounded-lg hover:bg-accent transition-colors text-xs font-medium px-2 py-1', action.class]"
              :title="action.label">
              {{ action.label }}
            </button>
          </template>
          <button v-if="can('user:delete')" @click="(e) => confirmDelete(row, e)"
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

    <!-- Create user modal -->
    <FormModal :open="showCreate" :title="t('user.create')" :saving="store.saving" @submit="submitCreate" @close="showCreate = false">
      <div class="space-y-4">
        <p v-if="createErrors.general" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ createErrors.general }}</p>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.username') }} *</label>
            <input v-model="createForm.username" type="text" autocomplete="off"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="createErrors.username ? 'border-red-400' : 'border-border'" />
            <p v-if="createErrors.username" class="mt-1 text-xs text-red-600">{{ createErrors.username }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.fullName') }} *</label>
            <input v-model="createForm.fullName" type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="createErrors.fullName ? 'border-red-400' : 'border-border'" />
            <p v-if="createErrors.fullName" class="mt-1 text-xs text-red-600">{{ createErrors.fullName }}</p>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.email') }}</label>
            <input v-model="createForm.email" type="email" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.phone') }}</label>
            <input v-model="createForm.phone" type="tel" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.password') }}</label>
          <input v-model="createForm.password" type="password" autocomplete="new-password"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.locale') }}</label>
            <select v-model="createForm.preferredLocale" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="tr">Türkçe</option>
              <option value="en">English</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('user.primaryCampus') }}</label>
            <select v-model="createForm.primaryCampusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.none') }}</option>
              <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Delete confirm -->
    <ConfirmModal :open="!!deleteTarget" :title="t('user.deleteTitle')" :message="t('user.deleteMessage', { name: deleteTarget?.fullName })"
      :confirm-label="t('common.delete')" :loading="deleteLoading" @confirm="doDelete" @cancel="deleteTarget = null" />
  </div>
</template>
