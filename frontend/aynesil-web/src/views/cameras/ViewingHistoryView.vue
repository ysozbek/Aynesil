<template>
  <div class="container-xxl py-6">
    <div class="mb-6">
      <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camera.viewingHistory.title') }}</h1>
      <p class="text-muted mb-0">{{ $t('camera.viewingHistory.subtitle') }}</p>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.from') }}</label>
            <input v-model="filters.from" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.to') }}</label>
            <input v-model="filters.to" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="cameraStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="cameraStore.viewingLogs.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('camera.viewingHistory.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('camera.viewingHistory.viewer') }}</th>
                <th>{{ $t('camera.fields.code') }}</th>
                <th>{{ $t('camera.viewingHistory.startedAt') }}</th>
                <th>{{ $t('camera.viewingHistory.endedAt') }}</th>
                <th class="text-end pe-4">{{ $t('camera.viewingHistory.duration') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="log in cameraStore.viewingLogs.items" :key="log.id">
                <td class="ps-4 fw-semibold">{{ log.guardianFullName ?? $t('camera.viewingHistory.staff') }}</td>
                <td>{{ log.cameraCode ?? '—' }}</td>
                <td class="text-muted fs-7">{{ formatDatetime(log.startedAt) }}</td>
                <td class="text-muted fs-7">{{ log.endedAt ? formatDatetime(log.endedAt) : '—' }}</td>
                <td class="text-end pe-4">
                  {{ log.durationSeconds ? `${Math.floor(log.durationSeconds / 60)}:${String(log.durationSeconds % 60).padStart(2, '0')}` : '—' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="cameraStore.viewingLogs.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!cameraStore.viewingLogs.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ cameraStore.viewingLogs.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!cameraStore.viewingLogs.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import type { ViewingLogQuery } from '@/types/camera.types'

const cameraStore = useCameraStore()
const authStore = useAuthStore()

const filters = reactive<ViewingLogQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, from: '', to: '',
  corporationId: authStore.user?.corporationId,
})

function formatDatetime(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

async function doFetch() {
  filters.page = 1
  await cameraStore.fetchViewingLogs(filters)
}

function changePage(page: number) {
  filters.page = page
  cameraStore.fetchViewingLogs(filters)
}

onMounted(doFetch)
</script>
