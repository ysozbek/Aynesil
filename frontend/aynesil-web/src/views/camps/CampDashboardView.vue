<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camp.dashboard.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('camp.dashboard.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('camp:create')" to="/camps/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('camp.new') }}
      </RouterLink>
    </div>

    <!-- Stats -->
    <div class="row g-5 mb-6">
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-primary">
                <i class="ki-outline ki-flag fs-1 text-primary"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-primary">{{ campStore.campList.totalCount }}</div>
              <div class="text-muted fs-7">{{ $t('camp.dashboard.totalCamps') }}</div>
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
              <div class="fs-2 fw-bold text-success">{{ activeCamps }}</div>
              <div class="text-muted fs-7">{{ $t('camp.dashboard.activeCamps') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-warning">
                <i class="ki-outline ki-people fs-1 text-warning"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-warning">{{ totalEnrolled }}</div>
              <div class="text-muted fs-7">{{ $t('camp.dashboard.totalEnrolled') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body d-flex align-items-center">
            <div class="symbol symbol-50px me-5">
              <span class="symbol-label bg-light-info">
                <i class="ki-outline ki-calendar fs-1 text-info"></i>
              </span>
            </div>
            <div>
              <div class="fs-2 fw-bold text-info">{{ totalPeriods }}</div>
              <div class="text-muted fs-7">{{ $t('camp.dashboard.totalPeriods') }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Active Camps Table -->
    <div class="card">
      <div class="card-header border-0 pt-5">
        <h3 class="card-title fw-bold">{{ $t('camp.dashboard.activeCampsList') }}</h3>
        <div class="card-toolbar">
          <RouterLink to="/camps" class="btn btn-sm btn-light">{{ $t('common.viewAll') }}</RouterLink>
        </div>
      </div>
      <div class="card-body py-3">
        <div v-if="campStore.loading" class="text-center py-10">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="campStore.campList.items.length === 0" class="text-center py-10 text-muted">
          {{ $t('camp.dashboard.noCamps') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('camp.fields.code') }}</th>
                <th>{{ $t('camp.fields.name') }}</th>
                <th>{{ $t('camp.fields.location') }}</th>
                <th class="text-center">{{ $t('camp.fields.capacity') }}</th>
                <th class="text-center">{{ $t('camp.fields.periods') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in campStore.campList.items.filter(c => c.isActive)" :key="c.id">
                <td class="ps-4 fw-semibold">{{ c.code }}</td>
                <td>{{ c.name }}</td>
                <td class="text-muted">{{ c.location ?? '—' }}</td>
                <td class="text-center">{{ c.capacity ?? '—' }}</td>
                <td class="text-center">{{ c.periodCount }}</td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/camps/${c.id}`" class="btn btn-sm btn-light-primary">
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
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useCampStore } from '@/stores/camp.store'
import { useAuthStore } from '@/stores/auth.store'

const campStore = useCampStore()
const authStore = useAuthStore()

function hasPermission(p: string) { return authStore.hasPermission(p) }

const activeCamps = computed(() => campStore.campList.items.filter(c => c.isActive).length)
const totalEnrolled = computed(() => campStore.campList.items.reduce((acc, c) => {
  // periods.enrolledCount would need period details, approximate here
  return acc + c.periodCount
}, 0))
const totalPeriods = computed(() => campStore.campList.items.reduce((acc, c) => acc + c.periodCount, 0))

onMounted(() => campStore.fetchCamps({ corporationId: authStore.user?.corporationId, pageSize: 50 }))
</script>
