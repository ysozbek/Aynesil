<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useStudentStore } from '@/stores/student.store'
import { useGoalStore } from '@/stores/goal.store'
import { useProgramStore } from '@/stores/program.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useStudentStore()
const goalStore = useGoalStore()
const programStore = useProgramStore()

const id = computed(() => route.params.id as string)
const summary = computed(() => store.summary)

const activeGoals = computed(() =>
  goalStore.studentGoalList.items.filter(g => g.status === 'active'),
)

onMounted(async () => {
  await Promise.all([
    store.fetchSummary(id.value),
    goalStore.fetchStudentGoals({ studentId: id.value, status: 'active', pageSize: 10 }),
    programStore.fetchStudentPrograms({ studentId: id.value, pageSize: 10 }),
  ])
})

onUnmounted(() => {
  store.clearCurrent()
  goalStore.clearCurrent()
  programStore.clearCurrent()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

function goalStatusClass(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-emerald-100 text-emerald-700',
    achieved: 'bg-blue-100 text-blue-700',
    discontinued: 'bg-red-100 text-red-700',
    on_hold: 'bg-amber-100 text-amber-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

function programStatusClass(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-emerald-100 text-emerald-700',
    completed: 'bg-blue-100 text-blue-700',
    cancelled: 'bg-red-100 text-red-700',
    pending: 'bg-amber-100 text-amber-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}
</script>

<template>
  <div>
    <!-- Back button -->
    <button
      @click="router.push({ name: 'student-detail', params: { id } })"
      class="text-sm text-muted-foreground hover:text-foreground mb-4 flex items-center gap-1"
    >
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
      </svg>
      {{ t('student.backToList') }}
    </button>

    <!-- Loading -->
    <div v-if="store.loading && !summary" class="space-y-4">
      <div class="h-32 rounded-xl bg-accent animate-pulse" />
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="h-24 rounded-xl bg-accent animate-pulse" />
      </div>
    </div>

    <template v-else-if="summary">
      <!-- Student Summary Card -->
      <div class="rounded-xl border border-border bg-[--color-card] p-6 shadow-sm mb-6">
        <div class="flex items-center gap-5">
          <!-- Photo placeholder -->
          <div class="w-16 h-16 rounded-full bg-accent flex items-center justify-center shrink-0">
            <svg class="w-8 h-8 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
          </div>
          <div>
            <h1 class="text-xl font-bold text-foreground">{{ summary.fullName }}</h1>
            <p class="text-sm text-muted-foreground mt-0.5">
              {{ t('student.studentNo') }}: {{ summary.studentNo ?? '—' }}
            </p>
            <div class="flex items-center gap-2 mt-2 flex-wrap">
              <span
                v-if="summary.statusLabel"
                class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700"
              >{{ summary.statusLabel }}</span>
              <span v-if="summary.primaryCampusName" class="text-xs text-muted-foreground">
                {{ summary.primaryCampusName }}
              </span>
              <span v-if="summary.birthDate" class="text-xs text-muted-foreground">
                {{ formatDate(summary.birthDate) }}
              </span>
            </div>
          </div>
          <div class="ml-auto">
            <button
              @click="router.push({ name: 'student-detail', params: { id } })"
              class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
            >
              {{ t('student.tab.overview') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Stats Row -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-foreground">{{ programStore.studentProgramList.totalCount }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('student.tab.programs') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-foreground">{{ activeGoals.length }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('common.activeGoals') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-foreground">—</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('common.upcomingSessions') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-foreground">—</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('common.recentAssessments') }}</p>
        </div>
      </div>

      <!-- Main Grid -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

        <!-- Active Programs -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('student.tab.programs') }}</h3>
            <span v-if="programStore.loading" class="w-4 h-4 rounded-full border-2 border-primary border-t-transparent animate-spin" />
          </div>
          <div class="p-4">
            <div v-if="programStore.studentProgramList.items.length === 0" class="text-center py-8 text-muted-foreground text-sm">
              {{ t('common.noData') }}
            </div>
            <ul v-else class="space-y-2">
              <li
                v-for="sp in programStore.studentProgramList.items"
                :key="sp.id"
                class="flex items-start justify-between gap-3 py-2 border-b border-border last:border-0"
              >
                <div>
                  <p class="text-sm font-medium text-foreground">{{ sp.programName }}</p>
                  <p class="text-xs text-muted-foreground">{{ sp.programCode }} · {{ sp.programTypeLabel ?? '—' }}</p>
                  <p v-if="sp.startDate" class="text-xs text-muted-foreground">{{ formatDate(sp.startDate) }}</p>
                </div>
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', programStatusClass(sp.status)]">
                  {{ sp.status }}
                </span>
              </li>
            </ul>
          </div>
        </div>

        <!-- Current Goals -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('common.activeGoals') }}</h3>
            <span v-if="goalStore.loading" class="w-4 h-4 rounded-full border-2 border-primary border-t-transparent animate-spin" />
          </div>
          <div class="p-4">
            <div v-if="activeGoals.length === 0" class="text-center py-8 text-muted-foreground text-sm">
              {{ t('common.noData') }}
            </div>
            <ul v-else class="space-y-2">
              <li
                v-for="goal in activeGoals"
                :key="goal.id"
                class="py-2 border-b border-border last:border-0"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-foreground truncate">{{ goal.statement }}</p>
                    <p class="text-xs text-muted-foreground mt-0.5">
                      {{ goal.developmentAreaLabel ?? '—' }}
                      <span v-if="goal.targetDate"> · {{ formatDate(goal.targetDate) }}</span>
                    </p>
                    <div v-if="goal.latestPercentComplete !== null" class="mt-1">
                      <div class="flex items-center gap-2">
                        <div class="flex-1 h-1.5 rounded-full bg-accent overflow-hidden">
                          <div
                            class="h-full rounded-full bg-primary transition-all"
                            :style="{ width: `${goal.latestPercentComplete}%` }"
                          />
                        </div>
                        <span class="text-xs text-muted-foreground">{{ goal.latestPercentComplete }}%</span>
                      </div>
                    </div>
                  </div>
                  <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', goalStatusClass(goal.status)]">
                    {{ goal.status }}
                  </span>
                </div>
              </li>
            </ul>
          </div>
        </div>

        <!-- Assigned Educators (placeholder) -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('common.assignedEducators') }}</h3>
          </div>
          <div class="p-4 text-center py-8 text-muted-foreground text-sm">
            {{ t('common.comingSoon') }}
          </div>
        </div>

        <!-- Attendance Summary (placeholder) -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm">
          <div class="px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('common.attendanceSummary') }}</h3>
          </div>
          <div class="p-4 text-center py-8 text-muted-foreground text-sm">
            {{ t('common.comingSoon') }}
          </div>
        </div>

        <!-- Recent Assessments (placeholder) -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm md:col-span-2">
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('common.recentAssessments') }}</h3>
            <button class="text-xs text-primary hover:underline">
              {{ t('common.viewAll') }}
            </button>
          </div>
          <div class="p-4 text-center py-8 text-muted-foreground text-sm">
            {{ t('common.comingSoon') }}
          </div>
        </div>

      </div>
    </template>

    <!-- Not found -->
    <div v-else-if="!store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button
        @click="router.push({ name: 'students' })"
        class="mt-4 text-sm text-primary hover:underline"
      >
        ← {{ t('student.backToList') }}
      </button>
    </div>
  </div>
</template>
