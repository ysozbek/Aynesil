<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAttendanceStore } from '@/stores/attendance.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import Pagination from '@/components/shared/Pagination.vue'

const { t } = useI18n()
const router = useRouter()
const store = useAttendanceStore()

const studentIdInput = ref('')
const loadedStudentId = ref('')

async function loadStudentData() {
  if (!studentIdInput.value.trim()) return
  loadedStudentId.value = studentIdInput.value.trim()
  await Promise.all([
    store.fetchAttendanceSummary(loadedStudentId.value),
    store.fetchStudentAttendance(loadedStudentId.value, { page: 1, pageSize: 20 }),
  ])
}

function formatDate(val: string): string {
  return new Date(val).toLocaleDateString('tr-TR')
}

function formatPct(val: number): string {
  return `${Math.round(val * 100) / 100}%`
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    present: 'bg-green-100 text-green-700',
    absent: 'bg-red-100 text-red-700',
    late: 'bg-amber-100 text-amber-700',
    excused: 'bg-blue-100 text-blue-700',
    left_early: 'bg-orange-100 text-orange-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}
</script>

<template>
  <div>
    <PageHeader :title="t('scheduling.attendance.dashboardTitle')" :description="t('scheduling.attendance.dashboardDescription')" />

    <!-- Student filter -->
    <div class="mb-6 flex items-center gap-3">
      <input
        v-model="studentIdInput"
        type="text"
        :placeholder="t('scheduling.attendance.studentIdPlaceholder')"
        @keydown.enter="loadStudentData"
        class="flex-1 max-w-sm px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <button
        @click="loadStudentData"
        :disabled="store.loading || !studentIdInput.trim()"
        class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-60"
      >
        <svg v-if="store.loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        {{ t('common.search') }}
      </button>
    </div>

    <div v-if="!loadedStudentId" class="text-center py-16 text-muted-foreground text-sm">
      {{ t('scheduling.attendance.enterStudentId') }}
    </div>

    <div v-else-if="store.loading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-20 rounded-xl bg-accent animate-pulse" />
    </div>

    <template v-else-if="store.summary">
      <!-- Summary cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-3xl font-bold text-green-600">{{ store.summary.present }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('scheduling.attendance.status.present') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-3xl font-bold text-red-600">{{ store.summary.absent }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('scheduling.attendance.status.absent') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-3xl font-bold text-amber-600">{{ store.summary.late }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('scheduling.attendance.status.late') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-3xl font-bold text-blue-600">{{ formatPct(store.summary.attendanceRate) }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('scheduling.attendance.rate') }}</p>
        </div>
      </div>

      <!-- Attendance History -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('scheduling.attendance.history') }}</h3>
        </div>
        <div v-if="store.studentAttendance.items.length === 0" class="py-8 text-center text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <table v-else class="w-full text-sm">
          <thead>
            <tr class="border-b border-border bg-accent/50">
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.session.titleField') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.attendance.status.label') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.attendance.reason') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.attendance.recordedAt') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="record in store.studentAttendance.items"
              :key="record.id"
              class="border-b border-border last:border-0 hover:bg-accent/30 cursor-pointer"
              @click="router.push({ name: 'session-detail', params: { id: record.sessionId } })"
            >
              <td class="px-4 py-3">{{ record.studentFullName }}</td>
              <td class="px-4 py-3">
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(record.status)]">
                  {{ t(`scheduling.attendance.status.${record.status}`) }}
                </span>
              </td>
              <td class="px-4 py-3 text-muted-foreground">{{ record.reasonLabel ?? '—' }}</td>
              <td class="px-4 py-3 text-muted-foreground">{{ formatDate(record.recordedAt) }}</td>
            </tr>
          </tbody>
        </table>
        <div class="p-4">
          <Pagination
            :page="store.studentAttendance.page"
            :page-size="store.studentAttendance.pageSize"
            :total-count="store.studentAttendance.totalCount"
            :total-pages="store.studentAttendance.totalPages"
            :has-previous-page="store.studentAttendance.hasPreviousPage"
            :has-next-page="store.studentAttendance.hasNextPage"
            @update:page="(p) => store.fetchStudentAttendance(loadedStudentId, { page: p, pageSize: 20 })"
            @update:page-size="(s) => store.fetchStudentAttendance(loadedStudentId, { page: 1, pageSize: s })"
          />
        </div>
      </div>
    </template>
  </div>
</template>
