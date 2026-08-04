<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useCalendarStore } from '@/stores/calendar.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { CalendarEventDto } from '@/types/scheduling.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const calendarStore = useCalendarStore()

const corporationId = computed(() => auth.user?.corporationId ?? '')

type ViewMode = 'school' | 'campus' | 'educator' | 'student'
const viewMode = ref<ViewMode>('school')
const resourceId = ref('')
const currentDate = ref(new Date())
const calendarView = ref<'week' | 'month' | 'day' | 'agenda'>('week')

const from = computed(() => {
  const d = new Date(currentDate.value)
  if (calendarView.value === 'day') return d.toISOString().slice(0, 10)
  if (calendarView.value === 'week') {
    const day = d.getDay()
    const start = new Date(d)
    start.setDate(d.getDate() - day)
    return start.toISOString().slice(0, 10)
  }
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10)
})

const to = computed(() => {
  const d = new Date(currentDate.value)
  if (calendarView.value === 'day') return d.toISOString().slice(0, 10)
  if (calendarView.value === 'week') {
    const day = d.getDay()
    const end = new Date(d)
    end.setDate(d.getDate() + (6 - day))
    return end.toISOString().slice(0, 10)
  }
  return new Date(d.getFullYear(), d.getMonth() + 1, 0).toISOString().slice(0, 10)
})

function formatDateHeader(): string {
  const d = currentDate.value
  if (calendarView.value === 'day') {
    return d.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })
  }
  if (calendarView.value === 'week') {
    return `${from.value} – ${to.value}`
  }
  return d.toLocaleDateString('tr-TR', { year: 'numeric', month: 'long' })
}

function prevPeriod() {
  const d = new Date(currentDate.value)
  if (calendarView.value === 'day') d.setDate(d.getDate() - 1)
  else if (calendarView.value === 'week') d.setDate(d.getDate() - 7)
  else d.setMonth(d.getMonth() - 1)
  currentDate.value = d
}

function nextPeriod() {
  const d = new Date(currentDate.value)
  if (calendarView.value === 'day') d.setDate(d.getDate() + 1)
  else if (calendarView.value === 'week') d.setDate(d.getDate() + 7)
  else d.setMonth(d.getMonth() + 1)
  currentDate.value = d
}

function goToday() {
  currentDate.value = new Date()
}

async function loadCalendar() {
  const query = { from: from.value, to: to.value }
  switch (viewMode.value) {
    case 'school':
      await calendarStore.fetchSchoolCalendar(corporationId.value, query)
      break
    case 'campus':
      if (resourceId.value) await calendarStore.fetchCampusCalendar(resourceId.value, query)
      break
    case 'educator':
      if (resourceId.value) await calendarStore.fetchEducatorCalendar(resourceId.value, query)
      break
    case 'student':
      if (resourceId.value) await calendarStore.fetchStudentCalendar(resourceId.value, query)
      break
  }
}

watch([viewMode, from, to], () => loadCalendar())
onMounted(() => loadCalendar())

function eventColor(event: CalendarEventDto): string {
  if (event.status === 'cancelled') return 'bg-red-100 border-red-300 text-red-700'
  if (event.status === 'completed') return 'bg-green-100 border-green-300 text-green-700'
  if (event.isMakeup) return 'bg-amber-100 border-amber-300 text-amber-700'
  if (event.type === 'calendar_entry') return 'bg-violet-100 border-violet-300 text-violet-700'
  return 'bg-blue-100 border-blue-300 text-blue-700'
}

