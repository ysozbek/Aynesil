<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCalendarStore } from '@/stores/calendar.store'
import { campusService } from '@/services/campus.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import EducatorPicker from '@/components/pickers/EducatorPicker.vue'
import type { CalendarEventDto } from '@/types/scheduling.types'
import type { CampusListItemDto } from '@/types/campus.types'

const { t } = useI18n()
const router = useRouter()
const calendarStore = useCalendarStore()

type ViewMode = 'school' | 'campus' | 'educator' | 'student'
const viewMode = ref<ViewMode>('school')
const resourceId = ref('')
const campuses = ref<CampusListItemDto[]>([])
const currentDate = ref(new Date())
const calendarView = ref<'week' | 'month' | 'day' | 'agenda'>('week')

// ── Date range ────────────────────────────────────────────────────────────────
// All calculations use the browser's local time. The API receives full ISO
// timestamps with an exclusive end (backend filters `starts_at < to`), so the
// last day of the range is included and the timezone offset is preserved.

function startOfDay(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate())
}

function addDays(d: Date, n: number): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n)
}

/** Monday-based week start (Turkish convention). */
function startOfWeek(d: Date): Date {
  const day = (d.getDay() + 6) % 7 // Mon=0 … Sun=6
  return addDays(startOfDay(d), -day)
}

/** Local calendar date key, e.g. 2026-11-09 (not UTC). */
function dayKey(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

const rangeStart = computed<Date>(() => {
  const d = currentDate.value
  if (calendarView.value === 'day') return startOfDay(d)
  if (calendarView.value === 'week') return startOfWeek(d)
  // month + agenda: month grid starts on the Monday on/before the 1st
  const first = new Date(d.getFullYear(), d.getMonth(), 1)
  return calendarView.value === 'month' ? startOfWeek(first) : first
})

/** Exclusive end of the range. */
const rangeEnd = computed<Date>(() => {
  const d = currentDate.value
  if (calendarView.value === 'day') return addDays(startOfDay(d), 1)
  if (calendarView.value === 'week') return addDays(startOfWeek(d), 7)
  const nextMonth = new Date(d.getFullYear(), d.getMonth() + 1, 1)
  if (calendarView.value === 'agenda') return nextMonth
  // month grid ends on the Sunday on/after the last day of the month
  const lastDay = addDays(nextMonth, -1)
  return addDays(startOfWeek(lastDay), 7)
})

const from = computed(() => rangeStart.value.toISOString())
const to = computed(() => rangeEnd.value.toISOString())

function formatDateHeader(): string {
  const d = currentDate.value
  if (calendarView.value === 'day') {
    return d.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })
  }
  if (calendarView.value === 'week') {
    const last = addDays(rangeEnd.value, -1)
    const startText = rangeStart.value.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' })
    const endText = last.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' })
    return `${startText} – ${endText}`
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
      await calendarStore.fetchSchoolCalendar(query)
      break
    case 'campus':
      if (resourceId.value)
        await calendarStore.fetchCampusCalendar(resourceId.value, query)
      break
    case 'educator':
      if (resourceId.value)
        await calendarStore.fetchEducatorCalendar(resourceId.value, query)
      break
    case 'student':
      if (resourceId.value)
        await calendarStore.fetchStudentCalendar(resourceId.value, query)
      break
  }
}

watch([viewMode, resourceId, from, to], () => loadCalendar())
onMounted(async () => {
  await Promise.all([
    loadCalendar(),
    campusService.list({ page: 1, pageSize: 200 }).then((res) => {
      if (res.success && res.data) campuses.value = res.data.items
    }),
  ])
})

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

// Week view: 7 days starting Monday
const weekDays = computed(() => Array.from({ length: 7 }, (_, i) => addDays(rangeStart.value, i)))

// Month view: full weeks covering the month (Mon–Sun rows)
const monthDays = computed(() => {
  const days: Date[] = []
  for (let d = rangeStart.value; d < rangeEnd.value; d = addDays(d, 1)) days.push(d)
  return days
})
const MONTH_CELL_LIMIT = 3
const weekDaysOfGrid = computed(() => monthDays.value.slice(0, 7))

// Hours for day/week view
const hours = Array.from({ length: 14 }, (_, i) => i + 7) // 7am to 8pm

const eventsByDay = computed(() => {
  const map = new Map<string, CalendarEventDto[]>()
  for (const e of calendarStore.events) {
    const key = dayKey(new Date(e.start))
    const list = map.get(key)
    if (list) list.push(e)
    else map.set(key, [e])
  }
  for (const list of map.values()) list.sort((a, b) => a.start.localeCompare(b.start))
  return map
})

function eventsOnDay(day: Date): CalendarEventDto[] {
  return eventsByDay.value.get(dayKey(day)) ?? []
}

/** Local hour of the event start (API timestamps are UTC). */
function eventHour(event: CalendarEventDto): number {
  return new Date(event.start).getHours()
}

function isToday(day: Date): boolean {
  return dayKey(day) === dayKey(new Date())
}

