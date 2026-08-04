<template>
  <div class="container-xxl py-6">
    <!-- Header -->
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('followUp.openReportLabel') }}</h1>
        <p class="text-muted mb-0">{{ $t('followUp.openReportSubtitle') }}</p>
      </div>
      <div class="d-flex gap-2">
        <RouterLink to="/consultancy/follow-ups" class="btn btn-light">
          <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
        </RouterLink>
      </div>
    </div>

    <!-- Stats -->
    <div class="row g-5 mb-6">
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-warning">{{ totalOpen }}</div>
            <div class="text-muted fs-7">{{ $t('followUp.stats.openTotal') }}</div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-danger">{{ overdueCount }}</div>
            <div class="text-muted fs-7">{{ $t('followUp.stats.overdue') }}</div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-info">{{ inProgressCount }}</div>
            <div class="text-muted fs-7">{{ $t('followUp.inProgress') }}</div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-xl-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-primary">{{ pendingCount }}</div>
            <div class="text-muted fs-7">{{ $t('followUp.pending') }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <div v-else-if="store.openFollowUps.length === 0" class="card">
      <div class="card-body text-center py-15 text-muted">
        <i class="ki-outline ki-check-circle fs-3x mb-4 d-block text-success"></i>
        <div class="fw-bold fs-5 text-success">{{ $t('followUp.report.allDone') }}</div>
        <div class="text-muted fs-7 mt-2">{{ $t('followUp.report.noOpenItems') }}</div>
      </div>
    </div>

    <div v-else>
      <!-- Overdue Section -->
      <div v-if="overdueItems.length > 0" class="card mb-6 border border-danger">
        <div class="card-header border-0 bg-light-danger">
          <h3 class="card-title fw-bold text-danger">
            <i class="ki-outline ki-time fs-2 text-danger me-2"></i>
            {{ $t('followUp.overdue') }}
            <span class="badge badge-danger ms-2">{{ overdueItems.length }}</span>
          </h3>
        </div>
        <div class="card-body py-3">
          <div class="table-responsive">
            <table class="table table-row-dashed align-middle gs-0 gy-3">
              <thead>
                <tr class="fw-bold text-muted bg-light">
                  <th class="ps-4">{{ $t('followUp.fields.title') }}</th>
                  <th>{{ $t('followUp.fields.plan') }}</th>
                  <th>{{ $t('followUp.dueDate') }}</th>
                  <th>{{ $t('followUp.assignedTo') }}</th>
                  <th>{{ $t('common.status') }}</th>
                  <th class="text-end pe-4">{{ $t('common.actions') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="f in overdueItems" :key="f.activityId" class="bg-light-danger">
                  <td class="ps-4 fw-semibold text-danger">{{ f.title }}</td>
                  <td class="text-muted fs-7">{{ f.planName ?? '—' }}</td>
                  <td class="fw-bold text-danger">{{ f.dueDate ?? '—' }}</td>
                  <td class="text-muted fs-7">{{ f.assignedTo ?? '—' }}</td>
                  <td><span :class="statusBadge(f.status)">{{ statusLabel(f.status) }}</span></td>
                  <td class="text-end pe-4">
                    <RouterLink :to="`/consultancy/follow-ups/${f.activityId}`" class="btn btn-sm btn-light-primary me-1">
                      <i class="ki-outline ki-eye fs-4"></i>
                    </RouterLink>
                    <button
                      v-if="f.status === 'in_progress' && hasPermission('follow_up:complete')"
                      class="btn btn-sm btn-light-success"
                      @click="openCompleteModal(f.activityId)"
                      :title="$t('followUp.markCompleted')"
                    >
                      <i class="ki-outline ki-check fs-4"></i>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Open (non-overdue) Section -->
      <div class="card">
        <div class="card-header border-0 pt-5">
          <h3 class="card-title fw-bold">{{ $t('followUp.report.upcoming') }}</h3>
        </div>
        <div class="card-body py-3">
          <div v-if="upcomingItems.length === 0" class="text-center py-6 text-muted">
            {{ $t('followUp.report.noUpcoming') }}
          </div>
          <div v-else class="table-responsive">
            <table class="table table-row-dashed align-middle gs-0 gy-3">
              <thead>
                <tr class="fw-bold text-muted bg-light">
                  <th class="ps-4">{{ $t('followUp.fields.title') }}</th>
                  <th>{{ $t('followUp.fields.plan') }}</th>
                  <th>{{ $t('followUp.dueDate') }}</th>
                  <th>{{ $t('followUp.assignedTo') }}</th>
                  <th>{{ $t('common.status') }}</th>
                  <th class="text-end pe-4">{{ $t('common.actions') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="f in upcomingItems" :key="f.activityId">
                  <td class="ps-4 fw-semibold">{{ f.title }}</td>
                  <td class="text-muted fs-7">{{ f.planName ?? '—' }}</td>
                  <td class="text-muted fs-7">{{ f.dueDate ?? '—' }}</td>
                  <td class="text-muted fs-7">{{ f.assignedTo ?? '—' }}</td>
                  <td><span :class="statusBadge(f.status)">{{ statusLabel(f.status) }}</span></td>
                  <td class="text-end pe-4">
                    <RouterLink :to="`/consultancy/follow-ups/${f.activityId}`" class="btn btn-sm btn-light-primary me-1">
                      <i class="ki-outline ki-eye fs-4"></i>
                    </RouterLink>
                    <button
                      v-if="f.status === 'pending' && hasPermission('follow_up:start')"
                      class="btn btn-sm btn-light-info"
                      @click="quickStart(f.activityId)"
                      :title="$t('followUp.start')"
                    >
                      <i class="ki-outline ki-arrow-right fs-4"></i>
                    </button>
                    <button
                      v-if="f.status === 'in_progress' && hasPermission('follow_up:complete')"
                      class="btn btn-sm btn-light-success"
                      @click="openCompleteModal(f.activityId)"
                      :title="$t('followUp.markCompleted')"
                    >
                      <i class="ki-outline ki-check fs-4"></i>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
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
import { computed, reactive, onMounted } from 'vue'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const store = useConsultancyStore()
const authStore = useAuthStore()

const completeModal = reactive({ show: false, followUpId: '', notes: '' })

// The open follow-ups report returns items already filtered (open=pending+in_progress)
// Backend sorts overdue first — we also group locally for visual separation
const overdueItems = computed(() => store.openFollowUps.filter(f => f.isOverdue))
const upcomingItems = computed(() => store.openFollowUps.filter(f => !f.isOverdue))

const totalOpen = computed(() => store.openFollowUps.length)
const overdueCount = computed(() => overdueItems.value.length)
const inProgressCount = computed(() => store.openFollowUps.filter(f => f.status === 'in_progress').length)
const pendingCount = computed(() => store.openFollowUps.filter(f => f.status === 'pending').length)

function hasPermission(p: string) { return authStore.hasPermission(p) }

function statusBadge(s: string) {
  const map: Record<string, string> = {
    pending: 'badge badge-light-warning',
    in_progress: 'badge badge-light-info',
  }
  return map[s] ?? 'badge badge-light'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    pending: t('followUp.pending'),
    in_progress: t('followUp.inProgress'),
  }
  return map[s] ?? s
}

async function quickStart(id: string) {
  await store.startFollowUp(id)
  await reload()
}

function openCompleteModal(id: string) {
  completeModal.followUpId = id
  completeModal.notes = ''
  completeModal.show = true
}

async function doComplete() {
  // Fetch full DTO to get rowVersion before completing
  await store.fetchFollowUp(completeModal.followUpId)
  const rv = store.currentFollowUp?.rowVersion ?? 1
  await store.completeFollowUp(completeModal.followUpId, {
    notes: completeModal.notes || undefined,
    rowVersion: rv,
  })
  completeModal.show = false
  await reload()
}

async function reload() {
  await store.fetchOpenFollowUps({ corporationId: authStore.user?.corporationId })
}

onMounted(reload)
</script>
