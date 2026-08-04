<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('leave.calendar.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('leave.calendar.subtitle') }}</p>
      </div>
      <div class="d-flex gap-2 align-items-center">
        <button class="btn btn-sm btn-light" @click="prevMonth">
          <i class="ki-outline ki-arrow-left fs-4"></i>
        </button>
        <span class="fw-bold fs-5 px-4">{{ monthLabel }}</span>
        <button class="btn btn-sm btn-light" @click="nextMonth">
          <i class="ki-outline ki-arrow-right fs-4"></i>
        </button>
      </div>
    </div>

    <div v-if="leaveStore.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <div v-else class="card">
      <div class="card-body">
        <!-- Calendar Grid -->
        <div class="row g-0 text-center mb-2">
          <div v-for="day in weekDays" :key="day" class="col fw-bold text-muted fs-7 py-2">{{ day }}</div>
        </div>
        <div class="row g-0">
          <div
            v-for="(cell, idx) in calendarCells"
            :key="idx"
            class="col border-top"
            style="min-height:100px;"
            :class="{ 'bg-light-secondary': !cell.day }"
          >
            <div class="p-2">
              <div v-if="cell.day" class="fw-semibold text-gray-700 mb-1">{{ cell.day }}</div>
              <div
                v-for="item in cell.items"
                :key="item.id"
                class="badge badge-light-primary fw-normal text-truncate w-100 mb-1 text-start"
                style="max-width:100%; cursor:pointer;"
                @click="router.push(`/leave/requests/${item.id}`)"
              >
                {{ item.educatorFullName ?? '—' }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Legend -->
    <div class="d-flex gap-4 mt-4">
      <div class="d-flex align-items-center gap-2">
        <span class="badge badge-light-warning">A</span>
        <span class="text-muted fs-7">{{ $t('leave.status.pending') }}</span>
      </div>
      <div class="d-flex align-items-center gap-2">
        <span class="badge badge-light-success">A</span>
        <span class="text-muted fs-7">{{ $t('leave.status.approved') }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const leaveStore = useLeaveStore()
const authStore = useAuthStore()

const currentDate = ref(new Date())
const weekDays = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

const monthLabel = computed(() => {
  return currentDate.value.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' })
})

const calendarCells = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  const startDow = (firstDay.getDay() + 6) % 7 // Monday=0
  const cells: { day: number | null; items: typeof leaveStore.calendar }[] = []

  for (let i = 0; i < startDow; i++) cells.push({ day: null, items: [] })
  for (let d = 1; d <= lastDay.getDate(); d++) {
    const date = new Date(year, month, d)
    const items = leaveStore.calendar.filter(item => {
      const start = new Date(item.startsAt)
      const end = new Date(item.endsAt)
      return date >= new Date(start.getFullYear(), start.getMonth(), start.getDate()) &&
             date <= new Date(end.getFullYear(), end.getMonth(), end.getDate())
    })
    cells.push({ day: d, items })
  }
  const rem = cells.length % 7
  if (rem > 0) for (let i = rem; i < 7; i++) cells.push({ day: null, items: [] })
  return cells
})

async function fetchCalendar() {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()
  const from = new Date(year, month, 1).toISOString()
  const to = new Date(year, month + 1, 0, 23, 59, 59).toISOString()
  await leaveStore.fetchCalendar({
    corporationId: authStore.user?.corporationId,
    from, to,
  })
}

function prevMonth() {
  const d = new Date(currentDate.value)
  d.setMonth(d.getMonth() - 1)
  currentDate.value = d
  fetchCalendar()
}

function nextMonth() {
  const d = new Date(currentDate.value)
  d.setMonth(d.getMonth() + 1)
  currentDate.value = d
  fetchCalendar()
}

onMounted(fetchCalendar)
</script>
