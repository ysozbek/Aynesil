<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camera.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('camera.list.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('camera:create')" to="/cameras/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('camera.new') }}
      </RouterLink>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-4">
            <label class="form-label fs-7">{{ $t('common.search') }}</label>
            <input v-model="filters.search" type="text" class="form-control form-control-sm" @input="debouncedFetch" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.isActive" class="form-select form-select-sm" @change="doFetch">
              <option :value="undefined">{{ $t('common.allStatuses') }}</option>
              <option :value="true">{{ $t('common.active') }}</option>
              <option :value="false">{{ $t('common.passive') }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <button class="btn btn-sm btn-light w-100" @click="resetFilters">{{ $t('common.cancel') }}</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="card">
      <div class="card-body py-3">
        <div v-if="cameraStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="cameraStore.cameraList.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-monitor-2 fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('camera.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('camera.fields.code') }}</th>
                <th>{{ $t('camera.fields.name') }}</th>
                <th>{{ $t('camera.fields.type') }}</th>
                <th>{{ $t('camera.fields.campus') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="cam in cameraStore.cameraList.items" :key="cam.id">
                <td class="ps-4 fw-semibold">{{ cam.code }}</td>
                <td>{{ cam.name }}</td>
                <td class="text-muted">{{ cam.cameraTypeCode ?? '—' }}</td>
                <td class="text-muted">{{ cam.campusName ?? '—' }}</td>
                <td>
                  <span :class="cam.isActive ? 'badge badge-light-success' : 'badge badge-light-danger'">
                    {{ cam.isActive ? $t('common.active') : $t('common.passive') }}
                  </span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/cameras/${cam.id}`" class="btn btn-sm btn-light-primary me-2">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                  <RouterLink
                    v-if="hasPermission('camera:update')"
                    :to="`/cameras/${cam.id}/edit`"
                    class="btn btn-sm btn-light me-2"
                  >
                    <i class="ki-outline ki-pencil fs-4"></i>
                  </RouterLink>
                  <button
                    v-if="hasPermission('camera:update')"
                    class="btn btn-sm btn-light"
                    @click="cameraStore.toggleActive(cam.id, !cam.isActive)"
                  >
                    <i :class="`ki-outline ki-${cam.isActive ? 'cross' : 'check'} fs-4`"></i>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="cameraStore.cameraList.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!cameraStore.cameraList.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ cameraStore.cameraList.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!cameraStore.cameraList.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
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
import type { CameraListQuery } from '@/types/camera.types'

const cameraStore = useCameraStore()
const authStore = useAuthStore()

const filters = reactive<CameraListQuery>({
  page: 1, pageSize: 20, search: '', isActive: undefined,
  corporationId: authStore.user?.corporationId,
})

function hasPermission(p: string) { return authStore.hasPermission(p) }

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(doFetch, 400)
}

async function doFetch() { filters.page = 1; await cameraStore.fetchCameras(filters) }
function resetFilters() { filters.search = ''; filters.isActive = undefined; filters.page = 1; doFetch() }
function changePage(page: number) { filters.page = page; cameraStore.fetchCameras(filters) }

onMounted(doFetch)
</script>
