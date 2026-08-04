<template>
  <div class="container-xxl py-6">
    <!-- Header -->
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('followUp.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('followUp.subtitle') }}</p>
      </div>
      <div class="d-flex gap-2">
        <RouterLink to="/consultancy/follow-ups/open" class="btn btn-light-warning">
          <i class="ki-outline ki-time fs-2 me-1"></i>{{ $t('followUp.openReportLabel') }}
        </RouterLink>
        <button
          v-if="hasPermission('follow_up:create')"
          class="btn btn-primary"
          @click="showCreateModal = true"
        >
          <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('followUp.new') }}
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.status" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option value="pending">{{ $t('followUp.pending') }}</option>
              <option value="in_progress">{{ $t('followUp.inProgress') }}</option>
              <option value="completed">{{ $t('followUp.completed') }}</option>
              <option value="cancelled">{{ $t('followUp.cancelled') }}</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">&nbsp;</label>
            <div class="form-check form-switch mt-2">
              <input
                v-model="filters.overdueOnly"
                class="form-check-input"
                type="checkbox"
                @change="doFetch"
              />
              <label class="form-check-label text-danger fw-semibold">
                {{ $t('followUp.overdueOnly') }}
              </label>
            </div>
          </div>
          <div class="col-md-2">
            <label class="form-label fs-7">&nbsp;</label>
            <button class="btn btn-sm btn-light w-100" @click="resetFilters">{{ $t('common.cancel') }}</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="card">
      <div class="card-body py-3">
        <div v-if="store.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="store.followUps.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-check-circle fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('followUp.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('followUp.fields.title') }}</th>
                <th>{{ $t('followUp.fields.plan') }}</th>
                <th>{{ $t('followUp.fields.visit') }}</th>
                <th>{{ $t('followUp.dueDate') }}</th>
                <th>{{ $t('followUp.assignedTo') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="f in store.followUps.items"
                :key="f.id"
                :class="{ 'table-warning': isOverdue(f) }"
              >
                <td class="ps-4 fw-semibold">
                  <span v-if="isOverdue(f)" class="badge badge-light-danger me-2 fs-8">{{ $t('followUp.overdue') }}</span>
                  {{ f.title }}
                </td>
                <td class="text-muted fs-7">{{ f.planName ?? '—' }}</td>
                <td class="text-muted fs-7">{{ f.visitDate ?? '—' }}</td>
                <td :class="isOverdue(f) ? 'text-danger fw-bold' : 'text-muted fs-7'">
                  {{ f.dueDate ?? '—' }}
                </td>
                <td class="text-muted fs-7">{{ f.assignedTo ?? '—' }}</td>
                <td><span :class="statusBadge(f.status)">{{ statusLabel(f.status) }}</span></td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/consultancy/follow-ups/${f.id}`" class="btn btn-sm btn-light-primary me-1">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                  <!-- Quick Start -->
                  <button
                    v-if="f.status === 'pending' && hasPermission('follow_up:start')"
                    class="btn btn-sm btn-light-info me-1"
                    :disabled="store.saving"
                    @click="quickStart(f.id)"
                    :title="$t('followUp.start')"
                  >
                    <i class="ki-outline ki-arrow-right fs-4"></i>
                  </button>
                  <!-- Quick Complete -->
                  <button
                    v-if="f.status === 'in_progress' && hasPermission('follow_up:complete')"
                    class="btn btn-sm btn-light-success"
                    @click="openCompleteModal(f.id, f.rowVersion ?? 1)"
                    :title="$t('followUp.markCompleted')"
                  >
                    <i class="ki-outline ki-check fs-4"></i>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="store.followUps.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!store.followUps.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ store.followUps.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!store.followUps.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Follow-up Modal -->
    <div v-if="showCreateModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('followUp.new') }}</h5>
            <button class="btn-close" @click="showCreateModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="row g-4">
              <div class="col-12">
                <label class="form-label required">{{ $t('followUp.fields.title') }}</label>
                <input v-model="createForm.title" type="text" class="form-control" />
              </div>
              <div class="col-12">
                <label class="form-label">{{ $t('followUp.fields.description') }}</label>
                <textarea v-model="createForm.description" class="form-control" rows="3"></textarea>
              </div>
              <div class="col-sm-6">
                <label class="form-label">{{ $t('followUp.dueDate') }}</label>
                <input v-model="createForm.dueDate" type="date" class="form-control" />
              </div>
              <div class="col-sm-6">
                <label class="form-label">{{ $t('followUp.assignedTo') }} ID</label>
                <input v-model="createForm.assignedTo" type="text" class="form-control" />
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showCreateModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="store.saving || !createForm.title" @click="doCreate">
              <span v-if="store.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Complete Modal -->
    <div v-if="completeModal.show" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('followUp.markCompleted') }}</h5>
            <button class="btn-close" @click="completeModal.show = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label">{{ $t('followUp.completionNote') }}</label>
            <textarea v-model="completeModal.notes" class="form-control" rows="3" :placeholder="$t('followUp.completionNotePlaceholder')"></textarea>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="completeModal.show = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-success" :disabled="store.saving" @click="doComplete">
              <span v-if="store.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('followUp.markCompleted') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { useI18n } from 'vue-i18n'
import type { FollowUpActivityListItemDto } from '@/types/consultancy.types'

const { t } = useI18n()
const store = useConsultancyStore()
const authStore = useAuthStore()
const showCreateModal = ref(false)

const filters = reactive({
  page: 1, pageSize: 20, status: '', overdueOnly: false,
  corporationId: authStore.user?.corporationId,
})

const createForm = reactive({
  title: '', description: '', dueDate: '', assignedTo: '',
})

const completeModal = reactive({
  show: false, followUpId: '', rowVersion: 1, notes: '',
})

function hasPermission(p: string) { return authStore.hasPermission(p) }

function isOverdue(f: FollowUpActivityListItemDto): boolean {
  if (!f.dueDate || f.status === 'completed' || f.status === 'cancelled') return false
  return new Date(f.dueDate) < new Date()
}

function statusBadge(s: string) {
  const map: Record<string, string> = {
    pending: 'badge badge-light-warning',
    in_progress: 'badge badge-light-info',
    completed: 'badge badge-light-success',
    cancelled: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    pending: t('followUp.pending'),
    in_progress: t('followUp.inProgress'),
    completed: t('followUp.completed'),
    cancelled: t('followUp.cancelled'),
  }
  return map[s] ?? s
}

async function quickStart(id: string) {
  await store.startFollowUp(id)
  await doFetch()
}

function openCompleteModal(id: string, rowVersion: number) {
  completeModal.followUpId = id
  completeModal.rowVersion = rowVersion
  completeModal.notes = ''
  completeModal.show = true
}

async function doComplete() {
  await store.completeFollowUp(completeModal.followUpId, {
    notes: completeModal.notes || undefined,
    rowVersion: completeModal.rowVersion,
  })
  completeModal.show = false
  await doFetch()
}

async function doCreate() {
  await store.createFollowUp({
    corporationId: authStore.user?.corporationId ?? '',
    title: createForm.title,
    description: createForm.description || undefined,
    dueDate: createForm.dueDate || undefined,
    assignedTo: createForm.assignedTo || undefined,
  })
  showCreateModal.value = false
  Object.assign(createForm, { title: '', description: '', dueDate: '', assignedTo: '' })
  await doFetch()
}

async function doFetch() {
  filters.page = 1
  await store.fetchFollowUps(filters)
}

function resetFilters() {
  filters.status = ''
  filters.overdueOnly = false
  filters.page = 1
  doFetch()
}

function changePage(page: number) {
  filters.page = page
  store.fetchFollowUps(filters)
}

onMounted(doFetch)
</script>
