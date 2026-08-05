<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ContractTemplateListItemDto } from '@/types/legal.types'

const { t } = useI18n()
const router = useRouter()
const contractStore = useContractStore()
const auth = useAuthStore()

const columns: Column<ContractTemplateListItemDto>[] = [
  { key: 'code', label: t('legal.template.fields.code') },
  { key: 'contractTypeCode', label: t('legal.template.fields.type'), width: '140px' },
  { key: 'version', label: t('legal.template.fields.version'), width: '90px', align: 'center' },
  { key: 'effectiveFrom', label: t('legal.template.fields.effectiveFrom'), width: '130px' },
  { key: 'isCurrent', label: t('legal.template.fields.current'), width: '110px' },
]

onMounted(() => {
  contractStore.fetchTemplates({ corporationId: auth.user?.corporationId })
})
</script>

<template>
  <div>
    <PageHeader
      :title="t('legal.template.contract.list.title')"
      :description="t('legal.template.contract.list.subtitle')"
    />

    <DataTable
      :columns="columns"
      :rows="contractStore.templates.items"
      :loading="contractStore.loading"
      :empty-text="t('legal.template.contract.list.noData')"
    >
      <template #cell-code="{ value }">
        <span class="font-medium text-foreground font-mono">{{ value ?? '—' }}</span>
      </template>
      <template #cell-contractTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-version="{ value }">v{{ value }}</template>
      <template #cell-effectiveFrom="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-isCurrent="{ value }">
        <span
          v-if="value"
          class="px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700"
        >
          {{ t('legal.template.current') }}
        </span>
        <span v-else class="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
          {{ t('legal.template.historical') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push(`/legal/contract-templates/${row.id}`)"
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
  </div>
</template>
