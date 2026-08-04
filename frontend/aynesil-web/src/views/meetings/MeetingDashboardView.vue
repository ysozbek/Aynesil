<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMeetingStore } from '@/stores/meeting.store'
import { useFollowUpStore } from '@/stores/followUp.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import { meetingService } from '@/services/meeting.service'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const store = useMeetingStore()
const followUpStore = useFollowUpStore()
const auth = useAuthStore()
const { can } = usePermission()

const scheduledCount = computed(() =>
  store.meetingList.items.filter((m) => m.status === 'scheduled').length
)
const completedCount = computed(() =>
  store.meetingList.items.filter((m) => m.status === 'completed').length
)

const upcomingMeetings = computed(() => {
  const now = new Date()
  return store.meetingList.items
    .filter((m) => m.scheduledAt && new Date(m.scheduledAt) >= now && m.status === 'scheduled')
    .slice(0, 8)
})

function day(d?: string): string {
  return d ? new Date(d).getDate().toString() : '—'
}
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
  const map: Record<string, string> = {
    parent: 'bg-blue-100 text-blue-700',
    internal: 'bg-gray-100 text-gray-600',
    prospect: 'bg-amber-100 text-amber-700',
    external: 'bg-violet-100 text-violet-700',
  }
  return (code && map[code.toLowerCase()]) ? map[code.toLowerCase()] : 'bg-gray-100 text-gray-600'
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

async function loadFollowUps() {
  const results = await Promise.all(
    store.meetingList.items.map(async (m) => {
      const res = await meetingService.get(m.id)
      if (res.success && res.data) {
        return res.data.followUps.map((fu) => ({ ...fu, meetingTitle: m.title }))
      }
      return []
    })
  )
  followUpStore.setFollowUps(results.flat())
}

onMounted(async () => {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0]
  const to = new Date(now.getFullYear(), now.getMonth() + 2, 0).toISOString().split('T')[0]
  await store.fetchMeetings({
    corporationId: auth.user?.corporationId,
    page: 1,
    pageSize: 50,
    from,
    to,
  })
  await loadFollowUps()
})
</script>

<template>
  <div>
    <PageHeader :title="t('meeting.dashboard.title')" :description="t('meeting.dashboard.subtitle')">
      <div class="flex flex-wrap items-center gap-2">
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
        <button
          @click="router.push({ name: 'meeting-calendar' })"
          class="flex items-center gap-2 px-4 py-2 border border-border rounded-lg text-sm font-medium hover:bg-accent"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          {{ t('meeting.nav.calendar') }}
        </button>
      </div>
    </PageHeader>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-primary">{{ store.meetingList.totalCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('meeting.dashboard.total') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-blue-600">{{ scheduledCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('meeting.dashboard.scheduled') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-green-600">{{ completedCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('meeting.dashboard.completed') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-amber-600">{{ followUpStore.pendingFollowUps.length }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('meeting.dashboard.pendingFollowUps') }}</p>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2 rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('meeting.dashboard.upcoming') }}</h3>
          <button
            @click="router.push({ name: 'meetings' })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>

        <div v-if="store.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-14 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="upcomingMeetings.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('meeting.dashboard.noUpcoming') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="m in upcomingMeetings"
            :key="m.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
            @click="router.push({ name: 'meeting-detail', params: { id: m.id } })"
          >
            <div class="shrink-0 text-center rounded-lg bg-primary/10 p-2 w-14">
              <p class="text-xl font-bold text-primary">{{ day(m.scheduledAt) }}</p>
              <p class="text-xs text-muted-foreground">{{ month(m.scheduledAt) }}</p>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ m.title }}</p>
              <p class="text-xs text-muted-foreground">
                {{ m.scheduledAt ? formatTime(m.scheduledAt) : '—' }}
                <span v-if="m.location"> · {{ m.location }}</span>
              </p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', meetingTypeClass(m.meetingTypeCode)]">
              {{ m.meetingTypeCode ?? '—' }}
            </span>
          </div>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('meeting.followUp.pendingTitle') }}</h3>
          <button
            @click="router.push({ name: 'follow-ups' })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>
        <div v-if="followUpStore.pendingFollowUps.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('meeting.followUp.none') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="fu in followUpStore.pendingFollowUps.slice(0, 6)"
            :key="fu.id"
            class="px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
            @click="router.push({ name: 'meeting-detail', params: { id: fu.meetingId } })"
          >
            <div class="flex items-center gap-2 mb-1">
              <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', followUpStatusClass(fu.status)]">
                {{ followUpStatusLabel(fu.status) }}
              </span>
            </div>
            <p class="text-sm font-medium text-foreground truncate">{{ fu.action }}</p>
            <p v-if="fu.dueDate" class="text-xs text-muted-foreground mt-0.5">{{ formatDate(fu.dueDate) }}</p>
          </div>
        </div>
      </div>
    </div>

    <div class="mt-6 grid grid-cols-2 md:grid-cols-3 gap-4">
      <button
        @click="router.push({ name: 'meetings' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <p class="font-semibold text-foreground text-sm">{{ t('meeting.nav.list') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('meeting.list.subtitle') }}</p>
      </button>
      <button
        @click="router.push({ name: 'meeting-calendar' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <p class="font-semibold text-foreground text-sm">{{ t('meeting.nav.calendar') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('meeting.calendar.subtitle') }}</p>
      </button>
      <button
        @click="router.push({ name: 'follow-ups' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <p class="font-semibold text-foreground text-sm">{{ t('meeting.nav.followUps') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('meeting.followUp.listSubtitle') }}</p>
      </button>
    </div>
  </div>
</template>
