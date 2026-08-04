<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { usePaymentStore } from '@/stores/payment.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = usePaymentStore()
const { can } = usePermission()

const paymentId = route.params.id as string
const captureModal = ref(false)
const refundModal = ref(false)
const refundAmount = ref(0)
const refundReason = ref('')

onMounted(() => store.fetchTransaction(paymentId))

const payment = computed(() => store.currentTransaction)

function formatDate(val: string | undefined | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
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

async function doCapture() {
  if (!payment.value) return
  await store.captureTransaction(paymentId, payment.value.rowVersion)
  captureModal.value = false
}

async function doRefund() {
  if (!payment.value) return
  const refundDto = await store.createRefund({
    paymentId: paymentId,
    amount: refundAmount.value || payment.value.amount,
    reason: refundReason.value || undefined,
  })
  refundModal.value = false
  refundAmount.value = 0
  refundReason.value = ''
}
</script>

<template>
  <div>
    <PageHeader
      :title="t('finance.payment.detail')"
      :description="payment?.studentFullName ?? ''"
    >
      <div v-if="payment" class="flex items-center gap-2">
        <span :class="['px-3 py-1 rounded-full text-sm font-medium', statusColor(payment.status)]">
          {{ t(`finance.payment.status.${payment.status}`) }}
        </span>
        <button
          v-if="can('payment:update') && payment.status === 'pending'"
          @click="captureModal = true"
          class="px-3 py-1.5 bg-green-600 text-white rounded-lg text-sm hover:bg-green-700"
        >{{ t('finance.payment.capture') }}</button>
        <button
          v-if="can('refund:create') && payment.status === 'captured'"
          @click="refundAmount = payment.amount; refundModal = true"
          class="px-3 py-1.5 bg-red-50 text-red-600 border border-red-200 rounded-lg text-sm hover:bg-red-100"
        >{{ t('finance.payment.refund') }}</button>
        <button @click="router.back()" class="px-3 py-1.5 border border-border rounded-lg text-sm hover:bg-accent">{{ t('common.back') }}</button>
      </div>
    </PageHeader>

    <div v-if="store.loading" class="h-40 rounded-xl bg-accent animate-pulse" />

    <div v-else-if="!payment" class="text-center py-16 text-muted-foreground">{{ t('errors.notFound') }}</div>

    <template v-else>
      <div class="max-w-2xl rounded-xl border border-border bg-[--color-card] shadow-sm p-6">
        <dl class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('student.fullName') }}</dt>
            <dd class="font-medium">{{ payment.studentFullName }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('finance.invoice.no') }}</dt>
            <dd>{{ payment.invoiceNo ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('finance.payment.amount') }}</dt>
            <dd class="text-lg font-bold">{{ formatCurrency(payment.amount, payment.currency) }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('finance.payment.method') }}</dt>
            <dd>{{ payment.paymentMethodLabel ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('finance.payment.paidAt') }}</dt>
            <dd>{{ formatDate(payment.paidAt) }}</dd>
          </div>
          <div v-if="payment.gatewayReference">
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('finance.payment.gatewayRef') }}</dt>
            <dd class="font-mono text-xs">{{ payment.gatewayReference }}</dd>
          </div>
          <div v-if="payment.notes" class="col-span-2">
            <dt class="text-xs text-muted-foreground mb-0.5">{{ t('finance.payment.notes') }}</dt>
            <dd>{{ payment.notes }}</dd>
          </div>
        </dl>
      </div>
    </template>

    <ConfirmModal
      :open="captureModal"
      :title="t('finance.payment.captureTitle')"
      :message="t('finance.payment.captureMessage')"
      :confirm-label="t('finance.payment.capture')"
      :loading="store.saving"
      @confirm="doCapture"
      @cancel="captureModal = false"
    />

    <!-- Refund Modal -->
    <div v-if="refundModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold mb-4">{{ t('finance.payment.refundTitle') }}</h3>
        <div class="space-y-3">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.payment.refundAmount') }}</label>
            <input v-model.number="refundAmount" type="number" min="0.01" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.payment.refundReason') }}</label>
            <input v-model="refundReason" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
        </div>
        <div class="flex justify-end gap-2 mt-4">
          <button @click="refundModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doRefund" :disabled="store.saving || refundAmount <= 0" class="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('finance.payment.refund') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
