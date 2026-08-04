<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useCreditLedgerStore } from '@/stores/creditLedger.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { CreditLedgerEntryDto } from '@/types/finance.types'

const { t } = useI18n()
const auth = useAuthStore()
const store = useCreditLedgerStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const studentIdInput = ref('')
const loadedStudentId = ref('')

const query = reactive({
  corporationId: corporationId.value,
  studentId: '',
  entryType: '',
  from: '',
  to: '',
  page: 1,
  pageSize: 20,
})

// Manual credit operations
const grantModal = ref(false)
const grantForm = reactive({ studentPackageId: '', amount: 0, reason: '' })
const adjustModal = ref(false)
const adjustForm = reactive({ studentPackageId: '', delta: 0, reason: '' })

onMounted(async () => {
  // Load without filter initially - wait for student selection
})

async function loadData() {
  if (!loadedStudentId.value) return
  await Promise.all([
    store.fetchCreditSummary(loadedStudentId.value),
    store.fetchCredits({ ...query, corporationId: corporationId.value, studentId: loadedStudentId.value }),
  ])
}

async function searchStudent() {
  if (!studentIdInput.value.trim()) return
  loadedStudentId.value = studentIdInput.value.trim()
  query.studentId = loadedStudentId.value
  await loadData()
}

watch(() => [query.entryType, query.from, query.to, query.page], () => {
  if (loadedStudentId.value) loadData()
})

const columns: Column<CreditLedgerEntryDto>[] = [
  { key: 'occurredAt', label: t('finance.credit.occurredAt'), sortable: true, width: '140px' },
  { key: 'entryType', label: t('finance.credit.entryType'), width: '110px' },
  { key: 'delta', label: t('finance.credit.delta'), width: '90px', align: 'right' },
  { key: 'runningBalance', label: t('finance.credit.runningBalance'), width: '100px', align: 'right' },
  { key: 'sessionTitle', label: t('scheduling.session.titleField') },
  { key: 'reason', label: t('finance.credit.reason') },
  { key: 'recordedByName', label: t('finance.credit.recordedBy'), width: '130px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' })
}

function entryTypeColor(type: string): string {
  const map: Record<string, string> = {
    consumed: 'bg-red-100 text-red-700',
    granted: 'bg-green-100 text-green-700',
    refunded: 'bg-blue-100 text-blue-700',
    adjusted: 'bg-violet-100 text-violet-700',
    purchase: 'bg-teal-100 text-teal-700',
  }
  return map[type] ?? 'bg-gray-100 text-gray-600'
}

function deltaColor(delta: number): string {
  return delta > 0 ? 'text-green-600 font-medium' : 'text-red-600 font-medium'
}

async function doGrant() {
  await store.grantCredits(grantForm)
  grantModal.value = false
  Object.assign(grantForm, { studentPackageId: '', amount: 0, reason: '' })
  await loadData()
}

