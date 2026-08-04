<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMeetingCalendarStore } from '@/stores/meetingCalendar.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { MeetingCalendarItemDto } from '@/types/meeting.types'

const { t } = useI18n()
const router = useRouter()
const calendarStore = useMeetingCalendarStore()
const auth = useAuthStore()
const { can } = usePermission()

const viewMode = ref<'month' | 'week' | 'day'>('month')
const currentDate = ref(new Date())

const weekDays = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

const periodLabel = computed(() => {
  const d = currentDate.value
  if (viewMode.value === 'month') return d.toLocaleString('tr-TR', { month: 'long', year: 'numeric' })
  if (viewMode.value === 'week') {
    const start = getWeekStart(d)
    const end = new Date(start)
    end.setDate(end.getDate() + 6)
    return `${start.toLocaleDateString('tr-TR')} – ${end.toLocaleDateString('tr-TR')}`
  }
  return d.toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
})

function getWeekStart(d: Date): Date {
  const day = d.getDay() || 7
  const start = new Date(d)
  start.setDate(d.getDate() - day + 1)
  start.setHours(0, 0, 0, 0)
  return start
}

const rangeFrom = computed(() => {
  if (viewMode.value === 'month') {
    const d = new Date(currentDate.value.getFullYear(), currentDate.value.getMonth(), 1)
    d.setDate(d.getDate() - d.getDay() + 1)
    return d.toISOString().split('T')[0]
  }
  if (viewMode.value === 'week') return getWeekStart(currentDate.value).toISOString().split('T')[0]
  return currentDate.value.toISOString().split('T')[0]
})

const rangeTo = computed(() => {
  if (viewMode.value === 'month') {
    const d = new Date(currentDate.value.getFullYear(), currentDate.value.getMonth() + 1, 0)
    return d.toISOString().split('T')[0]
  }
  if (viewMode.value === 'week') {
    const end = getWeekStart(currentDate.value)
    end.setDate(end.getDate() + 6)
    return end.toISOString().split('T')[0]
  }
  return currentDate.value.toISOString().split('T')[0]
})

const monthCells = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()
  const firstDay = new Date(year, month, 1)
  const startMonday = getWeekStart(firstDay)
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const cells = []
  const cursor = new Date(startMonday)
  for (let i = 0; i < 42; i++) {
    const dateStr = cursor.toISOString().split('T')[0]
    cells.push({
      day: cursor.getDate(),
      dateStr,
      isToday: cursor.getTime() === today.getTime(),
      isOtherMonth: cursor.getMonth() !== month,
      meetings: calendarStore.calendarItems.filter((m) => m.scheduledAt?.startsWith(dateStr) ?? false),
    })
    cursor.setDate(cursor.getDate() + 1)
  }
  return cells
})

const filteredMeetings = computed<MeetingCalendarItemDto[]>(() => {
  if (viewMode.value === 'week') {
    const start = getWeekStart(currentDate.value)
    const end = new Date(start)
    end.setDate(end.getDate() + 7)
    return calendarStore.calendarItems.filter((m) => {
      if (!m.scheduledAt) return false
      const d = new Date(m.scheduledAt)
      return d >= start && d < end
    })
  }
  if (viewMode.value === 'day') {
    const dateStr = currentDate.value.toISOString().split('T')[0]
    return calendarStore.calendarItems.filter((m) => m.scheduledAt?.startsWith(dateStr) ?? false)
  }
  return calendarStore.calendarItems
})

function prev() {
  const d = new Date(currentDate.value)
  if (viewMode.value === 'month') d.setMonth(d.getMonth() - 1)
  else if (viewMode.value === 'week') d.setDate(d.getDate() - 7)
  else d.setDate(d.getDate() - 1)
  currentDate.value = d
}

function next() {
  const d = new Date(currentDate.value)
  if (viewMode.value === 'month') d.setMonth(d.getMonth() + 1)
  else if (viewMode.value === 'week') d.setDate(d.getDate() + 7)
  else d.setDate(d.getDate() + 1)
  currentDate.value = d
}

function goToday() {
  currentDate.value = new Date()
}

