<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useLeadPipelineStore } from '@/stores/leadPipeline.store'
import { useLeadActivityStore } from '@/stores/leadActivity.store'
import { usePermission } from '@/composables/usePermission'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const pipelineStore = useLeadPipelineStore()
const activityStore = useLeadActivityStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const statsLoading = ref(false)

onMounted(async () => {
  if (!corporationId.value) return
  statsLoading.value = true
  try {
    await Promise.all([
      pipelineStore.fetchSummary(corporationId.value),
      activityStore.fetchFollowUps({ corporationId: corporationId.value, pageSize: 5 }),
    ])
  } finally {
    statsLoading.value = false
  }
})

const totalConverted = computed(() => pipelineStore.summary?.convertedLeads ?? 0)
const totalLost = computed(() => pipelineStore.summary?.lostLeads ?? 0)
const stages = computed(() => pipelineStore.summary?.stages ?? [])
const followUps = computed(() => activityStore.followUps.items)

function formatDateTime(date: string | null): string {
  if (!date) return '-'
  return new Date(date).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-foreground">{{ t('crm.dashboard.title') }}</h1>
        <p class="text-sm text-muted-foreground mt-1">{{ t('crm.dashboard.description') }}</p>
      </div>
      <button
        v-if="can('lead:create')"
        @click="router.push({ name: 'leads-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('crm.lead.create') }}
      </button>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('crm.dashboard.totalLeads') }}</p>
        <p class="mt-2 text-3xl font-bold text-foreground">
          <span v-if="statsLoading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ pipelineStore.summary ? (pipelineStore.summary.totalLeads) : '-' }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('crm.dashboard.convertedLeads') }}</p>
        <p class="mt-2 text-3xl font-bold text-emerald-600">
          <span v-if="statsLoading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ totalConverted }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('crm.dashboard.lostLeads') }}</p>
        <p class="mt-2 text-3xl font-bold text-red-500">
          <span v-if="statsLoading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ totalLost }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('crm.dashboard.followUpsDue') }}</p>
        <p class="mt-2 text-3xl font-bold text-amber-600">
          <span v-if="statsLoading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ activityStore.followUps.totalCount }}</span>
        </p>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- Pipeline Funnel -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <h3 class="text-sm font-semibold text-foreground mb-4">{{ t('crm.dashboard.pipelineSummary') }}</h3>
        <div v-if="pipelineStore.loading" class="space-y-2">
          <div v-for="i in 4" :key="i" class="h-8 rounded bg-accent animate-pulse" />
        </div>
        <div v-else-if="stages.length === 0" class="text-sm text-muted-foreground text-center py-6">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div v-for="stage in stages" :key="stage.stageId">
            <div class="flex items-center justify-between text-sm mb-1">
              <span class="text-foreground font-medium">{{ stage.stageName }}</span>
              <span class="text-muted-foreground">{{ stage.count }}</span>
            </div>
            <div class="h-2 rounded-full bg-accent overflow-hidden">
              <div
                class="h-full rounded-full bg-primary transition-all"
                :style="{ width: pipelineStore.summary && pipelineStore.summary.totalLeads > 0 ? `${Math.round((stage.count / pipelineStore.summary.totalLeads) * 100)}%` : '0%' }"
              />
            </div>
          </div>
        </div>
        <div class="mt-4 pt-3 border-t border-border">
          <button @click="router.push({ name: 'crm-pipeline' })" class="text-sm text-primary hover:underline">
            {{ t('common.viewAll') }} →
          </button>
        </div>
      </div>

      <!-- Upcoming Follow-Ups -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <h3 class="text-sm font-semibold text-foreground mb-4">{{ t('crm.dashboard.upcomingFollowUps') }}</h3>
        <div v-if="activityStore.loading" class="space-y-2">
          <div v-for="i in 3" :key="i" class="h-12 rounded bg-accent animate-pulse" />
        </div>
        <div v-else-if="followUps.length === 0" class="text-sm text-muted-foreground text-center py-6">
          {{ t('crm.dashboard.noFollowUps') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div v-for="item in followUps" :key="item.id" class="py-3">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-sm font-medium text-foreground">{{ item.subject ?? t('crm.activity.noSubject') }}</p>
                <p class="text-xs text-muted-foreground">{{ item.activityTypeName }}</p>
              </div>
              <span class="text-xs text-amber-600 font-medium whitespace-nowrap ml-3">
                {{ formatDateTime(item.followUpAt) }}
              </span>
            </div>
          </div>
        </div>
        <div class="mt-4 pt-3 border-t border-border">
          <button @click="router.push({ name: 'crm-activities' })" class="text-sm text-primary hover:underline">
            {{ t('common.viewAll') }} →
          </button>
        </div>
      </div>
    </div>

    <!-- Quick Nav -->
    <div class="mt-6 grid grid-cols-2 md:grid-cols-4 gap-4">
      <button
        v-if="can('lead:read')"
        @click="router.push({ name: 'leads' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-[--color-card] hover:bg-accent/30 transition-colors text-sm font-medium text-foreground"
      >
        <svg class="w-6 h-6 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
        {{ t('crm.nav.leads') }}
      </button>
      <button
        v-if="can('lead:read')"
        @click="router.push({ name: 'crm-pipeline' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-[--color-card] hover:bg-accent/30 transition-colors text-sm font-medium text-foreground"
      >
        <svg class="w-6 h-6 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" />
        </svg>
        {{ t('crm.nav.pipeline') }}
      </button>
      <button
        v-if="can('lead_activity:read')"
        @click="router.push({ name: 'crm-activities' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-[--color-card] hover:bg-accent/30 transition-colors text-sm font-medium text-foreground"
      >
        <svg class="w-6 h-6 text-emerald-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
        </svg>
        {{ t('crm.nav.activities') }}
      </button>
      <button
        v-if="can('lead:read')"
        @click="router.push({ name: 'crm-reports' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-[--color-card] hover:bg-accent/30 transition-colors text-sm font-medium text-foreground"
      >
        <svg class="w-6 h-6 text-violet-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
        {{ t('crm.nav.reports') }}
      </button>
    </div>
  </div>
</template>
