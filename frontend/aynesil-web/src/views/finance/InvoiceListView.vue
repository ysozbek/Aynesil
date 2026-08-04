<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useInvoiceStore } from '@/stores/invoice.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { InvoiceListItemDto } from '@/types/finance.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useInvoiceStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

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
onMounted(() => loadList())

async function loadList() {
  await store.fetchInvoices({
    ...query,
    corporationId: corporationId.value,
    studentId: query.studentId || undefined,
    status: query.status || undefined,
    from: query.from || undefined,
    to: query.to || undefined,
  })
}

const columns: Column<InvoiceListItemDto>[] = [
  { key: 'invoiceNo', label: t('finance.invoice.no'), width: '120px' },
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'issueDate', label: t('finance.invoice.issueDate'), width: '110px' },
  { key: 'dueDate', label: t('finance.invoice.dueDate'), width: '110px' },
  { key: 'total', label: t('finance.invoice.total'), width: '110px', align: 'right' },
  { key: 'paidAmount', label: t('finance.invoice.paid'), width: '110px', align: 'right' },
  { key: 'balance', label: t('finance.invoice.balance'), width: '110px', align: 'right' },
  { key: 'status', label: t('common.status'), width: '100px' },
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
    draft: 'bg-gray-100 text-gray-600',
    issued: 'bg-blue-100 text-blue-700',
    paid: 'bg-green-100 text-green-700',
    partial: 'bg-amber-100 text-amber-700',
    overdue: 'bg-red-100 text-red-700',
    void: 'bg-gray-100 text-gray-500',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.invoice.title')" :description="t('finance.invoice.description')">
      <button
        v-if="can('invoice:create')"
        @click="router.push({ name: 'invoice-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('finance.invoice.create') }}
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
        <option value="draft">{{ t('finance.invoice.status.draft') }}</option>
        <option value="issued">{{ t('finance.invoice.status.issued') }}</option>
        <option value="paid">{{ t('finance.invoice.status.paid') }}</option>
        <option value="partial">{{ t('finance.invoice.status.partial') }}</option>
        <option value="overdue">{{ t('finance.invoice.status.overdue') }}</option>
        <option value="void">{{ t('finance.invoice.status.void') }}</option>
      </select>
      <input v-model="query.from" type="date" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      <input v-model="query.to" type="date" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
    </div>

    <DataTable
      :columns="columns"
      :rows="store.invoiceList.items"
      :loading="store.loading"
      @row-click="(row) => router.push({ name: 'invoice-detail', params: { id: row.id } })"
    >
      <template #cell-issueDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-dueDate="{ value }">
        <span :class="value && new Date(String(value)) < new Date() ? 'text-red-600' : ''">{{ formatDate(value) }}</span>
      </template>
      <template #cell-total="{ row }">
        <span class="font-mono">{{ formatCurrency(row.total, row.currency) }}</span>
      </template>
      <template #cell-paidAmount="{ row }">
        <span class="font-mono text-green-600">{{ formatCurrency(row.paidAmount, row.currency) }}</span>
      </template>
      <template #cell-balance="{ row }">
        <span :class="['font-mono font-medium', row.balance > 0 ? 'text-red-600' : 'text-green-600']">
          {{ formatCurrency(row.balance, row.currency) }}
        </span>
      </template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ t(`finance.invoice.status.${row.status}`) }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'invoice-detail', params: { id: row.id } })"
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
        :page="store.invoiceList.page"
        :page-size="store.invoiceList.pageSize"
        :total-count="store.invoiceList.totalCount"
        :total-pages="store.invoiceList.totalPages"
        :has-previous-page="store.invoiceList.hasPreviousPage"
        :has-next-page="store.invoiceList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>
  </div>
</template>
