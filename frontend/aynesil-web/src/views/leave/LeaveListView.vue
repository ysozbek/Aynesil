<template>
  <div class="container-xxl py-6">
    <!-- Header -->
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('leave.list.title') }}</h1>
        <p class="text-muted fs-6 mb-0">{{ $t('leave.list.subtitle') }}</p>
      </div>
      <RouterLink
        v-if="hasPermission('leave_request:submit')"
        to="/leave/requests/new"
        class="btn btn-primary"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>
        {{ $t('leave.request.new') }}
      </RouterLink>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7 fw-semibold text-gray-700">{{ $t('common.search') }}</label>
            <input
              v-model="filters.search"
              type="text"
              class="form-control form-control-sm"
              :placeholder="$t('leave.list.searchPlaceholder')"
              @input="debouncedFetch"
            />
          </div>
          <div class="col-md-2">
            <label class="form-label fs-7 fw-semibold text-gray-700">{{ $t('common.status') }}</label>
            <select v-model="filters.status" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option value="Pending">{{ $t('leave.status.pending') }}</option>
              <option value="Approved">{{ $t('leave.status.approved') }}</option>
              <option value="Rejected">{{ $t('leave.status.rejected') }}</option>
              <option value="Cancelled">{{ $t('leave.status.cancelled') }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label fs-7 fw-semibold text-gray-700">{{ $t('leave.fields.unit') }}</label>
            <select v-model="filters.unit" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.select') }}</option>
              <option value="Day">{{ $t('leave.unit.day') }}</option>
              <option value="Hour">{{ $t('leave.unit.hour') }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label fs-7 fw-semibold text-gray-700">{{ $t('common.from') }}</label>
            <input v-model="filters.from" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
          <div class="col-md-2">
            <label class="form-label fs-7 fw-semibold text-gray-700">{{ $t('common.to') }}</label>
            <input v-model="filters.to" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
          <div class="col-md-1">
            <button class="btn btn-sm btn-light w-100" @click="resetFilters">
              {{ $t('common.cancel') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="card">
      <div class="card-body py-3">
        <div v-if="leaveStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="leaveStore.error" class="alert alert-danger">
          {{ leaveStore.error }}
        </div>
        <div v-else-if="leaveStore.leaveList.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-calendar fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('leave.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed table-row-gray-300 align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4 w-200px rounded-start">{{ $t('leave.fields.educator') }}</th>
                <th>{{ $t('leave.fields.leaveType') }}</th>
                <th>{{ $t('leave.fields.unit') }}</th>
                <th>{{ $t('leave.fields.startsAt') }}</th>
                <th>{{ $t('leave.fields.endsAt') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4 rounded-end">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in leaveStore.leaveList.items" :key="item.id">
                <td class="ps-4">
                  <span class="fw-semibold text-gray-800">{{ item.educatorFullName ?? '—' }}</span>
                </td>
                <td class="text-muted">{{ item.leaveTypeCode ?? '—' }}</td>
                <td>
                  <span class="badge badge-light fs-8">
                    {{ item.unit === 'Day' ? $t('leave.unit.day') : $t('leave.unit.hour') }}
                  </span>
                </td>
                <td class="text-muted fs-7">{{ formatDate(item.startsAt) }}</td>
                <td class="text-muted fs-7">{{ formatDate(item.endsAt) }}</td>
                <td>
                  <span :class="statusBadge(item.status)">{{ $t(`leave.status.${item.status.toLowerCase()}`) }}</span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/leave/requests/${item.id}`" class="btn btn-sm btn-light-primary me-2">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                  <RouterLink
                    v-if="item.status === 'Pending' && hasPermission('leave_request:update')"
                    :to="`/leave/requests/${item.id}/edit`"
                    class="btn btn-sm btn-light"
                  >
                    <i class="ki-outline ki-pencil fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="leaveStore.leaveList.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button
              class="btn btn-sm btn-light"
              :disabled="!leaveStore.leaveList.hasPreviousPage"
              @click="changePage(filters.page! - 1)"
            >
              {{ $t('common.back') }}
            </button>
            <span class="btn btn-sm btn-light-primary">
              {{ filters.page }} / {{ leaveStore.leaveList.totalPages }}
            </span>
            <button
              class="btn btn-sm btn-light"
              :disabled="!leaveStore.leaveList.hasNextPage"
              @click="changePage(filters.page! + 1)"
            >
              {{ $t('common.next') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import type { LeaveRequestListQuery } from '@/types/leave.types'

const leaveStore = useLeaveStore()
const authStore = useAuthStore()

const filters = reactive<LeaveRequestListQuery>({
  page: 1, pageSize: 20, search: '', status: '', unit: '', from: '', to: '',
  corporationId: authStore.user?.corporationId,
})

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }

function statusBadge(status: string) {
  const map: Record<string, string> = {
    Pending: 'badge badge-light-warning fw-bold',
    Approved: 'badge badge-light-success fw-bold',
    Rejected: 'badge badge-light-danger fw-bold',
    Cancelled: 'badge badge-light-dark fw-bold',
  }
  return map[status] ?? 'badge badge-light fw-bold'
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(doFetch, 400)
}

async function doFetch() {
  filters.page = 1
  await leaveStore.fetchLeaves(filters)
}

function resetFilters() {
  filters.search = ''
  filters.status = ''
  filters.unit = ''
  filters.from = ''
  filters.to = ''
  filters.page = 1
  doFetch()
}

function changePage(page: number) {
  filters.page = page
  leaveStore.fetchLeaves(filters)
}

onMounted(doFetch)
</script>