function isCurrentMonth(day: Date): boolean {
  return day.getMonth() === currentDate.value.getMonth()
}

function openDay(day: Date) {
  currentDate.value = day
  calendarView.value = 'day'
}

function openEvent(event: CalendarEventDto) {
  if (event.type === 'session') router.push({ name: 'session-detail', params: { id: event.id } })
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

      <select
        v-if="viewMode === 'campus'"
        v-model="resourceId"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option value="">{{ t('common.select') }}</option>
        <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <div v-else-if="viewMode === 'educator'" class="w-64">
        <EducatorPicker v-model="resourceId" size="sm" />
      </div>
      <div v-else-if="viewMode === 'student'" class="w-64">
        <StudentPicker v-model="resourceId" size="sm" />
      </div>

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
    <div v-else-if="calendarView === 'agenda'" class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
      <div v-if="calendarStore.events.length === 0" class="py-16 text-center text-muted-foreground">
        {{ t('scheduling.calendar.noEvents') }}
      </div>
      <div v-else class="divide-y divide-border">
        <div
          v-for="event in calendarStore.events"
          :key="event.id"
          class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
          @click="openEvent(event)"
        >
          <div class="text-center min-w-[72px]">
            <p class="text-xs text-muted-foreground">{{ new Date(event.start).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', weekday: 'short' }) }}</p>
            <p class="text-sm font-semibold">{{ formatTime(event.start) }}</p>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-foreground truncate">{{ event.title }}</p>
            <p v-if="event.roomName" class="text-xs text-muted-foreground">{{ event.roomName }}</p>
          </div>
          <span :class="['px-2 py-0.5 rounded-full text-xs font-medium border', eventColor(event)]">
            {{ event.status ? t(`statusBadge.session.${event.status}`) : event.type }}
          </span>
        </div>
      </div>
    </div>

    <!-- Week View -->
    <div v-else-if="calendarView === 'week'" class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
      <div class="grid grid-cols-8 border-b border-border bg-accent/50">
        <div class="px-2 py-2 text-xs text-muted-foreground" />
        <div
          v-for="day in weekDays"
          :key="day.toISOString()"
          class="px-2 py-2 text-center text-xs font-medium text-muted-foreground"
        >
          <p>{{ day.toLocaleDateString('tr-TR', { weekday: 'short' }) }}</p>
          <p :class="['text-base font-semibold mt-0.5', isToday(day) ? 'text-primary' : 'text-foreground']">
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
                v-if="eventHour(event) === hour"
                :class="['rounded p-1 mb-0.5 cursor-pointer text-xs border truncate', eventColor(event)]"
                @click="openEvent(event)"
              >
                {{ formatTime(event.start) }} {{ event.title }}
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Month View -->
    <div v-else-if="calendarView === 'month'" class="rounded-xl border border-border bg-card shadow-sm overflow-x-auto">
      <div class="min-w-[640px]">
        <div class="grid grid-cols-7 border-b border-border bg-accent/50">
          <div
            v-for="d in weekDaysOfGrid"
            :key="d.getDay()"
            class="px-2 py-2 text-center text-xs font-medium text-muted-foreground"
          >
            {{ d.toLocaleDateString('tr-TR', { weekday: 'short' }) }}
          </div>
        </div>
        <div class="grid grid-cols-7">
          <div
            v-for="day in monthDays"
            :key="dayKey(day)"
            :class="[
              'min-h-[110px] border-b border-r border-border/50 p-1 flex flex-col gap-0.5',
              isCurrentMonth(day) ? '' : 'bg-accent/30',
            ]"
          >
            <button
              type="button"
              @click="openDay(day)"
              :class="[
                'self-end w-7 h-7 rounded-full text-xs font-medium hover:bg-accent',
                isToday(day) ? 'bg-primary text-primary-foreground hover:bg-primary' : isCurrentMonth(day) ? 'text-foreground' : 'text-muted-foreground',
              ]"
            >
              {{ day.getDate() }}
            </button>
            <div
              v-for="event in eventsOnDay(day).slice(0, MONTH_CELL_LIMIT)"
              :key="event.id"
              :class="['rounded px-1 py-0.5 text-[11px] leading-tight border truncate', event.type === 'session' ? 'cursor-pointer' : '', eventColor(event)]"
              :title="`${formatTime(event.start)} ${event.title}`"
              @click="openEvent(event)"
            >
              {{ formatTime(event.start) }} {{ event.title }}
            </div>
            <button
              v-if="eventsOnDay(day).length > MONTH_CELL_LIMIT"
              type="button"
              @click="openDay(day)"
              class="text-left px-1 text-[11px] text-primary hover:underline"
            >
              {{ t('scheduling.calendar.moreEvents', { n: eventsOnDay(day).length - MONTH_CELL_LIMIT }) }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Day View -->
    <div v-else class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
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
                v-if="eventHour(event) === hour"
                :class="['rounded-lg p-2 cursor-pointer border text-xs', eventColor(event)]"
                @click="openEvent(event)"
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
