<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useMakeupSessionStore } from '@/stores/makeupSession.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { MakeupRequestListItemDto } from '@/types/scheduling.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useMakeupSessionStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
  studentId: '',
  status: '',
  from: '',
  to: '',
  page: 1,
  pageSize: 20,
})

watch(() => [query.status, query.studentId, query.from, query.to, query.page], () => loadList())
onMounted(() => loadList())

async function loadList() {
  await store.fetchMakeupRequests({
    ...query,
    corporationId: corporationId.value,
    status: query.status || undefined,
    studentId: query.studentId || undefined,
    from: query.from || undefined,
    to: query.to || undefined,
  })
}

const columns: Column<MakeupRequestListItemDto>[] = [
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'missedSessionTitle', label: t('scheduling.makeup.missedSession') },
  { key: 'missedSessionDate', label: t('scheduling.makeup.missedDate'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '110px' },
  { key: 'makeupSessionDate', label: t('scheduling.makeup.makeupDate'), width: '120px' },
  { key: 'expiresOn', label: t('scheduling.makeup.expiresOn'), width: '110px' },
  { key: 'requestedAt', label: t('scheduling.makeup.requestedAt'), sortable: true, width: '120px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    pending: 'bg-amber-100 text-amber-700',
    approved: 'bg-blue-100 text-blue-700',
    rejected: 'bg-red-100 text-red-700',
    scheduled: 'bg-violet-100 text-violet-700',
    completed: 'bg-green-100 text-green-700',
    expired: 'bg-gray-100 text-gray-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}

// Quick approve modal
const approveTarget = ref<MakeupRequestListItemDto | null>(null)
const approveLoading = ref(false)

async function doApprove() {
  if (!approveTarget.value) return
  approveLoading.value = true
  const detail = await store.fetchMakeupRequest(approveTarget.value.id)
  try {
    await store.approveRequest(approveTarget.value.id, { rowVersion: store.currentRequest!.rowVersion })
    approveTarget.value = null
    await loadList()
  } finally {
    approveLoading.value = false
  }
}

const rejectModal = ref(false)
const rejectTarget = ref<MakeupRequestListItemDto | null>(null)
const rejectReason = ref('')
const rejectLoading = ref(false)

async function doReject() {
  if (!rejectTarget.value) return
  rejectLoading.value = true
  await store.fetchMakeupRequest(rejectTarget.value.id)
  try {
    await store.rejectRequest(rejectTarget.value.id, { reason: rejectReason.value, rowVersion: store.currentRequest!.rowVersion })
    rejectModal.value = false
    rejectTarget.value = null
    rejectReason.value = ''
    await loadList()
  } finally {
    rejectLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.makeup.title')" :description="t('scheduling.makeup.description')">
      <button
        v-if="can('makeup_request:create')"
        @click="router.push({ name: 'makeup-request-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('scheduling.makeup.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <select v-model="query.status" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="pending">{{ t('scheduling.makeup.status.pending') }}</option>
        <option value="approved">{{ t('scheduling.makeup.status.approved') }}</option>
        <option value="rejected">{{ t('scheduling.makeup.status.rejected') }}</option>
        <option value="scheduled">{{ t('scheduling.makeup.status.scheduled') }}</option>
        <option value="completed">{{ t('scheduling.makeup.status.completed') }}</option>
        <option value="expired">{{ t('scheduling.makeup.status.expired') }}</option>
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
      :rows="store.requestList.items"
      :loading="store.loading"
      @row-click="(row) => router.push({ name: 'makeup-request-detail', params: { id: row.id } })"
    >
      <template #cell-missedSessionDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-makeupSessionDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-expiresOn="{ value }">{{ formatDate(value) }}</template>
      <template #cell-requestedAt="{ value }">{{ formatDate(value) }}</template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ t(`scheduling.makeup.status.${row.status}`) }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('makeup_request:update') && row.status === 'pending'"
            @click="approveTarget = row"
            class="p-1.5 rounded-lg hover:bg-green-50 text-muted-foreground hover:text-green-600"
            :title="t('scheduling.makeup.approve')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </button>
          <button
            v-if="can('makeup_request:update') && row.status === 'pending'"
            @click="rejectTarget = row; rejectModal = true"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
            :title="t('scheduling.makeup.reject')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
          <button
            @click="router.push({ name: 'makeup-request-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.requestList.page"
        :page-size="store.requestList.pageSize"
        :total-count="store.requestList.totalCount"
        :total-pages="store.requestList.totalPages"
        :has-previous-page="store.requestList.hasPreviousPage"
        :has-next-page="store.requestList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!approveTarget"
      :title="t('scheduling.makeup.approveTitle')"
      :message="t('scheduling.makeup.approveMessage', { student: approveTarget?.studentFullName })"
      :confirm-label="t('scheduling.makeup.approve')"
      :loading="approveLoading"
      @confirm="doApprove"
      @cancel="approveTarget = null"
    />

    <!-- Reject Modal -->
    <div v-if="rejectModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-4">{{ t('scheduling.makeup.rejectTitle') }}</h3>
        <textarea
          v-model="rejectReason"
          :placeholder="t('scheduling.makeup.rejectReason')"
          rows="3"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary resize-none"
        />
        <div class="flex justify-end gap-2 mt-4">
          <button @click="rejectModal = false; rejectTarget = null" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doReject" :disabled="rejectLoading" class="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
            {{ rejectLoading ? t('common.saving') : t('scheduling.makeup.reject') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
