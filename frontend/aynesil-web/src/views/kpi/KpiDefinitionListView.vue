<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('kpi.definitions.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('kpi.definitions.subtitle') }}</p>
      </div>
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
            <label class="form-label fs-7">{{ $t('kpi.fields.category') }}</label>
            <select v-model="filters.categoryId" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option v-for="c in kpiStore.categories" :key="c.id" :value="c.id">{{ c.label || c.code }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.isActive" class="form-select form-select-sm" @change="doFetch">
              <option :value="undefined">{{ $t('common.allStatuses') }}</option>
              <option :value="true">{{ $t('common.active') }}</option>
              <option :value="false">{{ $t('common.passive') }}</option>
            </select>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="kpiStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="kpiStore.definitions.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('kpi.definitions.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('kpi.fields.code') }}</th>
                <th>{{ $t('kpi.fields.name') }}</th>
                <th>{{ $t('kpi.fields.category') }}</th>
                <th>{{ $t('kpi.fields.unit') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in kpiStore.definitions.items" :key="d.id">
                <td class="ps-4 fw-semibold font-monospace">{{ d.code }}</td>
                <td>{{ d.name }}</td>
                <td class="text-muted">{{ d.categoryCode ?? '—' }}</td>
                <td class="text-muted">{{ d.unit ?? '—' }}</td>
                <td>
                  <span :class="d.isActive ? 'badge badge-light-success' : 'badge badge-light-danger'">
                    {{ d.isActive ? $t('common.active') : $t('common.passive') }}
                  </span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/kpi/definitions/${d.id}`" class="btn btn-sm btn-light-primary me-2">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                  <button
                    v-if="hasPermission('kpi:manage')"
                    class="btn btn-sm btn-light"
                    @click="kpiStore.toggleDefinition(d.id, !d.isActive)"
                  >
                    <i :class="`ki-outline ki-${d.isActive ? 'cross' : 'check'} fs-4`"></i>
                  </button>
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
import { reactive, onMounted } from 'vue'
import { useKpiStore } from '@/stores/kpi.store'
import { useAuthStore } from '@/stores/auth.store'
import type { KpiDefinitionListQuery } from '@/types/kpi.types'

const kpiStore = useKpiStore()
const authStore = useAuthStore()

const filters = reactive<KpiDefinitionListQuery>({
  page: 1, pageSize: 20, search: '', isActive: undefined,
  corporationId: authStore.user?.corporationId,
})

function hasPermission(p: string) { return authStore.hasPermission(p) }

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() { clearTimeout(debounceTimer); debounceTimer = setTimeout(doFetch, 400) }

async function doFetch() { filters.page = 1; await kpiStore.fetchDefinitions(filters) }

onMounted(async () => {
  await kpiStore.fetchCategories(authStore.user?.corporationId)
  await doFetch()
})
</script>