function formatTime(val: string): string {
  return new Date(val).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

// Generate week days for week view
const weekDays = computed(() => {
  const days: Date[] = []
  const start = new Date(from.value)
  for (let i = 0; i < 7; i++) {
    const d = new Date(start)
    d.setDate(start.getDate() + i)
    days.push(d)
  }
  return days
})

// Hours for day/week view
const hours = Array.from({ length: 14 }, (_, i) => i + 7) // 7am to 8pm

function eventsOnDay(day: Date): CalendarEventDto[] {
  const dayStr = day.toISOString().slice(0, 10)
  return calendarStore.events.filter(e => e.start.slice(0, 10) === dayStr)
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.calendar.title')" :description="t('scheduling.calendar.description')">
      <button
        @click="router.push({ name: 'session-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('scheduling.session.create') }}
      </button>
    </PageHeader>

    <!-- Toolbar -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <!-- View Mode -->
      <div class="flex rounded-lg border border-border overflow-hidden">
        <button
          v-for="mode in ['school', 'campus', 'educator', 'student']"
          :key="mode"
          @click="viewMode = mode as ViewMode; resourceId = ''"
          :class="[
            'px-3 py-1.5 text-sm transition-colors',
            viewMode === mode ? 'bg-primary text-primary-foreground' : 'bg-transparent text-muted-foreground hover:bg-accent'
          ]"
        >
          {{ t(`scheduling.calendar.view.${mode}`) }}
        </button>
      </div>

      <!-- Resource ID input for campus/educator/student -->
      <input
        v-if="viewMode !== 'school'"
        v-model="resourceId"
        type="text"
        :placeholder="t(`scheduling.calendar.${viewMode}Id`)"
        @keydown.enter="loadCalendar"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />

      <div class="flex-1" />

      <!-- Calendar view type -->
      <div class="flex rounded-lg border border-border overflow-hidden">
        <button
          v-for="v in ['day', 'week', 'month', 'agenda']"
          :key="v"
          @click="calendarView = v as typeof calendarView"
          :class="[
            'px-3 py-1.5 text-sm transition-colors',
            calendarView === v ? 'bg-primary text-primary-foreground' : 'bg-transparent text-muted-foreground hover:bg-accent'
          ]"
        >
          {{ t(`scheduling.calendar.${v}`) }}
        </button>
      </div>

      <!-- Navigation -->
      <div class="flex items-center gap-1">
        <button @click="prevPeriod" class="w-8 h-8 flex items-center justify-center rounded-lg border border-border hover:bg-accent">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <button @click="goToday" class="px-3 h-8 text-sm border border-border rounded-lg hover:bg-accent">{{ t('scheduling.calendar.today') }}</button>
        <button @click="nextPeriod" class="w-8 h-8 flex items-center justify-center rounded-lg border border-border hover:bg-accent">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
        <span class="ml-2 text-sm font-medium text-foreground min-w-[180px]">{{ formatDateHeader() }}</span>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="calendarStore.loading" class="h-96 rounded-xl bg-accent animate-pulse" />

    <!-- Agenda View -->
    <div v-else-if="calendarView === 'agenda'" class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
      <div v-if="calendarStore.events.length === 0" class="py-16 text-center text-muted-foreground">
        {{ t('scheduling.calendar.noEvents') }}
      </div>
      <div v-else class="divide-y divide-border">
        <div
          v-for="event in calendarStore.events"
          :key="event.id"
          class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
          @click="event.type === 'session' && router.push({ name: 'session-detail', params: { id: event.id } })"
        >
          <div class="text-center min-w-[72px]">
            <p class="text-xs text-muted-foreground">{{ event.start.slice(0, 10) }}</p>
            <p class="text-sm font-semibold">{{ formatTime(event.start) }}</p>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-foreground truncate">{{ event.title }}</p>
            <p v-if="event.roomName" class="text-xs text-muted-foreground">{{ event.roomName }}</p>
          </div>
          <span :class="['px-2 py-0.5 rounded-full text-xs font-medium border', eventColor(event)]">
            {{ event.status ? t(`scheduling.session.status.${event.status}`) : event.type }}
          </span>
        </div>
      </div>
    </div>

    <!-- Week View -->
    <div v-else-if="calendarView === 'week'" class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
      <div class="grid grid-cols-8 border-b border-border bg-accent/50">
        <div class="px-2 py-2 text-xs text-muted-foreground" />
        <div
          v-for="day in weekDays"
          :key="day.toISOString()"
          class="px-2 py-2 text-center text-xs font-medium text-muted-foreground"
        >
          <p>{{ day.toLocaleDateString('tr-TR', { weekday: 'short' }) }}</p>
          <p :class="['text-base font-semibold mt-0.5', day.toDateString() === new Date().toDateString() ? 'text-primary' : 'text-foreground']">
            {{ day.getDate() }}
          </p>
        </div>
      </div>
      <div class="overflow-y-auto max-h-[600px]">
        <div
          v-for="hour in hours"
          :key="hour"
          class="grid grid-cols-8 border-b border-border/50 min-h-[60px]"
        >
          <div class="px-2 py-1 text-xs text-muted-foreground border-r border-border">
            {{ hour.toString().padStart(2, '0') }}:00
          </div>
          <div
            v-for="day in weekDays"
            :key="day.toISOString()"
            class="border-r border-border/50 p-0.5"
          >
            <template v-for="event in eventsOnDay(day)" :key="event.id">
              <div
                v-if="parseInt(event.start.slice(11, 13)) === hour"
                :class="['rounded p-1 mb-0.5 cursor-pointer text-xs border truncate', eventColor(event)]"
                @click="event.type === 'session' && router.push({ name: 'session-detail', params: { id: event.id } })"
              >
                {{ formatTime(event.start) }} {{ event.title }}
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Month View -->
    <div v-else-if="calendarView === 'month'" class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
      <div class="grid grid-cols-7 border-b border-border bg-accent/50">
        <div v-for="d in ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']" :key="d" class="px-2 py-2 text-center text-xs font-medium text-muted-foreground">
          {{ d }}
        </div>
      </div>
      <div class="text-center py-16 text-muted-foreground text-sm">
        {{ t('scheduling.calendar.monthViewHint') }}
      </div>
    </div>

    <!-- Day View -->
    <div v-else class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
      <div class="overflow-y-auto max-h-[600px]">
        <div
          v-for="hour in hours"
          :key="hour"
          class="flex gap-4 border-b border-border/50 min-h-[64px] px-4"
        >
          <div class="w-16 text-xs text-muted-foreground py-2">{{ hour.toString().padStart(2, '0') }}:00</div>
          <div class="flex-1 py-1 space-y-1">
            <template v-for="event in eventsOnDay(currentDate)" :key="event.id">
              <div
                v-if="parseInt(event.start.slice(11, 13)) === hour"
                :class="['rounded-lg p-2 cursor-pointer border text-xs', eventColor(event)]"
                @click="event.type === 'session' && router.push({ name: 'session-detail', params: { id: event.id } })"
              >
                <p class="font-medium">{{ event.title }}</p>
                <p class="text-xs opacity-75">{{ formatTime(event.start) }} – {{ formatTime(event.end) }}</p>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
