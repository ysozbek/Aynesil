<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ConsultancyPlanListItemDto, PlanListQuery, InstitutionListItemDto } from '@/types/consultancy.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const store = useConsultancyStore()
const auth = useAuthStore()
const { can } = usePermission()
const refData = useRefDataStore()

const filters = reactive<PlanListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  status: '',
  corporationId: auth.user?.corporationId,
})

const columns: Column<ConsultancyPlanListItemDto>[] = [
  { key: 'name', label: t('consultancy.plan.fields.name') },
  { key: 'institutionName', label: t('consultancy.plan.fields.institution') },
  { key: 'consultancyTypeCode', label: t('consultancy.plan.fields.type'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '110px' },
  { key: 'periodStart', label: t('consultancy.plan.fields.periodStart'), width: '110px' },
  { key: 'periodEnd', label: t('consultancy.plan.fields.periodEnd'), width: '110px' },
  { key: 'visitCount', label: t('consultancy.plan.fields.visitCount'), width: '80px' },
]

const planTypes = ref<RefValueItem[]>([])
const institutions = ref<InstitutionListItemDto[]>([])

function formatDate(val: unknown) {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function statusClass(status: string) {
  const map: Record<string, string> = {
    draft: 'bg-gray-100 text-gray-600',
    active: 'bg-green-100 text-green-700',
    completed: 'bg-blue-100 text-blue-700',
    cancelled: 'bg-red-100 text-red-700',
  }
  return map[status?.toLowerCase()] ?? 'bg-gray-100 text-gray-600'
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { filters.page = 1; store.fetchPlans(filters) }, 400)
}

function doFetch() {
  filters.page = 1
  store.fetchPlans(filters)
}

watch(() => filters.page, () => store.fetchPlans(filters))

onMounted(async () => {
  planTypes.value = await refData.getValues('consultancy_type')
  await store.fetchInstitutions({ corporationId: auth.user?.corporationId, page: 1, pageSize: 200 })
  institutions.value = store.institutions.items
  await store.fetchPlans(filters)
})

// ── Create modal ──────────────────────────────────────────────────────────────
const showCreate = ref(false)
const saving = ref(false)
const formError = ref('')
const form = reactive({
  institutionId: '',
  consultancyTypeId: '',
  name: '',
  periodStart: '',
  periodEnd: '',
  scope: '',
})

function openCreate() {
  Object.assign(form, { institutionId: '', consultancyTypeId: '', name: '', periodStart: '', periodEnd: '', scope: '' })
  formError.value = ''
  showCreate.value = true
}

async function submitCreate() {
  if (!form.institutionId || !form.name.trim()) {
    formError.value = t('validation.required', { field: t('consultancy.plan.fields.name') })
    return
  }
  saving.value = true
  formError.value = ''
  try {
    const created = await store.createPlan({
      corporationId: auth.user!.corporationId!,
      institutionId: form.institutionId,
      consultancyTypeId: form.consultancyTypeId || undefined,
      name: form.name.trim(),
      periodStart: form.periodStart || undefined,
      periodEnd: form.periodEnd || undefined,
      scope: form.scope || undefined,
    })
    showCreate.value = false
    router.push({ name: 'consultancy-plan-detail', params: { id: created.id } })
  } catch (err: unknown) {
    formError.value = (err as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('consultancy.plan.list.title')" :description="t('consultancy.plan.list.subtitle')">
      <button
        v-if="can('consultancy_plan:create')"
        type="button"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('consultancy.plan.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          @input="debouncedFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option value="draft">{{ t('consultancy.plan.status.draft') }}</option>
          <option value="active">{{ t('consultancy.plan.status.active') }}</option>
          <option value="completed">{{ t('consultancy.plan.status.completed') }}</option>
          <option value="cancelled">{{ t('consultancy.plan.status.cancelled') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.plans.items"
      :loading="store.loading"
      :empty-text="t('consultancy.plan.list.noData')"
      @row-click="(row) => router.push({ name: 'consultancy-plan-detail', params: { id: row.id } })"
    >
      <template #cell-consultancyTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
          {{ t(`consultancy.plan.status.${String(value).toLowerCase()}`, String(value)) }}
        </span>
      </template>
      <template #cell-periodStart="{ value }">{{ formatDate(value) }}</template>
      <template #cell-periodEnd="{ value }">{{ formatDate(value) }}</template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.plans.page"
        :page-size="store.plans.pageSize"
        :total-count="store.plans.totalCount"
        :total-pages="store.plans.totalPages"
        :has-previous-page="store.plans.hasPreviousPage"
        :has-next-page="store.plans.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; store.fetchPlans(filters) }"
      />
    </div>

    <FormModal
      :open="showCreate"
      :title="t('consultancy.plan.new')"
      :saving="saving"
      @close="showCreate = false"
      @submit="submitCreate"
    >
      <div class="space-y-4">
        <p v-if="formError" class="text-sm text-red-600">{{ formError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.plan.fields.institution') }}</label>
          <select v-model="form.institutionId" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="inst in institutions" :key="inst.id" :value="inst.id">{{ inst.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.plan.fields.name') }}</label>
          <input v-model="form.name" type="text" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.plan.fields.type') }}</label>
          <select v-model="form.consultancyTypeId" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="pt in planTypes" :key="pt.id" :value="pt.id">{{ pt.label }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('consultancy.plan.fields.periodStart') }}</label>
            <input v-model="form.periodStart" type="date" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('consultancy.plan.fields.periodEnd') }}</label>
            <input v-model="form.periodEnd" type="date" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.plan.fields.scope') }}</label>
          <textarea v-model="form.scope" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
