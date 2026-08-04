<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useSessionStore } from '@/stores/session.store'
import { useMakeupSessionStore } from '@/stores/makeupSession.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const sessionStore = useSessionStore()
const makeupStore = useMakeupSessionStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const todayStr = new Date().toISOString().slice(0, 10)
const tomorrowStr = new Date(Date.now() + 86400000).toISOString().slice(0, 10)
const weekEnd = new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10)

onMounted(async () => {
  await Promise.all([
    sessionStore.fetchSessions({
      corporationId: corporationId.value,
      from: todayStr,
      to: todayStr,
      page: 1,
      pageSize: 10,
    }),
    makeupStore.fetchMakeupRequests({
      corporationId: corporationId.value,
      status: 'pending',
      page: 1,
      pageSize: 5,
    }),
  ])
})

const todaySessions = computed(() => sessionStore.sessionList.items)
const pendingMakeups = computed(() => makeupStore.requestList.items)

function formatTime(val: string): string {
  return new Date(val).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

function formatDate(val: string): string {
  return new Date(val).toLocaleDateString('tr-TR')
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
  return t(`scheduling.session.status.${status}`) || status
}
</script>

<template>
  <div>
    <PageHeader
      :title="t('scheduling.dashboard.title')"
      :description="t('scheduling.dashboard.description')"
    >
      <div class="flex items-center gap-2">
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
        <button
          @click="router.push({ name: 'scheduling-calendar' })"
          class="flex items-center gap-2 px-4 py-2 border border-border rounded-lg text-sm font-medium hover:bg-accent transition-colors"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          {{ t('scheduling.nav.calendar') }}
        </button>
      </div>
    </PageHeader>

    <!-- Quick Nav Cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <button
        @click="router.push({ name: 'sessions' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <div class="w-9 h-9 rounded-lg bg-blue-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('scheduling.nav.sessions') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('scheduling.session.title') }}</p>
      </button>

      <button
        @click="router.push({ name: 'scheduling-calendar' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <div class="w-9 h-9 rounded-lg bg-violet-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-violet-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('scheduling.nav.calendar') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('scheduling.calendar.title') }}</p>
      </button>

      <button
        @click="router.push({ name: 'makeup-requests' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <div class="w-9 h-9 rounded-lg bg-amber-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('scheduling.nav.makeupRequests') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ pendingMakeups.length }} {{ t('scheduling.makeup.pending') }}</p>
      </button>

      <button
        @click="router.push({ name: 'rooms' })"
        class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40 transition-colors"
      >
        <div class="w-9 h-9 rounded-lg bg-teal-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-teal-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('scheduling.nav.rooms') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('scheduling.room.title') }}</p>
      </button>
    </div>

    <!-- Today's Sessions -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2 rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('scheduling.dashboard.todaySessions') }}</h3>
          <button
            @click="router.push({ name: 'sessions', query: { from: todayStr, to: todayStr } })"
            class="text-xs text-primary hover:underline"
          >{{ t('common.viewAll') }}</button>
        </div>

        <div v-if="sessionStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-14 rounded-lg bg-accent animate-pulse" />
        </div>

        <div v-else-if="todaySessions.length === 0" class="py-10 text-center text-muted-foreground text-sm">
          {{ t('scheduling.dashboard.noSessionsToday') }}
        </div>

        <div v-else class="divide-y divide-border">
          <div
            v-for="session in todaySessions"
            :key="session.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
            @click="router.push({ name: 'session-detail', params: { id: session.id } })"
          >
            <div class="text-center min-w-[52px]">
              <p class="text-sm font-semibold text-foreground">{{ formatTime(session.startsAt) }}</p>
              <p class="text-xs text-muted-foreground">{{ formatTime(session.endsAt) }}</p>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ session.title }}</p>
              <p class="text-xs text-muted-foreground">
                {{ session.roomName ?? t('scheduling.session.noRoom') }} ·
                {{ session.participantCount }} {{ t('scheduling.session.participants') }}
              </p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', statusColor(session.status)]">
              {{ statusLabel(session.status) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Pending Makeup Requests -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('scheduling.makeup.pendingTitle') }}</h3>
          <button
            @click="router.push({ name: 'makeup-requests' })"
            class="text-xs text-primary hover:underline"
          >{{ t('common.viewAll') }}</button>
        </div>

        <div v-if="makeupStore.loading" class="p-4 space-y-3">
          <div v-for="i in 3" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>

        <div v-else-if="pendingMakeups.length === 0" class="py-10 text-center text-muted-foreground text-sm">
          {{ t('scheduling.makeup.noPending') }}
        </div>

        <div v-else class="divide-y divide-border">
          <div
            v-for="req in pendingMakeups"
            :key="req.id"
            class="px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
            @click="router.push({ name: 'makeup-request-detail', params: { id: req.id } })"
          >
            <p class="text-sm font-medium text-foreground truncate">{{ req.studentFullName }}</p>
            <p class="text-xs text-muted-foreground mt-0.5">{{ req.missedSessionTitle }}</p>
            <p class="text-xs text-muted-foreground">{{ formatDate(req.missedSessionDate) }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
