<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useBepStore } from '@/stores/bep.store'
import { usePermission } from '@/composables/usePermission'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { EducationPlanGoalDto } from '@/types/bep.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useBepStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const plan = computed(() => store.currentPlan)
const activeTab = ref<'overview' | 'goals' | 'reviews' | 'approvals' | 'revisions'>('overview')

const actionError = ref('')

onMounted(async () => {
  await store.fetchPlan(id.value)
})

onUnmounted(() => {
  store.clearCurrent()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

// ── Status badges ────────────────────────────────────────────────────────────
function statusColor(status: string): string {
  const map: Record<string, string> = {
    draft: 'bg-gray-100 text-gray-700',
    pending_review: 'bg-yellow-100 text-yellow-700',
    approved: 'bg-green-100 text-green-700',
    active: 'bg-blue-100 text-blue-700',
    closed: 'bg-slate-100 text-slate-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

function statusLabel(status: string): string {
  const map: Record<string, string> = {
    draft: t('bep.status.draft'),
    pending_review: t('bep.status.pending_review'),
    approved: t('bep.status.approved'),
    active: t('bep.status.active'),
    closed: t('bep.status.closed'),
  }
  return map[status] ?? status
}

function goalStatusColor(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-green-100 text-green-700',
    achieved: 'bg-teal-100 text-teal-700',
    discontinued: 'bg-red-100 text-red-700',
    on_hold: 'bg-orange-100 text-orange-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

function trendIcon(trend: string | null): string {
  if (trend === 'improving') return '↑'
  if (trend === 'declining') return '↓'
  return '→'
}

function trendColor(trend: string | null): string {
  if (trend === 'improving') return 'text-emerald-600'
  if (trend === 'declining') return 'text-red-600'
  return 'text-gray-500'
}

function decisionColor(decision: string): string {
  if (decision === 'approved') return 'bg-green-100 text-green-700'
  if (decision === 'rejected') return 'bg-red-100 text-red-700'
  return 'bg-gray-100 text-gray-700'
}

// ── Workflow actions ──────────────────────────────────────────────────────────
async function doSubmit() {
  actionError.value = ''
  try {
    await store.submitPlan(id.value)
  } catch (e: unknown) {
    actionError.value = (e as Error).message
  }
}

// ── Approve / Reject modal ────────────────────────────────────────────────────
const showApproveModal = ref(false)
const showRejectModal = ref(false)
const approvalForm = ref({ approverId: '', comment: '' })
const approvalError = ref('')

function openApproveModal() {
  approvalForm.value = { approverId: '', comment: '' }
  approvalError.value = ''
  showApproveModal.value = true
}

function openRejectModal() {
  approvalForm.value = { approverId: '', comment: '' }
  approvalError.value = ''
  showRejectModal.value = true
}

async function doApprove() {
  if (!approvalForm.value.approverId.trim()) {
    approvalError.value = t('validation.required', { field: 'Onaylayan ID' })
    return
  }
  try {
    await store.approvePlan(id.value, {
      approverId: approvalForm.value.approverId,
      comment: approvalForm.value.comment || null,
    })
    showApproveModal.value = false
  } catch (e: unknown) {
    approvalError.value = (e as Error).message
  }
}

async function doReject() {
  if (!approvalForm.value.approverId.trim()) {
    approvalError.value = t('validation.required', { field: 'Onaylayan ID' })
    return
  }
  try {
    await store.rejectPlan(id.value, {
      approverId: approvalForm.value.approverId,
      comment: approvalForm.value.comment || null,
    })
    showRejectModal.value = false
  } catch (e: unknown) {
    approvalError.value = (e as Error).message
  }
}

async function doActivate() {
  actionError.value = ''
  try {
    await store.activatePlan(id.value)
  } catch (e: unknown) {
    actionError.value = (e as Error).message
  }
}

async function doClose() {
  actionError.value = ''
  try {
    await store.closePlan(id.value)
  } catch (e: unknown) {
    actionError.value = (e as Error).message
  }
}

// ── Revise modal ──────────────────────────────────────────────────────────────
const showReviseModal = ref(false)
const reviseForm = ref({ changeSummary: '' })
const reviseError = ref('')

async function doRevise() {
  try {
    await store.revisePlan(id.value, { changeSummary: reviseForm.value.changeSummary || null })
    showReviseModal.value = false
  } catch (e: unknown) {
    reviseError.value = (e as Error).message
  }
}

// ── Guardian visibility ───────────────────────────────────────────────────────
async function toggleGuardianVisibility() {
  if (!plan.value) return
  try {
    await store.setGuardianVisibility(id.value, { visible: !plan.value.guardianVisible })
  } catch (e: unknown) {
    actionError.value = (e as Error).message
  }
}

// ── Add goal modal ────────────────────────────────────────────────────────────
const showAddGoalModal = ref(false)
const addGoalForm = ref({ studentGoalId: '', horizon: 'long_term', sortOrder: 1 })
const addGoalError = ref('')

function openAddGoalModal() {
  addGoalForm.value = { studentGoalId: '', horizon: 'long_term', sortOrder: 1 }
  addGoalError.value = ''
  showAddGoalModal.value = true
}

async function doAddGoal() {
  if (!addGoalForm.value.studentGoalId.trim()) {
    addGoalError.value = t('validation.required', { field: 'Öğrenci Hedef ID' })
    return
  }
  try {
    await store.addGoal(id.value, {
      studentGoalId: addGoalForm.value.studentGoalId,
      horizon: addGoalForm.value.horizon,
      sortOrder: addGoalForm.value.sortOrder,
    })
    showAddGoalModal.value = false
  } catch (e: unknown) {
    addGoalError.value = (e as Error).message
  }
}

async function doRemoveGoal(planGoalId: string, e: Event) {
  e.stopPropagation()
  await store.removeGoal(id.value, planGoalId)
}

async function moveGoal(goals: EducationPlanGoalDto[], index: number, direction: 'up' | 'down') {
  const newIndex = direction === 'up' ? index - 1 : index + 1
  if (newIndex < 0 || newIndex >= goals.length) return
  const items = goals.map((g, i) => {
    if (i === index) return { planGoalId: g.id, sortOrder: goals[newIndex].sortOrder }
    if (i === newIndex) return { planGoalId: g.id, sortOrder: goals[index].sortOrder }
    return { planGoalId: g.id, sortOrder: g.sortOrder }
  })
  await store.reorderGoals(id.value, { items })
}

// ── Add review modal ──────────────────────────────────────────────────────────
const showReviewModal = ref(false)
const reviewForm = ref({ reviewedOn: '', reviewerId: '', summary: '', outcome: '' })
const reviewError = ref('')

function openReviewModal() {
  reviewForm.value = { reviewedOn: '', reviewerId: '', summary: '', outcome: '' }
  reviewError.value = ''
  showReviewModal.value = true
}

async function doAddReview() {
  if (!reviewForm.value.reviewedOn) {
    reviewError.value = t('validation.required', { field: t('bep.review.reviewedOn') })
    return
  }
  try {
    await store.addReview(id.value, {
      reviewedOn: reviewForm.value.reviewedOn,
      reviewerId: reviewForm.value.reviewerId || null,
      summary: reviewForm.value.summary || null,
      outcome: reviewForm.value.outcome || null,
    })
    showReviewModal.value = false
  } catch (e: unknown) {
    reviewError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <!-- Loading skeleton -->
    <div v-if="store.loading && !plan" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- 404 -->
    <div v-else-if="!plan && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'bep-list' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('bep.backToList') }}
      </button>
    </div>

    <template v-else-if="plan">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'bep-list' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('bep.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ plan.title }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span class="text-sm text-muted-foreground">{{ plan.studentName }}</span>
            <span class="px-2 py-0.5 rounded-full text-xs font-mono font-medium bg-accent text-foreground">v{{ plan.version }}</span>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(plan.status)]">
              {{ statusLabel(plan.status) }}
            </span>
          </div>
        </div>

        <div class="flex items-center gap-2 flex-wrap justify-end">
          <p v-if="actionError" class="w-full text-xs text-red-600 text-right">{{ actionError }}</p>

          <!-- Draft actions -->
          <template v-if="plan.status === 'draft'">
            <button
              v-if="can('education_plan:submit')"
              @click="doSubmit"
              :disabled="store.saving"
              class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60"
            >
              {{ t('bep.submit') }}
            </button>
            <button
              v-if="can('education_plan:update')"
              @click="router.push({ name: 'bep-edit', params: { id: plan.id } })"
              class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
            >
              {{ t('common.edit') }}
            </button>
          </template>

          <!-- Pending review actions -->
          <template v-else-if="plan.status === 'pending_review'">
            <button
              v-if="can('education_plan:approve')"
              @click="openApproveModal"
              class="px-3 py-2 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors"
            >
              {{ t('bep.approve') }}
            </button>
            <button
              v-if="can('education_plan:approve')"
              @click="openRejectModal"
              class="px-3 py-2 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50 transition-colors"
            >
              {{ t('bep.reject') }}
            </button>
          </template>

          <!-- Approved actions -->
          <template v-else-if="plan.status === 'approved'">
            <button
              v-if="can('education_plan:approve')"
              @click="doActivate"
              :disabled="store.saving"
              class="px-3 py-2 text-sm rounded-lg bg-blue-600 text-white hover:bg-blue-700 transition-colors disabled:opacity-60"
            >
              {{ t('bep.activate') }}
            </button>
          </template>

          <!-- Active actions -->
          <template v-else-if="plan.status === 'active'">
            <button
              v-if="can('education_plan:update')"
              @click="doClose"
              :disabled="store.saving"
              class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors disabled:opacity-60"
            >
              {{ t('bep.close') }}
            </button>
            <button
              v-if="can('education_plan:revise')"
              @click="showReviseModal = true"
              class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
            >
              {{ t('bep.revise') }}
            </button>
            <button
              v-if="can('education_plan:update')"
              @click="toggleGuardianVisibility"
              :disabled="store.saving"
              :class="['px-3 py-2 text-sm rounded-lg border transition-colors disabled:opacity-60', plan.guardianVisible ? 'border-emerald-300 text-emerald-700 bg-emerald-50 hover:bg-emerald-100' : 'border-border hover:bg-accent']"
            >
              <span class="flex items-center gap-1.5">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                {{ t('bep.guardianVisible') }}
              </span>
            </button>
          </template>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6 overflow-x-auto">
          <button
            v-for="tab in ['overview', 'goals', 'reviews', 'approvals', 'revisions']"
            :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ t(`bep.tab.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 space-y-3 shadow-sm md:col-span-2">
          <h3 class="font-semibold text-foreground">{{ t('bep.tab.overview') }}</h3>
          <dl class="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.studentName') }}</dt>
              <dd class="font-medium text-foreground">{{ plan.studentName }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.academicPeriod') }}</dt>
              <dd class="font-medium text-foreground">{{ plan.academicPeriodName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('common.campus') }}</dt>
              <dd class="font-medium text-foreground">{{ plan.campusName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.preparedBy') }}</dt>
              <dd class="font-medium text-foreground">{{ plan.preparedByName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.approvedBy') }}</dt>
              <dd class="font-medium text-foreground">{{ plan.approvedByName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.effectiveFrom') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(plan.effectiveFrom) }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.effectiveTo') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(plan.effectiveTo) }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground text-xs uppercase mb-1">{{ t('bep.guardianVisible') }}</dt>
              <dd>
                <span v-if="plan.guardianVisible" class="px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">
                  {{ t('common.yes') }}
                </span>
                <span v-else class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
                  {{ t('common.no') }}
                </span>
              </dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Goals Tab -->
      <div v-else-if="activeTab === 'goals'" class="space-y-6">
        <div v-if="can('education_plan:manage_goals')" class="flex justify-end">
          <button
            @click="openAddGoalModal"
            class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('bep.addGoal') }}
          </button>
        </div>

        <!-- Long-term Goals -->
        <div>
          <h3 class="font-semibold text-foreground mb-3">{{ t('bep.longTermGoals') }}</h3>
          <div v-if="plan.longTermGoals.length === 0" class="text-center py-8 text-muted-foreground text-sm rounded-xl border border-dashed border-border">
            {{ t('common.noData') }}
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="(goal, index) in plan.longTermGoals"
              :key="goal.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-foreground">{{ goal.statement }}</p>
                  <div class="flex flex-wrap items-center gap-2 mt-2">
                    <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', goalStatusColor(goal.goalStatus)]">
                      {{ goal.goalStatus }}
                    </span>
                    <span v-if="goal.categoryLabel" class="text-xs text-muted-foreground">{{ goal.categoryLabel }}</span>
                    <span v-if="goal.targetDate" class="text-xs text-muted-foreground">{{ formatDate(goal.targetDate) }}</span>
                    <span v-if="goal.latestTrend" :class="['text-sm font-bold', trendColor(goal.latestTrend)]">
                      {{ trendIcon(goal.latestTrend) }}
                    </span>
                  </div>
                  <div v-if="goal.latestPercentComplete !== null" class="mt-2">
                    <div class="flex items-center gap-2">
                      <div class="flex-1 bg-gray-200 rounded-full h-1.5">
                        <div
                          class="bg-primary h-1.5 rounded-full transition-all"
                          :style="{ width: `${goal.latestPercentComplete}%` }"
                        />
                      </div>
                      <span class="text-xs text-muted-foreground font-mono">{{ goal.latestPercentComplete }}%</span>
                    </div>
                  </div>
                </div>
                <div v-if="can('education_plan:manage_goals')" class="flex items-center gap-1 shrink-0">
                  <button
                    v-if="index > 0"
                    @click="moveGoal(plan.longTermGoals, index, 'up')"
                    class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                    title="Yukarı taşı"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                    </svg>
                  </button>
                  <button
                    v-if="index < plan.longTermGoals.length - 1"
                    @click="moveGoal(plan.longTermGoals, index, 'down')"
                    class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                    title="Aşağı taşı"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                  <button
                    @click="doRemoveGoal(goal.id, $event)"
                    class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors"
                    :title="t('bep.removeGoal')"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Short-term Goals -->
        <div>
          <h3 class="font-semibold text-foreground mb-3">{{ t('bep.shortTermGoals') }}</h3>
          <div v-if="plan.shortTermGoals.length === 0" class="text-center py-8 text-muted-foreground text-sm rounded-xl border border-dashed border-border">
            {{ t('common.noData') }}
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="(goal, index) in plan.shortTermGoals"
              :key="goal.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-foreground">{{ goal.statement }}</p>
                  <div class="flex flex-wrap items-center gap-2 mt-2">
                    <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', goalStatusColor(goal.goalStatus)]">
                      {{ goal.goalStatus }}
                    </span>
                    <span v-if="goal.categoryLabel" class="text-xs text-muted-foreground">{{ goal.categoryLabel }}</span>
                    <span v-if="goal.targetDate" class="text-xs text-muted-foreground">{{ formatDate(goal.targetDate) }}</span>
                    <span v-if="goal.latestTrend" :class="['text-sm font-bold', trendColor(goal.latestTrend)]">
                      {{ trendIcon(goal.latestTrend) }}
                    </span>
                  </div>
                  <div v-if="goal.latestPercentComplete !== null" class="mt-2">
                    <div class="flex items-center gap-2">
                      <div class="flex-1 bg-gray-200 rounded-full h-1.5">
                        <div
                          class="bg-primary h-1.5 rounded-full transition-all"
                          :style="{ width: `${goal.latestPercentComplete}%` }"
                        />
                      </div>
                      <span class="text-xs text-muted-foreground font-mono">{{ goal.latestPercentComplete }}%</span>
                    </div>
                  </div>
                </div>
                <div v-if="can('education_plan:manage_goals')" class="flex items-center gap-1 shrink-0">
                  <button
                    v-if="index > 0"
                    @click="moveGoal(plan.shortTermGoals, index, 'up')"
                    class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                    title="Yukarı taşı"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                    </svg>
                  </button>
                  <button
                    v-if="index < plan.shortTermGoals.length - 1"
                    @click="moveGoal(plan.shortTermGoals, index, 'down')"
                    class="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                    title="Aşağı taşı"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                  <button
                    @click="doRemoveGoal(goal.id, $event)"
                    class="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 transition-colors"
                    :title="t('bep.removeGoal')"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Reviews Tab -->
      <div v-else-if="activeTab === 'reviews'" class="space-y-4">
        <div class="flex justify-end">
          <button
            v-if="can('education_plan:add_review')"
            @click="openReviewModal"
            class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('bep.review.add') }}
          </button>
        </div>
        <div v-if="plan.reviews.length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="review in plan.reviews"
            :key="review.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between">
              <div>
                <p class="text-sm font-medium text-foreground">{{ review.reviewerName ?? '—' }}</p>
                <p class="text-xs text-muted-foreground mt-0.5">{{ formatDate(review.reviewedOn) }}</p>
              </div>
              <span v-if="review.outcome" class="px-2 py-0.5 rounded-full text-xs font-medium bg-accent text-foreground">
                {{ review.outcome }}
              </span>
            </div>
            <p v-if="review.summary" class="mt-2 text-sm text-muted-foreground">{{ review.summary }}</p>
          </div>
        </div>
      </div>

      <!-- Approvals Tab -->
      <div v-else-if="activeTab === 'approvals'" class="space-y-3">
        <div v-if="plan.approvals.length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <div
          v-for="approval in plan.approvals"
          :key="approval.id"
          class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
        >
          <div class="flex items-start justify-between">
            <div>
              <p class="text-sm font-medium text-foreground">{{ approval.approverName ?? '—' }}</p>
              <p class="text-xs text-muted-foreground mt-0.5">{{ formatDate(approval.decidedAt) }}</p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', decisionColor(approval.decision)]">
              {{ approval.decision }}
            </span>
          </div>
          <p v-if="approval.comment" class="mt-2 text-sm text-muted-foreground">{{ approval.comment }}</p>
        </div>
      </div>

      <!-- Revisions Tab -->
      <div v-else-if="activeTab === 'revisions'" class="space-y-3">
        <div v-if="plan.revisions.length === 0" class="text-center py-12 text-muted-foreground text-sm">
          {{ t('common.noData') }}
        </div>
        <div
          v-for="rev in plan.revisions"
          :key="rev.id"
          class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
        >
          <div class="flex items-start justify-between">
            <div>
              <p class="text-sm font-medium text-foreground">
                v{{ rev.fromVersion }} → v{{ rev.toVersion }}
              </p>
              <p class="text-xs text-muted-foreground mt-0.5">
                {{ rev.revisedByName ?? '—' }} · {{ formatDate(rev.revisedAt) }}
              </p>
            </div>
          </div>
          <p v-if="rev.changeSummary" class="mt-2 text-sm text-muted-foreground">
            {{ rev.changeSummary }}
          </p>
        </div>
      </div>
    </template>

    <!-- Approve Modal -->
    <FormModal
      :open="showApproveModal"
      :title="t('bep.approve')"
      :saving="store.saving"
      @submit="doApprove"
      @close="showApproveModal = false"
    >
      <div class="space-y-4">
        <p v-if="approvalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ approvalError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">Onaylayan ID *</label>
          <input v-model="approvalForm.approverId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.comment') }}</label>
          <textarea v-model="approvalForm.comment" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Reject Modal -->
    <FormModal
      :open="showRejectModal"
      :title="t('bep.reject')"
      :saving="store.saving"
      @submit="doReject"
      @close="showRejectModal = false"
    >
      <div class="space-y-4">
        <p v-if="approvalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ approvalError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">Onaylayan ID *</label>
          <input v-model="approvalForm.approverId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.comment') }}</label>
          <textarea v-model="approvalForm.comment" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Revise Modal -->
    <FormModal
      :open="showReviseModal"
      :title="t('bep.revise')"
      :saving="store.saving"
      @submit="doRevise"
      @close="showReviseModal = false"
    >
      <div class="space-y-4">
        <p v-if="reviseError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ reviseError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.revision.changeSummary') }}</label>
          <textarea v-model="reviseForm.changeSummary" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Add Goal Modal -->
    <FormModal
      :open="showAddGoalModal"
      :title="t('bep.addGoal')"
      :saving="store.saving"
      @submit="doAddGoal"
      @close="showAddGoalModal = false"
    >
      <div class="space-y-4">
        <p v-if="addGoalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ addGoalError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">Öğrenci Hedef ID *</label>
          <input v-model="addGoalForm.studentGoalId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.horizon') }}</label>
            <select v-model="addGoalForm.horizon" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="long_term">{{ t('goal.studentGoal.horizon.longTerm') }}</option>
              <option value="short_term">{{ t('goal.studentGoal.horizon.shortTerm') }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">Sıra</label>
            <input v-model.number="addGoalForm.sortOrder" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>
    </FormModal>

    <!-- Add Review Modal -->
    <FormModal
      :open="showReviewModal"
      :title="t('bep.review.add')"
      :saving="store.saving"
      @submit="doAddReview"
      @close="showReviewModal = false"
    >
      <div class="space-y-4">
        <p v-if="reviewError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ reviewError }}</p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.review.reviewedOn') }} *</label>
            <input v-model="reviewForm.reviewedOn" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">İnceleyen ID</label>
            <input v-model="reviewForm.reviewerId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.review.summary') }}</label>
          <textarea v-model="reviewForm.summary" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.review.outcome') }}</label>
          <input v-model="reviewForm.outcome" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
