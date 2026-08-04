<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('legal.contract.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('legal.contract.list.subtitle') }}</p>
      </div>
      <RouterLink v-if="hasPermission('student_contract:generate')" to="/legal/contracts/new" class="btn btn-primary">
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('legal.contract.new') }}
      </RouterLink>
    </div>

    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.status" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option value="Draft">Draft</option>
              <option value="Sent">{{ $t('legal.contract.status.sent') }}</option>
              <option value="Active">{{ $t('legal.contract.status.active') }}</option>
              <option value="Expired">{{ $t('legal.contract.status.expired') }}</option>
              <option value="Terminated">{{ $t('legal.contract.status.terminated') }}</option>
            </select>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="contractStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="contractStore.contracts.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-document fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('legal.contract.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('legal.contract.fields.student') }}</th>
                <th>{{ $t('legal.contract.fields.template') }}</th>
                <th>{{ $t('legal.contract.fields.startsOn') }}</th>
                <th>{{ $t('legal.contract.fields.endsOn') }}</th>
                <th>{{ $t('legal.contract.fields.signedAt') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in contractStore.contracts.items" :key="c.id">
                <td class="ps-4 fw-semibold">{{ c.studentFullName ?? '—' }}</td>
                <td class="text-muted fs-7">{{ c.templateCode ? `${c.templateCode} v${c.templateVersion}` : '—' }}</td>
                <td class="text-muted fs-7">{{ c.startsOn ?? '—' }}</td>
                <td class="text-muted fs-7">{{ c.endsOn ?? '—' }}</td>
                <td class="text-muted fs-7">{{ c.signedAt ? formatDate(c.signedAt) : '—' }}</td>
                <td><span :class="statusBadge(c.status)">{{ c.status }}</span></td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/legal/contracts/${c.id}`" class="btn btn-sm btn-light-primary">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="contractStore.contracts.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!contractStore.contracts.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ contractStore.contracts.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!contractStore.contracts.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useContractStore } from '@/stores/contract.store'
import { useAuthStore } from '@/stores/auth.store'
import type { ContractListQuery } from '@/types/legal.types'

const contractStore = useContractStore()
const authStore = useAuthStore()

const filters = reactive<ContractListQuery>({
  page: 1, pageSize: 20, status: '',
  corporationId: authStore.user?.corporationId,
})

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }

function statusBadge(s: string) {
  const map: Record<string, string> = {
    Draft: 'badge badge-light-secondary', Sent: 'badge badge-light-warning',
    Active: 'badge badge-light-success', Expired: 'badge badge-light-dark',
    Terminated: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

async function doFetch() { filters.page = 1; await contractStore.fetchContracts(filters) }
function changePage(page: number) { filters.page = page; contractStore.fetchContracts(filters) }

onMounted(doFetch)
</script>
