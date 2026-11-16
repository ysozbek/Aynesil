<script setup lang="ts">
import { SchoolVisitStatus } from '@/constants/statuses'
import { computed, reactive, ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { SchoolVisitListItemDto, VisitListQuery } from '@/types/consultancy.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const consultancyStore = useConsultancyStore()
const { visible } = useModule(Modules.Consultancy)
const showCreateModal = ref(false)

const filters = reactive<VisitListQuery>({
  page: 1,
  pageSize: 20,
  status: '',
  dateFrom: '',
  dateTo: '',
  consultancyPlanId: (route.query.planId as string) || '',
})
const createForm = reactive({ institutionId: '', consultancyPlanId: '', visitDate: '', purpose: '' })
const createError = ref('')
const institutionPlans = computed(() =>
  consultancyStore.plans.items.filter((p) => p.institutionId === createForm.institutionId && p.status === 'active'),
)

async function openCreate() {
  Object.assign(createForm, { institutionId: '', consultancyPlanId: '', visitDate: '', purpose: '' })
  createError.value = ''
  showCreateModal.value = true
  await Promise.all([
    consultancyStore.fetchInstitutions({ page: 1, pageSize: 200 }),
    consultancyStore.fetchPlans({ page: 1, pageSize: 200, status: 'active' }),
  ])
}

const columns: Column<SchoolVisitListItemDto>[] = [
  { key: 'institutionName', label: t('consultancy.institution.fields.name') },
  { key: 'planName', label: t('consultancy.visit.fields.plan') },
  { key: 'visitDate', label: t('consultancy.visit.fields.visitDate'), width: '110px' },
  { key: 'purpose', label: t('consultancy.visit.fields.purpose') },
  { key: 'observationCount', label: t('consultancy.visit.fields.observations'), width: '100px' },
  { key: 'status', label: t('common.status'), width: '110px' },
]

async function doFetch() {
  filters.page = 1
  await consultancyStore.fetchVisits({ ...filters, consultancyPlanId: filters.consultancyPlanId || undefined })
}

async function doCreate() {
  if (!createForm.institutionId || !createForm.visitDate) {
    createError.value = t('validation.required', {
      field: !createForm.institutionId ? t('consultancy.visit.fields.institution') : t('consultancy.visit.fields.visitDate'),
    })
    return
  }
  createError.value = ''
  try {
    const result = await consultancyStore.createVisit({
      institutionId: createForm.institutionId,
      consultancyPlanId: createForm.consultancyPlanId || undefined,
      visitDate: createForm.visitDate,
      purpose: createForm.purpose || undefined,
    })
    showCreateModal.value = false
    router.push({ name: 'consultancy-visit-detail', params: { id: result.id } })
  } catch (e: unknown) {
    createError.value = (e as Error).message
  }
}

onMounted(doFetch)
</script>

<template>
  <div>
    <PageHeader :title="t('consultancy.visit.list.title')" :description="t('consultancy.visit.list.subtitle')">
      <button
        v-if="visible(Permissions.SchoolVisits.Create)"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('consultancy.visit.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.status" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option :value="SchoolVisitStatus.Planned">{{ t('statusBadge.schoolVisit.planned') }}</option>
          <option :value="SchoolVisitStatus.Completed">{{ t('statusBadge.schoolVisit.completed') }}</option>
          <option :value="SchoolVisitStatus.Cancelled">{{ t('statusBadge.schoolVisit.cancelled') }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.from') }}</label>
        <input v-model="filters.dateFrom" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch" />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.to') }}</label>
        <input v-model="filters.dateTo" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch" />
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="consultancyStore.visits.items"
      :loading="consultancyStore.loading"
      :empty-text="t('consultancy.visit.list.noData')"
      @row-click="(row) => router.push(`/consultancy/visits/${row.id}`)"
    >
      <template #cell-institutionName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-planName="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-visitDate="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
      </template>
      <template #cell-purpose="{ value }">
        <span class="text-muted-foreground truncate block max-w-[200px]">{{ value ?? '—' }}</span>
      </template>
      <template #cell-observationCount="{ value }">
        <span class="text-center block">{{ value }}</span>
      </template>
      <template #cell-status="{ value }">
        <StatusBadge entity="schoolVisit" :status="String(value)" />
      </template>
    </DataTable>

    <FormModal
      :open="showCreateModal"
      :title="t('consultancy.visit.new')"
      :saving="consultancyStore.saving"
      @submit="doCreate"
      @close="showCreateModal = false"
    >
      <div class="space-y-4">
        <p v-if="createError" class="text-sm text-red-600">{{ createError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.visit.fields.institution') }} *</label>
          <select v-model="createForm.institutionId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" @change="createForm.consultancyPlanId = ''">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="i in consultancyStore.institutions.items" :key="i.id" :value="i.id">{{ i.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.visit.fields.plan') }}</label>
          <select v-model="createForm.consultancyPlanId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('consultancy.visit.fields.noPlan') }}</option>
            <option v-for="p in institutionPlans" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.visit.fields.visitDate') }} *</label>
          <input v-model="createForm.visitDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.visit.fields.purpose') }}</label>
          <textarea v-model="createForm.purpose" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
