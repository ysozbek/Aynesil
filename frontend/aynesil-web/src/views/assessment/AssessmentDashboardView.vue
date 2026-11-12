<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAssessmentStore } from '@/stores/assessment.store'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { usePermission } from '@/composables/usePermission'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const sessionStore = useAssessmentStore()
const templateStore = useAssessmentTemplateStore()
const { can } = usePermission()

onMounted(async () => {
  await Promise.all([
    sessionStore.fetchList({ page: 1, pageSize: 5, sortDirection: 'desc' }),
    templateStore.fetchList({ isActive: true, page: 1, pageSize: 5 }),
  ])
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

const recentSessions = computed(() => sessionStore.list.items)
const pendingCount = computed(() => sessionStore.list.items.filter(s => s.status === 'planned' || s.status === 'in_progress').length)
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-foreground">{{ t('assessment.dashboard.title') }}</h1>
        <p class="text-sm text-muted-foreground mt-1">{{ t('assessment.dashboard.description') }}</p>
      </div>
      <button
        v-if="can(Permissions.AssessmentSessions.Create)"
        @click="router.push({ name: 'assessment-sessions-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('assessment.session.create') }}
      </button>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.totalSessions') }}</p>
        <p class="mt-2 text-3xl font-bold text-foreground">
          <span v-if="sessionStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ sessionStore.list.totalCount }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.pending') }}</p>
        <p class="mt-2 text-3xl font-bold text-amber-600">
          <span v-if="sessionStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ pendingCount }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.activeTemplates') }}</p>
        <p class="mt-2 text-3xl font-bold text-foreground">
          <span v-if="templateStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ templateStore.list.totalCount }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.completedSessions') }}</p>
        <p class="mt-2 text-3xl font-bold text-emerald-600">
          <span v-if="sessionStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ sessionStore.list.items.filter(s => s.status === 'completed').length }}</span>
        </p>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- Recent Sessions -->
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm">
        <h3 class="text-sm font-semibold text-foreground mb-4">{{ t('assessment.dashboard.recentSessions') }}</h3>
        <div v-if="sessionStore.loading" class="space-y-3">
          <div v-for="i in 3" :key="i" class="h-12 rounded bg-accent animate-pulse" />
        </div>
        <div v-else-if="recentSessions.length === 0" class="text-sm text-muted-foreground text-center py-6">
          {{ t('common.noData') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div v-for="s in recentSessions" :key="s.id"
            class="py-3 cursor-pointer hover:bg-accent/20 -mx-3 px-3 rounded-lg transition-colors"
            @click="router.push({ name: 'assessment-session-detail', params: { id: s.id } })">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-medium text-foreground truncate max-w-[180px]">
                  {{ s.templateName ?? '—' }}
                </p>
                <p class="text-xs text-muted-foreground">{{ s.templateName }}</p>
              </div>
              <StatusBadge entity="assessmentSession" :status="s.status" />
            </div>
          </div>
        </div>
        <div class="mt-4 pt-3 border-t border-border">
          <button @click="router.push({ name: 'assessment-sessions' })" class="text-sm text-primary hover:underline">
            {{ t('common.viewAll') }} →
          </button>
        </div>
      </div>

      <!-- Active Templates -->
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm">
        <h3 class="text-sm font-semibold text-foreground mb-4">{{ t('assessment.dashboard.activeTemplates') }}</h3>
        <div v-if="templateStore.loading" class="space-y-3">
          <div v-for="i in 3" :key="i" class="h-12 rounded bg-accent animate-pulse" />
        </div>
        <div v-else-if="templateStore.list.items.length === 0" class="text-sm text-muted-foreground text-center py-6">
          {{ t('common.noData') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div v-for="tpl in templateStore.list.items" :key="tpl.id"
            class="py-3 cursor-pointer hover:bg-accent/20 -mx-3 px-3 rounded-lg transition-colors"
            @click="router.push({ name: 'assessment-template-detail', params: { id: tpl.id } })">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-medium text-foreground">{{ tpl.name }}</p>
                <p class="text-xs text-muted-foreground">
                  {{ tpl.typeCode ?? '—' }} · v{{ tpl.version }}
                </p>
              </div>
              <span class="text-xs text-muted-foreground">{{ tpl.sectionCount }} {{ t('assessment.template.sections') }}</span>
            </div>
          </div>
        </div>
        <div class="mt-4 pt-3 border-t border-border">
          <button @click="router.push({ name: 'assessment-templates' })" class="text-sm text-primary hover:underline">
            {{ t('common.viewAll') }} →
          </button>
        </div>
      </div>
    </div>

    <!-- Quick Nav -->
    <div class="mt-6 grid grid-cols-2 md:grid-cols-4 gap-4">
      <button v-if="can(Permissions.AssessmentSessions.Read)"
        @click="router.push({ name: 'assessment-sessions' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-card hover:bg-accent/30 transition-colors text-sm font-medium text-foreground">
        <svg class="w-6 h-6 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        {{ t('assessment.nav.sessions') }}
      </button>
      <button v-if="can(Permissions.AssessmentTemplates.Read)"
        @click="router.push({ name: 'assessment-templates' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-card hover:bg-accent/30 transition-colors text-sm font-medium text-foreground">
        <svg class="w-6 h-6 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 6h16M4 12h16M4 18h7" />
        </svg>
        {{ t('assessment.nav.templates') }}
      </button>
    </div>
  </div>
</template>
