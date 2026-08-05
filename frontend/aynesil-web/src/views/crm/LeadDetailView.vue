<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeadStore } from '@/stores/lead.store'
import { useLeadActivityStore } from '@/stores/leadActivity.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import { useBranchStore } from '@/stores/branch.store'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useLeadStore()
const activityStore = useLeadActivityStore()
const refData = useRefDataStore()
const branchStore = useBranchStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const lead = computed(() => store.current)
const activeTab = ref<'overview' | 'activities' | 'interviews' | 'history'>('overview')

const statuses = ref<RefValueItem[]>([])
const stages = ref<RefValueItem[]>([])
const activityTypes = ref<RefValueItem[]>([])

onMounted(async () => {
  await store.fetchOne(id.value)
  await Promise.all([
    activityStore.fetchActivities(id.value),
    activityStore.fetchInterviews(id.value),
    activityStore.fetchHistory(id.value),
    refData.getValues('LEAD_STATUS').then(v => { statuses.value = v }),
    refData.getValues('pipeline_stage').then(v => { stages.value = v }),
    refData.getValues('ACTIVITY_TYPE').then(v => { activityTypes.value = v }),
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
  ])
})

onUnmounted(() => {
  store.clearCurrent()
  activityStore.clearActivities()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}
function formatDateTime(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// ── Status change ──────────────────────────────────────────────────────────────
const showStatusModal = ref(false)
const statusForm = ref({ newStatusId: '', newPipelineStageId: '' })
const statusError = ref('')

function openStatusModal() {
  statusForm.value = { newStatusId: lead.value?.statusId ?? '', newPipelineStageId: lead.value?.pipelineStageId ?? '' }
  statusError.value = ''
  showStatusModal.value = true
}

async function submitStatus() {
  if (!statusForm.value.newStatusId) { statusError.value = t('validation.required', { field: t('common.status') }); return }
  try {
    await store.changeStatus(id.value, {
      newStatusId: statusForm.value.newStatusId,
      newPipelineStageId: statusForm.value.newPipelineStageId || undefined,
      rowVersion: lead.value!.rowVersion,
    })
    showStatusModal.value = false
  } catch (e: unknown) {
    statusError.value = (e as Error).message
  }
}

// ── Log Activity ───────────────────────────────────────────────────────────────
const showActivityModal = ref(false)
const activityForm = ref({ activityTypeId: '', subject: '', body: '', direction: 'outbound', followUpAt: '' })
const activityError = ref('')

function openActivityModal() {
  activityForm.value = { activityTypeId: '', subject: '', body: '', direction: 'outbound', followUpAt: '' }
  activityError.value = ''
  showActivityModal.value = true
}

async function submitActivity() {
  try {
    await activityStore.logActivity(id.value, {
      activityTypeId: activityForm.value.activityTypeId || undefined,
      subject: activityForm.value.subject || undefined,
      body: activityForm.value.body || undefined,
      direction: activityForm.value.direction || undefined,
      followUpAt: activityForm.value.followUpAt || undefined,
      occurredAt: new Date().toISOString(),
    })
    showActivityModal.value = false
    await activityStore.fetchActivities(id.value)
  } catch (e: unknown) {
    activityError.value = (e as Error).message
  }
}

// ── Schedule Interview ─────────────────────────────────────────────────────────
const showInterviewModal = ref(false)
const interviewForm = ref({ campusId: '', scheduledAt: '' })
const interviewError = ref('')

function openInterviewModal() {
  interviewForm.value = { campusId: '', scheduledAt: '' }
  interviewError.value = ''
  showInterviewModal.value = true
}

async function submitInterview() {
  try {
    await activityStore.scheduleInterview(id.value, {
      campusId: interviewForm.value.campusId || undefined,
      scheduledAt: interviewForm.value.scheduledAt || undefined,
    })
    showInterviewModal.value = false
    await activityStore.fetchInterviews(id.value)
  } catch (e: unknown) {
    interviewError.value = (e as Error).message
  }
}

// ── Delete lead ────────────────────────────────────────────────────────────────
const showDelete = ref(false)
const deleteLoading = ref(false)

async function doDelete() {
  deleteLoading.value = true
  try {
    await store.remove(id.value)
    router.push({ name: 'leads' })
  } finally {
    deleteLoading.value = false
  }
}

// ── Interview actions ──────────────────────────────────────────────────────────
const completeInterviewTarget = ref<string | null>(null)
const completeInterviewForm = ref({ outcome: '', recommendation: '', rowVersion: 0 })
const completeInterviewError = ref('')

function openCompleteInterview(interviewId: string, rowVersion: number) {
  completeInterviewTarget.value = interviewId
  completeInterviewForm.value = { outcome: '', recommendation: '', rowVersion }
  completeInterviewError.value = ''
}

async function submitCompleteInterview() {
  if (!completeInterviewTarget.value) return
  try {
    await activityStore.completeInterview(completeInterviewTarget.value, {
      outcome: completeInterviewForm.value.outcome || undefined,
      recommendation: completeInterviewForm.value.recommendation || undefined,
      rowVersion: completeInterviewForm.value.rowVersion,
    })
    completeInterviewTarget.value = null
    await activityStore.fetchInterviews(id.value)
  } catch (e: unknown) {
    completeInterviewError.value = (e as Error).message
  }
}

function interviewStatusBadge(status: string): string {
  const map: Record<string, string> = {
    scheduled: 'bg-blue-100 text-blue-700',
    completed: 'bg-emerald-100 text-emerald-700',
    cancelled: 'bg-red-100 text-red-700',
    no_show: 'bg-amber-100 text-amber-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-700'
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="store.loading && !lead" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- 404 -->
    <div v-else-if="!lead && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'leads' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('crm.lead.backToList') }}
      </button>
    </div>

    <template v-else-if="lead">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'leads' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('crm.lead.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ lead.contactName }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span v-if="lead.pipelineStageName" class="px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-700">
              {{ lead.pipelineStageName }}
            </span>
            <span v-if="lead.statusName" class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
              {{ lead.statusName }}
            </span>
            <span v-if="lead.isConverted" class="px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">
              {{ t('crm.lead.converted') }}
            </span>
          </div>
        </div>
        <div class="flex items-center gap-2 flex-wrap">
          <button
            v-if="can('lead:update') && !lead.isConverted"
            @click="openStatusModal"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('crm.lead.changeStatus') }}
          </button>
          <button
            v-if="can('lead_activity:create')"
            @click="openActivityModal"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('crm.activity.log') }}
          </button>
          <button
            v-if="can('interview:create')"
            @click="openInterviewModal"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
          >
            {{ t('crm.interview.schedule') }}
          </button>
          <button
            v-if="can('lead:update')"
            @click="router.push({ name: 'lead-edit', params: { id: lead.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="can('lead:convert') && !lead.isConverted"
            @click="router.push({ name: 'lead-convert', params: { id: lead.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors"
          >
            {{ t('crm.lead.convert') }}
          </button>
          <button
            v-if="can('lead:delete') && !lead.isConverted"
            @click="showDelete = true"
            class="px-3 py-2 text-sm rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors"
          >
            {{ t('common.delete') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button v-for="tab in ['overview', 'activities', 'interviews', 'history']" :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']">
            {{ t(`crm.detail.tab.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 space-y-3 shadow-sm">
          <h3 class="font-semibold text-foreground">{{ t('crm.lead.contactInfo') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.contactName') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.contactName }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.phone') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.contactPhone ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.email') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.contactEmail ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.campus') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.campusName ?? '—' }}</dd>
            </div>
          </dl>
        </div>

        <div class="rounded-xl border border-border bg-[--color-card] p-5 space-y-3 shadow-sm">
          <h3 class="font-semibold text-foreground">{{ t('crm.lead.childInfo') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.childName') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.childName ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.childBirthDate') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(lead.childBirthDate) }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.source') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.sourceName ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.assignedTo') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.assignedToName ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('crm.lead.score') }}</dt>
              <dd class="font-medium text-foreground">{{ lead.score ?? '—' }}</dd>
            </div>
          </dl>
        </div>

        <div v-if="lead.presentingNeed || lead.referralDetail" class="md:col-span-2 rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
          <h3 class="font-semibold text-foreground mb-3">{{ t('crm.lead.clinicalInfo') }}</h3>
          <div v-if="lead.presentingNeed" class="mb-3">
            <p class="text-xs font-medium text-muted-foreground uppercase mb-1">{{ t('crm.lead.presentingNeed') }}</p>
            <p class="text-sm text-foreground">{{ lead.presentingNeed }}</p>
          </div>
          <div v-if="lead.referralDetail">
            <p class="text-xs font-medium text-muted-foreground uppercase mb-1">{{ t('crm.lead.referralDetail') }}</p>
            <p class="text-sm text-foreground">{{ lead.referralDetail }}</p>
          </div>
        </div>
      </div>

      <!-- Activities Tab -->
      <div v-else-if="activeTab === 'activities'">
        <div v-if="activityStore.loading" class="space-y-3">
          <div v-for="i in 3" :key="i" class="h-16 rounded-xl bg-accent animate-pulse" />
        </div>
        <div v-else-if="activityStore.activities.items.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('crm.activity.noActivities') }}
        </div>
        <div v-else class="space-y-3">
          <div v-for="act in activityStore.activities.items" :key="act.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-sm font-medium text-foreground">{{ act.subject ?? t('crm.activity.noSubject') }}</p>
                <p class="text-xs text-muted-foreground mt-0.5">
                  {{ act.activityTypeName }} · {{ act.direction === 'inbound' ? t('crm.activity.inbound') : t('crm.activity.outbound') }} · {{ formatDateTime(act.occurredAt) }}
                </p>
              </div>
              <span v-if="act.followUpAt" class="text-xs text-amber-600 font-medium">
                {{ t('crm.activity.followUp') }}: {{ formatDateTime(act.followUpAt) }}
              </span>
            </div>
            <p v-if="act.body" class="mt-2 text-sm text-muted-foreground">{{ act.body }}</p>
          </div>
        </div>
      </div>

      <!-- Interviews Tab -->
      <div v-else-if="activeTab === 'interviews'">
        <div v-if="activityStore.loading" class="space-y-3">
          <div v-for="i in 2" :key="i" class="h-20 rounded-xl bg-accent animate-pulse" />
        </div>
        <div v-else-if="activityStore.interviews.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('crm.interview.noInterviews') }}
        </div>
        <div v-else class="space-y-3">
          <div v-for="iv in activityStore.interviews" :key="iv.id"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm">
            <div class="flex items-start justify-between">
              <div>
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', interviewStatusBadge(iv.status)]">
                  {{ t(`crm.interview.status.${iv.status}`) }}
                </span>
                <p class="mt-2 text-sm text-foreground">{{ t('crm.interview.scheduledAt') }}: {{ formatDateTime(iv.scheduledAt) }}</p>
                <p v-if="iv.campusName" class="text-xs text-muted-foreground">{{ iv.campusName }}</p>
                <p v-if="iv.conductedByName" class="text-xs text-muted-foreground">{{ t('crm.interview.conductedBy') }}: {{ iv.conductedByName }}</p>
              </div>
              <div v-if="can('interview:manage') && iv.status === 'scheduled'" class="flex gap-2">
                <button @click="openCompleteInterview(iv.id, iv.rowVersion)"
                  class="px-2 py-1 text-xs rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors">
                  {{ t('crm.interview.complete') }}
                </button>
                <button @click="activityStore.cancelInterview(iv.id, iv.rowVersion).then(() => activityStore.fetchInterviews(id))"
                  class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent transition-colors">
                  {{ t('crm.interview.cancel') }}
                </button>
              </div>
            </div>
            <div v-if="iv.outcome" class="mt-2 text-sm text-foreground">
              <span class="font-medium">{{ t('crm.interview.outcome') }}:</span> {{ iv.outcome }}
            </div>
          </div>
        </div>
      </div>

      <!-- History Tab -->
      <div v-else-if="activeTab === 'history'">
        <div v-if="activityStore.loading" class="space-y-2">
          <div v-for="i in 3" :key="i" class="h-12 rounded-xl bg-accent animate-pulse" />
        </div>
        <div v-else-if="activityStore.history.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('crm.history.noHistory') }}
        </div>
        <div v-else class="relative pl-4 border-l-2 border-border ml-4 space-y-4">
          <div v-for="h in activityStore.history" :key="h.id" class="relative">
            <div class="absolute -left-[1.35rem] w-3 h-3 rounded-full bg-primary border-2 border-background" />
            <div class="rounded-xl border border-border bg-[--color-card] p-3 shadow-sm ml-4">
              <p class="text-xs text-muted-foreground mb-1">{{ formatDateTime(h.changedAt) }} · {{ h.changedByName }}</p>
              <p class="text-sm text-foreground">
                <span class="font-medium">{{ h.previousStatusCode ?? '—' }}</span>
                → <span class="font-medium">{{ h.newStatusCode ?? '—' }}</span>
              </p>
              <p v-if="h.newPipelineStageCode" class="text-xs text-muted-foreground">
                {{ t('crm.lead.pipelineStage') }}: {{ h.newPipelineStageCode }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Change Status Modal -->
    <FormModal :open="showStatusModal" :title="t('crm.lead.changeStatus')" :saving="store.saving" @submit="submitStatus" @close="showStatusModal = false">
      <div class="space-y-4">
        <p v-if="statusError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ statusError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.status') }} *</label>
          <select v-model="statusForm.newStatusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="s in statuses" :key="s.id" :value="s.id">{{ s.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.pipelineStage') }}</label>
          <select v-model="statusForm.newPipelineStageId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="s in stages" :key="s.id" :value="s.id">{{ s.label }}</option>
          </select>
        </div>
      </div>
    </FormModal>

    <!-- Log Activity Modal -->
    <FormModal :open="showActivityModal" :title="t('crm.activity.log')" :saving="activityStore.saving" @submit="submitActivity" @close="showActivityModal = false">
      <div class="space-y-4">
        <p v-if="activityError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ activityError }}</p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.activity.type') }}</label>
            <select v-model="activityForm.activityTypeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="a in activityTypes" :key="a.id" :value="a.id">{{ a.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.activity.direction') }}</label>
            <select v-model="activityForm.direction" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="outbound">{{ t('crm.activity.outbound') }}</option>
              <option value="inbound">{{ t('crm.activity.inbound') }}</option>
            </select>
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.activity.subject') }}</label>
          <input v-model="activityForm.subject" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.activity.notes') }}</label>
          <textarea v-model="activityForm.body" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.activity.followUpAt') }}</label>
          <input v-model="activityForm.followUpAt" type="datetime-local" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>

    <!-- Schedule Interview Modal -->
    <FormModal :open="showInterviewModal" :title="t('crm.interview.schedule')" :saving="activityStore.saving" @submit="submitInterview" @close="showInterviewModal = false">
      <div class="space-y-4">
        <p v-if="interviewError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ interviewError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.interview.campus') }}</label>
          <select v-model="interviewForm.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.interview.scheduledAt') }}</label>
          <input v-model="interviewForm.scheduledAt" type="datetime-local" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
    </FormModal>

    <!-- Complete Interview Modal -->
    <FormModal :open="!!completeInterviewTarget" :title="t('crm.interview.complete')" :saving="activityStore.saving" @submit="submitCompleteInterview" @close="completeInterviewTarget = null">
      <div class="space-y-4">
        <p v-if="completeInterviewError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ completeInterviewError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.interview.outcome') }}</label>
          <textarea v-model="completeInterviewForm.outcome" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.interview.recommendation') }}</label>
          <textarea v-model="completeInterviewForm.recommendation" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>
    </FormModal>

    <!-- Delete Confirm -->
    <ConfirmModal
      :open="showDelete"
      :title="t('crm.lead.deleteTitle')"
      :message="t('crm.lead.deleteMessage', { name: lead?.contactName })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="showDelete = false"
    />
  </div>
</template>
