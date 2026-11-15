<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { financeService } from '@/services/finance.service'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ScholarshipListItemDto, ScholarshipDto } from '@/types/finance.types'
import type { PaginatedResult as PR } from '@/types/api.types'
import type { RefValueItem } from '@/stores/refdata.store'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const refData = useRefDataStore()
const { can } = usePermission()

const scholarshipTypes = ref<RefValueItem[]>([])

const list = ref<PR<ScholarshipListItemDto>>({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false })
const loading = ref(false)
const saving = ref(false)

const query = reactive({
  studentId: '',
  scholarshipTypeId: '',
  page: 1,
  pageSize: 20,
})

const showForm = ref(false)
const formData = reactive({
  studentId: '', scholarshipTypeId: '', percentage: '' as string | number,
  amount: '' as string | number, validFrom: '', validTo: '', note: '',
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
      studentId: formData.studentId,
      scholarshipTypeId: formData.scholarshipTypeId,
      percentage: formData.percentage ? Number(formData.percentage) : undefined,
      amount: formData.amount ? Number(formData.amount) : undefined,
      validFrom: formData.validFrom,
      validTo: formData.validTo || undefined,
      note: formData.note || undefined,
    })
    showForm.value = false
    await loadList()
  } finally {
    saving.value = false
  }
}

// ── Ayrıntı / düzenleme ─────────────────────────────────────────────────────
const detail = ref<ScholarshipDto | null>(null)
const detailOpen = ref(false)
const detailLoading = ref(false)
const detailError = ref('')
const editing = ref(false)
const editSaving = ref(false)
const editForm = reactive({
  scholarshipTypeId: '', percentage: '' as string | number, amount: '' as string | number,
  validFrom: '', validTo: '', note: '',
})
const editErrors = reactive<Record<string, string>>({})

async function openDetail(row: ScholarshipListItemDto) {
  detail.value = null
  detailError.value = ''
  editing.value = false
  detailOpen.value = true
  detailLoading.value = true
  try {
    const res = await financeService.getScholarship(row.id)
    if (!res.success || !res.data) throw new Error(res.message ?? t('errors.notFound'))
    detail.value = res.data
  } catch (e: unknown) {
    detailError.value = (e as Error).message
  } finally {
    detailLoading.value = false
  }
}

function closeDetail() {
  detailOpen.value = false
  detail.value = null
  editing.value = false
}

function startEdit() {
  if (!detail.value) return
  const d = detail.value
  Object.assign(editForm, {
    scholarshipTypeId: d.scholarshipTypeId ?? '',
    percentage: d.percentage ?? '',
    amount: d.amount ?? '',
    validFrom: d.validFrom?.slice(0, 10) ?? '',
    validTo: d.validTo?.slice(0, 10) ?? '',
    note: d.note ?? '',
  })
  Object.keys(editErrors).forEach(k => delete editErrors[k])
  editing.value = true
}

function validateEdit(): boolean {
  Object.keys(editErrors).forEach(k => delete editErrors[k])
  const hasPct = editForm.percentage !== '' && editForm.percentage !== null
  const hasAmt = editForm.amount !== '' && editForm.amount !== null
  if (hasPct === hasAmt) editErrors.value = t('finance.scholarship.percentOrAmount')
  if (hasPct && (Number(editForm.percentage) <= 0 || Number(editForm.percentage) > 100)) editErrors.percentage = t('finance.scholarship.percentRange')
  if (hasAmt && Number(editForm.amount) <= 0) editErrors.amount = t('finance.scholarship.amountPositive')
  if (!editForm.validFrom) editErrors.validFrom = t('validation.required', { field: t('finance.scholarship.validFrom') })
  if (editForm.validFrom && editForm.validTo && editForm.validTo < editForm.validFrom) editErrors.validTo = t('finance.scholarship.validToBeforeFrom')
  return Object.keys(editErrors).length === 0
}

async function saveEdit() {
  if (!detail.value || !validateEdit()) return
  editSaving.value = true
  try {
    const hasPct = editForm.percentage !== '' && editForm.percentage !== null
    const res = await financeService.updateScholarship(detail.value.id, {
      scholarshipTypeId: editForm.scholarshipTypeId || undefined,
      percentage: hasPct ? Number(editForm.percentage) : undefined,
      amount: hasPct ? undefined : Number(editForm.amount),
      validFrom: editForm.validFrom,
      validTo: editForm.validTo || undefined,
      note: editForm.note.trim() || undefined,
      rowVersion: detail.value.rowVersion,
    })
    if (!res.success || !res.data) throw new Error(res.message ?? t('common.error'))
    detail.value = res.data
    editing.value = false
    await loadList()
  } catch (e: unknown) {
    editErrors.submit = (e as Error).message
  } finally {
    editSaving.value = false
  }
}

const typeLabel = (id?: string) => (id ? scholarshipTypes.value.find(x => x.id === id)?.label ?? '—' : '—')

