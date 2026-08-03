<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useGoalStore } from '@/stores/goal.store'
import { useBepStore } from '@/stores/bep.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const goalStore = useGoalStore()
const bepStore = useBepStore()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const studentIdInput = ref('')
const loadedStudentId = ref('')
const loading = ref(false)

async function loadStudentData() {
  if (!studentIdInput.value.trim()) return
  loadedStudentId.value = studentIdInput.value.trim()
  loading.value = true
  try {
    await Promise.all([
      goalStore.fetchStudentSummary(corporationId.value, loadedStudentId.value),
      goalStore.fetchSuccessRates({ corporationId: corporationId.value }),
      bepStore.fetchTrendReport(corporationId.value, loadedStudentId.value),
    ])
  } finally {
    loading.value = false
  }
}

const summary = computed(() => goalStore.summary)
const successRates = computed(() => goalStore.successRates)
const trendReport = computed(() => bepStore.trendReport)

function formatPct(val: number): string {
  return `${Math.round(val)}%`
}

function trendDisplay(trend: string | null): { icon: string; color: string } {
  if (trend === 'improving') return { icon: '↑', color: 'text-emerald-600' }
  if (trend === 'declining') return { icon: '↓', color: 'text-red-600' }
  if (trend === 'stable') return { icon: '→', color: 'text-gray-500' }
  return { icon: '—', color: 'text-muted-foreground' }
}

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}
</script>

