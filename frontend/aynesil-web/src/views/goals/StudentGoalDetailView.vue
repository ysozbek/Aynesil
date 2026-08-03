<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useGoalStore } from '@/stores/goal.store'
import { usePermission } from '@/composables/usePermission'
import FormModal from '@/components/shared/FormModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useGoalStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const goal = computed(() => store.currentStudentGoal)
const activeTab = ref<'overview' | 'progress' | 'trend'>('overview')

onMounted(async () => {
  await store.fetchStudentGoal(id.value)
  await store.fetchTrend(id.value)
})

onUnmounted(() => {
  store.clearCurrent()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-green-100 text-green-700',
    achieved: 'bg-teal-100 text-teal-700',
    discontinued: 'bg-red-100 text-red-700',
    on_hold: 'bg-orange-100 text-orange-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

function statusLabel(status: string): string {
  const map: Record<string, string> = {
    active: t('goal.studentGoal.status.active'),
    achieved: t('goal.studentGoal.status.achieved'),
    discontinued: t('goal.studentGoal.status.discontinued'),
    on_hold: t('goal.studentGoal.status.on_hold'),
  }
  return map[status] ?? status
}

function horizonColor(horizon: string): string {
  return horizon === 'long_term' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'
}

function horizonLabel(horizon: string): string {
  return horizon === 'long_term' ? t('goal.studentGoal.horizon.longTerm') : t('goal.studentGoal.horizon.shortTerm')
}

function trendDisplay(trend: string | null): { icon: string; color: string; label: string } {
  if (trend === 'improving') return { icon: '↑', color: 'text-emerald-600', label: t('goal.progress.trend.improving') }
  if (trend === 'declining') return { icon: '↓', color: 'text-red-600', label: t('goal.progress.trend.declining') }
  if (trend === 'stable') return { icon: '→', color: 'text-gray-500', label: t('goal.progress.trend.stable') }
  return { icon: '—', color: 'text-muted-foreground', label: '—' }
}

// ── Change Status Modal ───────────────────────────────────────────────────────
const showStatusModal = ref(false)
const statusForm = ref({ newStatus: '', achievedDate: '' })
const statusError = ref('')

function openStatusModal() {
  statusForm.value = { newStatus: goal.value?.status ?? '', achievedDate: goal.value?.achievedDate ?? '' }
  statusError.value = ''
  showStatusModal.value = true
}

async function submitStatus() {
  if (!statusForm.value.newStatus) {
    statusError.value = t('validation.required', { field: t('goal.studentGoal.status') })
    return
  }
  try {
    await store.changeGoalStatus(id.value, {
      newStatus: statusForm.value.newStatus,
      achievedDate: statusForm.value.achievedDate || null,
    })
    showStatusModal.value = false
  } catch (e: unknown) {
    statusError.value = (e as Error).message
  }
}

// ── Record Progress Modal ─────────────────────────────────────────────────────
const showProgressModal = ref(false)
const progressForm = ref({ measuredOn: '', measuredValue: '', percentComplete: '', trend: '', note: '' })
const progressError = ref('')

function openProgressModal() {
  progressForm.value = { measuredOn: '', measuredValue: '', percentComplete: '', trend: '', note: '' }
  progressError.value = ''
  showProgressModal.value = true
}

async function submitProgress() {
  if (!progressForm.value.measuredOn) {
    progressError.value = t('validation.required', { field: t('goal.progress.measuredOn') })
    return
  }
  try {
    await store.recordProgress(id.value, {
      measuredOn: progressForm.value.measuredOn,
      measuredValue: progressForm.value.measuredValue ? parseFloat(progressForm.value.measuredValue) : null,
      percentComplete: progressForm.value.percentComplete ? parseFloat(progressForm.value.percentComplete) : null,
      trend: progressForm.value.trend || null,
      note: progressForm.value.note || null,
      sessionId: null,
    })
    showProgressModal.value = false
    await store.fetchTrend(id.value)
  } catch (e: unknown) {
    progressError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="store.loading && !goal" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- 404 -->
    <div v-else-if="!goal && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'student-goal-list' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('goal.studentGoal.title') }}
      </button>
    </div>

    <template v-else-if="goal">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div class="flex-1 min-w-0">
          <button @click="router.push({ name: 'student-goal-list' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('goal.studentGoal.title') }}
          </button>
          <h1 class="text-xl font-bold text-foreground leading-snug">{{ goal.statement }}</h1>
          <div class="flex flex-wrap items-center gap-2 mt-2">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', horizonColor(goal.horizon)]">
              {{ horizonLabel(goal.horizon) }}
            </span>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(goal.status)]">
              {{ statusLabel(goal.status) }}
            </span>
            <span v-if="goal.categoryLabel" class="text-xs text-muted-foreground">{{ goal.categoryLabel }}</span>
            <span v-if="goal.developmentAreaLabel" class="text-xs text-muted-foreground">{{ goal.developmentAreaLabel }}</span>
          </div>
        </div>
        <div class="flex items-center gap-2 shrink-0">
          <button
            v-if="can('student_goal:change_status')"
            @click="openStatusModal"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('goal.studentGoal.changeStatus') }}
          </button>
          <button
            v-if="can('student_goal:update')"
            @click="router.push({ name: 'student-goal-edit', params: { id: goal.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            {{ t('common.edit') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button
            v-for="tab in ['overview', 'progress', 'trend']"
            :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ tab === 'overview' ? t('bep.tab.overview') : tab === 'progress' ? t('goal.progress.title') : t('goal.dashboard.trend') }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
          <dl class="grid grid-cols-2 md:grid-cols-3 gap-5 text-sm">
            <div class="md:col-span-3">
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.statement') }}</dt>
              <dd class="text-foreground">{{ goal.statement }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.horizon') }}</dt>
              <dd><span :class="['px-2 py-0.5 rounded-full text-xs font-medium', horizonColor(goal.horizon)]">{{ horizonLabel(goal.horizon) }}</span></dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.status') }}</dt>
              <dd><span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(goal.status)]">{{ statusLabel(goal.status) }}</span></dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.category') }}</dt>
              <dd class="text-foreground">{{ goal.categoryLabel ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.developmentArea') }}</dt>
              <dd class="text-foreground">{{ goal.developmentAreaLabel ?? '—' }}</dd>
            </div>
            <div v-if="goal.masteryCriteria" class="md:col-span-3">
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.masteryCriteria') }}</dt>
              <dd class="text-foreground">{{ goal.masteryCriteria }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.baseline') }}</dt>
              <dd class="text-foreground">{{ goal.baseline ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.targetValue') }}</dt>
              <dd class="font-mono text-foreground">{{ goal.targetValue ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.startDate') }}</dt>
              <dd class="text-foreground">{{ formatDate(goal.startDate) }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.targetDate') }}</dt>
              <dd class="text-foreground">{{ formatDate(goal.targetDate) }}</dd>
            </div>
            <div v-if="goal.achievedDate">
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('goal.studentGoal.achievedDate') }}</dt>
              <dd class="font-medium text-teal-700">{{ formatDate(goal.achievedDate) }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Progress Tab -->
      <div v-else-if="activeTab === 'progress'" class="space-y-4">
        <div class="flex justify-end">
          <button
            v-if="can('goal_progress:record')"
            @click="openProgressModal"
            class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('goal.progress.record') }}
          </button>
        </div>
        <div v-if="store.progressList.length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="p in store.progressList"
            :key="p.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between gap-3">
              <div>
                <p class="text-sm font-medium text-foreground">{{ formatDate(p.measuredOn) }}</p>
                <div class="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
                  <span v-if="p.measuredValue !== null">{{ t('goal.progress.measuredValue') }}: {{ p.measuredValue }}</span>
                  <span v-if="p.percentComplete !== null">{{ p.percentComplete }}%</span>
                  <span v-if="p.trend" :class="trendDisplay(p.trend).color">{{ trendDisplay(p.trend).label }}</span>
                </div>
                <p v-if="p.note" class="text-xs text-muted-foreground mt-1">{{ p.note }}</p>
              </div>
              <div v-if="p.percentComplete !== null" class="flex items-center gap-2 shrink-0">
                <div class="bg-gray-200 rounded-full h-2 w-24">
                  <div class="bg-primary h-2 rounded-full" :style="{ width: `${p.percentComplete}%` }" />
                </div>
                <span class="text-xs font-mono text-muted-foreground">{{ p.percentComplete }}%</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Trend Tab -->
      <div v-else-if="activeTab === 'trend'">
        <div v-if="!store.trend" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <template v-else>
          <!-- Summary card -->
          <div class="rounded-xl border border-border bg-[--color-card] p-6 shadow-sm mb-6">
            <div class="flex items-center gap-6">
              <div class="text-center">
                <p class="text-4xl font-bold text-primary">
                  {{ store.trend.latestPercentComplete ?? '—' }}<span v-if="store.trend.latestPercentComplete !== null" class="text-xl">%</span>
                </p>
                <p class="text-xs text-muted-foreground mt-1">{{ t('goal.progress.percentComplete') }}</p>
              </div>
              <div>
                <p :class="['text-3xl font-bold', trendDisplay(store.trend.currentTrend).color]">
                  {{ trendDisplay(store.trend.currentTrend).icon }}
                </p>
                <p class="text-xs text-muted-foreground">{{ trendDisplay(store.trend.currentTrend).label }}</p>
              </div>
            </div>
          </div>

          <!-- Progress series table -->
          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border bg-accent/50">
                  <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.measuredOn') }}</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.measuredValue') }}</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.percentComplete') }}</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.trend') }}</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('goal.progress.note') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="p in store.trend.progressSeries"
                  :key="p.id"
                  class="border-b border-border last:border-0 hover:bg-accent/30 transition-colors"
                >
                  <td class="px-4 py-3 text-foreground">{{ formatDate(p.measuredOn) }}</td>
                  <td class="px-4 py-3 font-mono">{{ p.measuredValue ?? '—' }}</td>
                  <td class="px-4 py-3">
                    <div v-if="p.percentComplete !== null" class="flex items-center gap-2">
                      <div class="bg-gray-200 rounded-full h-1.5 w-16">
                        <div class="bg-primary h-1.5 rounded-full" :style="{ width: `${p.percentComplete}%` }" />
                      </div>
                      <span class="font-mono text-xs">{{ p.percentComplete }}%</span>
                    </div>
                    <span v-else class="text-muted-foreground">—</span>
                  </td>
                  <td class="px-4 py-3">
                    <span :class="['font-bold', trendDisplay(p.trend).color]">{{ trendDisplay(p.trend).icon }}</span>
                  </td>
                  <td class="px-4 py-3 text-muted-foreground text-xs">{{ p.note ?? '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </template>
      </div>
    </template>

    <!-- Change Status Modal -->
    <FormModal
      :open="showStatusModal"
      :title="t('goal.studentGoal.changeStatus')"
      :saving="store.saving"
      @submit="submitStatus"
      @close="showStatusModal = false"
    >
      <div class="space-y-4">
        <p v-if="statusError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ statusError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.status') }} *</label>
          <select v-model="statusForm.newStatus" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option value="active">{{ t('goal.studentGoal.status.active') }}</option>
            <option value="achieved">{{ t('goal.studentGoal.status.achieved') }}</option>
            <option value="discontinued">{{ t('goal.studentGoal.status.discontinued') }}</option>
            <option value="on_hold">{{ t('goal.studentGoal.status.on_hold') }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.achievedDate') }}</label>
          <input v-model="statusForm.achievedDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>

    <!-- Record Progress Modal -->
    <FormModal
      :open="showProgressModal"
      :title="t('goal.progress.record')"
      :saving="store.saving"
      @submit="submitProgress"
      @close="showProgressModal = false"
    >
      <div class="space-y-4">
        <p v-if="progressError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ progressError }}</p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.progress.measuredOn') }} *</label>
            <input v-model="progressForm.measuredOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.progress.measuredValue') }}</label>
            <input v-model="progressForm.measuredValue" type="number" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.progress.percentComplete') }} (0–100)</label>
            <input v-model="progressForm.percentComplete" type="number" min="0" max="100" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.progress.trend') }}</label>
            <select v-model="progressForm.trend" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option value="improving">{{ t('goal.progress.trend.improving') }}</option>
              <option value="stable">{{ t('goal.progress.trend.stable') }}</option>
              <option value="declining">{{ t('goal.progress.trend.declining') }}</option>
            </select>
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.progress.note') }}</label>
          <textarea v-model="progressForm.note" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
