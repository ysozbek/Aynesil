<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { financeService } from '@/services/finance.service'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ScholarshipListItemDto, PaginatedResult } from '@/types/finance.types'
import type { PaginatedResult as PR } from '@/types/api.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const auth = useAuthStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const scholarshipTypes = ref<RefValueItem[]>([])

const list = ref<PR<ScholarshipListItemDto>>({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false })
const loading = ref(false)
const saving = ref(false)

const query = reactive({
  corporationId: corporationId.value,
  studentId: '',
  scholarshipTypeId: '',
  page: 1,
  pageSize: 20,
})

const showForm = ref(false)
const formData = reactive({
  studentId: '', scholarshipTypeId: '', percentage: '' as string | number,
  amount: '' as string | number, currency: 'TRY', validFrom: '', validTo: '', notes: '',
})

watch(() => [query.studentId, query.scholarshipTypeId, query.page], () => loadList())
onMounted(async () => {
  await Promise.all([
    loadList(),
    refData.getValues('SCHOLARSHIP_TYPE').then(v => { scholarshipTypes.value = v }),
  ])
})

async function loadList() {
  loading.value = true
  try {
    const res = await financeService.listScholarships({
      ...query,
      corporationId: corporationId.value,
      studentId: query.studentId || undefined,
      scholarshipTypeId: query.scholarshipTypeId || undefined,
    })
    if (res.success && res.data) list.value = res.data
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  saving.value = true
  try {
    await financeService.createScholarship({
      corporationId: corporationId.value,
      studentId: formData.studentId,
      scholarshipTypeId: formData.scholarshipTypeId,
      percentage: formData.percentage ? Number(formData.percentage) : undefined,
      amount: formData.amount ? Number(formData.amount) : undefined,
      currency: formData.currency || undefined,
      validFrom: formData.validFrom,
      validTo: formData.validTo || undefined,
      notes: formData.notes || undefined,
    })
    showForm.value = false
    await loadList()
  } finally {
    saving.value = false
  }
}

const columns: Column<ScholarshipListItemDto>[] = [
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'scholarshipTypeLabel', label: t('finance.scholarship.type'), width: '140px' },
  { key: 'percentage', label: t('finance.scholarship.percentage'), width: '90px', align: 'center' },
  { key: 'amount', label: t('finance.scholarship.amount'), width: '110px', align: 'right' },
  { key: 'validFrom', label: t('finance.scholarship.validFrom'), width: '110px' },
  { key: 'validTo', label: t('finance.scholarship.validTo'), width: '110px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.scholarship.title')" :description="t('finance.scholarship.description')">
      <button
        v-if="can('scholarship:create')"
        @click="showForm = true"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('finance.scholarship.create') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3">
      <input
        v-model="query.studentId"
        type="text"
        :placeholder="t('student.fullName') + ' ID'"
        @input="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <select v-model="query.scholarshipTypeId" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('finance.scholarship.allTypes') }}</option>
        <option v-for="st in scholarshipTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
      </select>
    </div>

    <DataTable :columns="columns" :rows="list.items" :loading="loading">
      <template #cell-scholarshipTypeLabel="{ value }"><span class="text-muted-foreground">{{ value ?? '—' }}</span></template>
      <template #cell-percentage="{ value }"><span v-if="value" class="font-mono">%{{ value }}</span><span v-else class="text-muted-foreground">—</span></template>
      <template #cell-amount="{ row }"><span v-if="row.amount" class="font-mono">{{ row.amount }} {{ row.currency }}</span><span v-else class="text-muted-foreground">—</span></template>
      <template #cell-validFrom="{ value }">{{ formatDate(value) }}</template>
      <template #cell-validTo="{ value }">{{ formatDate(value) }}</template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="list.page" :page-size="list.pageSize" :total-count="list.totalCount"
        :total-pages="list.totalPages" :has-previous-page="list.hasPreviousPage" :has-next-page="list.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-lg border border-border">
        <h3 class="font-semibold mb-4">{{ t('finance.scholarship.create') }}</h3>
        <div class="space-y-3">
          <div><label class="block text-sm font-medium mb-1">{{ t('student.fullName') }} ID *</label><input v-model="formData.studentId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.type') }} *</label>
            <select v-model="formData.scholarshipTypeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="st in scholarshipTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
            </select>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div><label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.percentage') }} (%)</label><input v-model="formData.percentage" type="number" min="0" max="100" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
            <div><label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.amount') }}</label><input v-model="formData.amount" type="number" min="0" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div><label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.validFrom') }} *</label><input v-model="formData.validFrom" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
            <div><label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.validTo') }}</label><input v-model="formData.validTo" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          </div>
        </div>
        <div class="flex justify-end gap-2 mt-4">
          <button @click="showForm = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="submitForm" :disabled="saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
