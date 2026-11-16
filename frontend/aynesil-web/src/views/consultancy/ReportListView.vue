<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ConsultancyReportDto, ConsultancyReportListItemDto } from '@/types/consultancy.types'
import { consultancyService } from '@/services/consultancy.service'
import { ref } from 'vue'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const { visible } = useModule(Modules.Consultancy)

const filters = reactive({
  page: 1,
  pageSize: 20,
  search: '',
  consultancyPlanId: (route.query.planId as string) || '',
  schoolVisitId: (route.query.visitId as string) || '',
})

const columns: Column<ConsultancyReportListItemDto>[] = [
  { key: 'title', label: t('consultancy.report.fields.title') },
  { key: 'planName', label: t('consultancy.report.fields.plan') },
  { key: 'visitDate', label: t('consultancy.report.fields.visitDate'), width: '120px' },
  { key: 'hasFile', label: t('consultancy.report.fields.file'), width: '90px' },
  { key: 'createdAt', label: t('common.createdAt'), width: '120px' },
]

function formatDate(val: unknown) {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { filters.page = 1; load() }, 400)
}

function load() {
  store.fetchReports({
    ...filters,
    consultancyPlanId: filters.consultancyPlanId || undefined,
    schoolVisitId: filters.schoolVisitId || undefined,
  })
}

watch(() => filters.page, load)
onMounted(async () => {
  load()
  // Ziyaret ayrıntısından "Rapor Yaz" ile gelindiyse oluşturma penceresi açık başlar.
  if (route.query.new === '1' && visible(Permissions.ConsultancyReports.Create)) await openCreate()
})

// ── Ayrıntı ve silme ───────────────────────────────────────────────────────────
const detail = ref<ConsultancyReportDto | null>(null)
const listError = ref('')
const toDelete = ref<ConsultancyReportListItemDto | null>(null)

async function openDetail(row: ConsultancyReportListItemDto) {
  listError.value = ''
  try {
    const res = await consultancyService.getReport(row.id)
    if (res.success && res.data) detail.value = res.data
  } catch (e: unknown) {
    listError.value = (e as Error).message
  }
}

async function doDelete() {
  if (!toDelete.value) return
  listError.value = ''
  try {
    await store.deleteReport(toDelete.value.id)
    load()
  } catch (e: unknown) {
    listError.value = (e as Error).message
  }
  toDelete.value = null
}

// ── Create ────────────────────────────────────────────────────────────────────
const showCreate = ref(false)
const saving = ref(false)
const formError = ref('')
const form = reactive({
  consultancyPlanId: '',
  schoolVisitId: '',
  title: '',
  summary: '',
})

async function openCreate() {
  form.consultancyPlanId = filters.consultancyPlanId || ''
  form.schoolVisitId = filters.schoolVisitId || ''
  form.title = ''
  form.summary = ''
  formError.value = ''
  showCreate.value = true
  if (!store.plans.items.length) {
    await store.fetchPlans({ page: 1, pageSize: 200 })
  }
  // Ziyaretten gelindiyse plan, ziyaretin planından önceden seçilir.
  if (form.schoolVisitId && !form.consultancyPlanId) {
    try {
      const res = await consultancyService.getVisit(form.schoolVisitId)
      if (res.success && res.data?.consultancyPlanId) form.consultancyPlanId = res.data.consultancyPlanId
    } catch { /* plan zorunlu değil: kullanıcı elle seçer */ }
  }
}

async function submitCreate() {
  if (!form.title.trim()) {
    formError.value = t('validation.required', { field: t('consultancy.report.fields.title') })
    return
  }
  if (!form.consultancyPlanId && !form.schoolVisitId) {
    formError.value = t('validation.required', { field: t('consultancy.report.fields.plan') })
    return
  }
  if (!form.summary.trim()) {
    formError.value = t('validation.required', { field: t('consultancy.report.fields.summary') })
    return
  }
  saving.value = true
  try {
    await store.createReport({
      consultancyPlanId: form.consultancyPlanId || undefined,
      schoolVisitId: form.schoolVisitId || undefined,
      title: form.title.trim(),
      summary: form.summary.trim(),
    })
    showCreate.value = false
    load()
  } catch (err: unknown) {
    formError.value = (err as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('consultancy.report.list.title')" :description="t('consultancy.report.list.subtitle')">
      <button
        v-if="visible(Permissions.ConsultancyReports.Create)"
        type="button"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('consultancy.report.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @input="debouncedFetch"
        />
      </div>
    </div>

    <p v-if="listError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ listError }}</p>

    <DataTable
      :columns="columns"
      :rows="store.reports.items"
      :loading="store.loading"
      :empty-text="t('consultancy.report.list.noData')"
      @row-click="openDetail"
    >
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="row.schoolVisitId"
            class="px-2 py-1 text-xs rounded border border-border hover:bg-accent"
            @click="router.push({ name: 'consultancy-visit-detail', params: { id: row.schoolVisitId } })"
          >
            {{ t('consultancy.report.fields.visit') }}
          </button>
          <button
            v-if="visible(Permissions.ConsultancyReports.Delete)"
            class="px-2 py-1 text-xs rounded border border-red-200 text-red-700 hover:bg-red-50"
            @click="listError = ''; toDelete = row"
          >
            {{ t('common.delete') }}
          </button>
        </div>
      </template>
      <template #cell-planName="{ value }">{{ value ?? '—' }}</template>
      <template #cell-visitDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-hasFile="{ value }">
        <span :class="value ? 'text-green-700' : 'text-muted-foreground'">
          {{ value ? t('common.yes') : t('common.no') }}
        </span>
      </template>
      <template #cell-createdAt="{ value }">{{ formatDate(value) }}</template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.reports.page"
        :page-size="store.reports.pageSize"
        :total-count="store.reports.totalCount"
        :total-pages="store.reports.totalPages"
        :has-previous-page="store.reports.hasPreviousPage"
        :has-next-page="store.reports.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; load() }"
      />
    </div>

    <ConfirmModal
      :open="!!toDelete"
      :title="t('consultancy.report.delete')"
      :message="t('consultancy.report.deleteConfirm')"
      :loading="store.saving"
      @confirm="doDelete"
      @cancel="toDelete = null"
    />

    <div v-if="detail" class="fixed inset-0 z-[9999] flex items-center justify-center p-4">
      <div class="absolute inset-0 bg-black/50" @click="detail = null" />
      <div class="relative z-10 w-full max-w-lg rounded-2xl bg-white p-6 shadow-2xl border border-gray-100">
        <h2 class="text-base font-semibold mb-1">{{ detail.title }}</h2>
        <p class="text-xs text-muted-foreground mb-4">
          {{ detail.planName ?? '—' }} · {{ formatDate(detail.visitDate) }} · {{ formatDate(detail.createdAt) }}
        </p>
        <p class="text-sm whitespace-pre-wrap">{{ detail.summary || t('consultancy.report.noSummary') }}</p>
        <div class="mt-6 text-right">
          <button class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent" @click="detail = null">
            {{ t('common.back') }}
          </button>
        </div>
      </div>
    </div>

    <FormModal
      :open="showCreate"
      :title="t('consultancy.report.new')"
      :saving="saving"
      @close="showCreate = false"
      @submit="submitCreate"
    >
      <div class="space-y-4">
        <p v-if="formError" class="text-sm text-red-600">{{ formError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.report.fields.plan') }}</label>
          <select v-model="form.consultancyPlanId" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="p in store.plans.items" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.report.fields.title') }}</label>
          <input v-model="form.title" type="text" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.report.fields.summary') }}</label>
          <textarea v-model="form.summary" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
