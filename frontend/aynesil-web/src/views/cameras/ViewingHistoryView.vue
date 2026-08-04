<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ViewingLogDto, ViewingLogQuery } from '@/types/camera.types'

const { t } = useI18n()
const cameraStore = useCameraStore()
const auth = useAuthStore()

const filters = reactive<ViewingLogQuery>({
  page: 1,
  pageSize: 20,
  from: '',
  to: '',
  corporationId: auth.user?.corporationId,
})

const columns: Column<ViewingLogDto>[] = [
  { key: 'guardianFullName', label: t('camera.viewingHistory.viewer') },
  { key: 'cameraCode', label: t('camera.fields.code'), width: '120px' },
  { key: 'startedAt', label: t('camera.viewingHistory.startedAt'), width: '160px' },
  { key: 'endedAt', label: t('camera.viewingHistory.endedAt'), width: '160px' },
  { key: 'durationSeconds', label: t('camera.viewingHistory.duration'), width: '100px', align: 'right' },
]

function formatDatetime(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

function formatDuration(seconds?: number) {
  if (!seconds) return '—'
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}

function doFetch() {
  filters.page = 1
  cameraStore.fetchViewingLogs(filters)
}

watch(
  () => filters.page,
  () => cameraStore.fetchViewingLogs(filters)
)

onMounted(() => cameraStore.fetchViewingLogs(filters))
</script>

<template>
  <div>
    <PageHeader
      :title="t('camera.viewingHistory.title')"
      :description="t('camera.viewingHistory.subtitle')"
    />

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.from') }}</label>
        <input
          v-model="filters.from"
          type="date"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.to') }}</label>
        <input
          v-model="filters.to"
          type="date"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        />
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="cameraStore.viewingLogs.items"
      :loading="cameraStore.loading"
      :empty-text="t('camera.viewingHistory.noData')"
    >
      <template #cell-guardianFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? t('camera.viewingHistory.staff') }}</span>
      </template>
      <template #cell-cameraCode="{ value }">
        <span class="text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-startedAt="{ value }">
        <span class="text-muted-foreground text-xs">{{ formatDatetime(String(value)) }}</span>
      </template>
      <template #cell-endedAt="{ value }">
        <span class="text-muted-foreground text-xs">{{ value ? formatDatetime(String(value)) : '—' }}</span>
      </template>
      <template #cell-durationSeconds="{ value }">
        <span class="text-foreground">{{ formatDuration(value as number | undefined) }}</span>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="cameraStore.viewingLogs.page"
        :page-size="cameraStore.viewingLogs.pageSize"
        :total-count="cameraStore.viewingLogs.totalCount"
        :total-pages="cameraStore.viewingLogs.totalPages"
        :has-previous-page="cameraStore.viewingLogs.hasPreviousPage"
        :has-next-page="cameraStore.viewingLogs.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; cameraStore.fetchViewingLogs(filters) }"
      />
    </div>
  </div>
</template>
