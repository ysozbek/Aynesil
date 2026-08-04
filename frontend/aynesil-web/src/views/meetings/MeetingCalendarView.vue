<template>
  <div class="p-6 space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ $t('meeting.calendar.title') }}</h1>
        <p class="text-sm text-gray-500">{{ $t('meeting.calendar.subtitle') }}</p>
      </div>
      <div class="flex gap-2">
        <div class="join">
          <button :class="['btn btn-sm join-item', viewMode === 'month' ? 'btn-active' : '']" @click="viewMode = 'month'">{{ $t('meeting.calendar.month') }}</button>
          <button :class="['btn btn-sm join-item', viewMode === 'week' ? 'btn-active' : '']" @click="viewMode = 'week'">{{ $t('meeting.calendar.week') }}</button>
          <button :class="['btn btn-sm join-item', viewMode === 'day' ? 'btn-active' : '']" @click="viewMode = 'day'">{{ $t('meeting.calendar.day') }}</button>
        </div>
        <router-link :to="{ name: 'meeting-new' }" class="btn btn-primary btn-sm">+ {{ $t('meeting.actions.create') }}</router-link>
      </div>
    </div>

    <!-- Navigation -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <button class="btn btn-ghost btn-sm" @click="prev">‹</button>
        <h2 class="text-lg font-semibold min-w-40 text-center">{{ periodLabel }}</h2>
        <button class="btn btn-ghost btn-sm" @click="next">›</button>
      </div>
      <button class="btn btn-ghost btn-sm" @click="goToday">{{ $t('meeting.calendar.today') }}</button>
    </div>

    <div v-if="calendarStore.loading" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <!-- Month grid -->
    <div v-else-if="viewMode === 'month'" class="border rounded-lg overflow-hidden">
      <!-- Day headers -->
      <div class="grid grid-cols-7 bg-base-200">
        <div v-for="day in weekDays" :key="day" class="p-2 text-center text-xs font-semibold text-gray-500 border-r last:border-0">
          {{ day }}
        </div>
      </div>
      <!-- Weeks -->
      <div class="grid grid-cols-7">
        <div
          v-for="cell in monthCells"
          :key="cell.dateStr"
          :class="['border-r border-b last:border-r-0 p-1 min-h-24 bg-base-100', cell.isOtherMonth ? 'opacity-40' : '', cell.isToday ? 'bg-primary/5' : '']"
        >
          <p :class="['text-xs font-medium mb-1', cell.isToday ? 'text-primary font-bold' : 'text-gray-500']">{{ cell.day }}</p>
          <div class="space-y-0.5">
            <div
              v-for="m in cell.meetings"
              :key="m.id"
              :class="['text-xs px-1 py-0.5 rounded cursor-pointer truncate', meetingColorClass(m.status)]"
              @click="$router.push({ name: 'meeting-detail', params: { id: m.id } })"
            >
              {{ m.title }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Week / Day list fallback -->
    <div v-else class="card bg-base-100 shadow overflow-hidden">
      <div class="divide-y">
        <div
          v-for="m in filteredMeetings"
          :key="m.id"
          class="px-5 py-3 flex items-center gap-4 hover:bg-base-50 cursor-pointer"
          @click="$router.push({ name: 'meeting-detail', params: { id: m.id } })"
        >
          <div class="text-center min-w-16">
            <p class="text-sm font-bold">{{ m.scheduledAt ? formatTime(m.scheduledAt) : '-' }}</p>
          </div>
          <div class="flex-1">
            <p class="font-medium">{{ m.title }}</p>
            <p v-if="m.location" class="text-xs text-gray-500">{{ m.location }}</p>
          </div>
          <span :class="['badge badge-sm', meetingStatusClass(m.status)]">{{ m.status }}</span>
          <span class="badge badge-ghost badge-xs">{{ m.participantCount }} {{ $t('meeting.fields.participants') }}</span>
        </div>
        <div v-if="!filteredMeetings.length" class="px-5 py-10 text-center text-gray-400">
          {{ $t('meeting.calendar.noMeetings') }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useMeetingCalendarStore } from '@/stores/meetingCalendar.store'
import { useAuthStore } from '@/stores/auth.store'
import type { MeetingCalendarItemDto } from '@/types/meeting.types'

const calendarStore = useMeetingCalendarStore()
const auth = useAuthStore()

const viewMode = ref<'month' | 'week' | 'day'>('month')
const currentDate = ref(new Date())

const weekDays = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

const periodLabel = computed(() => {
  const d = currentDate.value
  if (viewMode.value === 'month') return d.toLocaleString('tr-TR', { month: 'long', year: 'numeric' })
  if (viewMode.value === 'week') {
    const start = getWeekStart(d)
    const end = new Date(start); end.setDate(end.getDate() + 6)
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
    const end = getWeekStart(currentDate.value); end.setDate(end.getDate() + 6)
    return end.toISOString().split('T')[0]
  }
  return currentDate.value.toISOString().split('T')[0]
})

const monthCells = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()
  const firstDay = new Date(year, month, 1)
  const startMonday = getWeekStart(firstDay)
  const today = new Date(); today.setHours(0,0,0,0)

  const cells = []
  const cursor = new Date(startMonday)
  for (let i = 0; i < 42; i++) {
    const dateStr = cursor.toISOString().split('T')[0]
    cells.push({
      day: cursor.getDate(),
      dateStr,
      isToday: cursor.getTime() === today.getTime(),
      isOtherMonth: cursor.getMonth() !== month,
      meetings: calendarStore.calendarItems.filter(m => m.scheduledAt?.startsWith(dateStr) ?? false),
    })
    cursor.setDate(cursor.getDate() + 1)
  }
  return cells
})

const filteredMeetings = computed<MeetingCalendarItemDto[]>(() => {
  if (viewMode.value === 'week') {
    const start = getWeekStart(currentDate.value)
    const end = new Date(start); end.setDate(end.getDate() + 7)
    return calendarStore.calendarItems.filter(m => {
      if (!m.scheduledAt) return false
      const d = new Date(m.scheduledAt)
      return d >= start && d < end
    })
  }
  if (viewMode.value === 'day') {
    const dateStr = currentDate.value.toISOString().split('T')[0]
    return calendarStore.calendarItems.filter(m => m.scheduledAt?.startsWith(dateStr) ?? false)
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
  const map: Record<string, string> = { scheduled: 'bg-info/20 text-info', completed: 'bg-success/20 text-success', cancelled: 'bg-error/20 text-error', draft: 'bg-base-300 text-gray-500' }
  return map[s] ?? 'bg-base-300 text-gray-500'
}
function meetingStatusClass(s: string): string {
  const map: Record<string, string> = { draft: 'badge-ghost', scheduled: 'badge-info', completed: 'badge-success', cancelled: 'badge-error' }
  return map[s] ?? 'badge-ghost'
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
