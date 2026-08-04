<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { usePackageStore } from '@/stores/package.store'
import { usePaymentStore } from '@/stores/payment.store'
import { useInvoiceStore } from '@/stores/invoice.store'
import { financeService } from '@/services/finance.service'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RevenueReportDto, PackageReportDto } from '@/types/finance.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const packageStore = usePackageStore()
const paymentStore = usePaymentStore()
const invoiceStore = useInvoiceStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const today = new Date()
const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().slice(0, 10)
const todayStr = today.toISOString().slice(0, 10)

const revenueReport = ref<RevenueReportDto | null>(null)
const packageReport = ref<PackageReportDto | null>(null)
const reportLoading = ref(false)
const reportError = ref<string | null>(null)

const reportQuery = reactive({
  from: firstOfMonth,
  to: todayStr,
})

onMounted(async () => {
  await Promise.all([
    loadReports(),
    paymentStore.fetchTransactions({ corporationId: corporationId.value, page: 1, pageSize: 5, status: 'pending' }),
    invoiceStore.fetchInvoices({ corporationId: corporationId.value, page: 1, pageSize: 5, status: 'overdue' }),
  ])
})

async function loadReports() {
  reportLoading.value = true
  reportError.value = null
  try {
    const [rev, pkg] = await Promise.all([
      financeService.getRevenueReport({ corporationId: corporationId.value, ...reportQuery }),
      financeService.getPackageReport({ corporationId: corporationId.value, ...reportQuery }),
    ])
    if (rev.success) revenueReport.value = rev.data ?? null
    if (pkg.success) packageReport.value = pkg.data ?? null
  } catch (e: unknown) {
    reportError.value = (e as Error).message
  } finally {
    reportLoading.value = false
  }
}

function formatCurrency(val: number, currency = 'TRY'): string {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency }).format(val)
}

