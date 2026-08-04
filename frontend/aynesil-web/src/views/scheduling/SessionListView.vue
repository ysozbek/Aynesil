<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useSessionStore } from '@/stores/session.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { SessionListItemDto } from '@/types/scheduling.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useSessionStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const sessionTypes = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  campusId: '',
  status: '',
  sessionTypeId: '',
  from: '',
  to: '',
  page: 1,
  pageSize: 20,
  search: '',
  sortBy: 'startsAt',
  sortDirection: 'desc' as 'asc' | 'desc',
})

let searchTimer: ReturnType<typeof setTimeout>
function onSearchInput(val: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { query.search = val; query.page = 1 }, 350)
}

watch(
  () => [query.status, query.sessionTypeId, query.from, query.to, query.campusId, query.page, query.pageSize],
  () => loadList(),
)

onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('SESSION_TYPE').then(v => { sessionTypes.value = v }),
  ])
})

async function loadList() {
  await store.fetchSessions({
    ...query,
    corporationId: corporationId.value,
    status: query.status || undefined,
    sessionTypeId: query.sessionTypeId || undefined,
    campusId: query.campusId || undefined,
    from: query.from || undefined,
    to: query.to || undefined,
    search: query.search || undefined,
  })
}

function onSort(key: string, dir: 'asc' | 'desc') {
  query.sortBy = key
  query.sortDirection = dir
}

const columns: Column<SessionListItemDto>[] = [
  { key: 'title', label: t('scheduling.session.titleField') },
  { key: 'startsAt', label: t('scheduling.session.startsAt'), sortable: true, width: '160px' },
  { key: 'endsAt', label: t('scheduling.session.endsAt'), width: '120px' },
  { key: 'sessionTypeLabel', label: t('scheduling.session.type'), width: '130px' },
  { key: 'roomName', label: t('scheduling.session.room'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '110px' },
  { key: 'participantCount', label: t('scheduling.session.participants'), width: '100px', align: 'center' },
  { key: 'isMakeup', label: t('scheduling.session.isMakeup'), width: '90px', align: 'center' },
]

function formatDateTime(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' })
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    scheduled: 'bg-blue-100 text-blue-700',
    in_progress: 'bg-amber-100 text-amber-700',
    completed: 'bg-green-100 text-green-700',
    cancelled: 'bg-red-100 text-red-700',
    no_show: 'bg-gray-100 text-gray-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}

function statusLabel(status: string): string {
  const map: Record<string, string> = {
    scheduled: t('scheduling.session.status.scheduled'),
    in_progress: t('scheduling.session.status.in_progress'),
    completed: t('scheduling.session.status.completed'),
    cancelled: t('scheduling.session.status.cancelled'),
    no_show: t('scheduling.session.status.no_show'),
  }
  return map[status] ?? status
}

const deleteTarget = ref<SessionListItemDto | null>(null)
const deleteLoading = ref(false)

function confirmDelete(row: SessionListItemDto, e: Event) {
  e.stopPropagation()
  deleteTarget.value = row
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    await store.deleteSession(deleteTarget.value.id)
    deleteTarget.value = null
    await loadList()
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.session.title')" :description="t('scheduling.session.description')">
      <button
        v-if="can('session:create')"
        @click="router.push({ name: 'session-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('scheduling.session.create') }}
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

      <select v-model="query.status" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="scheduled">{{ t('scheduling.session.status.scheduled') }}</option>
        <option value="in_progress">{{ t('scheduling.session.status.in_progress') }}</option>
        <option value="completed">{{ t('scheduling.session.status.completed') }}</option>
        <option value="cancelled">{{ t('scheduling.session.status.cancelled') }}</option>
        <option value="no_show">{{ t('scheduling.session.status.no_show') }}</option>
      </select>

      <select v-model="query.sessionTypeId" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('scheduling.session.allTypes') }}</option>
        <option v-for="st in sessionTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
      </select>

      <input
        v-model="query.from"
        type="date"
        @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <input
        v-model="query.to"
        type="date"
        @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
    </div>

    <DataTable
      :columns="columns"
      :rows="store.sessionList.items"
      :loading="store.loading"
      :sort-by="query.sortBy"
      :sort-direction="query.sortDirection"
      @sort="onSort"
      @row-click="(row) => router.push({ name: 'session-detail', params: { id: row.id } })"
    >
      <template #cell-startsAt="{ value }">
        {{ formatDateTime(value) }}
      </template>
      <template #cell-endsAt="{ value }">
        <span class="text-muted-foreground text-xs">
          {{ value ? new Date(String(value)).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }) : '—' }}
        </span>
      </template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ statusLabel(row.status) }}
        </span>
      </template>
      <template #cell-roomName="{ value }">
        <span class="text-muted-foreground">{{ value ?? t('scheduling.session.noRoom') }}</span>
      </template>
      <template #cell-isMakeup="{ value }">
        <span v-if="value" class="px-1.5 py-0.5 rounded text-xs bg-amber-100 text-amber-700">{{ t('scheduling.session.makeup') }}</span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('session:read')"
            @click="router.push({ name: 'session-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('session:delete') && row.status === 'scheduled'"
            @click="confirmDelete(row, $event)"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
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
        :page="store.sessionList.page"
        :page-size="store.sessionList.pageSize"
        :total-count="store.sessionList.totalCount"
        :total-pages="store.sessionList.totalPages"
        :has-previous-page="store.sessionList.hasPreviousPage"
        :has-next-page="store.sessionList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('scheduling.session.deleteTitle')"
      :message="t('scheduling.session.deleteMessage', { title: deleteTarget?.title })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
