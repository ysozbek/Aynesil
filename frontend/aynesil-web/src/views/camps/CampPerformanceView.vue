<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCampStore } from '@/stores/camp.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const campStore = useCampStore()

const rows = computed(() => campStore.performance)
const totals = computed(() => {
  const r = rows.value
  const enrolled = r.reduce((a, x) => a + x.totalEnrolled, 0)
  const completed = r.reduce((a, x) => a + x.totalCompleted, 0)
  // Ortalamalar kamp başına eşit ağırlıklıdır; verisi olmayan (0) kamp ortalamayı bozmasın diye yalnızca oranı olanlar sayılır.
  const withAttendance = r.filter((x) => x.overallAttendanceRatePct > 0)
  const avg = (xs: number[]) => (xs.length ? Math.round((xs.reduce((a, v) => a + v, 0) / xs.length) * 10) / 10 : 0)
  return {
    enrolled, completed,
    completion: enrolled > 0 ? Math.round((completed / enrolled) * 1000) / 10 : 0,
    attendance: avg(withAttendance.map((x) => x.overallAttendanceRatePct)),
  }
})

const bar = (pct: number) => `${Math.min(100, Math.max(0, pct))}%`

onMounted(() => campStore.fetchPerformance())
</script>

<template>
  <div>
    <PageHeader :title="t('camp.performance.title')" :description="t('camp.performance.subtitle')">
      <button type="button" class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="router.push({ name: 'camp-dashboard' })">
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <p v-if="campStore.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ campStore.error }}</p>

    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold text-primary">{{ totals.enrolled }}</div>
        <div class="text-xs text-muted-foreground">{{ t('camp.performance.totalEnrolled') }}</div>
      </div>
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold text-emerald-700">{{ totals.completed }}</div>
        <div class="text-xs text-muted-foreground">{{ t('camp.performance.totalCompleted') }}</div>
      </div>
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold">{{ totals.completion }}%</div>
        <div class="text-xs text-muted-foreground">{{ t('camp.performance.avgCompletion') }}</div>
      </div>
      <div class="rounded-xl border border-border bg-card p-4 text-center">
        <div class="text-2xl font-bold">{{ totals.attendance }}%</div>
        <div class="text-xs text-muted-foreground">{{ t('camp.performance.avgAttendance') }}</div>
      </div>
    </div>

    <div class="rounded-xl border border-border bg-card overflow-x-auto">
      <div v-if="campStore.loading && rows.length === 0" class="py-12 text-center text-muted-foreground">{{ t('common.loading') }}</div>
      <p v-else-if="rows.length === 0" class="py-12 text-center text-sm text-muted-foreground">{{ t('camp.performance.noData') }}</p>
      <table v-else class="w-full text-sm">
        <thead>
          <tr class="text-left text-xs text-muted-foreground border-b border-border">
            <th class="py-3 px-4">{{ t('camp.fields.name') }}</th>
            <th class="px-4 text-center">{{ t('camp.performance.periods') }}</th>
            <th class="px-4 text-center">{{ t('camp.performance.enrolled') }}</th>
            <th class="px-4 text-center">{{ t('camp.performance.completed') }}</th>
            <th class="px-4 text-center">{{ t('camp.performance.withdrawn') }}</th>
            <th class="px-4 w-48">{{ t('camp.performance.completionRate') }}</th>
            <th class="px-4 w-48">{{ t('camp.performance.attendanceRate') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in rows" :key="r.campId" class="border-b border-border/60">
            <td class="py-3 px-4">
              <button type="button" class="font-medium text-primary hover:underline text-left" @click="router.push({ name: 'camp-detail', params: { id: r.campId } })">{{ r.campName }}</button>
              <div class="text-xs text-muted-foreground">{{ r.campCode }}</div>
            </td>
            <td class="px-4 text-center">{{ r.totalPeriods }}</td>
            <td class="px-4 text-center">{{ r.totalEnrolled }}</td>
            <td class="px-4 text-center text-emerald-700">{{ r.totalCompleted }}</td>
            <td class="px-4 text-center text-red-600">{{ r.totalWithdrawn }}</td>
            <td class="px-4">
              <div class="flex items-center gap-2">
                <div class="h-2 flex-1 rounded-full bg-gray-100"><div class="h-2 rounded-full bg-emerald-500" :style="{ width: bar(r.completionRatePct) }" /></div>
                <span class="w-12 text-right text-xs font-medium">{{ r.completionRatePct }}%</span>
              </div>
            </td>
            <td class="px-4">
              <div class="flex items-center gap-2">
                <div class="h-2 flex-1 rounded-full bg-gray-100"><div class="h-2 rounded-full bg-blue-500" :style="{ width: bar(r.overallAttendanceRatePct) }" /></div>
                <span class="w-12 text-right text-xs font-medium">{{ r.overallAttendanceRatePct }}%</span>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
