<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('consultancy.institution.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('consultancy.institution.list.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('institution:create')" to="/consultancy/institutions/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('consultancy.institution.new') }}
      </RouterLink>
    </div>

    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-4">
            <label class="form-label fs-7">{{ $t('common.search') }}</label>
            <input v-model="filters.search" type="text" class="form-control form-control-sm" @input="debouncedFetch" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('consultancy.institution.fields.city') }}</label>
            <input v-model="filters.city" type="text" class="form-control form-control-sm" @input="debouncedFetch" />
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="consultancyStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="consultancyStore.institutions.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-home fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('consultancy.institution.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('consultancy.institution.fields.name') }}</th>
                <th>{{ $t('consultancy.institution.fields.type') }}</th>
                <th>{{ $t('consultancy.institution.fields.city') }}</th>
                <th class="text-center">{{ $t('consultancy.institution.fields.planCount') }}</th>
                <th class="text-center">{{ $t('consultancy.institution.fields.visitCount') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="inst in consultancyStore.institutions.items" :key="inst.id">
                <td class="ps-4 fw-semibold">{{ inst.name }}</td>
                <td class="text-muted">{{ inst.institutionTypeCode ?? '—' }}</td>
                <td class="text-muted">{{ inst.city ?? '—' }}</td>
                <td class="text-center">{{ inst.planCount }}</td>
                <td class="text-center">{{ inst.visitCount }}</td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/consultancy/institutions/${inst.id}`" class="btn btn-sm btn-light-primary me-2">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                  <RouterLink v-if="hasPermission('institution:update')" :to="`/consultancy/institutions/${inst.id}/edit`" class="btn btn-sm btn-light">
                    <i class="ki-outline ki-pencil fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="consultancyStore.institutions.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!consultancyStore.institutions.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ consultancyStore.institutions.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!consultancyStore.institutions.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import type { InstitutionListQuery } from '@/types/consultancy.types'

const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()

const filters = reactive<InstitutionListQuery>({
  page: 1, pageSize: 20, search: '', city: '',
  corporationId: authStore.user?.corporationId,
})

function hasPermission(p: string) { return authStore.hasPermission(p) }

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() { clearTimeout(debounceTimer); debounceTimer = setTimeout(doFetch, 400) }

async function doFetch() { filters.page = 1; await consultancyStore.fetchInstitutions(filters) }
function changePage(page: number) { filters.page = page; consultancyStore.fetchInstitutions(filters) }

onMounted(doFetch)
</script>