<template>
  <div>
    <PageHeader :title="t('goal.dashboard.title')" :description="t('goal.dashboard.description')" />

    <!-- Student filter -->
    <div class="mb-6 flex items-center gap-3">
      <input
        v-model="studentIdInput"
        type="text"
        :placeholder="t('goal.dashboard.enterStudentId')"
        @keydown.enter="loadStudentData"
        class="flex-1 max-w-sm px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <button
        @click="loadStudentData"
        :disabled="loading || !studentIdInput.trim()"
        class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60"
      >
        <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        {{ t('goal.dashboard.load') }}
      </button>
    </div>

    <!-- Empty state -->
    <div v-if="!loadedStudentId" class="text-center py-24 text-muted-foreground">
      <svg class="w-12 h-12 mx-auto mb-3 text-muted-foreground/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
      </svg>
      <p class="text-sm">{{ t('goal.dashboard.enterStudentId') }}</p>
    </div>

    <!-- Loading skeleton -->
    <div v-else-if="loading" class="space-y-6">
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="h-24 rounded-xl bg-accent animate-pulse" />
      </div>
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Dashboard content -->
    <template v-else-if="summary">
      <!-- Summary cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-3xl font-bold text-foreground">{{ summary.totalGoals }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('goal.dashboard.totalGoals') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-3xl font-bold text-blue-600">{{ summary.activeGoals }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('goal.dashboard.activeGoals') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-3xl font-bold text-teal-600">{{ summary.achievedGoals }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('goal.dashboard.achievedGoals') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
          <p class="text-3xl font-bold text-emerald-600">{{ formatPct(summary.achievementRate) }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('goal.dashboard.achievementRate') }}</p>
        </div>
      </div>

      <!-- By Development Area -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden mb-6">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('goal.dashboard.byArea') }}</h3>
        </div>
        <div v-if="summary.byDevelopmentArea.length === 0" class="py-8 text-center text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <table v-else class="w-full text-sm">
          <thead>
            <tr class="border-b border-border bg-accent/50">
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.studentGoal.developmentArea') }}</th>
              <th class="text-center px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.dashboard.totalGoals') }}</th>
              <th class="text-center px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.dashboard.achievedGoals') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.dashboard.achievementRate') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="area in summary.byDevelopmentArea"
              :key="area.developmentAreaId ?? 'none'"
              class="border-b border-border last:border-0 hover:bg-accent/30 transition-colors"
            >
              <td class="px-4 py-3 font-medium text-foreground">{{ area.developmentAreaLabel ?? t('common.other') }}</td>
              <td class="px-4 py-3 text-center font-mono">{{ area.goalCount }}</td>
              <td class="px-4 py-3 text-center font-mono">{{ area.achievedCount }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <div class="flex-1 bg-gray-200 rounded-full h-2 max-w-[120px]">
                    <div
                      class="bg-emerald-500 h-2 rounded-full transition-all"
                      :style="{ width: `${area.achievementRate}%` }"
                    />
                  </div>
                  <span class="text-xs font-mono text-muted-foreground">{{ formatPct(area.achievementRate) }}</span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Success Rates by Category -->
      <div v-if="successRates.length > 0" class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden mb-6">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('goal.dashboard.successRates') }}</h3>
        </div>
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-border bg-accent/50">
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.studentGoal.category') }}</th>
              <th class="text-center px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.dashboard.totalGoals') }}</th>
              <th class="text-center px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.dashboard.achievedGoals') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.dashboard.achievementRate') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="rate in successRates"
              :key="rate.categoryId ?? 'none'"
              class="border-b border-border last:border-0 hover:bg-accent/30 transition-colors"
            >
              <td class="px-4 py-3 font-medium text-foreground">{{ rate.categoryLabel ?? t('common.other') }}</td>
              <td class="px-4 py-3 text-center font-mono">{{ rate.totalGoals }}</td>
              <td class="px-4 py-3 text-center font-mono">{{ rate.achievedGoals }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <div class="flex-1 bg-gray-200 rounded-full h-2 max-w-[120px]">
                    <div
                      class="bg-primary h-2 rounded-full transition-all"
                      :style="{ width: `${rate.successRate}%` }"
                    />
                  </div>
                  <span class="text-xs font-mono text-muted-foreground">{{ formatPct(rate.successRate) }}</span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Trend Report -->
      <div v-if="trendReport.length > 0" class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden mb-6">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('goal.dashboard.trend') }}</h3>
        </div>
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-border bg-accent/50">
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.studentGoal.statement') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.studentGoal.horizon') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.studentGoal.targetDate') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.percentComplete') }}</th>
              <th class="text-center px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.trend') }}</th>
              <th class="text-center px-4 py-3 text-xs font-medium text-muted-foreground uppercase">Ölçüm</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="row in trendReport"
              :key="row.studentGoalId"
              class="border-b border-border last:border-0 hover:bg-accent/30 transition-colors cursor-pointer"
              @click="router.push({ name: 'student-goal-detail', params: { id: row.studentGoalId } })"
            >
              <td class="px-4 py-3 text-foreground max-w-[280px]">
                <span :title="row.statement" class="line-clamp-2">{{ row.statement }}</span>
              </td>
              <td class="px-4 py-3">
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', row.horizon === 'long_term' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700']">
                  {{ row.horizon === 'long_term' ? t('goal.studentGoal.horizon.longTerm') : t('goal.studentGoal.horizon.shortTerm') }}
                </span>
              </td>
              <td class="px-4 py-3 text-muted-foreground">{{ formatDate(row.targetDate) }}</td>
              <td class="px-4 py-3">
                <div v-if="row.latestPercentComplete !== null" class="flex items-center gap-2">
                  <div class="bg-gray-200 rounded-full h-1.5 w-20">
                    <div class="bg-primary h-1.5 rounded-full" :style="{ width: `${row.latestPercentComplete}%` }" />
                  </div>
                  <span class="text-xs font-mono">{{ row.latestPercentComplete }}%</span>
                </div>
                <span v-else class="text-muted-foreground">—</span>
              </td>
              <td class="px-4 py-3 text-center">
                <span :class="['text-base font-bold', trendDisplay(row.currentTrend).color]">
                  {{ trendDisplay(row.currentTrend).icon }}
                </span>
              </td>
              <td class="px-4 py-3 text-center font-mono text-muted-foreground">{{ row.measurementCount }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Link to student goals list -->
      <div class="flex justify-end">
        <button
          @click="router.push({ name: 'student-goal-list', query: { studentId: loadedStudentId } })"
          class="flex items-center gap-2 text-sm text-primary hover:underline"
        >
          {{ t('goal.studentGoal.title') }} →
        </button>
      </div>
    </template>
  </div>
</template>
