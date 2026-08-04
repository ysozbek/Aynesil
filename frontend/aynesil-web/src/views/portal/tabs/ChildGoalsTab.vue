<template>
  <div class="space-y-4">
    <div v-if="store.loading" class="flex justify-center py-8">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <template v-else>
      <!-- Summary stats -->
      <div v-if="store.goalProgress.length" class="grid grid-cols-3 gap-4">
        <div class="stat bg-base-100 shadow rounded-lg p-4 text-center">
          <p class="stat-title text-xs">{{ $t('portal.goals.total') }}</p>
          <p class="stat-value text-2xl">{{ store.goalProgress.length }}</p>
        </div>
        <div class="stat bg-base-100 shadow rounded-lg p-4 text-center">
          <p class="stat-title text-xs">{{ $t('portal.goals.active') }}</p>
          <p class="stat-value text-2xl text-primary">{{ activeGoals }}</p>
        </div>
        <div class="stat bg-base-100 shadow rounded-lg p-4 text-center">
          <p class="stat-title text-xs">{{ $t('portal.goals.achieved') }}</p>
          <p class="stat-value text-2xl text-success">{{ achievedGoals }}</p>
        </div>
      </div>

      <!-- Goals list -->
      <div v-if="store.goalProgress.length" class="space-y-3">
        <div
          v-for="goal in store.goalProgress"
          :key="goal.goalId"
          class="card bg-base-100 shadow-sm"
        >
          <div class="card-body p-4">
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1">
                <p class="font-medium">{{ goal.statement }}</p>
                <div class="flex gap-2 mt-1 flex-wrap text-xs text-gray-500">
                  <span v-if="goal.horizon">{{ goal.horizon }}</span>
                  <span v-if="goal.targetDate">• {{ $t('portal.goals.targetDate') }}: {{ formatDate(goal.targetDate) }}</span>
                </div>
              </div>
              <div class="flex flex-col items-end gap-1">
                <span :class="['badge badge-sm', goalStatusClass(goal.status)]">{{ goal.status }}</span>
                <span v-if="goal.trend" class="text-xs" :class="trendClass(goal.trend)">
                  {{ trendIcon(goal.trend) }} {{ goal.trend }}
                </span>
              </div>
            </div>

            <!-- Progress bar -->
            <div v-if="goal.percentComplete !== undefined" class="mt-3">
              <div class="flex justify-between text-xs text-gray-500 mb-1">
                <span>{{ $t('portal.goals.progress') }}</span>
                <span>{{ Math.round(goal.percentComplete) }}%</span>
              </div>
              <div class="w-full bg-base-200 rounded-full h-2">
                <div
                  class="h-2 rounded-full transition-all"
                  :class="progressBarClass(goal.percentComplete)"
                  :style="{ width: `${Math.min(goal.percentComplete, 100)}%` }"
                ></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="card bg-base-100 shadow">
        <div class="card-body items-center text-center py-10">
          <p class="text-gray-500">{{ $t('portal.goals.noGoals') }}</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

const activeGoals = computed(() => store.goalProgress.filter(g => g.status === 'active').length)
const achievedGoals = computed(() => store.goalProgress.filter(g => g.status === 'achieved').length)

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}
function goalStatusClass(s: string): string {
  const map: Record<string, string> = { active: 'badge-primary', achieved: 'badge-success', on_hold: 'badge-warning', abandoned: 'badge-error' }
  return map[s] ?? 'badge-ghost'
}
function trendClass(t: string): string {
  return t === 'improving' ? 'text-success' : t === 'declining' ? 'text-error' : 'text-warning'
}
function trendIcon(t: string): string {
  return t === 'improving' ? '↑' : t === 'declining' ? '↓' : '→'
}
function progressBarClass(pct: number): string {
  if (pct >= 75) return 'bg-success'
  if (pct >= 40) return 'bg-warning'
  return 'bg-info'
}

onMounted(() => store.fetchGoalProgress(props.studentId))
</script>
