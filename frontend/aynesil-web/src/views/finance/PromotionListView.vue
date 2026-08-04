<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { financeService } from '@/services/finance.service'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { PromotionListItemDto } from '@/types/finance.types'
import type { PaginatedResult } from '@/types/api.types'

const { t } = useI18n()
const auth = useAuthStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const list = ref<PaginatedResult<PromotionListItemDto>>({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false })
const loading = ref(false)
const saving = ref(false)

const query = reactive({ corporationId: corporationId.value, isActive: '' as '' | 'true' | 'false', page: 1, pageSize: 20 })
const showForm = ref(false)
const editTarget = ref<PromotionListItemDto | null>(null)
const formData = reactive({
  code: '', name: '', value: 0, isPercentage: true,
  maxRedemptions: '' as string | number, validFrom: '', validTo: '',
})

watch(() => [query.isActive, query.page], () => loadList())
onMounted(() => loadList())

async function loadList() {
  loading.value = true
  try {
    const res = await financeService.listPromotions({
      ...query,
      corporationId: corporationId.value,
      isActive: query.isActive === 'true' ? true : query.isActive === 'false' ? false : undefined,
    })
    if (res.success && res.data) list.value = res.data
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  saving.value = true
  try {
    if (editTarget.value) {
      const detail = await financeService.getPromotion(editTarget.value.id)
      if (detail.success && detail.data) {
        await financeService.updatePromotion(editTarget.value.id, {
          name: formData.name, value: formData.value, isPercentage: formData.isPercentage,
          maxRedemptions: formData.maxRedemptions ? Number(formData.maxRedemptions) : undefined,
          validFrom: formData.validFrom || undefined, validTo: formData.validTo || undefined,
          rowVersion: detail.data.rowVersion,
        })
      }
    } else {
      await financeService.createPromotion({
        corporationId: corporationId.value, code: formData.code, name: formData.name,
        value: formData.value, isPercentage: formData.isPercentage,
        maxRedemptions: formData.maxRedemptions ? Number(formData.maxRedemptions) : undefined,
        validFrom: formData.validFrom || undefined, validTo: formData.validTo || undefined,
      })
    }
    showForm.value = false
    editTarget.value = null
    await loadList()
  } finally {
    saving.value = false
  }
}

const columns: Column<PromotionListItemDto>[] = [
  { key: 'code', label: t('finance.promotion.code'), width: '100px' },
  { key: 'name', label: t('finance.promotion.name') },
  { key: 'value', label: t('finance.promotion.value'), width: '90px', align: 'center' },
  { key: 'redemptionCount', label: t('finance.promotion.redemptions'), width: '90px', align: 'center' },
  { key: 'validTo', label: t('finance.promotion.validTo'), width: '110px' },
  { key: 'isActive', label: t('common.status'), width: '90px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.promotion.title')" :description="t('finance.promotion.description')">
      <button
        v-if="can('promotion:create')"
        @click="editTarget = null; Object.assign(formData, { code: '', name: '', value: 0, isPercentage: true, maxRedemptions: '', validFrom: '', validTo: '' }); showForm = true"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('finance.promotion.create') }}
      </button>
    </PageHeader>

    <div class="mb-4">
      <select v-model="query.isActive" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="true">{{ t('common.active') }}</option>
        <option value="false">{{ t('common.passive') }}</option>
      </select>
    </div>

    <DataTable :columns="columns" :rows="list.items" :loading="loading">
      <template #cell-value="{ row }">
        <span class="font-mono">{{ row.isPercentage ? `%${row.value}` : row.value }}</span>
      </template>
      <template #cell-validTo="{ value }">
        <span :class="value && new Date(String(value)) < new Date() ? 'text-red-600' : ''">{{ formatDate(value) }}</span>
      </template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', value ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600']">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="can('promotion:update')"
            @click="editTarget = row; Object.assign(formData, { name: row.name, value: row.value, isPercentage: row.isPercentage, maxRedemptions: row.maxRedemptions ?? '', validFrom: '', validTo: row.validTo ?? '' }); showForm = true"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            v-if="can('promotion:update') && !row.isActive"
            @click="financeService.activatePromotion(row.id).then(() => loadList())"
            class="p-1.5 rounded-lg hover:bg-green-50 text-muted-foreground hover:text-green-600"
            :title="t('common.activate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </button>
          <button
            v-if="can('promotion:update') && row.isActive"
            @click="financeService.deactivatePromotion(row.id).then(() => loadList())"
            class="p-1.5 rounded-lg hover:bg-amber-50 text-muted-foreground hover:text-amber-600"
            :title="t('common.deactivate')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="list.page" :page-size="list.pageSize" :total-count="list.totalCount"
        :total-pages="list.totalPages" :has-previous-page="list.hasPreviousPage" :has-next-page="list.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-[--color-card] rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold mb-4">{{ editTarget ? t('finance.promotion.edit') : t('finance.promotion.create') }}</h3>
        <div class="space-y-3">
          <div v-if="!editTarget"><label class="block text-sm font-medium mb-1">{{ t('finance.promotion.code') }} *</label><input v-model="formData.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          <div><label class="block text-sm font-medium mb-1">{{ t('finance.promotion.name') }} *</label><input v-model="formData.name" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          <div class="flex items-center gap-3">
            <div class="flex-1"><label class="block text-sm font-medium mb-1">{{ t('finance.promotion.value') }}</label><input v-model.number="formData.value" type="number" min="0" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
            <label class="flex items-center gap-1.5 text-sm mt-4"><input type="checkbox" v-model="formData.isPercentage" class="rounded" /> {{ t('finance.promotion.isPercentage') }}</label>
          </div>
          <div><label class="block text-sm font-medium mb-1">{{ t('finance.promotion.maxRedemptions') }}</label><input v-model="formData.maxRedemptions" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          <div class="grid grid-cols-2 gap-3">
            <div><label class="block text-sm font-medium mb-1">{{ t('finance.scholarship.validFrom') }}</label><input v-model="formData.validFrom" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
            <div><label class="block text-sm font-medium mb-1">{{ t('finance.promotion.validTo') }}</label><input v-model="formData.validTo" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none" /></div>
          </div>
        </div>
        <div class="flex justify-end gap-2 mt-4">
          <button @click="showForm = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="submitForm" :disabled="saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
