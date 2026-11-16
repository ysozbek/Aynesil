<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCampStore } from '@/stores/camp.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { CampListItemDto, CampListQuery } from '@/types/camp.types'

const { t } = useI18n()
const router = useRouter()
const campStore = useCampStore()
const { visible } = useModule(Modules.Camps)

const filters = reactive<CampListQuery>({ page: 1, pageSize: 20, search: '', isActive: undefined })
const actionError = ref('')

const columns: Column<CampListItemDto>[] = [
  { key: 'code', label: t('camp.fields.code'), width: '140px' },
  { key: 'name', label: t('camp.fields.name') },
  { key: 'campTypeCode', label: t('camp.fields.type'), width: '140px' },
  { key: 'location', label: t('camp.fields.location') },
  { key: 'periodCount', label: t('camp.fields.periods'), width: '90px' },
  { key: 'isActive', label: t('common.status'), width: '100px' },
]

function fetch(resetPage = false) {
  if (resetPage) filters.page = 1
  return campStore.fetchCamps({ ...filters })
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => fetch(true), 400)
}

watch(() => filters.page, () => fetch())
onMounted(() => fetch())

// ── Etkinleştir / pasifleştir / sil ───────────────────────────────────────────
type Action = 'activate' | 'deactivate' | 'delete'
const pending = ref<{ action: Action; camp: CampListItemDto } | null>(null)

async function confirmAction() {
  if (!pending.value) return
  const { action, camp } = pending.value
  actionError.value = ''
  try {
    if (action === 'activate') await campStore.activateCamp(camp.id)
    else if (action === 'deactivate') await campStore.deactivateCamp(camp.id)
    else await campStore.deleteCamp(camp.id)
    await fetch()
  } catch (e: unknown) {
    // 409: bağlı kayıt varsa sunucunun anlamlı iletisi gösterilir.
    actionError.value = (e as Error).message
  } finally {
    pending.value = null
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('camp.list.title')" :description="t('camp.list.subtitle')">
      <button
        v-if="visible(Permissions.Camps.Read)"
        type="button"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
        @click="router.push({ name: 'camp-dashboard' })"
      >
        {{ t('camp.dashboard.title') }}
      </button>
      <button
        v-if="visible(Permissions.Camps.Create)"
        type="button"
        class="px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        @click="router.push({ name: 'camp-new' })"
      >
        {{ t('camp.new') }}
      </button>
    </PageHeader>

    <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ actionError }}</p>
    <p v-if="campStore.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ campStore.error }}</p>

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
        <select v-model="filters.isActive" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="fetch(true)">
          <option :value="undefined">{{ t('common.allStatuses') }}</option>
          <option :value="true">{{ t('common.active') }}</option>
          <option :value="false">{{ t('common.passive') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="campStore.campList.items"
      :loading="campStore.loading"
      :empty-text="t('camp.list.noData')"
      @row-click="(row) => router.push({ name: 'camp-detail', params: { id: row.id } })"
    >
      <template #cell-campTypeCode="{ value }">{{ value ?? '—' }}</template>
      <template #cell-location="{ value }">{{ value ?? '—' }}</template>
      <template #cell-isActive="{ value }">
        <StatusBadge :value="Boolean(value)" :true-label="t('common.active')" :false-label="t('common.passive')" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-2">
          <button
            v-if="visible(Permissions.Camps.Update)"
            type="button" class="text-xs text-primary hover:underline"
            @click.stop="router.push({ name: 'camp-edit', params: { id: row.id } })"
          >{{ t('common.edit') }}</button>
          <button
            v-if="visible(Permissions.Camps.Activate)"
            type="button" class="text-xs text-primary hover:underline"
            @click.stop="pending = { action: row.isActive ? 'deactivate' : 'activate', camp: row }"
          >{{ row.isActive ? t('camp.actions.deactivate') : t('camp.actions.activate') }}</button>
          <button
            v-if="visible(Permissions.Camps.Delete)"
            type="button" class="text-xs text-red-600 hover:underline"
            @click.stop="pending = { action: 'delete', camp: row }"
          >{{ t('common.delete') }}</button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="campStore.campList.page"
        :page-size="campStore.campList.pageSize"
        :total-count="campStore.campList.totalCount"
        :total-pages="campStore.campList.totalPages"
        :has-previous-page="campStore.campList.hasPreviousPage"
        :has-next-page="campStore.campList.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; fetch(true) }"
      />
    </div>

    <ConfirmModal
      :open="!!pending"
      :title="pending ? t(`camp.actions.${pending.action}Title`) : ''"
      :message="pending ? t(`camp.actions.${pending.action}Message`, { name: pending.camp.name }) : ''"
      :confirm-label="pending ? (pending.action === 'delete' ? t('common.delete') : t(`camp.actions.${pending.action}`)) : ''"
      :confirm-class="pending?.action === 'activate' ? 'bg-primary text-primary-foreground hover:opacity-90' : undefined"
      :loading="campStore.saving"
      @confirm="confirmAction"
      @cancel="pending = null"
    />
  </div>
</template>
