<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ConsultancyReportListItemDto } from '@/types/consultancy.types'
import { ref } from 'vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const auth = useAuthStore()
const { can } = usePermission()

const filters = reactive({
  page: 1,
  pageSize: 20,
  search: '',
  corporationId: auth.user?.corporationId as string | undefined,
  consultancyPlanId: (route.query.planId as string) || '',
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
  store.fetchReports({ ...filters, consultancyPlanId: filters.consultancyPlanId || undefined })
}

watch(() => filters.page, load)
onMounted(load)

// ── Create ────────────────────────────────────────────────────────────────────
const showCreate = ref(false)
const saving = ref(false)
const formError = ref('')
const form = reactive({
  consultancyPlanId: '',
  title: '',
  summary: '',
})

function openCreate() {
  form.consultancyPlanId = filters.consultancyPlanId || ''
  form.title = ''
  form.summary = ''
  formError.value = ''
  showCreate.value = true
  if (!store.plans.items.length) {
    store.fetchPlans({ corporationId: auth.user?.corporationId, page: 1, pageSize: 200 })
  }
}

async function submitCreate() {
  if (!form.title.trim()) {
    formError.value = t('validation.required', { field: t('consultancy.report.fields.title') })
    return
  }
  if (!form.consultancyPlanId) {
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
      corporationId: auth.user!.corporationId!,
      consultancyPlanId: form.consultancyPlanId,
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
        v-if="can('consultancy_report:create')"
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

    <DataTable
      :columns="columns"
      :rows="store.reports.items"
      :loading="store.loading"
      :empty-text="t('consultancy.report.list.noData')"
    >
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
