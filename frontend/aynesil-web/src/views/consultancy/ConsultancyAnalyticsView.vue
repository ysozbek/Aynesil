<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useModule } from '@/composables/useModule'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'
import { Modules } from '@/constants/modules'
import type { PermissionCode } from '@/constants/permissions'

type TabKey = 'agreements' | 'institutions' | 'outcomes' | 'visitHistory'

const { t } = useI18n()
const router = useRouter()
const store = useConsultancyStore()
const { visible } = useModule(Modules.Consultancy)

// Her sekme kendi ucunun iznine bağlı; izni olmayan sekme hiç görünmez.
const tabDefs: { key: TabKey; permission: PermissionCode }[] = [
  { key: 'agreements', permission: Permissions.ConsultancyAgreements.Read },
  { key: 'institutions', permission: Permissions.Institutions.Read },
  { key: 'outcomes', permission: Permissions.ConsultancyPlans.Read },
  { key: 'visitHistory', permission: Permissions.SchoolVisits.Read },
]
const tabs = computed(() => tabDefs.filter((d) => visible(d.permission)))
const active = ref<TabKey>('agreements')

const filters = reactive({ institutionId: '', planStatus: '', dateFrom: '', dateTo: '' })
const error = ref('')

function formatDate(val?: string | null) {
  return val ? new Date(`${val}T00:00:00`).toLocaleDateString('tr-TR') : '—'
}

async function load() {
  error.value = ''
  store.error = null
  const inst = filters.institutionId || undefined
  switch (active.value) {
    case 'agreements':
      await store.fetchAgreementSummary({ institutionId: inst })
      break
    case 'institutions':
      await store.fetchInstitutionReport({})
      break
    case 'outcomes':
      await store.fetchOutcomes({ institutionId: inst, status: filters.planStatus || undefined })
      break
    case 'visitHistory':
      await store.fetchVisitHistory({
        institutionId: inst,
        dateFrom: filters.dateFrom || undefined,
        dateTo: filters.dateTo || undefined,
      })
      break
  }
  error.value = store.error ?? ''
}

watch(active, load)

onMounted(async () => {
  if (tabs.value.length && !tabs.value.some((x) => x.key === active.value)) active.value = tabs.value[0].key
  if (visible(Permissions.Institutions.Read)) await store.fetchInstitutions({ page: 1, pageSize: 200 })
  await load()
})

const showInstitutionFilter = computed(() => active.value !== 'institutions')
</script>

