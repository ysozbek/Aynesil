<script setup lang="ts">
/**
 * İade talepleri — listele, işleme al, başarısız işaretle.
 * İadeler silinmez: işleme alma, işlenen iadeler ödeme tutarına ulaşınca ödemeyi "iade edildi" yapar; başarısız işaretleme yalnızca iadenin durumunu değiştirir.
 * Yalnızca bekleyen (pending) iadelerde işlem yapılır (backend geçişi doğrular).
 */
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { usePaymentStore } from '@/stores/payment.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { RefundListItemDto } from '@/types/finance.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const store = usePaymentStore()
const { can } = usePermission()

const query = reactive({ status: '', page: 1, pageSize: 20 })

async function loadList() {
  await store.fetchRefunds({ status: query.status || undefined, page: query.page, pageSize: query.pageSize })
}
watch(() => [query.status, query.page], loadList)
onMounted(loadList)

const columns: Column<RefundListItemDto>[] = [
  { key: 'paymentId', label: t('finance.refund.payment'), width: '120px' },
  { key: 'amount', label: t('finance.payment.amount'), width: '130px', align: 'right' },
  { key: 'reason', label: t('finance.refund.reason') },
  { key: 'status', label: t('common.status'), width: '120px' },
  { key: 'processedAt', label: t('finance.refund.processedAt'), width: '130px' },
]

const money = (v: number) => new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(v)
const formatDate = (v: unknown) => (v ? new Date(String(v)).toLocaleDateString('tr-TR') : '—')

const target = ref<RefundListItemDto | null>(null)
const action = ref<'process' | 'fail'>('process')
const actionError = ref('')

function ask(row: RefundListItemDto, a: 'process' | 'fail') {
  target.value = row
  action.value = a
  actionError.value = ''
}

async function confirm() {
  if (!target.value) return
  actionError.value = ''
  try {
    if (action.value === 'process') await store.processRefund(target.value.id)
    else await store.failRefund(target.value.id)
    target.value = null
    await loadList()
  } catch (e: unknown) {
    actionError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.refund.title')" :description="t('finance.refund.description')" />

    <p v-if="store.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ store.error }}</p>

    <div class="mb-4">
      <select v-model="query.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" @change="query.page = 1">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="pending">{{ t('statusBadge.refund.pending') }}</option>
        <option value="processed">{{ t('statusBadge.refund.processed') }}</option>
        <option value="failed">{{ t('statusBadge.refund.failed') }}</option>
      </select>
    </div>

    <DataTable :columns="columns" :rows="store.refundList.items" :loading="store.loading">
      <template #cell-paymentId="{ value }">
        <button class="font-mono text-xs text-primary hover:underline" @click.stop="router.push(`/finance/payments/${value}`)">
          {{ String(value).slice(0, 8) }}
        </button>
      </template>
      <template #cell-amount="{ value }"><span class="font-mono">{{ money(Number(value)) }}</span></template>
      <template #cell-reason="{ value }"><span class="text-muted-foreground">{{ value ?? '—' }}</span></template>
      <template #cell-status="{ row }"><StatusBadge entity="refund" :status="row.status" /></template>
      <template #cell-processedAt="{ value }">{{ formatDate(value) }}</template>
      <template #actions="{ row }">
        <div v-if="row.status === 'pending' && can(Permissions.Refunds.Process)" class="flex items-center justify-end gap-1">
          <button class="px-2.5 py-1 text-xs rounded-lg border border-border hover:bg-green-50 text-green-700" @click="ask(row, 'process')">
            {{ t('finance.refund.process') }}
          </button>
          <button class="px-2.5 py-1 text-xs rounded-lg border border-border hover:bg-red-50 text-red-600" @click="ask(row, 'fail')">
            {{ t('finance.refund.markFailed') }}
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.refundList.page"
        :page-size="store.refundList.pageSize"
        :total-count="store.refundList.totalCount"
        :total-pages="store.refundList.totalPages"
        :has-previous-page="store.refundList.hasPreviousPage"
        :has-next-page="store.refundList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!target"
      :title="action === 'process' ? t('finance.refund.processTitle') : t('finance.refund.failTitle')"
      :message="actionError || (action === 'process' ? t('finance.refund.processMessage', { amount: money(target?.amount ?? 0) }) : t('finance.refund.failMessage'))"
      :confirm-label="action === 'process' ? t('finance.refund.process') : t('finance.refund.markFailed')"
      :confirm-class="action === 'process' ? 'bg-primary text-primary-foreground hover:opacity-90' : undefined"
      :loading="store.saving"
      @confirm="confirm"
      @cancel="target = null"
    />
  </div>
</template>
