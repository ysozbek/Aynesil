<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRefDataStore, type RefValueItem } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'

const { t } = useI18n()
const refDataStore = useRefDataStore()
const { can } = usePermission()

const canManage = computed(() => can('ref_data:manage'))

const REF_TYPES = [
  { code: 'session_type', labelKey: 'refData.types.session_type' },
  { code: 'therapy_type', labelKey: 'refData.types.therapy_type' },
  { code: 'program_type', labelKey: 'refData.types.program_type' },
  { code: 'payment_method', labelKey: 'refData.types.payment_method' },
  { code: 'student_status', labelKey: 'refData.types.student_status' },
  { code: 'enrollment_status', labelKey: 'refData.types.enrollment_status' },
]

const selected = ref(REF_TYPES[0].code)
const values = ref<RefValueItem[]>([])
const loading = ref(false)

async function loadValues(typeCode: string) {
  loading.value = true
  selected.value = typeCode
  try {
    values.value = await refDataStore.getValues(typeCode, false)
  } finally {
    loading.value = false
  }
}

onMounted(() => loadValues(selected.value))

// ── Create / Edit modal ───────────────────────────────────────────────────────
const showForm = ref(false)
const saving = ref(false)
const editTarget = ref<RefValueItem | null>(null)
const form = reactive({
  code: '',
  label: '',
  sortOrder: 0,
  isDefault: false,
})
const formError = ref('')
const formErrors = reactive<Record<string, string>>({})

function openCreate() {
  editTarget.value = null
  form.code = ''
  form.label = ''
  form.sortOrder = 0
  form.isDefault = false
  formError.value = ''
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showForm.value = true
}

function canEdit(row: RefValueItem) {
  // Tenant-owned or shared catalog values; platform system vocabulary is read-only.
  return !row.isSystem
}

function openEdit(row: RefValueItem, e: Event) {
  e.stopPropagation()
  if (!canEdit(row)) return
  editTarget.value = row
  form.code = row.code
  form.label = row.label
  form.sortOrder = row.sortOrder
  form.isDefault = row.isDefault
  formError.value = ''
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showForm.value = true
}

function validateForm(): boolean {
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  let valid = true
  if (!editTarget.value && !form.code.trim()) {
    formErrors.code = t('validation.required', { field: t('refData.code') })
    valid = false
  } else if (!editTarget.value && !/^[a-z][a-z0-9_]*$/.test(form.code.trim().toLowerCase())) {
    formErrors.code = t('refData.codeFormat')
    valid = false
  }
  if (!form.label.trim()) {
    formErrors.label = t('validation.required', { field: t('refData.label') })
    valid = false
  }
  return valid
}

async function submitForm() {
  if (!validateForm()) return
  saving.value = true
  formError.value = ''
  try {
    if (editTarget.value) {
      await refDataStore.updateValue(editTarget.value.id, {
        label: form.label.trim(),
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      })
    } else {
      await refDataStore.createValue(selected.value, {
        code: form.code.trim().toLowerCase(),
        label: form.label.trim(),
        sortOrder: form.sortOrder,
        isDefault: form.isDefault,
      })
    }
    showForm.value = false
    await loadValues(selected.value)
  } catch (err: unknown) {
    const ax = err as { response?: { data?: { message?: string } }; message?: string }
    formError.value = ax.response?.data?.message || ax.message || t('errors.serverError')
  } finally {
    saving.value = false
  }
}

// ── Activate / Deactivate ─────────────────────────────────────────────────────
const statusTarget = ref<RefValueItem | null>(null)
const statusLoading = ref(false)

function confirmToggleStatus(row: RefValueItem, e: Event) {
  e.stopPropagation()
  statusTarget.value = row
}

async function doToggleStatus() {
  if (!statusTarget.value) return
  statusLoading.value = true
  try {
    await refDataStore.setActive(statusTarget.value.id, !statusTarget.value.isActive)
    statusTarget.value = null
    await loadValues(selected.value)
  } finally {
    statusLoading.value = false
  }
}

const selectedLabel = computed(() => {
  const rt = REF_TYPES.find((r) => r.code === selected.value)
  return rt ? t(rt.labelKey) : selected.value
})
</script>

