<template>
  <div class="p-6 space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ $t('meeting.dashboard.title') }}</h1>
      <p class="text-sm text-gray-500">{{ $t('meeting.dashboard.subtitle') }}</p>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div class="card bg-primary text-primary-content shadow">
        <div class="card-body p-4">
          <p class="text-sm opacity-80">{{ $t('meeting.dashboard.total') }}</p>
          <p class="text-3xl font-bold">{{ store.meetingList.totalCount }}</p>
        </div>
      </div>
      <div class="card bg-info text-info-content shadow">
        <div class="card-body p-4">
          <p class="text-sm opacity-80">{{ $t('meeting.dashboard.scheduled') }}</p>
          <p class="text-3xl font-bold">{{ scheduledCount }}</p>
        </div>
      </div>
      <div class="card bg-success text-success-content shadow">
        <div class="card-body p-4">
          <p class="text-sm opacity-80">{{ $t('meeting.dashboard.completed') }}</p>
          <p class="text-3xl font-bold">{{ completedCount }}</p>
        </div>
      </div>
      <div class="card bg-warning text-warning-content shadow">
        <div class="card-body p-4">
          <p class="text-sm opacity-80">{{ $t('meeting.dashboard.pendingFollowUps') }}</p>
          <p class="text-3xl font-bold">{{ followUpStore.pendingFollowUps.length }}</p>
        </div>
      </div>
    </div>

    <!-- Upcoming meetings -->
    <div class="card bg-base-100 shadow">
      <div class="card-header border-b px-6 py-4 flex items-center justify-between">
        <h2 class="font-semibold">{{ $t('meeting.dashboard.upcoming') }}</h2>
        <div class="flex gap-2">
          <router-link :to="{ name: 'meeting-calendar' }" class="btn btn-ghost btn-sm">{{ $t('meeting.nav.calendar') }}</router-link>
          <router-link :to="{ name: 'meetings' }" class="btn btn-ghost btn-sm">{{ $t('common.viewAll') }}</router-link>
        </div>
      </div>
      <div class="card-body p-0">
        <div v-if="store.loading" class="flex justify-center py-8">
          <span class="loading loading-spinner text-primary"></span>
        </div>
        <div v-else class="divide-y">
          <div
            v-for="m in upcomingMeetings"
            :key="m.id"
            class="px-6 py-4 flex items-center gap-4 hover:bg-base-50"
          >
            <div class="flex-shrink-0 text-center bg-primary/10 rounded-lg p-2 w-14">
              <p class="text-xl font-bold text-primary">{{ day(m.scheduledAt) }}</p>
              <p class="text-xs text-gray-500">{{ month(m.scheduledAt) }}</p>
            </div>
            <div class="flex-1 min-w-0">
              <p class="font-medium truncate">{{ m.title }}</p>
              <p class="text-sm text-gray-500">
                {{ m.scheduledAt ? formatTime(m.scheduledAt) : '-' }}
                <span v-if="m.location"> · {{ m.location }}</span>
              </p>
            </div>
            <span :class="['badge badge-sm', meetingTypeClass(m.meetingTypeCode)]">
              {{ m.meetingTypeCode ?? '-' }}
            </span>
            <router-link :to="{ name: 'meeting-detail', params: { id: m.id } }" class="btn btn-ghost btn-xs">
              {{ $t('common.view') }}
            </router-link>
          </div>
          <div v-if="!upcomingMeetings.length" class="px-6 py-8 text-center text-gray-400">
            {{ $t('meeting.dashboard.noUpcoming') }}
          </div>
        </div>
      </div>
    </div>

    <!-- Pending Follow-ups -->
    <div v-if="followUpStore.pendingFollowUps.length" class="card bg-base-100 shadow">
      <div class="card-header border-b px-6 py-4 flex items-center justify-between">
        <h2 class="font-semibold">{{ $t('meeting.followUp.pendingTitle') }}</h2>
        <router-link :to="{ name: 'follow-ups' }" class="btn btn-ghost btn-sm">{{ $t('common.viewAll') }}</router-link>
      </div>
      <div class="card-body p-0">
        <div class="divide-y">
          <div
            v-for="fu in followUpStore.pendingFollowUps.slice(0, 5)"
            :key="fu.id"
            class="px-6 py-3 flex items-center gap-3"
          >
            <span :class="['badge badge-sm', followUpStatusClass(fu.status)]">{{ fu.status }}</span>
            <span class="flex-1 text-sm">{{ fu.action }}</span>
            <span v-if="fu.dueDate" class="text-xs text-gray-500">{{ formatDate(fu.dueDate) }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick actions -->
    <div class="flex flex-wrap gap-3">
      <router-link :to="{ name: 'meeting-new' }" class="btn btn-primary btn-sm">
        + {{ $t('meeting.actions.create') }}
      </router-link>
      <router-link :to="{ name: 'meeting-calendar' }" class="btn btn-outline btn-sm">
        {{ $t('meeting.nav.calendar') }}
      </router-link>
      <router-link :to="{ name: 'follow-ups' }" class="btn btn-outline btn-sm">
        {{ $t('meeting.nav.followUps') }}
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useMeetingStore } from '@/stores/meeting.store'
import { useFollowUpStore } from '@/stores/followUp.store'

const store = useMeetingStore()
const followUpStore = useFollowUpStore()

const scheduledCount = computed(() => store.meetingList.items.filter(m => m.status === 'scheduled').length)
const completedCount = computed(() => store.meetingList.items.filter(m => m.status === 'completed').length)

const upcomingMeetings = computed(() => {
  const now = new Date()
  return store.meetingList.items
    .filter(m => m.scheduledAt && new Date(m.scheduledAt) >= now && m.status === 'scheduled')
    .slice(0, 8)
})

function day(d?: string): string { return d ? new Date(d).getDate().toString() : '-' }
function month(d?: string): string {
  if (!d) return ''
  return new Date(d).toLocaleString('tr-TR', { month: 'short' })
}
function formatTime(d: string): string {
  return new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}
function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}
function meetingTypeClass(code?: string): string {
  const map: Record<string, string> = { parent: 'badge-info', internal: 'badge-ghost', prospect: 'badge-warning', external: 'badge-secondary' }
  return (code && map[code.toLowerCase()]) ? map[code.toLowerCase()] : 'badge-ghost'
}
function followUpStatusClass(s: string): string {
  const map: Record<string, string> = { pending: 'badge-warning', in_progress: 'badge-info', completed: 'badge-success', cancelled: 'badge-ghost' }
  return map[s] ?? 'badge-ghost'
}

onMounted(async () => {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0]
  const to = new Date(now.getFullYear(), now.getMonth() + 2, 0).toISOString().split('T')[0]
  await store.fetchMeetings({ page: 1, pageSize: 50, from, to })
})
</script>
