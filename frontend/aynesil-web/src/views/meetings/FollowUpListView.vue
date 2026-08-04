<template>
  <div class="p-6 space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ $t('meeting.followUp.listTitle') }}</h1>
      <p class="text-sm text-gray-500">{{ $t('meeting.followUp.listSubtitle') }}</p>
    </div>

    <!-- Filters -->
    <div class="flex gap-3 flex-wrap items-end">
      <div>
        <label class="label label-text text-xs">{{ $t('common.status') }}</label>
        <select v-model="statusFilter" class="select select-sm select-bordered">
          <option value="">{{ $t('common.allStatuses') }}</option>
          <option value="pending">Pending</option>
          <option value="in_progress">In Progress</option>
          <option value="completed">Completed</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="meetingStore.loading" class="flex justify-center py-10">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <template v-else>
      <!-- Overdue -->
      <div v-if="followUpStore.overdueFollowUps.length" class="card bg-error/10 border border-error/30 shadow">
        <div class="card-header border-b border-error/20 px-5 py-3">
          <h2 class="font-semibold text-error text-sm">⚠ {{ $t('meeting.followUp.overdue') }} ({{ followUpStore.overdueFollowUps.length }})</h2>
        </div>
        <div class="divide-y">
          <FollowUpRow
            v-for="fu in followUpStore.overdueFollowUps"
            :key="fu.id"
            :follow-up="fu"
            @complete="completeFollowUp(fu)"
          />
        </div>
      </div>

      <!-- All / filtered -->
      <div class="card bg-base-100 shadow overflow-hidden">
        <div class="overflow-x-auto">
          <table class="table table-sm">
            <thead>
              <tr>
                <th>{{ $t('meeting.followUp.action') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th>{{ $t('meeting.followUp.dueDate') }}</th>
                <th>{{ $t('meeting.followUp.meeting') }}</th>
                <th>{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!filteredFollowUps.length">
                <td colspan="5" class="text-center py-8 text-gray-400">{{ $t('common.noData') }}</td>
              </tr>
              <tr v-for="fu in filteredFollowUps" :key="fu.id">
                <td>{{ fu.action }}</td>
                <td><span :class="['badge badge-sm', followUpStatusClass(fu.status)]">{{ fu.status }}</span></td>
                <td>
                  <span :class="isOverdue(fu.dueDate, fu.status) ? 'text-error font-medium' : ''">
                    {{ fu.dueDate ? formatDate(fu.dueDate) : '-' }}
                  </span>
                </td>
                <td>{{ fu.meetingTitle ?? '-' }}</td>
                <td>
                  <button
                    v-if="fu.status !== 'completed' && fu.status !== 'cancelled'"
                    class="btn btn-xs btn-success"
                    @click="completeFollowUp(fu)"
                    :disabled="followUpStore.saving"
                  >
                    ✓ {{ $t('meeting.followUp.complete') }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, defineComponent, h } from 'vue'
import { useMeetingStore } from '@/stores/meeting.store'
import { useFollowUpStore } from '@/stores/followUp.store'
import type { MeetingFollowUpDto } from '@/types/meeting.types'

const meetingStore = useMeetingStore()
const followUpStore = useFollowUpStore()
const statusFilter = ref('')

// Inline row component to avoid extra files
const FollowUpRow = defineComponent({
  props: { followUp: { type: Object as () => MeetingFollowUpDto & { meetingTitle?: string }, required: true } },
  emits: ['complete'],
  setup(props, { emit }) {
    return () => h('div', { class: 'px-5 py-3 flex items-center gap-3' }, [
      h('span', { class: `badge badge-sm badge-error` }, props.followUp.status),
      h('span', { class: 'flex-1 text-sm' }, props.followUp.action),
      props.followUp.dueDate ? h('span', { class: 'text-xs text-error' }, formatDate(props.followUp.dueDate)) : null,
      h('button', { class: 'btn btn-xs btn-success', onClick: () => emit('complete') }, '✓'),
    ])
  },
})

const filteredFollowUps = computed(() => {
  let items = followUpStore.allFollowUps
  if (statusFilter.value) items = items.filter(f => f.status === statusFilter.value)
  return items
})

function formatDate(d: string): string { return new Date(d).toLocaleDateString('tr-TR') }
function followUpStatusClass(s: string): string {
  const map: Record<string, string> = { pending: 'badge-warning', in_progress: 'badge-info', completed: 'badge-success', cancelled: 'badge-ghost' }
  return map[s] ?? 'badge-ghost'
}
function isOverdue(dueDate?: string, status?: string): boolean {
  if (!dueDate || status === 'completed' || status === 'cancelled') return false
  return new Date(dueDate) < new Date()
}

async function completeFollowUp(fu: MeetingFollowUpDto) {
  await followUpStore.updateStatus(fu.meetingId, fu.id, { status: 'completed' })
}

onMounted(async () => {
  await meetingStore.fetchMeetings({ page: 1, pageSize: 100 })
  const allFollowUps = meetingStore.meetingList.items.flatMap(m => [])
  // Collect follow-ups from meetings that have been loaded individually
  if (meetingStore.currentMeeting) {
    followUpStore.setFollowUps(meetingStore.currentMeeting.followUps)
  }
})
</script>