<template>
  <div>
    <PageHeader :title="t('consultancyAnalytics.title')" :description="t('consultancyAnalytics.subtitle')" />

    <div class="mb-4 flex flex-wrap gap-2 border-b border-border">
      <button
        v-for="tab in tabs"
        :key="tab.key"
        type="button"
        class="px-4 py-2 text-sm -mb-px border-b-2"
        :class="active === tab.key ? 'border-primary text-primary font-medium' : 'border-transparent text-muted-foreground hover:text-foreground'"
        @click="active = tab.key"
      >
        {{ t(`consultancyAnalytics.tabs.${tab.key}`) }}
      </button>
    </div>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div v-if="showInstitutionFilter && visible(Permissions.Institutions.Read)">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('consultancyAnalytics.columns.institution') }}</label>
        <select v-model="filters.institutionId" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="load">
          <option value="">{{ t('consultancyAnalytics.allInstitutions') }}</option>
          <option v-for="i in store.institutions.items" :key="i.id" :value="i.id">{{ i.name }}</option>
        </select>
      </div>
      <div v-if="active === 'outcomes'">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.planStatus" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="load">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="draft">{{ t('statusBadge.consultancyPlan.draft') }}</option>
          <option value="active">{{ t('statusBadge.consultancyPlan.active') }}</option>
          <option value="completed">{{ t('statusBadge.consultancyPlan.completed') }}</option>
          <option value="cancelled">{{ t('statusBadge.consultancyPlan.cancelled') }}</option>
        </select>
      </div>
      <template v-if="active === 'visitHistory'">
        <div>
          <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.from') }}</label>
          <input v-model="filters.dateFrom" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="load" />
        </div>
        <div>
          <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.to') }}</label>
          <input v-model="filters.dateTo" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="load" />
        </div>
      </template>
    </div>

    <p v-if="error" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ error }}</p>
    <div v-if="store.loading" class="py-12 text-center text-sm text-muted-foreground">{{ t('common.loading') }}</div>

    <div v-else class="bg-white rounded-xl border border-border overflow-x-auto">
      <!-- Sözleşme özeti -->
      <table v-if="active === 'agreements'" class="w-full text-sm">
        <thead class="bg-muted/40 text-left text-xs text-muted-foreground">
          <tr>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.plan') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.institution') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.totalAgreements') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.draft') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.sent') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.signed') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.expired') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.cancelled') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in store.agreementSummary" :key="r.planId" class="border-t border-border hover:bg-accent/40 cursor-pointer"
              @click="router.push({ name: 'consultancy-plan-detail', params: { id: r.planId } })">
            <td class="px-4 py-2 font-medium">{{ r.planName }}</td>
            <td class="px-4 py-2 text-muted-foreground">{{ r.institutionName }}</td>
            <td class="px-4 py-2 text-right">{{ r.totalAgreements }}</td>
            <td class="px-4 py-2 text-right">{{ r.draftCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.sentCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.signedCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.expiredCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.cancelledCount }}</td>
          </tr>
          <tr v-if="!store.agreementSummary.length">
            <td colspan="8" class="px-4 py-10 text-center text-muted-foreground">{{ t('consultancyAnalytics.empty') }}</td>
          </tr>
        </tbody>
      </table>

      <!-- Okul (kurum) raporu -->
      <table v-else-if="active === 'institutions'" class="w-full text-sm">
        <thead class="bg-muted/40 text-left text-xs text-muted-foreground">
          <tr>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.institution') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.city') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.totalPlans') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.activePlans') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.completedPlans') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.totalVisits') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.completedVisits') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.totalObservations') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.totalReports') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in store.institutionReport" :key="r.institutionId" class="border-t border-border">
            <td class="px-4 py-2 font-medium">{{ r.institutionName }}</td>
            <td class="px-4 py-2 text-muted-foreground">{{ r.city ?? '—' }}</td>
            <td class="px-4 py-2 text-right">{{ r.totalPlans }}</td>
            <td class="px-4 py-2 text-right">{{ r.activePlans }}</td>
            <td class="px-4 py-2 text-right">{{ r.completedPlans }}</td>
            <td class="px-4 py-2 text-right">{{ r.totalVisits }}</td>
            <td class="px-4 py-2 text-right">{{ r.completedVisits }}</td>
            <td class="px-4 py-2 text-right">{{ r.totalObservations }}</td>
            <td class="px-4 py-2 text-right">{{ r.totalReports }}</td>
          </tr>
          <tr v-if="!store.institutionReport.length">
            <td colspan="9" class="px-4 py-10 text-center text-muted-foreground">{{ t('consultancyAnalytics.empty') }}</td>
          </tr>
        </tbody>
      </table>

      <!-- Sonuçlar -->
      <table v-else-if="active === 'outcomes'" class="w-full text-sm">
        <thead class="bg-muted/40 text-left text-xs text-muted-foreground">
          <tr>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.plan') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.institution') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.period') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.status') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.visits') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.completedVisitCount') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.observations') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.reports') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in store.outcomes" :key="r.planId" class="border-t border-border hover:bg-accent/40 cursor-pointer"
              @click="router.push({ name: 'consultancy-plan-detail', params: { id: r.planId } })">
            <td class="px-4 py-2 font-medium">{{ r.planName }}</td>
            <td class="px-4 py-2 text-muted-foreground">{{ r.institutionName }}</td>
            <td class="px-4 py-2 text-muted-foreground">{{ formatDate(r.periodStart) }} — {{ formatDate(r.periodEnd) }}</td>
            <td class="px-4 py-2"><StatusBadge entity="consultancyPlan" :status="r.status" /></td>
            <td class="px-4 py-2 text-right">{{ r.visitCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.completedVisitCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.observationCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.reportCount }}</td>
          </tr>
          <tr v-if="!store.outcomes.length">
            <td colspan="8" class="px-4 py-10 text-center text-muted-foreground">{{ t('consultancyAnalytics.empty') }}</td>
          </tr>
        </tbody>
      </table>

      <!-- Ziyaret geçmişi -->
      <table v-else class="w-full text-sm">
        <thead class="bg-muted/40 text-left text-xs text-muted-foreground">
          <tr>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.date') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.institution') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.plan') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.purpose') }}</th>
            <th class="px-4 py-2">{{ t('consultancyAnalytics.columns.status') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.observations') }}</th>
            <th class="px-4 py-2 text-right">{{ t('consultancyAnalytics.columns.reports') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in store.visitHistory" :key="r.visitId" class="border-t border-border hover:bg-accent/40 cursor-pointer"
              @click="router.push({ name: 'consultancy-visit-detail', params: { id: r.visitId } })">
            <td class="px-4 py-2 whitespace-nowrap">{{ formatDate(r.visitDate) }}</td>
            <td class="px-4 py-2 font-medium">{{ r.institutionName }}</td>
            <td class="px-4 py-2 text-muted-foreground">{{ r.planName ?? '—' }}</td>
            <td class="px-4 py-2 text-muted-foreground max-w-[240px] truncate">{{ r.purpose ?? '—' }}</td>
            <td class="px-4 py-2"><StatusBadge entity="schoolVisit" :status="r.status" /></td>
            <td class="px-4 py-2 text-right">{{ r.observationCount }}</td>
            <td class="px-4 py-2 text-right">{{ r.reportCount }}</td>
          </tr>
          <tr v-if="!store.visitHistory.length">
            <td colspan="7" class="px-4 py-10 text-center text-muted-foreground">{{ t('consultancyAnalytics.empty') }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