async function doAdjust() {
  await store.adjustCredits(adjustForm)
  adjustModal.value = false
  Object.assign(adjustForm, { studentPackageId: '', delta: 0, reason: '' })
  await loadData()
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.credit.title')" :description="t('finance.credit.description')">
      <div class="flex gap-2">
        <button
          v-if="can('credit:grant')"
          @click="grantModal = true"
          class="px-4 py-2 bg-green-600 text-white rounded-lg text-sm hover:bg-green-700"
        >{{ t('finance.credit.grant') }}</button>
        <button
          v-if="can('credit:adjust')"
          @click="adjustModal = true"
          class="px-4 py-2 border border-border rounded-lg text-sm hover:bg-accent"
        >{{ t('finance.credit.adjust') }}</button>
      </div>
    </PageHeader>

    <!-- Student search -->
    <div class="mb-6 flex items-center gap-3">
      <input
        v-model="studentIdInput"
        type="text"
        :placeholder="t('finance.credit.studentIdPlaceholder')"
        @keydown.enter="searchStudent"
        class="flex-1 max-w-sm px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <button @click="searchStudent" :disabled="store.loading || !studentIdInput.trim()" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-60">
        {{ t('common.search') }}
      </button>
    </div>

    <div v-if="!loadedStudentId" class="text-center py-16 text-muted-foreground text-sm">
      {{ t('finance.credit.enterStudentId') }}
    </div>

    <template v-else>
      <!-- Summary -->
      <div v-if="store.summary" class="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-foreground">{{ store.summary.activePackages }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('finance.credit.activePackages') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-teal-600">{{ store.summary.totalGranted }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('finance.credit.totalGranted') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-red-600">{{ store.summary.totalConsumed }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('finance.credit.totalConsumed') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-green-600">{{ store.summary.totalRemaining }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('finance.credit.totalRemaining') }}</p>
        </div>
        <div class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm text-center">
          <p class="text-2xl font-bold text-amber-600">{{ store.summary.expiringWithin30Days }}</p>
          <p class="text-xs text-muted-foreground mt-1">{{ t('finance.credit.expiringIn30') }}</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="mb-4 flex items-center gap-3">
        <select v-model="query.entryType" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
          <option value="">{{ t('finance.credit.allTypes') }}</option>
          <option value="consumed">{{ t('finance.credit.type.consumed') }}</option>
          <option value="granted">{{ t('finance.credit.type.granted') }}</option>
          <option value="refunded">{{ t('finance.credit.type.refunded') }}</option>
          <option value="adjusted">{{ t('finance.credit.type.adjusted') }}</option>
          <option value="purchase">{{ t('finance.credit.type.purchase') }}</option>
        </select>
        <input v-model="query.from" type="date" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
        <input v-model="query.to" type="date" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <DataTable
        :columns="columns"
        :rows="store.entryList.items"
        :loading="store.loading"
      >
        <template #cell-occurredAt="{ value }">{{ formatDate(value) }}</template>
        <template #cell-entryType="{ value }">
          <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', entryTypeColor(String(value))]">
            {{ t(`finance.credit.type.${value}`) }}
          </span>
        </template>
        <template #cell-delta="{ value }">
          <span :class="deltaColor(Number(value))">{{ Number(value) > 0 ? '+' : '' }}{{ value }}</span>
        </template>
        <template #cell-runningBalance="{ value }">
          <span class="font-mono font-medium">{{ value }}</span>
        </template>
        <template #cell-sessionTitle="{ value }">
          <span class="text-muted-foreground">{{ value ?? '—' }}</span>
        </template>
        <template #cell-reason="{ value }">
          <span class="text-muted-foreground">{{ value ?? '—' }}</span>
        </template>
      </DataTable>

      <div class="mt-4">
        <Pagination
          :page="store.entryList.page"
          :page-size="store.entryList.pageSize"
          :total-count="store.entryList.totalCount"
          :total-pages="store.entryList.totalPages"
          :has-previous-page="store.entryList.hasPreviousPage"
          :has-next-page="store.entryList.hasNextPage"
          @update:page="(p) => { query.page = p }"
          @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
        />
      </div>
    </template>

    <!-- Grant Modal -->
    <div v-if="grantModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-4">{{ t('finance.credit.grantTitle') }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.studentPackage.title') }} ID</label>
            <input v-model="grantForm.studentPackageId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.credit.amount') }}</label>
            <input v-model.number="grantForm.amount" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.credit.reason') }}</label>
            <input v-model="grantForm.reason" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
        </div>
        <div class="flex justify-end gap-2 mt-5">
          <button @click="grantModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doGrant" :disabled="store.saving || !grantForm.studentPackageId || grantForm.amount < 1" class="px-4 py-2 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('finance.credit.grant') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Adjust Modal -->
    <div v-if="adjustModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-4">{{ t('finance.credit.adjustTitle') }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.studentPackage.title') }} ID</label>
            <input v-model="adjustForm.studentPackageId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.credit.delta') }} ({{ t('finance.credit.deltaHint') }})</label>
            <input v-model.number="adjustForm.delta" type="number" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.credit.reason') }} *</label>
            <input v-model="adjustForm.reason" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" />
          </div>
        </div>
        <div class="flex justify-end gap-2 mt-5">
          <button @click="adjustModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doAdjust" :disabled="store.saving || !adjustForm.studentPackageId || !adjustForm.reason" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('finance.credit.adjust') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
