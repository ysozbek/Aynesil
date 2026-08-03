<script setup lang="ts">
/**
 * Assessment Session Detail — full lifecycle, scoring, report, and recommendations.
 * ABAC: Loads only data returned by backend. Handles 403/404 gracefully.
 */
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAssessmentStore } from '@/stores/assessment.store'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { usePermission } from '@/composables/usePermission'
import { useAuthStore } from '@/stores/auth.store'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { AssessmentItemDto, ResponseItemPayload } from '@/types/assessment.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useAssessmentStore()
const templateStore = useAssessmentTemplateStore()
const auth = useAuthStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const session = computed(() => store.current)
const activeTab = ref<'overview' | 'scoring' | 'report' | 'recommendations'>('overview')

onMounted(async () => {
  await store.fetchOne(id.value)
  if (session.value) {
    await templateStore.fetchOne(session.value.templateId)
    await Promise.all([
      can('assessment_report:read') ? store.fetchReport(id.value) : Promise.resolve(),
      can('program_recommendation:read') ? store.fetchRecommendations(id.value) : Promise.resolve(),
    ])
  }
})

onUnmounted(() => store.clearCurrent())

// ── Responses / Scoring ────────────────────────────────────────────────────────
const responses = ref<Record<string, ResponseItemPayload>>({})
const savingResponses = ref(false)

function initResponses() {
  if (!session.value) return
  session.value.responses.forEach(r => {
    responses.value[r.itemId] = {
      itemId: r.itemId,
      numericValue: r.numericValue ?? undefined,
      textValue: r.textValue ?? undefined,
      choiceValue: r.choiceValue ?? undefined,
      note: r.note ?? undefined,
    }
  })
}

function getResponse(itemId: string): ResponseItemPayload {
  if (!responses.value[itemId]) {
    responses.value[itemId] = { itemId }
  }
  return responses.value[itemId]
}

async function saveResponses() {
  savingResponses.value = true
  try {
    await store.submitResponses(id.value, {
      responses: Object.values(responses.value).filter(r => r.numericValue !== undefined || r.textValue !== undefined || r.choiceValue !== undefined),
    })
  } finally {
    savingResponses.value = false
  }
}

// ── Workflow ───────────────────────────────────────────────────────────────────
const workflowLoading = ref(false)
const workflowError = ref('')
const cancelConfirm = ref(false)

async function startSession() {
  if (!session.value) return
  workflowLoading.value = true
  workflowError.value = ''
  try {
    await store.start(id.value, session.value.rowVersion)
    initResponses()
    activeTab.value = 'scoring'
  } catch (e: unknown) {
    workflowError.value = (e as Error).message
  } finally {
    workflowLoading.value = false
  }
}

async function completeSession() {
  if (!session.value) return
  workflowLoading.value = true
  workflowError.value = ''
  try {
    await store.complete(id.value, session.value.rowVersion)
  } catch (e: unknown) {
    workflowError.value = (e as Error).message
  } finally {
    workflowLoading.value = false
  }
}

async function cancelSession() {
  if (!session.value) return
  workflowLoading.value = true
  try {
    await store.cancel(id.value, session.value.rowVersion)
    cancelConfirm.value = false
  } finally {
    workflowLoading.value = false
  }
}

// ── Report ─────────────────────────────────────────────────────────────────────
const showReportForm = ref(false)
const reportForm = ref({ summary: '', findings: '' })
const reportError = ref('')

function openReportForm() {
  reportForm.value = {
    summary: store.currentReport?.summary ?? '',
    findings: store.currentReport?.findings ?? '',
  }
  reportError.value = ''
  showReportForm.value = true
}

async function submitReport() {
  try {
    if (store.currentReport) {
      await store.updateReport(id.value, {
        reportId: store.currentReport.id,
        summary: reportForm.value.summary || undefined,
        findings: reportForm.value.findings || undefined,
        rowVersion: store.currentReport.rowVersion,
      })
    } else {
      await store.createReport(id.value, {
        corporationId: session.value!.corporationId,
        summary: reportForm.value.summary || undefined,
        findings: reportForm.value.findings || undefined,
      })
    }
    showReportForm.value = false
  } catch (e: unknown) {
    reportError.value = (e as Error).message
  }
}

async function finalizeReport() {
  if (!store.currentReport) return
  workflowLoading.value = true
  try {
    await store.finalizeReport(id.value, {
      reportId: store.currentReport.id,
      rowVersion: store.currentReport.rowVersion,
    })
  } finally {
    workflowLoading.value = false
  }
}

