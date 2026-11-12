<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { FollowUpActivityListItemDto, FollowUpListQuery } from '@/types/consultancy.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const store = useConsultancyStore()
const { can } = usePermission()
const showCreateModal = ref(false)

const filters = reactive({
  page: 1,
  pageSize: 20,
  status: '',
  overdueOnly: false,
})

const createForm = reactive({
  title: '',
  description: '',
  dueDate: '',
  assignedTo: '',
})

const completeModal = reactive({
  show: false,
  followUpId: '',
  rowVersion: 1,
  notes: '',
})

const columns: Column<FollowUpActivityListItemDto>[] = [
  { key: 'title', label: t('followUp.fields.title') },
  { key: 'planName', label: t('followUp.fields.plan') },
  { key: 'visitDate', label: t('followUp.fields.visit'), width: '100px' },
  { key: 'dueDate', label: t('followUp.dueDate'), width: '100px' },
  { key: 'assignedTo', label: t('followUp.assignedTo'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '110px' },
]

function isOverdue(f: FollowUpActivityListItemDto): boolean {
  if (!f.dueDate || f.status === 'completed' || f.status === 'cancelled') return false
  return new Date(f.dueDate) < new Date()
}

async function quickStart(id: string) {
  await store.startFollowUp(id)
  await doFetch()
}

/** List items may not carry rowVersion; fall back to 1 as before. */
function rowVersionOf(row: FollowUpActivityListItemDto): number {
  return (row as FollowUpActivityListItemDto & { rowVersion?: number }).rowVersion ?? 1
}

function openCompleteModal(id: string, rowVersion: number) {
  completeModal.followUpId = id
  completeModal.rowVersion = rowVersion
  completeModal.notes = ''
  completeModal.show = true
}

async function doComplete() {
  await store.completeFollowUp(completeModal.followUpId, {
    notes: completeModal.notes || undefined,
    rowVersion: completeModal.rowVersion,
  })
  completeModal.show = false
  await doFetch()
}

async function doCreate() {
  if (!createForm.title.trim()) return
  await store.createFollowUp({
    title: createForm.title,
    description: createForm.description || undefined,
    dueDate: createForm.dueDate || undefined,
    assignedTo: createForm.assignedTo || undefined,
  })
  showCreateModal.value = false
  Object.assign(createForm, { title: '', description: '', dueDate: '', assignedTo: '' })
  await doFetch()
}

function buildQuery(): FollowUpListQuery {
  return {
    page: filters.page,
    pageSize: filters.pageSize,
    status: filters.status || undefined,
    overdueOnly: filters.overdueOnly ? true : undefined,
  }
}

async function doFetch() {
  filters.page = 1
  await store.fetchFollowUps(buildQuery())
}

function resetFilters() {
  filters.status = ''
  filters.overdueOnly = false
  filters.page = 1
  doFetch()
}

watch(
  () => filters.page,
  () => store.fetchFollowUps(buildQuery())
)

onMounted(doFetch)
</script>

<template>
  <div>
    <PageHeader :title="t('followUp.title')" :description="t('followUp.subtitle')">
      <div class="flex items-center gap-2">
        <button
          @click="router.push('/consultancy/follow-ups/open')"
          class="px-4 py-2 text-sm rounded-lg border border-amber-200 text-amber-700 hover:bg-amber-50"
        >
          {{ t('followUp.openReportLabel') }}
        </button>
        <button
          v-if="can(Permissions.FollowUps.Create)"
          @click="showCreateModal = true"
          class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('followUp.new') }}
        </button>
      </div>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="pending">{{ t('followUp.pending') }}</option>
          <option value="in_progress">{{ t('followUp.inProgress') }}</option>
          <option value="completed">{{ t('followUp.completed') }}</option>
          <option value="cancelled">{{ t('followUp.cancelled') }}</option>
        </select>
      </div>
      <div class="flex items-center gap-2 h-9">
        <input
          v-model="filters.overdueOnly"
          type="checkbox"
          class="rounded border-border"
          @change="doFetch"
        />
        <label class="text-sm font-medium text-red-600">{{ t('followUp.overdueOnly') }}</label>
      </div>
      <button @click="resetFilters" class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent">
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.followUps.items"
      :loading="store.loading"
      :empty-text="t('followUp.noData')"
      @row-click="(row) => router.push(`/consultancy/follow-ups/${row.id}`)"
    >
      <template #cell-title="{ row, value }">
        <div class="flex items-center gap-2">
          <span v-if="isOverdue(row)" class="px-1.5 py-0.5 rounded text-xs font-medium bg-red-100 text-red-700">
            {{ t('followUp.overdue') }}
          </span>
          <span class="font-medium text-foreground">{{ value }}</span>
        </div>
      </template>
      <template #cell-planName="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-visitDate="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-dueDate="{ row, value }">
        <span :class="isOverdue(row) ? 'font-bold text-red-600 text-xs' : 'text-muted-foreground text-xs'">
          {{ value ?? '—' }}
        </span>
      </template>
      <template #cell-assignedTo="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ value }">
        <StatusBadge entity="consultancyFollowUp" :status="String(value)" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="row.status === 'pending' && can(Permissions.FollowUps.Start)"
            :disabled="store.saving"
            @click="quickStart(row.id)"
            class="p-1.5 rounded-lg hover:bg-accent text-sky-600 hover:text-sky-700"
            :title="t('followUp.start')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </button>
          <button
            v-if="row.status === 'in_progress' && can(Permissions.FollowUps.Complete)"
            @click="openCompleteModal(row.id, rowVersionOf(row))"
            class="p-1.5 rounded-lg hover:bg-accent text-green-600 hover:text-green-700"
            :title="t('followUp.markCompleted')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.followUps.page"
        :page-size="store.followUps.pageSize"
        :total-count="store.followUps.totalCount"
        :total-pages="store.followUps.totalPages"
        :has-previous-page="store.followUps.hasPreviousPage"
        :has-next-page="store.followUps.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; store.fetchFollowUps(buildQuery()) }"
      />
    </div>

    <FormModal
      :open="showCreateModal"
      :title="t('followUp.new')"
      wide
      :saving="store.saving"
      @submit="doCreate"
      @close="showCreateModal = false"
    >
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.fields.title') }} *</label>
          <input v-model="createForm.title" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.fields.description') }}</label>
          <textarea v-model="createForm.description" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.dueDate') }}</label>
          <input v-model="createForm.dueDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.assignedTo') }} ID</label>
          <input v-model="createForm.assignedTo" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <FormModal
      :open="completeModal.show"
      :title="t('followUp.markCompleted')"
      :saving="store.saving"
      @submit="doComplete"
      @close="completeModal.show = false"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.completionNote') }}</label>
        <textarea
          v-model="completeModal.notes"
          rows="3"
          :placeholder="t('followUp.completionNotePlaceholder')"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent"
        />
      </div>
    </FormModal>
  </div>
</template>
