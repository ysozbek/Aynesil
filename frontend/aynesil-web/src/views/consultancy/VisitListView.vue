<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { SchoolVisitListItemDto, VisitListQuery } from '@/types/consultancy.types'

const { t } = useI18n()
const router = useRouter()
const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()
const { can } = usePermission()
const showCreateModal = ref(false)

const filters = reactive<VisitListQuery>({
  page: 1,
  pageSize: 20,
  status: '',
  from: '',
  to: '',
  corporationId: authStore.user?.corporationId,
})
const createForm = reactive({ institutionId: '', visitDate: '', purpose: '' })

const columns: Column<SchoolVisitListItemDto>[] = [
  { key: 'institutionName', label: t('consultancy.institution.fields.name') },
  { key: 'planName', label: t('consultancy.visit.fields.plan') },
  { key: 'visitDate', label: t('consultancy.visit.fields.visitDate'), width: '110px' },
  { key: 'purpose', label: t('consultancy.visit.fields.purpose') },
  { key: 'observationCount', label: t('consultancy.visit.fields.observations'), width: '100px' },
  { key: 'status', label: t('common.status'), width: '110px' },
]

function visitStatusClass(s: string) {
  const map: Record<string, string> = {
    Scheduled: 'bg-blue-100 text-blue-700',
    Completed: 'bg-green-100 text-green-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function visitStatusLabel(s: string) {
  const map: Record<string, string> = {
    Scheduled: t('consultancy.visit.status.scheduled'),
    Completed: t('consultancy.visit.status.completed'),
    Cancelled: t('consultancy.visit.status.cancelled'),
  }
  return map[s] ?? s
}

async function doFetch() {
  filters.page = 1
  await consultancyStore.fetchVisits(filters)
}

async function doCreate() {
  const result = await consultancyStore.createVisit({
    corporationId: authStore.user?.corporationId ?? '',
    institutionId: createForm.institutionId,
    visitDate: createForm.visitDate,
    purpose: createForm.purpose || undefined,
  })
  showCreateModal.value = false
  router.push(`/consultancy/visits/${result.id}`)
}

onMounted(doFetch)
</script>

<template>
  <div>
    <PageHeader :title="t('consultancy.visit.list.title')" :description="t('consultancy.visit.list.subtitle')">
      <button
        v-if="can('school_visit:create')"
        @click="showCreateModal = true"
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
          <option value="Scheduled">{{ t('consultancy.visit.status.scheduled') }}</option>
          <option value="Completed">{{ t('consultancy.visit.status.completed') }}</option>
          <option value="Cancelled">{{ t('consultancy.visit.status.cancelled') }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.from') }}</label>
        <input v-model="filters.from" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch" />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.to') }}</label>
        <input v-model="filters.to" type="date" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="doFetch" />
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
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', visitStatusClass(String(value))]">
          {{ visitStatusLabel(String(value)) }}
        </span>
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
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.name') }} ID *</label>
          <input v-model="createForm.institutionId" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
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
