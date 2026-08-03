<script setup lang="ts">
/**
 * CRM Reports — Conversion analysis by lead source and date range.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useBranchStore } from '@/stores/branch.store'
import { leadService } from '@/services/lead.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { ConversionReportDto } from '@/types/crm.types'

const { t } = useI18n()
const auth = useAuthStore()
const branchStore = useBranchStore()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const filters = reactive({
  campusId: '',
  from: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0],
  to: new Date().toISOString().split('T')[0],
})

const report = ref<ConversionReportDto | null>(null)
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  if (branchStore.list.items.length === 0) await branchStore.fetchList({ pageSize: 200 })
  await loadReport()
})

async function loadReport() {
  if (!corporationId.value) return
  loading.value = true
  error.value = ''
  try {
    const res = await leadService.getConversionReport({
      corporationId: corporationId.value,
      from: new Date(filters.from).toISOString(),
      to: new Date(filters.to + 'T23:59:59').toISOString(),
      campusId: filters.campusId || undefined,
    })
    if (res.success && res.data) report.value = res.data
  } catch (e: unknown) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

function pct(rate: number): string {
  return `${(rate * 100).toFixed(1)}%`
}

function barWidth(rate: number): string {
  return `${Math.min(100, rate * 100)}%`
}
</script>

<template>
  <div>
    <PageHeader :title="t('crm.reports.title')" :description="t('crm.reports.description')" />

    <!-- Filters -->
    <div class="mb-6 flex items-center gap-3 flex-wrap">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('crm.reports.from') }}</label>
        <input v-model="filters.from" type="date"
          class="h-9 px-3 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('crm.reports.to') }}</label>
        <input v-model="filters.to" type="date"
          class="h-9 px-3 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('campus.title') }}</label>
        <select v-model="filters.campusId"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
          <option value="">{{ t('common.allCampuses') }}</option>
          <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>
      <div class="self-end">
        <button @click="loadReport" :disabled="loading"
          class="h-9 px-4 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60">
          {{ t('common.filter') }}
        </button>
      </div>
    </div>

    <p v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3 mb-4">{{ error }}</p>

    <!-- Loading -->
    <div v-if="loading" class="space-y-4">
      <div class="h-24 rounded-xl bg-accent animate-pulse" />
      <div class="h-64 rounded-xl bg-accent animate-pulse" />
    </div>

    <template v-else-if="report">
      <!-- Summary KPIs -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-xs text-muted-foreground uppercase">{{ t('crm.reports.totalLeads') }}</p>
          <p class="text-3xl font-bold text-foreground mt-1">{{ report.totalLeads }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-xs text-muted-foreground uppercase">{{ t('crm.reports.converted') }}</p>
          <p class="text-3xl font-bold text-emerald-600 mt-1">{{ report.convertedLeads }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-xs text-muted-foreground uppercase">{{ t('crm.reports.conversionRate') }}</p>
          <p class="text-3xl font-bold text-primary mt-1">{{ pct(report.conversionRate) }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-xs text-muted-foreground uppercase">{{ t('crm.reports.sources') }}</p>
          <p class="text-3xl font-bold text-foreground mt-1">{{ report.bySource.length }}</p>
        </div>
      </div>

      <!-- By Source -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <h3 class="font-semibold text-foreground mb-4">{{ t('crm.reports.bySource') }}</h3>
        <div v-if="report.bySource.length === 0" class="text-sm text-muted-foreground text-center py-8">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-4">
          <div v-for="src in report.bySource" :key="src.sourceId">
            <div class="flex items-center justify-between text-sm mb-1">
              <span class="font-medium text-foreground">{{ src.sourceName }}</span>
              <span class="text-muted-foreground">
                {{ src.converted }}/{{ src.total }} · <strong class="text-primary">{{ pct(src.rate) }}</strong>
              </span>
            </div>
            <div class="h-2.5 rounded-full bg-accent overflow-hidden">
              <div
                class="h-full rounded-full bg-primary transition-all"
                :style="{ width: barWidth(src.rate) }"
              />
            </div>
          </div>
        </div>
      </div>
    </template>

    <div v-else class="text-center py-24 text-muted-foreground text-sm">
      {{ t('crm.reports.noReport') }}
    </div>
  </div>
</template>