function formatTime(d: string): string {
  return new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

function meetingColorClass(s: string): string {
  const map: Record<string, string> = {
    scheduled: 'bg-blue-100 text-blue-700',
    completed: 'bg-green-100 text-green-700',
    cancelled: 'bg-red-100 text-red-700',
    draft: 'bg-gray-100 text-gray-600',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function meetingStatusClass(s: string): string {
  return meetingColorClass(s)
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

async function loadCalendar() {
  await calendarStore.fetchCalendar({
    corporationId: auth.user?.corporationId ?? undefined,
    from: rangeFrom.value,
    to: rangeTo.value,
  })
}

watch([viewMode, rangeFrom], loadCalendar)
onMounted(loadCalendar)
</script>

<template>
  <div>
    <PageHeader :title="t('meeting.calendar.title')" :description="t('meeting.calendar.subtitle')">
      <div class="flex flex-wrap items-center gap-2">
        <div class="flex rounded-lg border border-border overflow-hidden">
          <button
            :class="[
              'px-3 py-1.5 text-sm font-medium transition-colors',
              viewMode === 'month' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent',
            ]"
            @click="viewMode = 'month'"
          >
            {{ t('meeting.calendar.month') }}
          </button>
          <button
            :class="[
              'px-3 py-1.5 text-sm font-medium border-l border-border transition-colors',
              viewMode === 'week' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent',
            ]"
            @click="viewMode = 'week'"
          >
            {{ t('meeting.calendar.week') }}
          </button>
          <button
            :class="[
              'px-3 py-1.5 text-sm font-medium border-l border-border transition-colors',
              viewMode === 'day' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent',
            ]"
            @click="viewMode = 'day'"
          >
            {{ t('meeting.calendar.day') }}
          </button>
        </div>
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
      </div>
    </PageHeader>

    <div class="flex items-center justify-between mb-4">
      <div class="flex items-center gap-2">
        <button @click="prev" class="p-2 rounded-lg border border-border hover:bg-accent">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <span class="text-sm font-semibold text-foreground min-w-[180px] text-center capitalize">{{ periodLabel }}</span>
        <button @click="next" class="p-2 rounded-lg border border-border hover:bg-accent">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
      <button @click="goToday" class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent">
        {{ t('meeting.calendar.today') }}
      </button>
    </div>

    <div v-if="calendarStore.loading" class="py-16 text-center text-sm text-muted-foreground">
      {{ t('common.loading') }}
    </div>

    <div v-else-if="viewMode === 'month'" class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
      <div class="grid grid-cols-7 border-b border-border bg-accent/40">
        <div
          v-for="day in weekDays"
          :key="day"
          class="px-2 py-2 text-center text-xs font-semibold text-muted-foreground"
        >
          {{ day }}
        </div>
      </div>
      <div class="grid grid-cols-7">
        <div
          v-for="cell in monthCells"
          :key="cell.dateStr"
          :class="[
            'min-h-24 border-b border-r border-border p-1.5 last:border-r-0',
            cell.isOtherMonth ? 'bg-accent/20' : '',
            cell.isToday ? 'bg-primary/5' : '',
          ]"
        >
          <p :class="['text-xs font-semibold mb-1 px-1', cell.isToday ? 'text-primary' : 'text-foreground']">
            {{ cell.day }}
          </p>
          <button
            v-for="m in cell.meetings"
            :key="m.id"
            type="button"
            :class="['w-full text-left text-[10px] leading-tight px-1.5 py-1 rounded mb-0.5 truncate', meetingColorClass(m.status)]"
            @click="router.push({ name: 'meeting-detail', params: { id: m.id } })"
          >
            {{ m.title }}
          </button>
        </div>
      </div>
    </div>

    <div v-else class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
      <div v-if="filteredMeetings.length === 0" class="py-10 text-center text-sm text-muted-foreground">
        {{ t('meeting.calendar.noMeetings') }}
      </div>
      <div v-else class="divide-y divide-border">
        <div
          v-for="m in filteredMeetings"
          :key="m.id"
          class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
          @click="router.push({ name: 'meeting-detail', params: { id: m.id } })"
        >
          <div class="text-center min-w-[52px] shrink-0">
            <p class="text-sm font-semibold text-foreground">{{ m.scheduledAt ? formatTime(m.scheduledAt) : '—' }}</p>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-foreground truncate">{{ m.title }}</p>
            <p v-if="m.location" class="text-xs text-muted-foreground">{{ m.location }}</p>
          </div>
          <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', meetingStatusClass(m.status)]">
            {{ meetingStatusLabel(m.status) }}
          </span>
          <span class="text-xs text-muted-foreground shrink-0">
            {{ m.participantCount }} {{ t('meeting.fields.participants') }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
