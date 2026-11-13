<script setup lang="ts">
import { ContractStatus } from '@/constants/statuses'
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import { legalService } from '@/services/legal.service'
import { studentService } from '@/services/student.service'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { ContractListQuery, ContractTemplateListItemDto, StudentContractListItemDto } from '@/types/legal.types'
import type { StudentGuardianDto } from '@/types/student.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const contractStore = useContractStore()
const { can } = usePermission()

const filters = reactive<ContractListQuery>({
  page: 1,
  pageSize: 20,
  status: '',
})

const columns: Column<StudentContractListItemDto>[] = [
  { key: 'studentFullName', label: t('legal.contract.fields.student') },
  { key: 'templateCode', label: t('legal.contract.fields.template') },
  { key: 'startsOn', label: t('legal.contract.fields.startsOn'), width: '110px' },
  { key: 'endsOn', label: t('legal.contract.fields.endsOn'), width: '110px' },
  { key: 'signedAt', label: t('legal.contract.fields.signedAt'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '120px' },
]

function formatDate(dt: unknown) {
  if (!dt) return '—'
  return new Date(String(dt)).toLocaleDateString('tr-TR')
}

function doFetch() {
  filters.page = 1
  contractStore.fetchContracts(filters)
}

function resetFilters() {
  filters.status = ''
  filters.page = 1
  contractStore.fetchContracts(filters)
}

watch(
  () => filters.page,
  () => contractStore.fetchContracts(filters)
)

// ── Yeni sözleşme (taslak) ───────────────────────────────────────────────────
// /legal/contracts/new rotası bu görünümü açar ve formu gösterir. Oluşan sözleşme taslaktır;
// gönder → imzala → etkinleştir adımları detay sayfasındadır.
const showCreate = ref(false)
const createError = ref('')
const templates = ref<ContractTemplateListItemDto[]>([])
const guardians = ref<StudentGuardianDto[]>([])
const createForm = reactive({ studentId: '', templateId: '', guardianId: '', startsOn: '', endsOn: '' })

async function openCreate() {
  Object.assign(createForm, { studentId: '', templateId: '', guardianId: '', startsOn: '', endsOn: '' })
  createError.value = ''
  guardians.value = []
  showCreate.value = true
  try {
    const res = await legalService.listContractTemplates({ isCurrent: true, pageSize: 100 })
    if (res.success && res.data) templates.value = res.data.items
  } catch (e: unknown) {
    createError.value = (e as Error).message
  }
}

function closeCreate() {
  showCreate.value = false
  if (route.name === 'contract-new') router.replace({ name: 'contracts' })
}

watch(() => createForm.studentId, async (id) => {
  createForm.guardianId = ''
  guardians.value = []
  if (!id) return
  try {
    const res = await studentService.getGuardians(id)
    if (res.success && res.data) guardians.value = res.data
  } catch { /* veli listesi yüklenemezse veli seçimi boş kalır */ }
})

async function submitCreate() {
  createError.value = ''
  if (!createForm.studentId) { createError.value = t('validation.required', { field: t('legal.contract.fields.student') }); return }
  if (createForm.startsOn && createForm.endsOn && createForm.endsOn < createForm.startsOn) {
    createError.value = t('legal.contract.form.dateOrder'); return
  }
  try {
    const created = await contractStore.createContract({
      studentId: createForm.studentId,
      templateId: createForm.templateId || undefined,
      guardianId: createForm.guardianId || undefined,
      startsOn: createForm.startsOn || undefined,
      endsOn: createForm.endsOn || undefined,
    })
    showCreate.value = false
    router.push({ name: 'contract-detail', params: { id: created.id } })
  } catch (e: unknown) {
    createError.value = (e as Error).message
  }
}

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary'

onMounted(() => {
  contractStore.fetchContracts(filters)
  if (route.name === 'contract-new') openCreate()
})
</script>

<template>
  <div>
    <PageHeader :title="t('legal.contract.list.title')" :description="t('legal.contract.list.subtitle')">
      <button
        v-if="can(Permissions.StudentContracts.Generate)"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('legal.contract.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.status"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option :value="ContractStatus.Draft">{{ t('statusBadge.contract.draft') }}</option>
          <option :value="ContractStatus.Sent">{{ t('statusBadge.contract.sent') }}</option>
          <option :value="ContractStatus.Signed">{{ t('statusBadge.contract.signed') }}</option>
          <option :value="ContractStatus.Active">{{ t('statusBadge.contract.active') }}</option>
          <option :value="ContractStatus.Expired">{{ t('statusBadge.contract.expired') }}</option>
          <option :value="ContractStatus.Terminated">{{ t('statusBadge.contract.terminated') }}</option>
        </select>
      </div>
      <button
        @click="resetFilters"
        class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="contractStore.contracts.items"
      :loading="contractStore.loading"
      :empty-text="t('legal.contract.list.noData')"
      @row-click="(row) => router.push({ name: 'contract-detail', params: { id: row.id } })"
    >
      <template #cell-studentFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-templateCode="{ row }">
        <span class="text-muted-foreground">
          {{ row.templateCode ? `${row.templateCode} v${row.templateVersion}` : '—' }}
        </span>
      </template>
      <template #cell-startsOn="{ value }">{{ value ?? '—' }}</template>
      <template #cell-endsOn="{ value }">{{ value ?? '—' }}</template>
      <template #cell-signedAt="{ value }">{{ formatDate(value) }}</template>
      <template #cell-status="{ value }">
        <StatusBadge entity="contract" :status="String(value)" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'contract-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="contractStore.contracts.page"
        :page-size="contractStore.contracts.pageSize"
        :total-count="contractStore.contracts.totalCount"
        :total-pages="contractStore.contracts.totalPages"
        :has-previous-page="contractStore.contracts.hasPreviousPage"
        :has-next-page="contractStore.contracts.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; contractStore.fetchContracts(filters) }"
      />
    </div>

    <FormModal
      :open="showCreate"
      :title="t('legal.contract.new')"
      :saving="contractStore.saving"
      @submit="submitCreate"
      @close="closeCreate"
    >
      <div class="space-y-4">
        <p v-if="createError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ createError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.student') }} *</label>
          <StudentPicker v-model="createForm.studentId" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.template') }}</label>
          <select v-model="createForm.templateId" :class="inputClass">
            <option value="">—</option>
            <option v-for="tpl in templates" :key="tpl.id" :value="tpl.id">{{ tpl.code }} v{{ tpl.version }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.form.guardian') }}</label>
          <select v-model="createForm.guardianId" :disabled="!guardians.length" :class="inputClass">
            <option value="">—</option>
            <option v-for="g in guardians" :key="g.guardianId" :value="g.guardianId">{{ g.guardianFullName }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.startsOn') }}</label>
            <input v-model="createForm.startsOn" type="date" :class="inputClass" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.endsOn') }}</label>
            <input v-model="createForm.endsOn" type="date" :class="inputClass" />
          </div>
        </div>
      </div>
    </FormModal>
  </div>
</template>
