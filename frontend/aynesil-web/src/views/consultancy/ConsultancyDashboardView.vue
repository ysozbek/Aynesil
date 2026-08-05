<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()
const { can } = usePermission()

const upcomingVisits = computed(() =>
  consultancyStore.visits.items.filter(v => v.status !== 'Completed' && v.status !== 'Cancelled').slice(0, 5)
)
const pendingFollowUps = computed(() =>
  consultancyStore.followUps.items.filter(f => f.status === 'Open' || f.status === 'InProgress').length
)

function visitStatusClass(s: string) {
  const map: Record<string, string> = {
    Scheduled: 'bg-blue-100 text-blue-700',
    Completed: 'bg-green-100 text-green-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function followUpStatusClass(s: string) {
  const map: Record<string, string> = {
    Open: 'bg-amber-100 text-amber-700',
    InProgress: 'bg-sky-100 text-sky-700',
    Completed: 'bg-green-100 text-green-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

onMounted(async () => {
  const corp = authStore.user?.corporationId
  await Promise.all([
    consultancyStore.fetchInstitutions({ corporationId: corp }),
    consultancyStore.fetchPlans({ corporationId: corp, status: 'Active' }),
    consultancyStore.fetchVisits({ corporationId: corp, pageSize: 10 }),
    consultancyStore.fetchFollowUps({ corporationId: corp, status: 'Open', pageSize: 10 }),
  ])
})
</script>

<template>
  <div>
    <PageHeader :title="t('consultancy.dashboard.title')" :description="t('consultancy.dashboard.subtitle')">
      <button
        v-if="can('institution:create')"
        @click="router.push('/consultancy/institutions/new')"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('consultancy.institution.new') }}
      </button>
    </PageHeader>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm flex items-center gap-4">
        <div class="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center shrink-0">
          <svg class="w-5 h-5 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
        </div>
        <div>
          <p class="text-2xl font-bold text-primary">{{ consultancyStore.institutions.totalCount }}</p>
          <p class="text-xs text-muted-foreground mt-0.5">{{ t('consultancy.dashboard.totalInstitutions') }}</p>
        </div>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm flex items-center gap-4">
        <div class="w-10 h-10 rounded-lg bg-green-100 flex items-center justify-center shrink-0">
          <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <div>
          <p class="text-2xl font-bold text-green-600">{{ consultancyStore.plans.totalCount }}</p>
          <p class="text-xs text-muted-foreground mt-0.5">{{ t('consultancy.dashboard.activePlans') }}</p>
        </div>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm flex items-center gap-4">
        <div class="w-10 h-10 rounded-lg bg-amber-100 flex items-center justify-center shrink-0">
          <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>
        <div>
          <p class="text-2xl font-bold text-amber-600">{{ consultancyStore.visits.totalCount }}</p>
          <p class="text-xs text-muted-foreground mt-0.5">{{ t('consultancy.dashboard.totalVisits') }}</p>
        </div>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm flex items-center gap-4">
        <div class="w-10 h-10 rounded-lg bg-red-100 flex items-center justify-center shrink-0">
          <svg class="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <div>
          <p class="text-2xl font-bold text-red-600">{{ pendingFollowUps }}</p>
          <p class="text-xs text-muted-foreground mt-0.5">{{ t('consultancy.dashboard.openFollowUps') }}</p>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('consultancy.dashboard.upcomingVisits') }}</h3>
          <button @click="router.push('/consultancy/visits')" class="text-xs text-primary hover:underline">
            {{ t('common.viewAll') }}
          </button>
        </div>
        <div v-if="consultancyStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="consultancyStore.visits.items.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('consultancy.dashboard.noVisits') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="v in upcomingVisits"
            :key="v.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer"
            @click="router.push(`/consultancy/visits/${v.id}`)"
          >
            <div class="w-9 h-9 rounded-lg bg-primary/10 flex items-center justify-center shrink-0">
              <svg class="w-4 h-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ v.institutionName }}</p>
              <p class="text-xs text-muted-foreground">{{ v.planName ?? '—' }} · {{ v.visitDate }}</p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', visitStatusClass(v.status)]">
              {{ v.status }}
            </span>
          </div>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('consultancy.dashboard.openFollowUpsList') }}</h3>
          <button @click="router.push('/consultancy/follow-ups')" class="text-xs text-primary hover:underline">
            {{ t('common.viewAll') }}
          </button>
        </div>
        <div v-if="consultancyStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="consultancyStore.followUps.items.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('consultancy.dashboard.noFollowUps') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="f in consultancyStore.followUps.items.slice(0, 6)"
            :key="f.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer"
            @click="router.push(`/consultancy/follow-ups/${f.id}`)"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ f.title }}</p>
              <p class="text-xs text-muted-foreground">{{ f.planName ?? '—' }} · {{ f.dueDate ?? '—' }}</p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', followUpStatusClass(f.status)]">
              {{ f.status }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
