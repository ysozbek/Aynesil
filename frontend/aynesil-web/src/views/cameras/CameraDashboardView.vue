<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camera.dashboard.title') }}</h1>
        <p class="text-muted fs-6 mb-0">{{ $t('camera.dashboard.subtitle') }}</p>
      </div>
      <RouterLink
        v-if="hasPermission('camera:create')"
        to="/cameras/new"
        class="btn btn-primary"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('camera.new') }}
      </RouterLink>
    </div>

    <!-- Stats -->
    <div class="row g-5 mb-6">
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-primary">
                <i class="ki-outline ki-monitor-2 fs-1 text-primary"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-primary">{{ totalCameras }}</div>
              <div class="text-muted fs-7">{{ $t('camera.dashboard.totalCameras') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-success">
                <i class="ki-outline ki-check-circle fs-1 text-success"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-success">{{ activeCameras }}</div>
              <div class="text-muted fs-7">{{ $t('camera.dashboard.activeCameras') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-warning">
                <i class="ki-outline ki-shield-tick fs-1 text-warning"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-warning">{{ activeAuthorizations }}</div>
              <div class="text-muted fs-7">{{ $t('camera.dashboard.activeAuthorizations') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-info">
                <i class="ki-outline ki-eye fs-1 text-info"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-info">{{ cameraStore.viewingLogs.totalCount }}</div>
              <div class="text-muted fs-7">{{ $t('camera.dashboard.recentViews') }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-5">
      <!-- Camera List -->
      <div class="col-xl-8">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title fw-bold">{{ $t('camera.dashboard.cameraStatus') }}</h3>
            <div class="card-toolbar">
              <RouterLink to="/cameras" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="cameraStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else class="table-responsive">
              <table class="table table-row-dashed align-middle gs-0 gy-3">
                <thead>
                  <tr class="fw-bold text-muted bg-light">
                    <th class="ps-4">{{ $t('camera.fields.code') }}</th>
                    <th>{{ $t('camera.fields.name') }}</th>
                    <th>{{ $t('camera.fields.campus') }}</th>
                    <th>{{ $t('common.status') }}</th>
                    <th class="text-end pe-4">{{ $t('common.actions') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="c in cameraStore.cameraList.items" :key="c.id">
                    <td class="ps-4 fw-semibold">{{ c.code }}</td>
                    <td>{{ c.name }}</td>
                    <td class="text-muted">{{ c.campusName ?? '—' }}</td>
                    <td>
                      <span :class="c.isActive ? 'badge badge-light-success' : 'badge badge-light-danger'">
                        {{ c.isActive ? $t('common.active') : $t('common.passive') }}
                      </span>
                    </td>
                    <td class="text-end pe-4">
                      <RouterLink :to="`/cameras/${c.id}`" class="btn btn-sm btn-light-primary">
                        <i class="ki-outline ki-eye fs-4"></i>
                      </RouterLink>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- Recent Viewing Logs -->
      <div class="col-xl-4">
        <div class="card h-100">
          <div class="card-header border-0 pt-5">
            <h3 class="card-title fw-bold">{{ $t('camera.dashboard.accessLogs') }}</h3>
            <div class="card-toolbar">
              <RouterLink to="/cameras/viewing-history" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
            </div>
          </div>
          <div class="card-body py-3">
            <div v-if="cameraStore.loading" class="text-center py-10">
              <div class="spinner-border text-primary"></div>
            </div>
            <div v-else-if="cameraStore.viewingLogs.items.length === 0" class="text-center py-10 text-muted">
              {{ $t('camera.dashboard.noLogs') }}
            </div>
            <div v-else>
              <div
                v-for="log in cameraStore.viewingLogs.items.slice(0, 8)"
                :key="log.id"
                class="d-flex align-items-center mb-4"
              >
                <div class="symbol symbol-35px me-3">
                  <span class="symbol-label bg-light-info">
                    <i class="ki-outline ki-eye fs-4 text-info"></i>
                  </span>
                </div>
                <div class="flex-grow-1">
                  <div class="fw-semibold text-gray-800 fs-7">{{ log.guardianFullName ?? $t('camera.dashboard.staffViewer') }}</div>
                  <div class="text-muted fs-8">{{ log.cameraCode ?? '—' }} · {{ formatDatetime(log.startedAt) }}</div>
                </div>
                <div class="text-muted fs-8">
                  {{ log.durationSeconds ? `${Math.round(log.durationSeconds / 60)} dk` : '—' }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'

const cameraStore = useCameraStore()
const authStore = useAuthStore()

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDatetime(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

const totalCameras = computed(() => cameraStore.cameraList.totalCount)
const activeCameras = computed(() => cameraStore.cameraList.items.filter(c => c.isActive).length)
const activeAuthorizations = computed(() => cameraStore.authorizations.items.filter(a => a.isCurrentlyValid && !a.isRevoked).length)

onMounted(async () => {
  const corp = authStore.user?.corporationId
  await Promise.all([
    cameraStore.fetchCameras({ corporationId: corp, pageSize: 50 }),
    cameraStore.fetchAuthorizations({ corporationId: corp, isCurrentlyValid: true }),
    cameraStore.fetchViewingLogs({ corporationId: corp, pageSize: 20 }),
  ])
})
</script>