function formatDate(val: string): string {
  return new Date(val).toLocaleDateString('tr-TR')
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.dashboard.title')" :description="t('finance.dashboard.description')">
      <div class="flex items-center gap-2">
        <button
          v-if="can('payment:create')"
          @click="router.push({ name: 'payment-new' })"
          class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('finance.payment.create') }}
        </button>
      </div>
    </PageHeader>

    <!-- Quick Nav Cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <button @click="router.push({ name: 'packages' })" class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40">
        <div class="w-9 h-9 rounded-lg bg-blue-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('finance.nav.packages') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('finance.package.title') }}</p>
      </button>

      <button @click="router.push({ name: 'invoices' })" class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40">
        <div class="w-9 h-9 rounded-lg bg-green-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('finance.nav.invoices') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('finance.invoice.title') }}</p>
      </button>

      <button @click="router.push({ name: 'payments' })" class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40">
        <div class="w-9 h-9 rounded-lg bg-violet-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-violet-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('finance.nav.payments') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('finance.payment.title') }}</p>
      </button>

      <button @click="router.push({ name: 'credit-ledger' })" class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-left hover:border-primary/40">
        <div class="w-9 h-9 rounded-lg bg-amber-100 flex items-center justify-center mb-3">
          <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <p class="font-semibold text-foreground text-sm">{{ t('finance.nav.credits') }}</p>
        <p class="text-xs text-muted-foreground mt-0.5">{{ t('finance.credit.title') }}</p>
      </button>
    </div>

    <!-- Report Period Selector -->
    <div class="mb-4 flex items-center gap-3">
      <label class="text-sm text-muted-foreground">{{ t('finance.dashboard.period') }}:</label>
      <input v-model="reportQuery.from" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      <span class="text-muted-foreground">–</span>
      <input v-model="reportQuery.to" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      <button @click="loadReports" :disabled="reportLoading" class="px-3 h-9 text-sm border border-border rounded-lg hover:bg-accent disabled:opacity-50">
        {{ reportLoading ? t('common.loading') : t('common.filter') }}
      </button>
    </div>

    <div v-if="reportLoading" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div v-for="i in 2" :key="i" class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <div v-else class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Revenue Report -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('finance.dashboard.revenueSummary') }}</h3>
        </div>
        <div v-if="!revenueReport" class="py-8 text-center text-muted-foreground text-sm">{{ t('common.noData') }}</div>
        <div v-else class="p-5">
          <div class="grid grid-cols-2 gap-4 mb-4">
            <div class="text-center">
              <p class="text-2xl font-bold text-foreground">{{ formatCurrency(revenueReport.totalRevenue) }}</p>
              <p class="text-xs text-muted-foreground mt-1">{{ t('finance.dashboard.totalRevenue') }}</p>
            </div>
            <div class="text-center">
              <p class="text-2xl font-bold text-blue-600">{{ revenueReport.totalTransactions }}</p>
              <p class="text-xs text-muted-foreground mt-1">{{ t('finance.dashboard.totalTransactions') }}</p>
            </div>
          </div>
          <div v-if="revenueReport.byMethod.length > 0" class="space-y-2">
            <p class="text-xs font-medium text-muted-foreground uppercase">{{ t('finance.dashboard.byMethod') }}</p>
            <div v-for="m in revenueReport.byMethod" :key="m.paymentMethodLabel" class="flex items-center justify-between text-sm">
              <span>{{ m.paymentMethodLabel }}</span>
              <span class="font-medium">{{ formatCurrency(m.totalAmount) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Package Report -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('finance.dashboard.packageSummary') }}</h3>
        </div>
        <div v-if="!packageReport" class="py-8 text-center text-muted-foreground text-sm">{{ t('common.noData') }}</div>
        <div v-else class="p-5">
          <div class="grid grid-cols-2 gap-4 mb-4">
            <div class="text-center">
              <p class="text-2xl font-bold text-foreground">{{ packageReport.totalPackagesSold }}</p>
              <p class="text-xs text-muted-foreground mt-1">{{ t('finance.dashboard.packagesSold') }}</p>
            </div>
            <div class="text-center">
              <p class="text-2xl font-bold text-green-600">{{ formatCurrency(packageReport.totalRevenue) }}</p>
              <p class="text-xs text-muted-foreground mt-1">{{ t('finance.dashboard.packageRevenue') }}</p>
            </div>
          </div>
          <div v-if="packageReport.topPackages.length > 0" class="space-y-2">
            <p class="text-xs font-medium text-muted-foreground uppercase">{{ t('finance.dashboard.topPackages') }}</p>
            <div v-for="p in packageReport.topPackages" :key="p.packageName" class="flex items-center justify-between text-sm">
              <span class="truncate flex-1">{{ p.packageName }}</span>
              <span class="font-medium ml-2">{{ p.soldCount }} {{ t('finance.dashboard.sold') }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Outstanding Invoices -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('finance.dashboard.overdueInvoices') }}</h3>
          <button @click="router.push({ name: 'invoices', query: { status: 'overdue' } })" class="text-xs text-primary hover:underline">{{ t('common.viewAll') }}</button>
        </div>
        <div v-if="invoiceStore.loading" class="p-4 space-y-2">
          <div v-for="i in 3" :key="i" class="h-10 rounded bg-accent animate-pulse" />
        </div>
        <div v-else-if="invoiceStore.invoiceList.items.length === 0" class="py-8 text-center text-muted-foreground text-sm">
          {{ t('finance.dashboard.noOverdueInvoices') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="inv in invoiceStore.invoiceList.items"
            :key="inv.id"
            class="flex items-center justify-between px-4 py-3 hover:bg-accent/30 cursor-pointer"
            @click="router.push({ name: 'invoice-detail', params: { id: inv.id } })"
          >
            <div>
              <p class="text-sm font-medium text-foreground">{{ inv.studentFullName }}</p>
              <p class="text-xs text-muted-foreground">{{ inv.invoiceNo }}</p>
            </div>
            <p class="text-sm font-semibold text-red-600">{{ formatCurrency(inv.balance) }}</p>
          </div>
        </div>
      </div>

      <!-- Recent Payments -->
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('finance.dashboard.pendingPayments') }}</h3>
          <button @click="router.push({ name: 'payments' })" class="text-xs text-primary hover:underline">{{ t('common.viewAll') }}</button>
        </div>
        <div v-if="paymentStore.loading" class="p-4 space-y-2">
          <div v-for="i in 3" :key="i" class="h-10 rounded bg-accent animate-pulse" />
        </div>
        <div v-else-if="paymentStore.transactionList.items.length === 0" class="py-8 text-center text-muted-foreground text-sm">
          {{ t('finance.dashboard.noPendingPayments') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="pay in paymentStore.transactionList.items"
            :key="pay.id"
            class="flex items-center justify-between px-4 py-3 hover:bg-accent/30 cursor-pointer"
            @click="router.push({ name: 'payment-detail', params: { id: pay.id } })"
          >
            <div>
              <p class="text-sm font-medium text-foreground">{{ pay.studentFullName }}</p>
              <p class="text-xs text-muted-foreground">{{ pay.paymentMethodLabel ?? '—' }}</p>
            </div>
            <p class="text-sm font-semibold text-foreground">{{ formatCurrency(pay.amount) }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
