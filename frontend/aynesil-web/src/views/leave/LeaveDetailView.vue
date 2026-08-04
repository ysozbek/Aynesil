<template>
  <div class="container-xxl py-6">
    <!-- Back -->
    <div class="mb-5">
      <RouterLink to="/leave/requests" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div v-if="leaveStore.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <div v-else-if="!leave" class="text-center py-20 text-muted">
      {{ $t('errors.notFound') }}
    </div>

    <div v-else>
      <!-- Title Row -->
      <div class="d-flex align-items-center justify-content-between mb-6">
        <div>
          <h1 class="text-gray-900 fw-bold fs-2">{{ $t('leave.detail.title') }}</h1>
          <p class="text-muted mb-0">{{ leave.educatorFullName }}</p>
        </div>
        <div class="d-flex gap-2">
          <span :class="statusBadge(leave.status)" class="fs-7 px-4 py-2">
            {{ $t(`leave.status.${leave.status.toLowerCase()}`) }}
          </span>
          <RouterLink
            v-if="leave.status === 'Pending' && hasPermission('leave_request:update')"
            :to="`/leave/requests/${leave.id}/edit`"
            class="btn btn-sm btn-light"
          >
            <i class="ki-outline ki-pencil fs-4 me-1"></i>{{ $t('common.edit') }}
          </RouterLink>
          <button
            v-if="leave.status === 'Pending' && hasPermission('leave_request:approve')"
            class="btn btn-sm btn-success"
            @click="showApproveModal = true"
          >
            <i class="ki-outline ki-check fs-4 me-1"></i>{{ $t('leave.actions.approve') }}
          </button>
          <button
            v-if="leave.status === 'Pending' && hasPermission('leave_request:approve')"
            class="btn btn-sm btn-danger"
            @click="showRejectModal = true"
          >
            <i class="ki-outline ki-cross fs-4 me-1"></i>{{ $t('leave.actions.reject') }}
          </button>
          <button
            v-if="(leave.status === 'Pending' || leave.status === 'Approved') && hasPermission('leave_request:cancel')"
            class="btn btn-sm btn-light-danger"
            @click="doCancel"
          >
            {{ $t('leave.actions.cancel') }}
          </button>
        </div>
      </div>

      <div class="row g-6">
        <!-- Main Info -->
        <div class="col-xl-8">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('leave.detail.requestInfo') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="row g-4">
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('leave.fields.leaveType') }}</div>
                  <div class="fw-semibold">{{ leave.leaveTypeCode ?? '—' }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('leave.fields.unit') }}</div>
                  <div class="fw-semibold">
                    {{ leave.unit === 'Day' ? $t('leave.unit.day') : $t('leave.unit.hour') }}
                    <span v-if="leave.quantity" class="text-muted ms-2">({{ leave.quantity }})</span>
                  </div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('leave.fields.startsAt') }}</div>
                  <div class="fw-semibold">{{ formatDatetime(leave.startsAt) }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('leave.fields.endsAt') }}</div>
                  <div class="fw-semibold">{{ formatDatetime(leave.endsAt) }}</div>
                </div>
                <div class="col-12">
                  <div class="text-muted fs-7 mb-1">{{ $t('leave.fields.reason') }}</div>
                  <div class="fw-semibold">{{ leave.reason ?? '—' }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Session Impact -->
          <div v-if="leaveStore.sessionImpact.length > 0" class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold text-warning">
                <i class="ki-outline ki-information-5 fs-3 me-2 text-warning"></i>
                {{ $t('leave.detail.sessionImpact') }}
              </h3>
            </div>
            <div class="card-body pt-0">
              <div class="table-responsive">
                <table class="table table-row-dashed align-middle gs-0 gy-3">
                  <thead>
                    <tr class="fw-bold text-muted">
                      <th>{{ $t('leave.detail.sessionTitle') }}</th>
                      <th>{{ $t('leave.detail.sessionStart') }}</th>
                      <th>{{ $t('leave.detail.sessionEnd') }}</th>
                      <th>{{ $t('common.status') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="s in leaveStore.sessionImpact" :key="s.sessionId">
                      <td class="fw-semibold">{{ s.sessionTitle ?? '—' }}</td>
                      <td class="text-muted fs-7">{{ formatDatetime(s.sessionStartsAt) }}</td>
                      <td class="text-muted fs-7">{{ formatDatetime(s.sessionEndsAt) }}</td>
                      <td><span class="badge badge-light">{{ s.sessionStatus }}</span></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Approvals -->
          <div class="card">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('leave.detail.approvalHistory') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div v-if="leave.approvals.length === 0" class="text-muted text-center py-6">
                {{ $t('leave.detail.noApprovals') }}
              </div>
              <div v-else>
                <div
                  v-for="a in leave.approvals"
                  :key="a.id"
                  class="d-flex align-items-start mb-5 p-4 rounded bg-light"
                >
                  <div class="symbol symbol-40px me-4">
                    <span :class="`symbol-label bg-light-${a.decision === 'Approved' ? 'success' : a.decision === 'Rejected' ? 'danger' : 'warning'}`">
                      <i :class="`ki-outline ki-${a.decision === 'Approved' ? 'check' : a.decision === 'Rejected' ? 'cross' : 'time'} fs-2 text-${a.decision === 'Approved' ? 'success' : a.decision === 'Rejected' ? 'danger' : 'warning'}`"></i>
                    </span>
                  </div>
                  <div class="flex-grow-1">
                    <div class="fw-semibold">{{ $t('leave.detail.step') }} {{ a.stepNo }}: {{ a.decision }}</div>
                    <div class="text-muted fs-7">{{ a.comment ?? '—' }}</div>
                    <div v-if="a.decidedAt" class="text-muted fs-8">{{ formatDatetime(a.decidedAt) }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Sidebar -->
        <div class="col-xl-4">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('leave.detail.educator') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="d-flex align-items-center">
                <div class="symbol symbol-50px me-4">
                  <span class="symbol-label bg-light-primary">
                    <i class="ki-outline ki-user fs-1 text-primary"></i>
                  </span>
                </div>
                <div>
                  <div class="fw-bold text-gray-800">{{ leave.educatorFullName ?? '—' }}</div>
                  <div class="text-muted fs-7">{{ $t('leave.fields.educator') }}</div>
                </div>
              </div>
              <hr class="my-4" />
              <div class="row g-3 text-center">
                <div class="col-6">
                  <div class="fw-bold fs-3 text-primary">
                    {{ formatDate(leave.createdAt) }}
                  </div>
                  <div class="text-muted fs-8">{{ $t('common.createdAt') }}</div>
                </div>
                <div class="col-6">
                  <div class="fw-bold fs-3 text-gray-700">
                    {{ formatDate(leave.updatedAt) }}
                  </div>
                  <div class="text-muted fs-8">{{ $t('common.updatedAt') }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Approve Modal -->
    <div v-if="showApproveModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('leave.actions.approve') }}</h5>
            <button class="btn-close" @click="showApproveModal = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label">{{ $t('leave.detail.comment') }}</label>
            <textarea v-model="actionComment" class="form-control" rows="3"></textarea>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showApproveModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-success" :disabled="leaveStore.saving" @click="doApprove">
              <span v-if="leaveStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('leave.actions.approve') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Reject Modal -->
    <div v-if="showRejectModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('leave.actions.reject') }}</h5>
            <button class="btn-close" @click="showRejectModal = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label">{{ $t('leave.detail.rejectReason') }}</label>
            <textarea v-model="actionComment" class="form-control" rows="3" :placeholder="$t('leave.detail.rejectReasonPlaceholder')"></textarea>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showRejectModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-danger" :disabled="leaveStore.saving" @click="doReject">
              <span v-if="leaveStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('leave.actions.reject') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const router = useRouter()
const leaveStore = useLeaveStore()
const authStore = useAuthStore()

const id = route.params.id as string
const leave = computed(() => leaveStore.currentLeave)
const showApproveModal = ref(false)
const showRejectModal = ref(false)
const actionComment = ref('')

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }
function formatDatetime(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

function statusBadge(status: string) {
  const map: Record<string, string> = {
    Pending: 'badge badge-light-warning fw-bold',
    Approved: 'badge badge-light-success fw-bold',
    Rejected: 'badge badge-light-danger fw-bold',
    Cancelled: 'badge badge-light-dark fw-bold',
  }
  return map[status] ?? 'badge badge-light fw-bold'
}

async function doApprove() {
  if (!leave.value) return
  await leaveStore.approveLeave(id, { comment: actionComment.value, rowVersion: leave.value.rowVersion })
  showApproveModal.value = false
  actionComment.value = ''
}

async function doReject() {
  if (!leave.value) return
  await leaveStore.rejectLeave(id, { comment: actionComment.value, rowVersion: leave.value.rowVersion })
  showRejectModal.value = false
  actionComment.value = ''
}

async function doCancel() {
  if (!leave.value || !confirm('Bu izin iptal edilecek. Onaylıyor musunuz?')) return
  await leaveStore.cancelLeave(id, { rowVersion: leave.value.rowVersion })
  router.push('/leave/requests')
}

onMounted(async () => {
  leaveStore.clearCurrent()
  await leaveStore.fetchLeave(id)
  if (leaveStore.currentLeave) {
    await leaveStore.fetchSessionImpact(id)
  }
})
</script>
