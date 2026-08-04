<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { LeaveRequestListItemDto, LeaveRequestListQuery } from '@/types/leave.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const leaveStore = useLeaveStore()
const auth = useAuthStore()
const { can } = usePermission()

const filters = reactive<LeaveRequestListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  status: (route.query.status as string) || '',
  unit: '',
  from: '',
  to: '',
  corporationId: auth.user?.corporationId,
})

const columns: Column<LeaveRequestListItemDto>[] = [
  { key: 'educatorFullName', label: t('leave.fields.educator') },
  { key: 'leaveTypeCode', label: t('leave.fields.leaveType'), width: '120px' },
  { key: 'unit', label: t('leave.fields.unit'), width: '90px' },
  { key: 'startsAt', label: t('leave.fields.startsAt'), width: '110px' },
  { key: 'endsAt', label: t('leave.fields.endsAt'), width: '110px' },
  { key: 'status', label: t('common.status'), width: '110px' },
]

function formatDate(dt: unknown) {
  if (!dt) return '—'
  return new Date(String(dt)).toLocaleDateString('tr-TR')
}

function statusClass(status: string) {
  const map: Record<string, string> = {
    Pending: 'bg-amber-100 text-amber-700',
    Approved: 'bg-green-100 text-green-700',
    Rejected: 'bg-red-100 text-red-700',
    Cancelled: 'bg-gray-100 text-gray-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    filters.page = 1
    leaveStore.fetchLeaves(filters)
  }, 400)
}

function doFetch() {
  filters.page = 1
  leaveStore.fetchLeaves(filters)
}

function resetFilters() {
  filters.search = ''
  filters.status = ''
  filters.unit = ''
  filters.from = ''
  filters.to = ''
  filters.page = 1
  leaveStore.fetchLeaves(filters)
}

watch(
  () => filters.page,
  () => leaveStore.fetchLeaves(filters)
)

onMounted(() => leaveStore.fetchLeaves(filters))
</script>

<template>
  <div>
    <PageHeader :title="t('leave.list.title')" :description="t('leave.list.subtitle')">
      <button
        v-if="can('leave_request:submit')"
        @click="router.push({ name: 'leave-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('leave.request.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          :placeholder="t('leave.list.searchPlaceholder')"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          @input="debouncedFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="Pending">{{ t('leave.status.pending') }}</option>
          <option value="Approved">{{ t('leave.status.approved') }}</option>
          <option value="Rejected">{{ t('leave.status.rejected') }}</option>
          <option value="Cancelled">{{ t('leave.status.cancelled') }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('leave.fields.unit') }}</label>
        <select v-model="filters.unit" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.select') }}</option>
          <option value="Day">{{ t('leave.unit.day') }}</option>
          <option value="Hour">{{ t('leave.unit.hour') }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.from') }}</label>
        <input v-model="filters.from" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch" />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.to') }}</label>
        <input v-model="filters.to" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch" />
      </div>
      <button
        @click="resetFilters"
        class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="leaveStore.leaveList.items"
      :loading="leaveStore.loading"
      :empty-text="t('leave.list.noData')"
      @row-click="(row) => router.push({ name: 'leave-detail', params: { id: row.id } })"
    >
      <template #cell-educatorFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-leaveTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-unit="{ value }">
        {{ value === 'Day' ? t('leave.unit.day') : t('leave.unit.hour') }}
      </template>
      <template #cell-startsAt="{ value }">{{ formatDate(value) }}</template>
      <template #cell-endsAt="{ value }">{{ formatDate(value) }}</template>
      <template #cell-status="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
          {{ t(`leave.status.${String(value).toLowerCase()}`) }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="row.status === 'Pending' && can('leave_request:update')"
            @click="router.push({ name: 'leave-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="leaveStore.leaveList.page"
        :page-size="leaveStore.leaveList.pageSize"
        :total-count="leaveStore.leaveList.totalCount"
        :total-pages="leaveStore.leaveList.totalPages"
        :has-previous-page="leaveStore.leaveList.hasPreviousPage"
        :has-next-page="leaveStore.leaveList.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; leaveStore.fetchLeaves(filters) }"
      />
    </div>
  </div>
</template>
