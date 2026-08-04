<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInvoiceStore } from '@/stores/invoice.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { AddInvoiceLinePayload } from '@/types/finance.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useInvoiceStore()
const { can } = usePermission()

const invoiceId = route.params.id as string

const addLineModal = ref(false)
const lineForm = reactive<AddInvoiceLinePayload>({ description: '', quantity: 1, unitPrice: 0, discountAmount: 0 })
const voidModal = ref(false)
const voidReason = ref('')
const issueModal = ref(false)

onMounted(() => store.fetchInvoice(invoiceId))

const invoice = computed(() => store.currentInvoice)

function formatDate(val: string | undefined | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
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

async function doAddLine() {
  if (!lineForm.description.trim()) return
  await store.addInvoiceLine(invoiceId, { ...lineForm })
  addLineModal.value = false
  Object.assign(lineForm, { description: '', quantity: 1, unitPrice: 0, discountAmount: 0 })
}

async function removeLine(lineId: string) {
  await store.removeInvoiceLine(invoiceId, lineId)
}

async function doIssue() {
  if (!invoice.value) return
  await store.issueInvoice(invoiceId, invoice.value.rowVersion)
  issueModal.value = false
}

async function doVoid() {
  if (!invoice.value) return
  await store.voidInvoice(invoiceId, voidReason.value, invoice.value.rowVersion)
  voidModal.value = false
  voidReason.value = ''
}

function printInvoice() {
  window.print()
}
</script>

<template>
  <div>
    <PageHeader
      :title="invoice?.invoiceNo ?? t('finance.invoice.detail')"
      :description="invoice ? invoice.studentFullName : ''"
    >
      <div v-if="invoice" class="flex items-center gap-2">
        <span :class="['px-3 py-1 rounded-full text-sm font-medium', statusColor(invoice.status)]">
          {{ t(`finance.invoice.status.${invoice.status}`) }}
        </span>
        <button
          v-if="can('invoice:update') && invoice.status === 'draft'"
          @click="issueModal = true"
          class="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
        >{{ t('finance.invoice.issue') }}</button>
        <button
          v-if="can('invoice:update') && (invoice.status === 'issued' || invoice.status === 'overdue')"
          @click="voidModal = true"
          class="px-3 py-1.5 bg-red-50 text-red-600 border border-red-200 rounded-lg text-sm hover:bg-red-100"
        >{{ t('finance.invoice.void') }}</button>
        <button @click="printInvoice" class="px-3 py-1.5 border border-border rounded-lg text-sm hover:bg-accent">
          {{ t('finance.invoice.print') }}
        </button>
        <button @click="router.back()" class="px-3 py-1.5 border border-border rounded-lg text-sm hover:bg-accent">
          {{ t('common.back') }}
        </button>
      </div>
    </PageHeader>

    <div v-if="store.loading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-20 rounded-xl bg-accent animate-pulse" />
    </div>

    <template v-else-if="invoice">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <!-- Invoice Info -->
        <div class="md:col-span-2 rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
          <div class="grid grid-cols-2 gap-4 text-sm mb-6">
            <div>
              <p class="text-xs text-muted-foreground mb-0.5">{{ t('student.fullName') }}</p>
              <p class="font-medium">{{ invoice.studentFullName }}</p>
            </div>
            <div>
              <p class="text-xs text-muted-foreground mb-0.5">{{ t('finance.invoice.no') }}</p>
              <p class="font-medium">{{ invoice.invoiceNo }}</p>
            </div>
            <div>
              <p class="text-xs text-muted-foreground mb-0.5">{{ t('finance.invoice.issueDate') }}</p>
              <p>{{ formatDate(invoice.issueDate) }}</p>
            </div>
            <div>
              <p class="text-xs text-muted-foreground mb-0.5">{{ t('finance.invoice.dueDate') }}</p>
              <p :class="invoice.dueDate && new Date(invoice.dueDate) < new Date() && invoice.status !== 'paid' ? 'text-red-600 font-medium' : ''">
                {{ formatDate(invoice.dueDate) }}
              </p>
            </div>
          </div>

          <!-- Lines -->
          <div class="border border-border rounded-lg overflow-hidden">
            <div class="flex items-center justify-between p-3 border-b border-border bg-accent/50">
              <h4 class="text-sm font-medium">{{ t('finance.invoice.lines') }}</h4>
              <button
                v-if="can('invoice:update') && invoice.status === 'draft'"
                @click="addLineModal = true"
                class="text-xs text-primary hover:underline"
              >+ {{ t('finance.invoice.addLine') }}</button>
            </div>
            <div v-if="invoice.lines.length === 0" class="py-6 text-center text-muted-foreground text-sm">
              {{ t('finance.invoice.noLines') }}
            </div>
            <table v-else class="w-full text-sm">
              <thead>
                <tr class="border-b border-border">
                  <th class="text-left px-3 py-2 text-xs text-muted-foreground">{{ t('finance.invoice.lineDescription') }}</th>
                  <th class="text-right px-3 py-2 text-xs text-muted-foreground">{{ t('finance.invoice.qty') }}</th>
                  <th class="text-right px-3 py-2 text-xs text-muted-foreground">{{ t('finance.invoice.unitPrice') }}</th>
                  <th class="text-right px-3 py-2 text-xs text-muted-foreground">{{ t('finance.invoice.discount') }}</th>
                  <th class="text-right px-3 py-2 text-xs text-muted-foreground">{{ t('finance.invoice.lineTotal') }}</th>
                  <th v-if="can('invoice:update') && invoice.status === 'draft'" class="px-3 py-2" />
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in invoice.lines" :key="line.id" class="border-b border-border last:border-0">
                  <td class="px-3 py-2">{{ line.description }}</td>
                  <td class="px-3 py-2 text-right font-mono">{{ line.quantity }}</td>
                  <td class="px-3 py-2 text-right font-mono">{{ formatCurrency(line.unitPrice, invoice.currency) }}</td>
                  <td class="px-3 py-2 text-right font-mono text-muted-foreground">{{ line.discountAmount > 0 ? formatCurrency(line.discountAmount, invoice.currency) : '—' }}</td>
                  <td class="px-3 py-2 text-right font-mono font-medium">{{ formatCurrency(line.lineTotal, invoice.currency) }}</td>
                  <td v-if="can('invoice:update') && invoice.status === 'draft'" class="px-3 py-2">
                    <button @click="removeLine(line.id)" class="text-xs text-red-500 hover:underline">{{ t('common.delete') }}</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Totals -->
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
          <h4 class="text-sm font-semibold text-foreground mb-4">{{ t('finance.invoice.totals') }}</h4>
          <div class="space-y-2 text-sm">
            <div class="flex justify-between">
              <span class="text-muted-foreground">{{ t('finance.invoice.subtotal') }}</span>
              <span class="font-mono">{{ formatCurrency(invoice.subtotal, invoice.currency) }}</span>
            </div>
            <div class="flex justify-between" v-if="invoice.discountTotal > 0">
              <span class="text-muted-foreground">{{ t('finance.invoice.discountTotal') }}</span>
              <span class="font-mono text-red-600">-{{ formatCurrency(invoice.discountTotal, invoice.currency) }}</span>
            </div>
            <div class="flex justify-between font-semibold text-base border-t border-border pt-2 mt-2">
              <span>{{ t('finance.invoice.total') }}</span>
              <span class="font-mono">{{ formatCurrency(invoice.total, invoice.currency) }}</span>
            </div>
            <div class="flex justify-between text-green-600">
              <span>{{ t('finance.invoice.paid') }}</span>
              <span class="font-mono">{{ formatCurrency(invoice.paidAmount, invoice.currency) }}</span>
            </div>
            <div class="flex justify-between font-semibold" :class="invoice.balance > 0 ? 'text-red-600' : 'text-green-600'">
              <span>{{ t('finance.invoice.balance') }}</span>
              <span class="font-mono">{{ formatCurrency(invoice.balance, invoice.currency) }}</span>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Add Line Modal -->
    <div v-if="addLineModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold mb-4">{{ t('finance.invoice.addLine') }}</h3>
        <div class="space-y-3">
          <input v-model="lineForm.description" type="text" :placeholder="t('finance.invoice.lineDescription')" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          <div class="grid grid-cols-3 gap-2">
            <input v-model.number="lineForm.quantity" type="number" min="1" :placeholder="t('finance.invoice.qty')" class="px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
            <input v-model.number="lineForm.unitPrice" type="number" min="0" step="0.01" :placeholder="t('finance.invoice.unitPrice')" class="px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
            <input v-model.number="lineForm.discountAmount" type="number" min="0" step="0.01" :placeholder="t('finance.invoice.discount')" class="px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
        </div>
        <div class="flex justify-end gap-2 mt-4">
          <button @click="addLineModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doAddLine" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('common.add') }}
          </button>
        </div>
      </div>
    </div>

    <ConfirmModal
      :open="issueModal"
      :title="t('finance.invoice.issueTitle')"
      :message="t('finance.invoice.issueMessage')"
      :confirm-label="t('finance.invoice.issue')"
      :loading="store.saving"
      @confirm="doIssue"
      @cancel="issueModal = false"
    />

    <!-- Void Modal -->
    <div v-if="voidModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold mb-4">{{ t('finance.invoice.voidTitle') }}</h3>
        <textarea v-model="voidReason" rows="3" :placeholder="t('finance.invoice.voidReason')" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none resize-none" />
        <div class="flex justify-end gap-2 mt-4">
          <button @click="voidModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doVoid" :disabled="store.saving" class="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('finance.invoice.void') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
