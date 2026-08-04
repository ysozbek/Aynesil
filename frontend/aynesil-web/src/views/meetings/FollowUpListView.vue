<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMeetingStore } from '@/stores/meeting.store'
import { useFollowUpStore } from '@/stores/followUp.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import { meetingService } from '@/services/meeting.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { MeetingFollowUpDto } from '@/types/meeting.types'

type FollowUpRow = MeetingFollowUpDto & { meetingTitle?: string }

const { t } = useI18n()
const router = useRouter()
const meetingStore = useMeetingStore()
const followUpStore = useFollowUpStore()
const auth = useAuthStore()
const { can } = usePermission()

const statusFilter = ref('')
const loading = ref(false)

const columns: Column<FollowUpRow>[] = [
  { key: 'action', label: t('meeting.followUp.action') },
  { key: 'status', label: t('common.status'), width: '130px' },
  { key: 'dueDate', label: t('meeting.followUp.dueDate'), width: '120px' },
  { key: 'meetingTitle', label: t('meeting.followUp.meeting'), width: '180px' },
]

const filteredFollowUps = computed(() => {
  let items = followUpStore.allFollowUps
  if (statusFilter.value) items = items.filter((f) => f.status === statusFilter.value)
  return items
})

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}

function followUpStatusClass(s: string): string {
  const map: Record<string, string> = {
    pending: 'bg-amber-100 text-amber-700',
    in_progress: 'bg-blue-100 text-blue-700',
    completed: 'bg-green-100 text-green-700',
    cancelled: 'bg-gray-100 text-gray-600',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function followUpStatusLabel(s: string): string {
  const map: Record<string, string> = {
    pending: t('followUp.pending'),
    in_progress: t('followUp.inProgress'),
    completed: t('followUp.completed'),
    cancelled: t('followUp.cancelled'),
  }
  return map[s] ?? s
}

function isOverdue(dueDate?: string, status?: string): boolean {
  if (!dueDate || status === 'completed' || status === 'cancelled') return false
  return new Date(dueDate) < new Date()
}

async function loadFollowUps() {
  loading.value = true
  try {
    await meetingStore.fetchMeetings({
      corporationId: auth.user?.corporationId,
      page: 1,
      pageSize: 100,
    })
    const results = await Promise.all(
      meetingStore.meetingList.items.map(async (m) => {
        const res = await meetingService.get(m.id)
        if (res.success && res.data) {
          return res.data.followUps.map((fu) => ({ ...fu, meetingTitle: m.title }))
        }
        return []
      })
    )
    followUpStore.setFollowUps(results.flat())
  } finally {
    loading.value = false
  }
}

async function completeFollowUp(fu: FollowUpRow) {
  await followUpStore.updateStatus(fu.meetingId, fu.id, { status: 'completed' })
}

onMounted(loadFollowUps)
</script>

<template>
  <div>
    <PageHeader :title="t('meeting.followUp.listTitle')" :description="t('meeting.followUp.listSubtitle')" />

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="statusFilter" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="pending">{{ t('followUp.pending') }}</option>
          <option value="in_progress">{{ t('followUp.inProgress') }}</option>
          <option value="completed">{{ t('followUp.completed') }}</option>
          <option value="cancelled">{{ t('followUp.cancelled') }}</option>
        </select>
      </div>
    </div>

    <div
      v-if="followUpStore.overdueFollowUps.length"
      class="mb-6 rounded-xl border border-red-200 bg-red-50/50 shadow-sm overflow-hidden"
    >
      <div class="px-4 py-3 border-b border-red-200">
        <h3 class="font-semibold text-red-700 text-sm">
          {{ t('meeting.followUp.overdue') }} ({{ followUpStore.overdueFollowUps.length }})
        </h3>
      </div>
      <div class="divide-y divide-red-100">
        <div
          v-for="fu in followUpStore.overdueFollowUps"
          :key="fu.id"
          class="flex items-center gap-3 px-4 py-3"
        >
          <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">
            {{ followUpStatusLabel(fu.status) }}
          </span>
          <span class="flex-1 text-sm text-foreground">{{ fu.action }}</span>
          <span v-if="fu.dueDate" class="text-xs text-red-600 font-medium">{{ formatDate(fu.dueDate) }}</span>
          <button
            v-if="can('meeting:manage_follow_ups') && fu.status !== 'completed' && fu.status !== 'cancelled'"
            @click="completeFollowUp(fu)"
            :disabled="followUpStore.saving"
            class="px-2 py-1 text-xs rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
          >
            {{ t('followUp.markCompleted') }}
          </button>
        </div>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="filteredFollowUps"
      :loading="loading"
      :empty-text="t('followUp.noData')"
      @row-click="(row) => router.push({ name: 'meeting-detail', params: { id: row.meetingId } })"
    >
      <template #cell-action="{ value }">
        <span class="font-medium text-foreground">{{ value }}</span>
      </template>
      <template #cell-status="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', followUpStatusClass(String(value))]">
          {{ followUpStatusLabel(String(value)) }}
        </span>
      </template>
      <template #cell-dueDate="{ row, value }">
        <span :class="isOverdue(String(value), row.status) ? 'text-red-600 font-medium' : 'text-muted-foreground'">
          {{ value ? formatDate(String(value)) : '—' }}
        </span>
      </template>
      <template #cell-meetingTitle="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('meeting:manage_follow_ups') && row.status !== 'completed' && row.status !== 'cancelled'"
            @click="completeFollowUp(row)"
            :disabled="followUpStore.saving"
            class="px-2 py-1 text-xs rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
          >
            {{ t('meeting.followUp.complete') }}
          </button>
        </div>
      </template>
    </DataTable>
  </div>
</template>
