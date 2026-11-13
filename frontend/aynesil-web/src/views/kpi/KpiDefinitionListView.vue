<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useKpiStore } from '@/stores/kpi.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import { kpiService } from '@/services/kpi.service'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { KpiDefinitionListItemDto, KpiDefinitionListQuery } from '@/types/kpi.types'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const router = useRouter()
const kpiStore = useKpiStore()
const { can } = usePermission()

const filters = reactive<KpiDefinitionListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  isActive: undefined,
})

const columns: Column<KpiDefinitionListItemDto>[] = [
  { key: 'code', label: t('kpi.fields.code'), width: '120px' },
  { key: 'name', label: t('kpi.fields.name') },
  { key: 'categoryCode', label: t('kpi.fields.category'), width: '140px' },
  { key: 'unit', label: t('kpi.fields.unit'), width: '100px' },
  { key: 'isActive', label: t('common.status'), width: '110px' },
]

// ── Oluştur / düzenle ────────────────────────────────────────────────────────
// Platform (global) tanımlar salt okunur: corporationId boşsa düzenleme gösterilmez.
const showForm = ref(false)
const editTarget = ref<KpiDefinitionListItemDto | null>(null)
const rowVersion = ref(0)
const formError = ref('')
const form = reactive({ code: '', name: '', categoryId: '', unit: '', spec: '' })

function openCreate() {
  editTarget.value = null
  Object.assign(form, { code: '', name: '', categoryId: '', unit: '', spec: '' })
  formError.value = ''
  showForm.value = true
}

async function openEdit(row: KpiDefinitionListItemDto) {
  formError.value = ''
  try {
    const res = await kpiService.getDefinition(row.id)
    if (!res.success || !res.data) return
    editTarget.value = row
    rowVersion.value = res.data.rowVersion
    Object.assign(form, {
      code: res.data.code, name: res.data.name, categoryId: res.data.categoryId ?? '',
      unit: res.data.unit ?? '', spec: res.data.spec && res.data.spec !== '{}' ? res.data.spec : '',
    })
    showForm.value = true
  } catch (e: unknown) {
    kpiStore.error = (e as Error).message
  }
}

async function submitForm() {
  formError.value = ''
  if (!editTarget.value && !/^[a-z][a-z0-9_.]*$/.test(form.code)) { formError.value = t('kpi.definitions.codeFormat'); return }
  if (!form.name.trim()) { formError.value = t('validation.required', { field: t('kpi.fields.name') }); return }
  if (form.spec.trim()) {
    try {
      const parsed = JSON.parse(form.spec)
      if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) throw new Error('not object')
    } catch { formError.value = t('kpi.definitions.specInvalid'); return }
  }
  const common = {
    name: form.name.trim(),
    categoryId: form.categoryId || undefined,
    unit: form.unit.trim() || undefined,
    spec: form.spec.trim() || undefined,
  }
  try {
    if (editTarget.value) {
      await kpiStore.updateDefinition(editTarget.value.id, { ...common, rowVersion: rowVersion.value })
    } else {
      await kpiStore.createDefinition({ code: form.code.trim(), ...common })
    }
    showForm.value = false
    await kpiStore.fetchDefinitions(filters)
  } catch (e: unknown) {
    formError.value = (e as Error).message
  }
}

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary'

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(doFetch, 400)
}

async function doFetch() {
  filters.page = 1
  await kpiStore.fetchDefinitions(filters)
}

watch(
  () => filters.page,
  () => kpiStore.fetchDefinitions(filters)
)

onMounted(async () => {
  await kpiStore.fetchCategories()
  await kpiStore.fetchDefinitions(filters)
})
</script>

<template>
  <div>
    <PageHeader :title="t('kpi.definitions.title')" :description="t('kpi.definitions.subtitle')">
      <button
        v-if="can(Permissions.Kpi.Manage)"
        class="px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        @click="openCreate"
      >
        {{ t('kpi.definitions.create') }}
      </button>
    </PageHeader>

    <p v-if="kpiStore.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ kpiStore.error }}</p>

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
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('kpi.fields.category') }}</label>
        <select
          v-model="filters.categoryId"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option v-for="c in kpiStore.categories" :key="c.id" :value="c.id">{{ c.label || c.code }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.isActive"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option :value="undefined">{{ t('common.allStatuses') }}</option>
          <option :value="true">{{ t('common.active') }}</option>
          <option :value="false">{{ t('common.passive') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="kpiStore.definitions.items"
      :loading="kpiStore.loading"
      :empty-text="t('kpi.definitions.noData')"
      @row-click="(row) => router.push({ name: 'kpi-definition-detail', params: { id: row.id } })"
    >
      <template #cell-code="{ value }">
        <span class="font-mono text-xs font-semibold text-foreground">{{ value }}</span>
      </template>
      <template #cell-name="{ value }">
        <span class="text-foreground">{{ value }}</span>
      </template>
      <template #cell-categoryCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-unit="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-isActive="{ value }">
        <StatusBadge :value="Boolean(value)" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'kpi-definition-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can(Permissions.Kpi.Manage) && row.corporationId"
            @click="openEdit(row)"
            class="px-2.5 py-1 text-xs rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="can(Permissions.Kpi.Manage)"
            @click="kpiStore.toggleDefinition(row.id, !row.isActive)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="row.isActive ? t('common.passive') : t('common.active')"
          >
            <svg v-if="row.isActive" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <FormModal
      :open="showForm"
      :title="editTarget ? t('kpi.definitions.editTitle') : t('kpi.definitions.create')"
      :saving="kpiStore.saving"
      @submit="submitForm"
      @close="showForm = false"
    >
      <div class="space-y-4">
        <p v-if="formError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ formError }}</p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('kpi.fields.code') }} *</label>
          <input v-model="form.code" type="text" :disabled="!!editTarget" :class="inputClass" placeholder="ornek.kod" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('kpi.fields.name') }} *</label>
          <input v-model="form.name" type="text" maxlength="200" :class="inputClass" />
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('kpi.fields.category') }}</label>
            <select v-model="form.categoryId" :class="inputClass">
              <option value="">—</option>
              <option v-for="c in kpiStore.categories" :key="c.id" :value="c.id">{{ c.label || c.code }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('kpi.fields.unit') }}</label>
            <input v-model="form.unit" type="text" maxlength="20" :class="inputClass" placeholder="%, count, hours" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('kpi.definitions.spec') }}</label>
          <textarea v-model="form.spec" rows="6" :class="inputClass + ' font-mono'" placeholder="{}" />
          <p class="mt-1 text-xs text-muted-foreground">{{ t('kpi.definitions.specHint') }}</p>
        </div>
      </div>
    </FormModal>

    <div class="mt-4">
      <Pagination
        :page="kpiStore.definitions.page"
        :page-size="kpiStore.definitions.pageSize"
        :total-count="kpiStore.definitions.totalCount"
        :total-pages="kpiStore.definitions.totalPages"
        :has-previous-page="kpiStore.definitions.hasPreviousPage"
        :has-next-page="kpiStore.definitions.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; kpiStore.fetchDefinitions(filters) }"
      />
    </div>
  </div>
</template>
