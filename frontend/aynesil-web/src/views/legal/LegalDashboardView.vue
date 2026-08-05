<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { useConsentStore } from '@/stores/consent.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const contractStore = useContractStore()
const consentStore = useConsentStore()
const auth = useAuthStore()
const { can } = usePermission()

const pendingSignatures = computed(() =>
  contractStore.contracts.items.filter((c) => c.status === 'Sent').length
)
const activeContracts = computed(() =>
  contractStore.contracts.items.filter((c) => c.status === 'Active').length
)
const grantedConsents = computed(() =>
  consentStore.consents.items.filter((c) => c.state === 'Granted').length
)
const expiringContracts = computed(() => {
  const thirtyDays = Date.now() + 30 * 24 * 60 * 60 * 1000
  return contractStore.contracts.items.filter(
    (c) => c.status === 'Active' && c.endsOn && new Date(c.endsOn).getTime() < thirtyDays
  ).length
})

function contractStatusClass(s: string) {
  const map: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-600',
    Sent: 'bg-amber-100 text-amber-700',
    Active: 'bg-green-100 text-green-700',
    Expired: 'bg-gray-100 text-gray-700',
    Terminated: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function consentStateClass(s: string) {
  const map: Record<string, string> = {
    Granted: 'bg-green-100 text-green-700',
    Withdrawn: 'bg-red-100 text-red-700',
    Pending: 'bg-amber-100 text-amber-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function contractStatusLabel(s: string) {
  const key = s.toLowerCase()
  return t(`legal.contract.status.${key}`, s)
}

function consentStateLabel(s: string) {
  const key = s.toLowerCase()
  return t(`legal.consent.state.${key}`, s)
}

onMounted(async () => {
  const corp = auth.user?.corporationId
  await Promise.all([
    contractStore.fetchContracts({ corporationId: corp, pageSize: 20 }),
    consentStore.fetchConsents({ corporationId: corp, pageSize: 20 }),
  ])
})
</script>

<template>
  <div>
    <PageHeader :title="t('legal.dashboard.title')" :description="t('legal.dashboard.subtitle')">
      <button
        v-if="can('student_contract:generate')"
        @click="router.push({ name: 'contract-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('legal.contract.new') }}
      </button>
    </PageHeader>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-amber-600">{{ pendingSignatures }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('legal.dashboard.pendingSignatures') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-green-600">{{ activeContracts }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('legal.dashboard.activeContracts') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-sky-600">{{ grantedConsents }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('legal.dashboard.grantedConsents') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-red-600">{{ expiringContracts }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('legal.dashboard.expiringSoon') }}</p>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('legal.dashboard.recentContracts') }}</h3>
          <button
            @click="router.push({ name: 'contracts' })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>

        <div v-if="contractStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="contractStore.contracts.items.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('legal.dashboard.noContracts') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="c in contractStore.contracts.items.slice(0, 6)"
            :key="c.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
            @click="router.push({ name: 'contract-detail', params: { id: c.id } })"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ c.studentFullName ?? '—' }}</p>
              <p class="text-xs text-muted-foreground">
                {{ c.templateCode ?? '—' }}<template v-if="c.templateVersion"> v{{ c.templateVersion }}</template>
              </p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', contractStatusClass(c.status)]">
              {{ contractStatusLabel(c.status) }}
            </span>
          </div>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('legal.dashboard.consentStatus') }}</h3>
          <button
            @click="router.push({ name: 'consents' })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>

        <div v-if="consentStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="consentStore.consents.items.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('legal.dashboard.noConsents') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="c in consentStore.consents.items.slice(0, 6)"
            :key="c.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer transition-colors"
            @click="router.push({ name: 'consent-detail', params: { id: c.id } })"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ c.studentFullName ?? '—' }}</p>
              <p class="text-xs text-muted-foreground">{{ c.consentTypeCode ?? '—' }}</p>
            </div>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium shrink-0', consentStateClass(c.state)]">
              {{ consentStateLabel(c.state) }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
