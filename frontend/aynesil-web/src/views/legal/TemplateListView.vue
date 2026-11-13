<script setup lang="ts">
/**
 * Sözleşme / onam şablonları — liste, oluştur, düzenle, yeni sürüm.
 * Aynı görünüm iki rota için kullanılır (route.meta.templateKind: 'contract' | 'consent').
 * Düzenleme ve yeni sürüm yalnızca güncel sürümde; imzalanmış/kullanılmış sürümler değişmez (yeni sürümle ilerlenir).
 */
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { useConsentStore } from '@/stores/consent.store'
import { legalService } from '@/services/legal.service'
import { usePermission } from '@/composables/usePermission'
import { Permissions } from '@/constants/permissions'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import TemplateFormModal from '@/components/legal/TemplateFormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { TemplateFormValue, TemplateMode } from '@/components/legal/TemplateFormModal.vue'
import type { ConsentTemplateDto, ContractTemplateDto } from '@/types/legal.types'

type Row = Record<string, unknown>

const { t } = useI18n()
const route = useRoute()
const { can } = usePermission()
const contractStore = useContractStore()
const consentStore = useConsentStore()

const kind = computed<'contract' | 'consent'>(() => (route.meta.templateKind === 'consent' ? 'consent' : 'contract'))
const store = computed(() => (kind.value === 'contract' ? contractStore : consentStore))
const perms = computed(() => (kind.value === 'contract' ? Permissions.ContractTemplates : Permissions.ConsentTemplates))

const showArchived = ref(false)
const formOpen = ref(false)
const formMode = ref<TemplateMode>('create')
const formTemplate = ref<ContractTemplateDto | ConsentTemplateDto | null>(null)
const formError = ref('')

const columns = computed<Column<Row>[]>(() => [
  { key: 'code', label: t('legal.template.fields.code') },
  { key: kind.value === 'contract' ? 'contractTypeCode' : 'consentTypeCode', label: t('legal.template.fields.type'), width: '160px' },
  { key: 'version', label: t('legal.template.fields.version'), width: '90px', align: 'center' },
  { key: 'effectiveFrom', label: t('legal.template.fields.effectiveFrom'), width: '130px' },
  { key: 'isCurrent', label: t('legal.template.fields.current'), width: '110px' },
])

function load() {
  return store.value.fetchTemplates({ isCurrent: showArchived.value ? undefined : true, pageSize: 100 })
}
onMounted(load)
watch([kind, showArchived], load)

function typeCodeOf(row: Row): string | undefined {
  return (row.contractTypeCode ?? row.consentTypeCode) as string | undefined
}

function openCreate() {
  formMode.value = 'create'
  formTemplate.value = null
  formError.value = ''
  formOpen.value = true
}

async function openExisting(row: Row, mode: TemplateMode) {
  formError.value = ''
  try {
    const id = row.id as string
    const res = kind.value === 'contract'
      ? await legalService.getContractTemplate(id)
      : await legalService.getConsentTemplate(id)
    if (!res.success || !res.data) return
    formTemplate.value = res.data
    formMode.value = mode
    formOpen.value = true
  } catch (e: unknown) {
    store.value.error = (e as Error).message
  }
}

async function submit(v: TemplateFormValue) {
  formError.value = ''
  const translations = [{ locale: 'tr', title: v.title.trim(), body: v.body }]
  try {
    if (kind.value === 'contract') {
      if (formMode.value === 'create') {
        await contractStore.createTemplate({ code: v.code, contractTypeId: v.typeId, effectiveFrom: v.effectiveFrom, translations })
      } else if (formMode.value === 'edit') {
        await contractStore.updateTemplate(formTemplate.value!.id, {
          contractTypeId: v.typeId, effectiveFrom: v.effectiveFrom, translations, rowVersion: formTemplate.value!.rowVersion,
        })
      } else {
        await contractStore.newTemplateVersion(formTemplate.value!.id, { effectiveFrom: v.effectiveFrom, translations })
      }
    } else if (formMode.value === 'create') {
      await consentStore.createTemplate({ code: v.code, consentTypeId: v.typeId, isMandatory: v.isMandatory, effectiveFrom: v.effectiveFrom, translations })
    } else if (formMode.value === 'edit') {
      await consentStore.updateTemplate(formTemplate.value!.id, {
        consentTypeId: v.typeId, isMandatory: v.isMandatory, effectiveFrom: v.effectiveFrom, translations,
        rowVersion: formTemplate.value!.rowVersion,
      })
    } else {
      await consentStore.newTemplateVersion(formTemplate.value!.id, { effectiveFrom: v.effectiveFrom, translations })
    }
    formOpen.value = false
    await load()
  } catch (e: unknown) {
    formError.value = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="t(`legal.template.${kind}.list.title`)"
      :description="t(`legal.template.${kind}.list.subtitle`)"
    >
      <button
        v-if="can(perms.Create)"
        class="px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        @click="openCreate"
      >
        {{ t('legal.template.actions.create') }}
      </button>
    </PageHeader>

    <p v-if="store.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ store.error }}</p>

    <label class="mb-4 flex items-center gap-2 text-sm text-foreground">
      <input v-model="showArchived" type="checkbox" />
      {{ t('legal.template.showArchived') }}
    </label>

    <DataTable
      :columns="columns"
      :rows="store.templates.items as unknown as Row[]"
      :loading="store.loading"
      :empty-text="t(`legal.template.${kind}.list.noData`)"
    >
      <template #cell-code="{ value }">
        <span class="font-medium text-foreground font-mono">{{ value ?? '—' }}</span>
      </template>
      <template #cell-contractTypeCode="{ row }">
        <span class="text-muted-foreground">{{ typeCodeOf(row) ?? '—' }}</span>
      </template>
      <template #cell-consentTypeCode="{ row }">
        <span class="text-muted-foreground">{{ typeCodeOf(row) ?? '—' }}</span>
      </template>
      <template #cell-version="{ value }">v{{ value }}</template>
      <template #cell-effectiveFrom="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-isCurrent="{ value }">
        <StatusBadge :value="Boolean(value)" :true-label="t('legal.template.current')" :false-label="t('legal.template.historical')" />
      </template>
      <template #actions="{ row }">
        <div v-if="row.isCurrent" class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can(perms.Update)"
            class="px-2.5 py-1 text-xs rounded-lg border border-border hover:bg-accent"
            @click="openExisting(row as Row, 'edit')"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="can(perms.Version)"
            class="px-2.5 py-1 text-xs rounded-lg border border-border hover:bg-accent"
            @click="openExisting(row as Row, 'newVersion')"
          >
            {{ t('legal.template.actions.newVersion') }}
          </button>
        </div>
      </template>
    </DataTable>

    <TemplateFormModal
      :open="formOpen"
      :kind="kind"
      :mode="formMode"
      :template="formTemplate"
      :saving="store.saving"
      :error="formError"
      @submit="submit"
      @close="formOpen = false"
    />
  </div>
</template>
