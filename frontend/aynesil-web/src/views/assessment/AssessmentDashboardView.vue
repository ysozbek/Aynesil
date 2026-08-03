<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useAssessmentStore } from '@/stores/assessment.store'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { usePermission } from '@/composables/usePermission'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const sessionStore = useAssessmentStore()
const templateStore = useAssessmentTemplateStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

onMounted(async () => {
  if (!corporationId.value) return
  await Promise.all([
    sessionStore.fetchList({ corporationId: corporationId.value, page: 1, pageSize: 5, sortDirection: 'desc' }),
    templateStore.fetchList({ corporationId: corporationId.value, isActive: true, page: 1, pageSize: 5 }),
  ])
})

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
        v-if="can('assessment_session:create')"
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
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.totalSessions') }}</p>
        <p class="mt-2 text-3xl font-bold text-foreground">
          <span v-if="sessionStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ sessionStore.list.totalCount }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.pending') }}</p>
        <p class="mt-2 text-3xl font-bold text-amber-600">
          <span v-if="sessionStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ pendingCount }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.activeTemplates') }}</p>
        <p class="mt-2 text-3xl font-bold text-foreground">
          <span v-if="templateStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ templateStore.list.totalCount }}</span>
        </p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide">{{ t('assessment.dashboard.completedSessions') }}</p>
        <p class="mt-2 text-3xl font-bold text-emerald-600">
          <span v-if="sessionStore.loading" class="inline-block h-8 w-16 rounded bg-accent animate-pulse" />
          <span v-else>{{ sessionStore.list.items.filter(s => s.status === 'completed').length }}</span>
        </p>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- Recent Sessions -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
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
                  {{ s.leadContactName ?? s.studentName ?? '—' }}
                </p>
                <p class="text-xs text-muted-foreground">{{ s.templateName }}</p>
              </div>
              <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(s.status)]">
                {{ t(`assessment.session.status.${s.status}`) }}
              </span>
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
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
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
                  {{ tpl.typeName ?? '—' }} · v{{ tpl.version }}
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
      <button v-if="can('assessment_session:read')"
        @click="router.push({ name: 'assessment-sessions' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-[--color-card] hover:bg-accent/30 transition-colors text-sm font-medium text-foreground">
        <svg class="w-6 h-6 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        {{ t('assessment.nav.sessions') }}
      </button>
      <button v-if="can('assessment_template:read')"
        @click="router.push({ name: 'assessment-templates' })"
        class="flex flex-col items-center gap-2 p-4 rounded-xl border border-border bg-[--color-card] hover:bg-accent/30 transition-colors text-sm font-medium text-foreground">
        <svg class="w-6 h-6 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 6h16M4 12h16M4 18h7" />
        </svg>
        {{ t('assessment.nav.templates') }}
      </button>
    </div>
  </div>
</template>
