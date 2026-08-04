<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMeetingStore } from '@/stores/meeting.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { MeetingListItemDto, MeetingListQuery } from '@/types/meeting.types'

const { t } = useI18n()
const router = useRouter()
const store = useMeetingStore()
const auth = useAuthStore()
const { can } = usePermission()

const filters = reactive<MeetingListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  status: '',
  from: '',
  to: '',
  corporationId: auth.user?.corporationId,
})

const columns: Column<MeetingListItemDto>[] = [
  { key: 'title', label: t('meeting.fields.title') },
  { key: 'meetingTypeCode', label: t('meeting.fields.type'), width: '120px' },
  { key: 'scheduledAt', label: t('meeting.fields.scheduledAt'), width: '160px' },
  { key: 'location', label: t('meeting.fields.location'), width: '140px' },
  { key: 'status', label: t('common.status'), width: '120px' },
  { key: 'participantCount', label: t('meeting.fields.participants'), width: '100px' },
]

function formatDateTime(d: unknown) {
  if (!d) return '—'
  return new Date(String(d)).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

function meetingStatusClass(s: string) {
  const map: Record<string, string> = {
    draft: 'bg-gray-100 text-gray-600',
    scheduled: 'bg-blue-100 text-blue-700',
    completed: 'bg-green-100 text-green-700',
    cancelled: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function meetingStatusLabel(s: string) {
  const map: Record<string, string> = {
    draft: 'Taslak',
    scheduled: 'Planlandı',
    completed: 'Tamamlandı',
    cancelled: 'İptal Edildi',
  }
  return map[s] ?? s
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    filters.page = 1
    store.fetchMeetings(filters)
  }, 400)
}

function doFetch() {
  filters.page = 1
  store.fetchMeetings(filters)
}

function resetFilters() {
  filters.search = ''
  filters.status = ''
  filters.from = ''
  filters.to = ''
  filters.page = 1
  store.fetchMeetings(filters)
}

watch(
  () => filters.page,
  () => store.fetchMeetings(filters)
)

onMounted(() => store.fetchMeetings(filters))
</script>

<template>
  <div>
    <PageHeader :title="t('meeting.list.title')" :description="t('meeting.list.subtitle')">
      <button
        v-if="can('meeting:create')"
        @click="router.push({ name: 'meeting-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('meeting.actions.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          :placeholder="t('common.search')"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          @input="debouncedFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="draft">Taslak</option>
          <option value="scheduled">Planlandı</option>
          <option value="completed">Tamamlandı</option>
          <option value="cancelled">İptal Edildi</option>
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
      :rows="store.meetingList.items"
      :loading="store.loading"
      :empty-text="t('common.noData')"
      @row-click="(row) => router.push({ name: 'meeting-detail', params: { id: row.id } })"
    >
      <template #cell-title="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-meetingTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-scheduledAt="{ value }">{{ formatDateTime(value) }}</template>
      <template #cell-location="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', meetingStatusClass(String(value))]">
          {{ meetingStatusLabel(String(value)) }}
        </span>
      </template>
      <template #cell-participantCount="{ value }">{{ value ?? 0 }}</template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('meeting:update') && (row.status === 'scheduled' || row.status === 'draft')"
            @click="router.push({ name: 'meeting-edit', params: { id: row.id } })"
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
        :page="store.meetingList.page"
        :page-size="store.meetingList.pageSize"
        :total-count="store.meetingList.totalCount"
        :total-pages="store.meetingList.totalPages"
        :has-previous-page="store.meetingList.hasPreviousPage"
        :has-next-page="store.meetingList.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; store.fetchMeetings(filters) }"
      />
    </div>
  </div>
</template>
