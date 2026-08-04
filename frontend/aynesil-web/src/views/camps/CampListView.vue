<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camp.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('camp.list.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('camp:create')" to="/camps/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('camp.new') }}
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
        <div v-if="campStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="campStore.campList.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-flag fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('camp.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('camp.fields.code') }}</th>
                <th>{{ $t('camp.fields.name') }}</th>
                <th>{{ $t('camp.fields.type') }}</th>
                <th>{{ $t('camp.fields.location') }}</th>
                <th class="text-center">{{ $t('camp.fields.periods') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in campStore.campList.items" :key="c.id">
                <td class="ps-4 fw-semibold">{{ c.code }}</td>
                <td>{{ c.name }}</td>
                <td class="text-muted">{{ c.campTypeCode ?? '—' }}</td>
                <td class="text-muted">{{ c.location ?? '—' }}</td>
                <td class="text-center">{{ c.periodCount }}</td>
                <td>
                  <span :class="c.isActive ? 'badge badge-light-success' : 'badge badge-light-danger'">
                    {{ c.isActive ? $t('common.active') : $t('common.passive') }}
                  </span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/camps/${c.id}`" class="btn btn-sm btn-light-primary me-2">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                  <RouterLink v-if="hasPermission('camp:update')" :to="`/camps/${c.id}/edit`" class="btn btn-sm btn-light">
                    <i class="ki-outline ki-pencil fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="campStore.campList.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!campStore.campList.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ campStore.campList.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!campStore.campList.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useCampStore } from '@/stores/camp.store'
import { useAuthStore } from '@/stores/auth.store'
import type { CampListQuery } from '@/types/camp.types'

const campStore = useCampStore()
const authStore = useAuthStore()

const filters = reactive<CampListQuery>({
  page: 1, pageSize: 20, search: '', isActive: undefined,
  corporationId: authStore.user?.corporationId,
})

function hasPermission(p: string) { return authStore.hasPermission(p) }

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() { clearTimeout(debounceTimer); debounceTimer = setTimeout(doFetch, 400) }

async function doFetch() { filters.page = 1; await campStore.fetchCamps(filters) }
function resetFilters() { filters.search = ''; filters.isActive = undefined; filters.page = 1; doFetch() }
function changePage(page: number) { filters.page = page; campStore.fetchCamps(filters) }

onMounted(doFetch)
</script>