// ── Recommendations ────────────────────────────────────────────────────────────
const showRecommendationForm = ref(false)
const recForm = ref({ recommendedIntensity: '', rationale: '' })
const recError = ref('')

async function submitRecommendation() {
  if (!session.value) return
  try {
    await store.createRecommendation(id.value, {
      corporationId: session.value.corporationId,
      leadId: session.value.leadId ?? undefined,
      studentId: session.value.studentId ?? undefined,
      recommendedIntensity: recForm.value.recommendedIntensity || undefined,
      rationale: recForm.value.rationale || undefined,
      recommendedBy: auth.user?.userId,
    })
    showRecommendationForm.value = false
  } catch (e: unknown) {
    recError.value = (e as Error).message
  }
}

// ── Helpers ────────────────────────────────────────────────────────────────────
const statusColor = (status: string): string => {
  const map: Record<string, string> = {
    planned: 'bg-blue-100 text-blue-700',
    in_progress: 'bg-amber-100 text-amber-700',
    completed: 'bg-emerald-100 text-emerald-700',
    cancelled: 'bg-red-100 text-red-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

function formatDateTime(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const template = computed(() => templateStore.current)

function getChoices(item: AssessmentItemDto): string[] {
  if (!item.choices) return []
  try { return JSON.parse(item.choices) } catch { return item.choices.split(',').map(s => s.trim()) }
}
</script>

<template>
  <div>
    <div v-if="store.loading && !session" class="space-y-3">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>
    <div v-else-if="!session && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
    </div>

    <template v-else-if="session">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'assessment-sessions' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('assessment.session.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ session.templateName }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(session.status)]">
              {{ t(`assessment.session.status.${session.status}`) }}
            </span>
            <span v-if="session.leadContactName" class="text-sm text-muted-foreground">{{ session.leadContactName }}</span>
            <span v-else-if="session.studentName" class="text-sm text-muted-foreground">{{ session.studentName }}</span>
            <span v-if="session.totalScore !== null && session.totalScore !== undefined" class="text-sm font-mono font-bold text-primary">
              {{ t('assessment.session.score') }}: {{ session.totalScore }}
            </span>
          </div>
          <p v-if="workflowError" class="mt-1 text-xs text-red-600">{{ workflowError }}</p>
        </div>
        <div class="flex items-center gap-2 flex-wrap">
          <button
            v-if="can('assessment_session:start') && session.status === 'planned'"
            @click="startSession" :disabled="workflowLoading"
            class="px-3 py-2 text-sm rounded-lg bg-blue-600 text-white hover:bg-blue-700 transition-colors disabled:opacity-60">
            {{ t('assessment.session.start') }}
          </button>
          <button
            v-if="can('assessment_session:complete') && session.status === 'in_progress'"
            @click="completeSession" :disabled="workflowLoading"
            class="px-3 py-2 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-60">
            {{ t('assessment.session.complete') }}
          </button>
          <button
            v-if="can('assessment_session:cancel') && (session.status === 'planned' || session.status === 'in_progress')"
            @click="cancelConfirm = true"
            class="px-3 py-2 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50 transition-colors">
            {{ t('assessment.session.cancel') }}
          </button>
          <button
            v-if="can('assessment_session:update') && (session.status === 'planned' || session.status === 'in_progress')"
            @click="router.push({ name: 'assessment-session-edit', params: { id: session.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
            {{ t('common.edit') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button v-for="tab in ['overview', 'scoring', 'report', 'recommendations']" :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']">
            {{ t(`assessment.detail.tab.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
          <h3 class="font-semibold text-foreground mb-3">{{ t('assessment.session.details') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('assessment.session.template') }}</dt>
              <dd class="font-medium text-foreground">{{ session.templateName }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('assessment.session.assessor') }}</dt>
              <dd class="font-medium text-foreground">{{ session.assessorName ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('campus.title') }}</dt>
              <dd class="font-medium text-foreground">{{ session.campusName ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('assessment.session.scheduledAt') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDateTime(session.scheduledAt) }}</dd>
            </div>
            <div v-if="session.completedAt" class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('assessment.session.completedAt') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDateTime(session.completedAt) }}</dd>
            </div>
            <div v-if="session.totalScore !== null && session.totalScore !== undefined" class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('assessment.session.totalScore') }}</dt>
              <dd class="font-bold text-primary font-mono">{{ session.totalScore }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Scoring Tab -->
      <div v-else-if="activeTab === 'scoring'">
        <div class="mb-4 flex items-center justify-between">
          <p class="text-sm text-muted-foreground">{{ session.responses.length }} {{ t('assessment.scoring.responsesLogged') }}</p>
          <button
            v-if="can('assessment_session:submit_responses') && session.status === 'in_progress'"
            @click="saveResponses" :disabled="savingResponses"
            class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60">
            <svg v-if="savingResponses" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            {{ savingResponses ? t('common.saving') : t('assessment.scoring.save') }}
          </button>
        </div>

        <div v-if="!template" class="text-center py-12 text-muted-foreground">{{ t('common.loading') }}</div>
        <div v-else-if="!template.sections.length" class="text-center py-12 text-muted-foreground">{{ t('assessment.section.none') }}</div>
        <div v-else class="space-y-6">
          <div v-for="section in [...template.sections].sort((a, b) => a.sortOrder - b.sortOrder)" :key="section.id">
            <div class="mb-2 px-1 flex items-center gap-2">
              <span class="text-sm font-semibold text-foreground font-mono">{{ section.code }}</span>
              <span v-if="section.developmentAreaName" class="text-xs text-muted-foreground">· {{ section.developmentAreaName }}</span>
            </div>
            <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
              <div v-for="(item, idx) in [...section.items].sort((a, b) => a.sortOrder - b.sortOrder)" :key="item.id"
                :class="['p-4', idx < section.items.length - 1 ? 'border-b border-border' : '']">
                <p class="text-sm font-medium text-foreground mb-2">{{ item.prompt }}</p>
                <div class="flex items-center gap-3">
                  <!-- Numeric -->
                  <input v-if="item.responseType === 'numeric' || item.responseType === 'scale'"
                    v-model.number="getResponse(item.id).numericValue"
                    type="number" step="0.1"
                    :disabled="session.status !== 'in_progress'"
                    class="w-24 px-3 py-1.5 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50" />
                  <!-- Boolean -->
                  <template v-else-if="item.responseType === 'boolean'">
                    <label class="flex items-center gap-1.5 text-sm cursor-pointer">
                      <input type="radio" v-model.number="getResponse(item.id).numericValue" :value="1" :disabled="session.status !== 'in_progress'" />
                      {{ t('common.yes') }}
                    </label>
                    <label class="flex items-center gap-1.5 text-sm cursor-pointer">
                      <input type="radio" v-model.number="getResponse(item.id).numericValue" :value="0" :disabled="session.status !== 'in_progress'" />
                      {{ t('common.no') }}
                    </label>
                  </template>
                  <!-- Text -->
                  <textarea v-else-if="item.responseType === 'text'"
                    v-model="getResponse(item.id).textValue"
                    rows="2"
                    :disabled="session.status !== 'in_progress'"
                    class="flex-1 px-3 py-1.5 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50 resize-none" />
                  <!-- Choice -->
                  <select v-else-if="item.responseType === 'choice'"
                    v-model="getResponse(item.id).choiceValue"
                    :disabled="session.status !== 'in_progress'"
                    class="px-3 py-1.5 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50">
                    <option value="">{{ t('common.select') }}</option>
                    <option v-for="choice in getChoices(item)" :key="choice" :value="choice">{{ choice }}</option>
                  </select>
                  <!-- Note -->
                  <input v-model="getResponse(item.id).note" type="text" :placeholder="t('assessment.scoring.note')"
                    :disabled="session.status !== 'in_progress'"
                    class="flex-1 px-3 py-1.5 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50" />
                  <span class="text-xs text-muted-foreground ml-auto whitespace-nowrap">w: {{ item.weight }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Report Tab -->
      <div v-else-if="activeTab === 'report'">
        <div v-if="!can('assessment_report:read')" class="text-center py-12 text-muted-foreground">{{ t('errors.forbidden') }}</div>
        <template v-else>
          <div class="mb-4 flex items-center justify-between">
            <h3 class="font-semibold text-foreground">{{ t('assessment.report.title') }}</h3>
            <div class="flex items-center gap-2">
              <button v-if="can('assessment_report:create') && !store.currentReport && session.status === 'completed'"
                @click="openReportForm"
                class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
                {{ t('assessment.report.create') }}
              </button>
              <button v-if="can('assessment_report:update') && store.currentReport && !store.currentReport.isFinalized"
                @click="openReportForm"
                class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
                {{ t('common.edit') }}
              </button>
              <button v-if="can('assessment_report:finalize') && store.currentReport && !store.currentReport.isFinalized"
                @click="finalizeReport" :disabled="workflowLoading"
                class="px-3 py-1.5 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-60">
                {{ t('assessment.report.finalize') }}
              </button>
            </div>
          </div>

          <div v-if="!store.currentReport" class="text-center py-12 text-muted-foreground">
            {{ session.status !== 'completed' ? t('assessment.report.sessionNotComplete') : t('assessment.report.noReport') }}
          </div>
          <div v-else class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
            <div class="flex items-center justify-between">
              <span v-if="store.currentReport.isFinalized" class="px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">
                {{ t('assessment.report.finalized') }} · {{ formatDate(store.currentReport.finalizedAt) }}
              </span>
              <span v-else class="px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700">
                {{ t('assessment.report.draft') }}
              </span>
            </div>
            <div v-if="store.currentReport.summary">
              <p class="text-xs font-medium text-muted-foreground uppercase mb-1">{{ t('assessment.report.summary') }}</p>
              <p class="text-sm text-foreground">{{ store.currentReport.summary }}</p>
            </div>
            <div v-if="store.currentReport.findings">
              <p class="text-xs font-medium text-muted-foreground uppercase mb-1">{{ t('assessment.report.findings') }}</p>
              <p class="text-sm text-foreground">{{ store.currentReport.findings }}</p>
            </div>
          </div>
        </template>
      </div>

      <!-- Recommendations Tab -->
      <div v-else-if="activeTab === 'recommendations'">
        <div v-if="!can('program_recommendation:read')" class="text-center py-12 text-muted-foreground">{{ t('errors.forbidden') }}</div>
        <template v-else>
          <div class="mb-4 flex items-center justify-between">
            <h3 class="font-semibold text-foreground">{{ t('assessment.recommendation.title') }}</h3>
            <button v-if="can('program_recommendation:create') && session.status === 'completed'"
              @click="showRecommendationForm = true"
              class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
              + {{ t('assessment.recommendation.add') }}
            </button>
          </div>

          <div v-if="store.recommendations.length === 0" class="text-center py-12 text-muted-foreground">
            {{ t('assessment.recommendation.none') }}
          </div>
          <div v-else class="space-y-3">
            <div v-for="rec in store.recommendations" :key="rec.id"
              class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <p class="text-sm font-medium text-foreground">{{ rec.recommendedProgramName ?? t('assessment.recommendation.noProgram') }}</p>
                  <p v-if="rec.recommendedIntensity" class="text-xs text-muted-foreground">{{ t('assessment.recommendation.intensity') }}: {{ rec.recommendedIntensity }}</p>
                  <p v-if="rec.rationale" class="text-xs text-foreground mt-1">{{ rec.rationale }}</p>
                </div>
                <span class="text-xs text-muted-foreground whitespace-nowrap">{{ rec.recommendedByName }}</span>
              </div>
            </div>
          </div>
        </template>
      </div>
    </template>

    <!-- Report Form Modal -->
    <FormModal :open="showReportForm" :title="store.currentReport ? t('assessment.report.edit') : t('assessment.report.create')" :saving="store.saving" @submit="submitReport" @close="showReportForm = false">
      <div class="space-y-4">
        <p v-if="reportError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ reportError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.report.summary') }}</label>
          <textarea v-model="reportForm.summary" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.report.findings') }}</label>
          <textarea v-model="reportForm.findings" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Recommendation Form Modal -->
    <FormModal :open="showRecommendationForm" :title="t('assessment.recommendation.add')" :saving="store.saving" @submit="submitRecommendation" @close="showRecommendationForm = false">
      <div class="space-y-4">
        <p v-if="recError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ recError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.recommendation.intensity') }}</label>
          <input v-model="recForm.recommendedIntensity" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.recommendation.rationale') }}</label>
          <textarea v-model="recForm.rationale" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Cancel confirm -->
    <ConfirmModal :open="cancelConfirm" :title="t('assessment.session.cancelTitle')" :message="t('assessment.session.cancelMessage')"
      :confirm-label="t('assessment.session.cancel')" :loading="workflowLoading"
      @confirm="cancelSession" @cancel="cancelConfirm = false" />
  </div>
</template>
