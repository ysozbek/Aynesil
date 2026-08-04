<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('leave.balance.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('leave.balance.subtitle') }}</p>
      </div>
      <button
        v-if="hasPermission('leave_request:approve')"
        class="btn btn-light-primary"
        @click="showCarryForwardModal = true"
      >
        <i class="ki-outline ki-arrows-circle fs-2 me-1"></i>
        {{ $t('leave.balance.carryForward') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('leave.fields.periodYear') }}</label>
            <input v-model.number="filters.periodYear" type="number" class="form-control form-control-sm" @change="doFetch" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('leave.fields.leaveType') }}</label>
            <select v-model="filters.leaveTypeId" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option v-for="lt in leaveTypes" :key="lt.id" :value="lt.id">{{ lt.label || lt.code }}</option>
            </select>
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
        <div v-else-if="leaveStore.balances.length === 0" class="text-center py-15 text-muted">
          {{ $t('leave.balance.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('leave.fields.educator') }}</th>
                <th>{{ $t('leave.fields.leaveType') }}</th>
                <th>{{ $t('leave.fields.periodYear') }}</th>
                <th class="text-end">{{ $t('leave.balance.entitled') }}</th>
                <th class="text-end">{{ $t('leave.balance.used') }}</th>
                <th class="text-end">{{ $t('leave.balance.remaining') }}</th>
                <th>{{ $t('leave.fields.unit') }}</th>
                <th v-if="hasPermission('leave_request:approve')" class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="b in leaveStore.balances" :key="b.id">
                <td class="ps-4 fw-semibold">{{ b.educatorFullName }}</td>
                <td>{{ b.leaveTypeCode ?? '—' }}</td>
                <td>{{ b.periodYear }}</td>
                <td class="text-end">{{ b.entitled }}</td>
                <td class="text-end text-warning">{{ b.used }}</td>
                <td class="text-end fw-bold text-success">{{ b.remaining }}</td>
                <td>{{ b.unit === 'Day' ? $t('leave.unit.day') : $t('leave.unit.hour') }}</td>
                <td v-if="hasPermission('leave_request:approve')" class="text-end pe-4">
                  <button
                    class="btn btn-sm btn-light-primary"
                    @click="openEntitlement(b)"
                  >
                    <i class="ki-outline ki-pencil fs-4"></i>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Entitlement Modal -->
    <div v-if="showEntitlementModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('leave.balance.setEntitlement') }}</h5>
            <button class="btn-close" @click="showEntitlementModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="mb-4">
              <div class="text-muted fs-7">{{ selectedBalance?.educatorFullName }}</div>
              <div class="fw-semibold">{{ selectedBalance?.leaveTypeCode }} · {{ selectedBalance?.periodYear }}</div>
            </div>
            <label class="form-label required">{{ $t('leave.balance.entitled') }}</label>
            <input v-model.number="newEntitled" type="number" step="0.5" min="0" class="form-control" />
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showEntitlementModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="leaveStore.saving" @click="doSetEntitlement">
              <span v-if="leaveStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Carry Forward Modal -->
    <div v-if="showCarryForwardModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('leave.balance.carryForward') }}</h5>
            <button class="btn-close" @click="showCarryForwardModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="row g-4">
              <div class="col-6">
                <label class="form-label required">{{ $t('leave.balance.fromYear') }}</label>
                <input v-model.number="cfFrom" type="number" class="form-control" />
              </div>
              <div class="col-6">
                <label class="form-label required">{{ $t('leave.balance.toYear') }}</label>
                <input v-model.number="cfTo" type="number" class="form-control" />
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showCarryForwardModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="leaveStore.saving" @click="doCarryForward">
              <span v-if="leaveStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('leave.balance.carryForward') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'
import type { LeaveBalanceDto } from '@/types/leave.types'

const leaveStore = useLeaveStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()

const filters = reactive({ periodYear: new Date().getFullYear(), leaveTypeId: '' })
const showEntitlementModal = ref(false)
const showCarryForwardModal = ref(false)
const selectedBalance = ref<LeaveBalanceDto | null>(null)
const newEntitled = ref(0)
const cfFrom = ref(new Date().getFullYear() - 1)
const cfTo = ref(new Date().getFullYear())

const leaveTypes = computed(() => refDataStore.getByCategory?.('leave_type') ?? [])
function hasPermission(p: string) { return authStore.hasPermission(p) }

async function doFetch() {
  await leaveStore.fetchBalances({
    corporationId: authStore.user?.corporationId,
    periodYear: filters.periodYear || undefined,
    leaveTypeId: filters.leaveTypeId || undefined,
  })
}

function openEntitlement(b: LeaveBalanceDto) {
  selectedBalance.value = b
  newEntitled.value = b.entitled
  showEntitlementModal.value = true
}

async function doSetEntitlement() {
  if (!selectedBalance.value) return
  await leaveStore.setEntitlement(selectedBalance.value.id, { entitled: newEntitled.value })
  showEntitlementModal.value = false
  await doFetch()
}

async function doCarryForward() {
  await leaveStore.carryForward({
    corporationId: authStore.user?.corporationId ?? '',
    fromYear: cfFrom.value,
    toYear: cfTo.value,
  })
  showCarryForwardModal.value = false
  await doFetch()
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('leave_type')
  await doFetch()
})
</script>
