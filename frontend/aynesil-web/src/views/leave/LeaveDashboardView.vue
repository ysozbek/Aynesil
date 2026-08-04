<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const leaveStore = useLeaveStore()
const auth = useAuthStore()
const { can } = usePermission()

function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('tr-TR')
}

const pendingLeaves = computed(() =>
  leaveStore.leaveList.items.filter((l) => l.status === 'Pending')
)
const pendingCount = computed(() => pendingLeaves.value.length)
const approvedCount = computed(() =>
  leaveStore.leaveList.items.filter((l) => l.status === 'Approved').length
)
const upcomingCount = computed(() => {
  const now = new Date()
  return leaveStore.leaveList.items.filter(
    (l) => l.status === 'Approved' && new Date(l.startsAt) > now
  ).length
})

onMounted(async () => {
  const corp = auth.user?.corporationId
  await Promise.all([
    leaveStore.fetchLeaves({ corporationId: corp, pageSize: 50 }),
    leaveStore.fetchBalances({ corporationId: corp }),
  ])
})
</script>

<template>
  <div>
    <PageHeader :title="t('leave.dashboard.title')" :description="t('leave.dashboard.subtitle')">
      <button
        v-if="can('leave_request:submit')"
        @click="router.push({ name: 'leave-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('leave.request.new') }}
      </button>
    </PageHeader>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-amber-600">{{ pendingCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('leave.dashboard.pendingApprovals') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-green-600">{{ approvedCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('leave.dashboard.approvedThisMonth') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-primary">{{ upcomingCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('leave.dashboard.upcomingLeaves') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-sky-600">{{ leaveStore.balances.length }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('leave.dashboard.balanceRecords') }}</p>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2 rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('leave.dashboard.pendingRequests') }}</h3>
          <button
            @click="router.push({ name: 'leave-list', query: { status: 'Pending' } })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>

        <div v-if="leaveStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="pendingLeaves.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('leave.dashboard.noPending') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="item in pendingLeaves.slice(0, 8)"
            :key="item.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer"
            @click="router.push({ name: 'leave-detail', params: { id: item.id } })"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ item.educatorFullName ?? '—' }}</p>
              <p class="text-xs text-muted-foreground">
                {{ item.leaveTypeCode ?? '—' }} · {{ formatDate(item.startsAt) }} – {{ formatDate(item.endsAt) }}
              </p>
            </div>
            <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700">
              {{ t('leave.status.pending') }}
            </span>
          </div>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('leave.dashboard.balanceSummary') }}</h3>
          <button @click="router.push({ name: 'leave-balances' })" class="text-xs text-primary hover:underline">
            {{ t('common.viewAll') }}
          </button>
        </div>
        <div v-if="leaveStore.balances.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('leave.dashboard.noBalances') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="bal in leaveStore.balances.slice(0, 6)"
            :key="bal.id"
            class="flex items-center justify-between px-4 py-3"
          >
            <div class="min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ bal.educatorFullName }}</p>
              <p class="text-xs text-muted-foreground">{{ bal.leaveTypeCode }} · {{ bal.periodYear }}</p>
            </div>
            <div class="text-right shrink-0">
              <p class="text-sm font-semibold text-foreground">{{ bal.remaining }} / {{ bal.entitled }}</p>
              <p class="text-xs text-muted-foreground">{{ bal.unit }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
