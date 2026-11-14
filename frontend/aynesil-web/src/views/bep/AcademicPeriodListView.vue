<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useBepStore } from '@/stores/bep.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type { AcademicPeriodListItemDto } from '@/types/bep.types'
import { Permissions } from '@/constants/permissions'

// Akademik dönemler: liste, oluştur, düzenle, "güncel dönem yap", sil (kullanılmıyorsa ve güncel değilse).
const { t } = useI18n()
const store = useBepStore()
const refData = useRefDataStore()
const { can } = usePermission()

const terms = ref<RefValueItem[]>([])
const pageError = ref('')

const showForm = ref(false)
const editTarget = ref<AcademicPeriodListItemDto | null>(null)
const formError = ref('')
const form = reactive({ name: '', startDate: '', endDate: '', termId: '', isCurrent: false })

const deleteTarget = ref<AcademicPeriodListItemDto | null>(null)
const currentTarget = ref<AcademicPeriodListItemDto | null>(null)

const columns: Column<AcademicPeriodListItemDto>[] = [
  { key: 'name', label: t('bep.period.name') },
  { key: 'termLabel', label: t('bep.period.term'), width: '130px' },
  { key: 'startDate', label: t('bep.period.startDate'), width: '120px' },
  { key: 'endDate', label: t('bep.period.endDate'), width: '120px' },
  { key: 'isCurrent', label: t('bep.period.current'), width: '110px', align: 'center' },
]

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'

function formatDate(v: unknown) {
  return v ? new Date(String(v)).toLocaleDateString('tr-TR') : '—'
}

async function load() {
  pageError.value = ''
  try {
    await store.fetchPeriods({ page: 1, pageSize: 200 })
  } catch (e: unknown) {
    pageError.value = (e as Error).message
  }
}

function openCreate() {
  editTarget.value = null
  Object.assign(form, { name: '', startDate: '', endDate: '', termId: '', isCurrent: false })
  formError.value = ''
  showForm.value = true
}

function openEdit(row: AcademicPeriodListItemDto) {
  editTarget.value = row
  Object.assign(form, {
    name: row.name,
    startDate: row.startDate.slice(0, 10),
    endDate: row.endDate.slice(0, 10),
    termId: row.termId ?? '',
    isCurrent: row.isCurrent,
  })
  formError.value = ''
  showForm.value = true
}

async function submit() {
  formError.value = ''
  if (!form.name.trim()) { formError.value = t('validation.required', { field: t('bep.period.name') }); return }
  if (!form.startDate || !form.endDate) { formError.value = t('bep.period.datesRequired'); return }
  if (form.endDate <= form.startDate) { formError.value = t('bep.period.dateOrder'); return }
  try {
    if (editTarget.value) {
      // Liste satırında rowVersion yok; güncel kaydı çekip onunla gönder (eşzamanlılık kontrolü).
      await store.fetchPeriod(editTarget.value.id)
      const rowVersion = store.currentPeriod?.rowVersion ?? 0
      await store.updatePeriod(editTarget.value.id, {
        name: form.name.trim(),
        startDate: form.startDate,
        endDate: form.endDate,
        termId: form.termId || null,
        rowVersion,
      })
    } else {
      await store.createPeriod({
        name: form.name.trim(),
        startDate: form.startDate,
        endDate: form.endDate,
        termId: form.termId || null,
        isCurrent: form.isCurrent,
      })
    }
    showForm.value = false
    await load()
  } catch (e: unknown) {
    formError.value = (e as Error).message
  }
}

async function doSetCurrent() {
  if (!currentTarget.value) return
  try {
    await store.setCurrentPeriod(currentTarget.value.id)
    await load()
  } catch (e: unknown) {
    pageError.value = (e as Error).message
  } finally {
    currentTarget.value = null
  }
}

async function doDelete() {
  if (!deleteTarget.value) return
  try {
    await store.deletePeriod(deleteTarget.value.id)
    await load()
  } catch (e: unknown) {
    pageError.value = (e as Error).message
  } finally {
    deleteTarget.value = null
  }
}

onMounted(async () => {
  load()
  terms.value = await refData.getValues('ACADEMIC_TERM')
})
</script>

<template>
  <div>
    <PageHeader :title="t('bep.period.title')" :description="t('bep.period.description')">
      <button
        v-if="can(Permissions.AcademicPeriods.Manage)"
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        {{ t('bep.period.create') }}
      </button>
    </PageHeader>

    <p v-if="pageError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ pageError }}</p>

    <DataTable :columns="columns" :rows="store.periodList.items" :loading="store.loading">
      <template #cell-startDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-endDate="{ value }">{{ formatDate(value) }}</template>
      <template #cell-isCurrent="{ value }">
        <span v-if="value" class="px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
          {{ t('bep.period.current') }}
        </span>
        <span v-else class="text-muted-foreground">—</span>
      </template>
      <template #actions="{ row }">
        <div v-if="can(Permissions.AcademicPeriods.Manage)" class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="!row.isCurrent"
            @click="currentTarget = row"
            class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent"
          >{{ t('bep.period.makeCurrent') }}</button>
          <button
            @click="openEdit(row)"
            class="px-2 py-1 text-xs rounded-lg border border-border hover:bg-accent"
          >{{ t('common.edit') }}</button>
          <button
            v-if="!row.isCurrent"
            @click="deleteTarget = row"
            class="px-2 py-1 text-xs rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          >{{ t('common.delete') }}</button>
        </div>
      </template>
    </DataTable>

    <FormModal
      :open="showForm"
      :title="editTarget ? t('bep.period.edit') : t('bep.period.create')"
      :saving="store.saving"
      @submit="submit"
      @close="showForm = false"
    >
      <p v-if="formError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ formError }}</p>
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.period.name') }} *</label>
          <input v-model="form.name" type="text" maxlength="200" :class="inputClass" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.period.term') }}</label>
          <select v-model="form.termId" :class="inputClass">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="v in terms" :key="v.id" :value="v.id">{{ v.label }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.period.startDate') }} *</label>
            <input v-model="form.startDate" type="date" :class="inputClass" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.period.endDate') }} *</label>
            <input v-model="form.endDate" type="date" :class="inputClass" />
          </div>
        </div>
        <label v-if="!editTarget" class="flex items-center gap-2 text-sm">
          <input v-model="form.isCurrent" type="checkbox" class="rounded" />
          {{ t('bep.period.makeCurrent') }}
        </label>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!currentTarget"
      :title="t('bep.period.makeCurrent')"
      :message="t('bep.period.makeCurrentConfirm', { name: currentTarget?.name })"
      :confirm-label="t('bep.period.makeCurrent')"
      confirm-class="bg-primary text-primary-foreground hover:opacity-90"
      :loading="store.saving"
      @confirm="doSetCurrent"
      @cancel="currentTarget = null"
    />

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('bep.period.deleteTitle')"
      :message="t('bep.period.deleteConfirm', { name: deleteTarget?.name })"
      :confirm-label="t('common.delete')"
      :loading="store.saving"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
