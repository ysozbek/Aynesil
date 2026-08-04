<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { usePaymentStore } from '@/stores/payment.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = usePaymentStore()
const refData = useRefDataStore()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const paymentMethods = ref<RefValueItem[]>([])

const form = reactive({
  studentId: '',
  invoiceId: '',
  amount: 0,
  currency: 'TRY',
  paymentMethodId: '',
  gatewayReference: '',
  paidAt: new Date().toISOString().slice(0, 10),
  notes: '',
})

const errors = reactive<Record<string, string>>({})

onMounted(async () => {
  await refData.getValues('PAYMENT_METHOD').then(v => { paymentMethods.value = v })
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete (errors as Record<string, string>)[k])
  if (!form.studentId.trim()) errors.studentId = t('validation.required', { field: t('student.fullName') })
  if (form.amount <= 0) errors.amount = t('validation.numeric', { field: t('finance.payment.amount') })
  if (!form.paymentMethodId) errors.paymentMethodId = t('validation.required', { field: t('finance.payment.method') })
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    const created = await store.createTransaction({
      corporationId: corporationId.value,
      studentId: form.studentId,
      invoiceId: form.invoiceId || undefined,
      amount: form.amount,
      currency: form.currency,
      paymentMethodId: form.paymentMethodId,
      gatewayReference: form.gatewayReference || undefined,
      paidAt: form.paidAt ? new Date(form.paidAt).toISOString() : undefined,
      notes: form.notes || undefined,
    })
    router.push({ name: 'payment-detail', params: { id: created.id } })
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.payment.create')" :description="t('finance.payment.createDescription')" />

    <div class="max-w-2xl">
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-5">

        <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">{{ errors.submit }}</div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('student.fullName') }} ID <span class="text-red-500">*</span></label>
          <input v-model="form.studentId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.studentId ? 'border-red-400' : 'border-border'" />
          <p v-if="errors.studentId" class="text-xs text-red-500 mt-1">{{ errors.studentId }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.invoice.no') }} ID</label>
          <input v-model="form.invoiceId" type="text" :placeholder="t('finance.payment.invoiceIdHint')" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.payment.amount') }} <span class="text-red-500">*</span></label>
            <input v-model.number="form.amount" type="number" min="0" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.amount ? 'border-red-400' : 'border-border'" />
            <p v-if="errors.amount" class="text-xs text-red-500 mt-1">{{ errors.amount }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.currency') }}</label>
            <select v-model="form.currency" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="TRY">TRY</option>
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.payment.method') }} <span class="text-red-500">*</span></label>
          <select v-model="form.paymentMethodId" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.paymentMethodId ? 'border-red-400' : 'border-border'">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="pm in paymentMethods" :key="pm.id" :value="pm.id">{{ pm.label }}</option>
          </select>
          <p v-if="errors.paymentMethodId" class="text-xs text-red-500 mt-1">{{ errors.paymentMethodId }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.payment.paidAt') }}</label>
          <input v-model="form.paidAt" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.payment.notes') }}</label>
          <textarea v-model="form.notes" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button @click="router.back()" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="submit" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
