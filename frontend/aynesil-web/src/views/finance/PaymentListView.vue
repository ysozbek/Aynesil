<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { usePaymentStore } from '@/stores/payment.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { PaymentListItemDto } from '@/types/finance.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = usePaymentStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const paymentMethods = ref<RefValueItem[]>([])

const query = reactive({
  corporationId: corporationId.value,
  studentId: '',
  status: '',
  from: '',
  to: '',
  page: 1,
  pageSize: 20,
})

watch(() => [query.status, query.studentId, query.from, query.to, query.page], () => loadList())
onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('PAYMENT_METHOD').then(v => { paymentMethods.value = v }),
  ])
})

async function loadList() {
  await store.fetchTransactions({
    ...query,
    corporationId: corporationId.value,
    studentId: query.studentId || undefined,
    status: query.status || undefined,
    from: query.from || undefined,
    to: query.to || undefined,
  })
}

const columns: Column<PaymentListItemDto>[] = [
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'invoiceNo', label: t('finance.invoice.no'), width: '120px' },
  { key: 'amount', label: t('finance.payment.amount'), width: '120px', align: 'right' },
  { key: 'paymentMethodLabel', label: t('finance.payment.method'), width: '130px' },
  { key: 'status', label: t('common.status'), width: '100px' },
  { key: 'paidAt', label: t('finance.payment.paidAt'), width: '120px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function formatCurrency(val: number, currency = 'TRY'): string {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency }).format(val)
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    pending: 'bg-amber-100 text-amber-700',
    captured: 'bg-green-100 text-green-700',
    failed: 'bg-red-100 text-red-700',
    refunded: 'bg-gray-100 text-gray-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.payment.title')" :description="t('finance.payment.description')">
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
    </PageHeader>

    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <input
        v-model="query.studentId"
        type="text"
        :placeholder="t('student.fullName') + ' ID'"
        @input="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <select v-model="query.status" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="pending">{{ t('finance.payment.status.pending') }}</option>
        <option value="captured">{{ t('finance.payment.status.captured') }}</option>
        <option value="failed">{{ t('finance.payment.status.failed') }}</option>
        <option value="refunded">{{ t('finance.payment.status.refunded') }}</option>
      </select>
      <input v-model="query.from" type="date" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      <input v-model="query.to" type="date" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
    </div>

    <DataTable
      :columns="columns"
      :rows="store.transactionList.items"
      :loading="store.loading"
      @row-click="(row) => router.push({ name: 'payment-detail', params: { id: row.id } })"
    >
      <template #cell-invoiceNo="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-amount="{ row }">
        <span class="font-mono font-medium">{{ formatCurrency(row.amount, row.currency) }}</span>
      </template>
      <template #cell-paymentMethodLabel="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ t(`finance.payment.status.${row.status}`) }}
        </span>
      </template>
      <template #cell-paidAt="{ value }">{{ formatDate(value) }}</template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'payment-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.transactionList.page"
        :page-size="store.transactionList.pageSize"
        :total-count="store.transactionList.totalCount"
        :total-pages="store.transactionList.totalPages"
        :has-previous-page="store.transactionList.hasPreviousPage"
        :has-next-page="store.transactionList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>
  </div>
</template>