const columns: Column<ScholarshipListItemDto>[] = [
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'scholarshipTypeName', label: t('finance.scholarship.type'), width: '140px' },
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
        v-if="can(Permissions.Scholarships.Grant)"
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

    <DataTable :columns="columns" :rows="list.items" :loading="loading" @row-click="openDetail">
      <template #cell-scholarshipTypeName="{ value }"><span class="text-muted-foreground">{{ value ?? '—' }}</span></template>
      <template #cell-percentage="{ value }"><span v-if="value" class="font-mono">%{{ value }}</span><span v-else class="text-muted-foreground">—</span></template>
      <template #cell-amount="{ row }"><span v-if="row.amount" class="font-mono">{{ row.amount }}</span><span v-else class="text-muted-foreground">—</span></template>
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

    <!-- Ayrıntı / düzenleme -->
    <div v-if="detailOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-card rounded-xl shadow-xl p-6 w-full max-w-lg border border-border">
        <h3 class="font-semibold mb-4">{{ editing ? t('finance.scholarship.edit') : t('finance.scholarship.detail') }}</h3>
        <p v-if="detailLoading" class="text-sm text-muted-foreground py-6 text-center">{{ t('common.loading') }}</p>
        <p v-else-if="detailError" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">{{ detailError }}</p>

        <dl v-else-if="detail && !editing" class="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
          <div class="col-span-2"><dt class="text-xs text-muted-foreground">{{ t('student.fullName') }}</dt><dd class="font-medium">{{ detail.studentFullName }}</dd></div>
          <div><dt class="text-xs text-muted-foreground">{{ t('finance.scholarship.type') }}</dt><dd>{{ typeLabel(detail.scholarshipTypeId) }}</dd></div>
          <div><dt class="text-xs text-muted-foreground">{{ detail.percentage ? t('finance.scholarship.percentage') : t('finance.scholarship.amount') }}</dt>
            <dd class="font-mono">{{ detail.percentage ? '%' + detail.percentage : detail.amount }}</dd></div>
          <div><dt class="text-xs text-muted-foreground">{{ t('finance.scholarship.validFrom') }}</dt><dd>{{ formatDate(detail.validFrom) }}</dd></div>
          <div><dt class="text-xs text-muted-foreground">{{ t('finance.scholarship.validTo') }}</dt><dd>{{ formatDate(detail.validTo) }}</dd></div>
          <div class="col-span-2"><dt class="text-xs text-muted-foreground">{{ t('finance.scholarship.note') }}</dt><dd class="whitespace-pre-wrap">{{ detail.note || '—' }}</dd></div>
        </dl>

        <div v-else-if="detail && editing" class="space-y-3">
          <p v-if="editErrors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">{{ editErrors.submit }}</p>
          <p class="text-xs text-muted-foreground">{{ detail.studentFullName }}</p>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.type') }}</label>
            <select v-model="editForm.scholarshipTypeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="st in scholarshipTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
            </select>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.percentage') }}</label>
              <input v-model="editForm.percentage" type="number" min="0" max="100" step="0.01" :disabled="editForm.amount !== ''" :class="['w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none disabled:opacity-50', editErrors.percentage ? 'border-red-400' : 'border-border']" />
              <p v-if="editErrors.percentage" class="mt-1 text-xs text-red-600">{{ editErrors.percentage }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.amount') }}</label>
              <input v-model="editForm.amount" type="number" min="0" step="0.01" :disabled="editForm.percentage !== ''" :class="['w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none disabled:opacity-50', editErrors.amount ? 'border-red-400' : 'border-border']" />
              <p v-if="editErrors.amount" class="mt-1 text-xs text-red-600">{{ editErrors.amount }}</p>
            </div>
          </div>
          <p v-if="editErrors.value" class="text-xs text-red-600">{{ editErrors.value }}</p>
          <p class="text-xs text-muted-foreground">{{ t('finance.scholarship.percentOrAmountHint') }}</p>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.validFrom') }} *</label>
              <input v-model="editForm.validFrom" type="date" :class="['w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none', editErrors.validFrom ? 'border-red-400' : 'border-border']" />
              <p v-if="editErrors.validFrom" class="mt-1 text-xs text-red-600">{{ editErrors.validFrom }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.validTo') }}</label>
              <input v-model="editForm.validTo" type="date" :class="['w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none', editErrors.validTo ? 'border-red-400' : 'border-border']" />
              <p v-if="editErrors.validTo" class="mt-1 text-xs text-red-600">{{ editErrors.validTo }}</p>
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.note') }}</label>
            <textarea v-model="editForm.note" rows="2" maxlength="1000" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none resize-none" />
          </div>
        </div>

        <div class="flex justify-end gap-2 mt-4">
          <template v-if="editing">
            <button @click="editing = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
            <button @click="saveEdit" :disabled="editSaving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
              {{ editSaving ? t('common.saving') : t('common.save') }}
            </button>
          </template>
          <template v-else>
            <button @click="closeDetail" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.close') }}</button>
            <button v-if="detail && can(Permissions.Scholarships.Update)" @click="startEdit" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90">{{ t('common.edit') }}</button>
          </template>
        </div>
      </div>
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-card rounded-xl shadow-xl p-6 w-full max-w-lg border border-border">
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
