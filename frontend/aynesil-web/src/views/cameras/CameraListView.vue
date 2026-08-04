<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { CameraListItemDto, CameraListQuery } from '@/types/camera.types'

const { t } = useI18n()
const router = useRouter()
const cameraStore = useCameraStore()
const auth = useAuthStore()
const { can } = usePermission()

const filters = reactive<CameraListQuery>({
  page: 1,
  pageSize: 20,
  search: '',
  isActive: undefined,
  corporationId: auth.user?.corporationId,
})

const columns: Column<CameraListItemDto>[] = [
  { key: 'code', label: t('camera.fields.code'), width: '120px' },
  { key: 'name', label: t('camera.fields.name') },
  { key: 'cameraTypeCode', label: t('camera.fields.type'), width: '120px' },
  { key: 'campusName', label: t('camera.fields.campus'), width: '140px' },
  { key: 'isActive', label: t('common.status'), width: '110px' },
]

function statusClass(isActive: boolean) {
  return isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    filters.page = 1
    cameraStore.fetchCameras(filters)
  }, 400)
}

function doFetch() {
  filters.page = 1
  cameraStore.fetchCameras(filters)
}

function resetFilters() {
  filters.search = ''
  filters.isActive = undefined
  filters.page = 1
  cameraStore.fetchCameras(filters)
}

watch(
  () => filters.page,
  () => cameraStore.fetchCameras(filters)
)

onMounted(() => cameraStore.fetchCameras(filters))
</script>

<template>
  <div>
    <PageHeader :title="t('camera.list.title')" :description="t('camera.list.subtitle')">
      <button
        v-if="can('camera:create')"
        @click="router.push({ name: 'camera-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('camera.new') }}
      </button>
    </PageHeader>

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
      <button
        @click="resetFilters"
        class="h-9 px-3 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.cancel') }}
      </button>
    </div>

    <DataTable
      :columns="columns"
      :rows="cameraStore.cameraList.items"
      :loading="cameraStore.loading"
      :empty-text="t('camera.list.noData')"
      @row-click="(row) => router.push({ name: 'camera-detail', params: { id: row.id } })"
    >
      <template #cell-code="{ value }">
        <span class="font-medium text-foreground">{{ value }}</span>
      </template>
      <template #cell-cameraTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-campusName="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-isActive="{ value }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(Boolean(value))]">
          {{ value ? t('common.active') : t('common.passive') }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'camera-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('camera:update')"
            @click="router.push({ name: 'camera-edit', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.edit')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>
          </button>
          <button
            v-if="can('camera:update')"
            @click="cameraStore.toggleActive(row.id, !row.isActive)"
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

    <div class="mt-4">
      <Pagination
        :page="cameraStore.cameraList.page"
        :page-size="cameraStore.cameraList.pageSize"
        :total-count="cameraStore.cameraList.totalCount"
        :total-pages="cameraStore.cameraList.totalPages"
        :has-previous-page="cameraStore.cameraList.hasPreviousPage"
        :has-next-page="cameraStore.cameraList.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; cameraStore.fetchCameras(filters) }"
      />
    </div>
  </div>
</template>
