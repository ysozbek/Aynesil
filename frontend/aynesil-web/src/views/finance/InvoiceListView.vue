<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInvoiceStore } from '@/stores/invoice.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import { toLocalDateString } from '@/utils/datetime'
import type { Column } from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { InvoiceListItemDto } from '@/types/finance.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const store = useInvoiceStore()
const { can } = usePermission()

const query = reactive({
  studentId: '',
  status: typeof route.query.status === 'string' ? route.query.status : '',
  from: '',
  to: '',
  page: 1,
  pageSize: 20,
})

watch(() => [query.status, query.studentId, query.from, query.to, query.page], () => loadList())
onMounted(() => {
  loadList()
  if (route.name === 'invoice-new') openCreate()
})

// ── Yeni fatura (taslak) ─────────────────────────────────────────────────────
// /finance/invoices/new bu görünümü açar ve formu gösterir. Kalemler ve kesme işlemi detay sayfasındadır.
const showCreate = ref(false)
const createError = ref('')
const createForm = reactive({ studentId: '', issueDate: '', dueDate: '', currency: 'TRY' })

function openCreate() {
  Object.assign(createForm, { studentId: '', issueDate: toLocalDateString(), dueDate: '', currency: 'TRY' })
  createError.value = ''
  showCreate.value = true
}

function closeCreate() {
  showCreate.value = false
  if (route.name === 'invoice-new') router.replace({ name: 'invoices' })
}

async function submitCreate() {
  createError.value = ''
  if (!createForm.studentId) { createError.value = t('validation.required', { field: t('student.fullName') }); return }
  if (!createForm.issueDate) { createError.value = t('validation.required', { field: t('finance.invoice.issueDate') }); return }
  if (createForm.dueDate && createForm.dueDate < createForm.issueDate) { createError.value = t('finance.invoice.dueBeforeIssue'); return }
  try {
    const created = await store.createInvoice({
      studentId: createForm.studentId,
      issueDate: createForm.issueDate,
      dueDate: createForm.dueDate || undefined,
      currency: createForm.currency,
    })
    showCreate.value = false
    router.push({ name: 'invoice-detail', params: { id: created.id } })
  } catch (e: unknown) {
    createError.value = (e as Error).message
  }
}

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary'

async function loadList() {
  await store.fetchInvoices({
    page: query.page,
    pageSize: query.pageSize,
    studentId: query.studentId || undefined,
    // 'overdue' is derived (issued/partial past due date), so it maps to the overdue flag
    status: query.status && query.status !== 'overdue' ? query.status : undefined,
    overdue: query.status === 'overdue' ? true : undefined,
    issuedFrom: query.from || undefined,
    issuedTo: query.to || undefined,
  })
}

const columns: Column<InvoiceListItemDto>[] = [
  { key: 'invoiceNo', label: t('finance.invoice.no'), width: '120px' },
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'issueDate', label: t('finance.invoice.issueDate'), width: '110px' },
  { key: 'dueDate', label: t('finance.invoice.dueDate'), width: '110px' },
  { key: 'grandTotal', label: t('finance.invoice.total'), width: '110px', align: 'right' },
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

</script>

<template>
  <div>
    <PageHeader :title="t('finance.invoice.title')" :description="t('finance.invoice.description')">
      <button
        v-if="can(Permissions.Invoices.Create)"
        @click="openCreate"
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
        <option value="draft">{{ t('statusBadge.invoice.draft') }}</option>
        <option value="issued">{{ t('statusBadge.invoice.issued') }}</option>
        <option value="paid">{{ t('statusBadge.invoice.paid') }}</option>
        <option value="partial">{{ t('statusBadge.invoice.partial') }}</option>
        <option value="overdue">{{ t('statusBadge.invoice.overdue') }}</option>
        <option value="void">{{ t('statusBadge.invoice.void') }}</option>
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
      <template #cell-invoiceNo="{ value }">{{ value ?? t('finance.payment.invoiceDraft') }}</template>
      <template #cell-issueDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-dueDate="{ value }">
        <span :class="value && new Date(String(value)) < new Date() ? 'text-red-600' : ''">{{ formatDate(value) }}</span>
      </template>
      <template #cell-grandTotal="{ row }">
        <span class="font-mono">{{ formatCurrency(row.grandTotal, row.currency) }}</span>
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
        <StatusBadge entity="invoice" :status="row.status" />
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

    <FormModal
      :open="showCreate"
      :title="t('finance.invoice.create')"
      :saving="store.saving"
      @submit="submitCreate"
      @close="closeCreate"
    >
      <div class="space-y-4">
        <p v-if="createError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ createError }}</p>
        <p class="text-xs text-muted-foreground">{{ t('finance.invoice.createHint') }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.fullName') }} *</label>
          <StudentPicker v-model="createForm.studentId" />
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('finance.invoice.issueDate') }} *</label>
            <input v-model="createForm.issueDate" type="date" :class="inputClass" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('finance.invoice.dueDate') }}</label>
            <input v-model="createForm.dueDate" type="date" :class="inputClass" />
          </div>
        </div>
      </div>
    </FormModal>
  </div>
</template>
