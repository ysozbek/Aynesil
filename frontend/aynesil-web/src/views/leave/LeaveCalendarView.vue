<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const leaveStore = useLeaveStore()
const auth = useAuthStore()

const currentDate = ref(new Date())
const weekDays = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

const monthLabel = computed(() =>
  currentDate.value.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' })
)

const calendarCells = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  const startDow = (firstDay.getDay() + 6) % 7
  const cells: { day: number | null; items: typeof leaveStore.calendar }[] = []

  for (let i = 0; i < startDow; i++) cells.push({ day: null, items: [] })
  for (let d = 1; d <= lastDay.getDate(); d++) {
    const date = new Date(year, month, d)
    const items = leaveStore.calendar.filter((item) => {
      const start = new Date(item.startsAt)
      const end = new Date(item.endsAt)
      return (
        date >= new Date(start.getFullYear(), start.getMonth(), start.getDate()) &&
        date <= new Date(end.getFullYear(), end.getMonth(), end.getDate())
      )
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
    corporationId: auth.user?.corporationId,
    from,
    to,
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

function statusChip(status: string) {
  if (status === 'Approved') return 'bg-green-100 text-green-700'
  if (status === 'Pending') return 'bg-amber-100 text-amber-700'
  return 'bg-primary/10 text-primary'
}

onMounted(fetchCalendar)
</script>

<template>
  <div>
    <PageHeader :title="t('leave.calendar.title')" :description="t('leave.calendar.subtitle')">
      <div class="flex items-center gap-2">
        <button @click="prevMonth" class="p-2 rounded-lg border border-border hover:bg-accent">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <span class="text-sm font-semibold text-foreground min-w-[140px] text-center capitalize">{{ monthLabel }}</span>
        <button @click="nextMonth" class="p-2 rounded-lg border border-border hover:bg-accent">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
    </PageHeader>

    <div v-if="leaveStore.loading" class="py-16 text-center text-sm text-muted-foreground">
      {{ t('common.loading') }}
    </div>

    <div v-else class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
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
          v-for="(cell, idx) in calendarCells"
          :key="idx"
          class="min-h-[96px] border-b border-r border-border p-1.5 last:border-r-0"
          :class="!cell.day ? 'bg-accent/20' : ''"
        >
          <p v-if="cell.day" class="text-xs font-semibold text-foreground mb-1 px-1">{{ cell.day }}</p>
          <button
            v-for="item in cell.items"
            :key="item.id"
            type="button"
            :class="[
              'w-full text-left text-[10px] leading-tight px-1.5 py-1 rounded mb-0.5 truncate',
              statusChip(item.status),
            ]"
            @click="router.push({ name: 'leave-detail', params: { id: item.id } })"
          >
            {{ item.educatorFullName ?? '—' }}
          </button>
        </div>
      </div>
    </div>

    <div class="flex gap-4 mt-4 text-xs text-muted-foreground">
      <span class="flex items-center gap-1.5">
        <span class="w-3 h-3 rounded bg-amber-100 border border-amber-200" />
        {{ t('leave.status.pending') }}
      </span>
      <span class="flex items-center gap-1.5">
        <span class="w-3 h-3 rounded bg-green-100 border border-green-200" />
        {{ t('leave.status.approved') }}
      </span>
    </div>
  </div>
</template>