<template>
  <div>
    <PageHeader :title="t('refData.title')" :description="t('refData.description')">
      <button
        v-if="canManage"
        type="button"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('refData.addValue') }}
      </button>
    </PageHeader>

    <div class="flex gap-5">
      <!-- Left: type list -->
      <div class="w-56 shrink-0">
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div class="px-4 py-3 border-b border-gray-100 text-xs font-semibold text-gray-400 uppercase tracking-wider">
            {{ t('refData.categories') }}
          </div>
          <nav class="p-2">
            <button
              v-for="rt in REF_TYPES"
              :key="rt.code"
              type="button"
              @click="loadValues(rt.code)"
              class="w-full text-left px-3 py-2 rounded-lg text-sm transition-colors"
              :class="selected === rt.code
                ? 'bg-primary text-primary-foreground font-medium'
                : 'text-gray-600 hover:bg-gray-50'"
            >
              {{ t(rt.labelKey) }}
            </button>
          </nav>
        </div>
      </div>

      <!-- Right: values -->
      <div class="flex-1">
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div class="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
            <span class="text-sm font-medium text-gray-700">{{ selectedLabel }}</span>
            <button
              v-if="canManage"
              type="button"
              @click="openCreate"
              class="px-3 py-1.5 text-xs bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity"
            >
              + {{ t('refData.addValue') }}
            </button>
          </div>

          <div v-if="loading" class="p-8 text-center text-gray-400 text-sm">
            {{ t('common.loading') }}
          </div>

          <div v-else-if="values.length === 0" class="p-8 text-center text-gray-400 text-sm">
            {{ t('refData.empty') }}
          </div>

          <table v-else class="w-full text-sm">
            <thead class="bg-gray-50 border-b border-gray-100">
              <tr>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                  {{ t('refData.code') }}
                </th>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                  {{ t('refData.label') }}
                </th>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                  {{ t('refData.sortOrder') }}
                </th>
                <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                  {{ t('common.status') }}
                </th>
                <th
                  v-if="canManage"
                  class="text-right px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider"
                >
                  {{ t('common.actions') }}
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-50">
              <tr v-for="v in values" :key="v.id" class="hover:bg-gray-50 transition-colors">
                <td class="px-6 py-3 font-mono text-xs text-gray-500">
                  {{ v.code }}
                  <span
                    v-if="v.isDefault"
                    class="ml-1 inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-blue-50 text-blue-700"
                  >
                    {{ t('refData.default') }}
                  </span>
                  <span
                    v-if="!v.isTenantOwned"
                    class="ml-1 inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-gray-100 text-gray-500"
                  >
                    {{ t('refData.shared') }}
                  </span>
                </td>
                <td class="px-6 py-3 text-gray-800">{{ v.label }}</td>
                <td class="px-6 py-3 text-gray-500">{{ v.sortOrder }}</td>
                <td class="px-6 py-3">
                  <span
                    class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                    :class="v.isActive
                      ? 'bg-green-100 text-green-700'
                      : 'bg-gray-100 text-gray-500'"
                  >
                    {{ v.isActive ? t('common.active') : t('common.passive') }}
                  </span>
                </td>
                <td v-if="canManage" class="px-6 py-3 text-right space-x-2 whitespace-nowrap">
                  <button
                    v-if="canEdit(v)"
                    type="button"
                    @click="openEdit(v, $event)"
                    class="text-xs font-medium text-primary hover:underline"
                  >
                    {{ t('common.edit') }}
                  </button>
                  <button
                    type="button"
                    @click="confirmToggleStatus(v, $event)"
                    class="text-xs font-medium hover:underline"
                    :class="v.isActive ? 'text-amber-600' : 'text-green-700'"
                  >
                    {{ v.isActive ? t('common.deactivate') : t('common.activate') }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <FormModal
      :open="showForm"
      :title="editTarget ? t('refData.editValue') : t('refData.addValue')"
      :saving="saving"
      @close="showForm = false"
      @submit="submitForm"
    >
      <div class="space-y-4">
        <p v-if="formError" class="text-sm text-red-600">{{ formError }}</p>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('refData.code') }}</label>
          <input
            v-model="form.code"
            type="text"
            :disabled="!!editTarget"
            class="w-full px-3 py-2 rounded-lg border border-gray-200 text-sm disabled:bg-gray-50 disabled:text-gray-500"
            :placeholder="t('refData.codePlaceholder')"
          />
          <p v-if="formErrors.code" class="mt-1 text-xs text-red-600">{{ formErrors.code }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('refData.label') }}</label>
          <input
            v-model="form.label"
            type="text"
            class="w-full px-3 py-2 rounded-lg border border-gray-200 text-sm"
          />
          <p v-if="formErrors.label" class="mt-1 text-xs text-red-600">{{ formErrors.label }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('refData.sortOrder') }}</label>
          <input
            v-model.number="form.sortOrder"
            type="number"
            min="0"
            class="w-full px-3 py-2 rounded-lg border border-gray-200 text-sm"
          />
        </div>

        <label class="flex items-center gap-2 text-sm text-gray-700">
          <input v-model="form.isDefault" type="checkbox" class="rounded border-gray-300" />
          {{ t('refData.setDefault') }}
        </label>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!statusTarget"
      :title="statusTarget?.isActive ? t('common.deactivate') : t('common.activate')"
      :message="statusTarget?.isActive
        ? t('refData.confirmDeactivate', { label: statusTarget?.label })
        : t('refData.confirmActivate', { label: statusTarget?.label })"
      :loading="statusLoading"
      @confirm="doToggleStatus"
      @cancel="statusTarget = null"
    />
  </div>
</template>
