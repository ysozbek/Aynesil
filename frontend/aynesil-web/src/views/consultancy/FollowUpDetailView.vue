<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <button class="btn btn-sm btn-light" @click="router.back()">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </button>
    </div>

    <div v-if="store.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>
    <div v-else-if="!followUp" class="text-center py-20 text-muted">{{ $t('errors.notFound') }}</div>

    <div v-else>
      <!-- Overdue Banner -->
      <div v-if="isOverdue" class="alert alert-danger d-flex align-items-center mb-6">
        <i class="ki-outline ki-time fs-1 text-danger me-4"></i>
        <div class="fw-bold fs-6">{{ $t('followUp.overdueAlert') }}</div>
      </div>

      <!-- Title Row -->
      <div class="d-flex align-items-center justify-content-between mb-6">
        <div>
          <h1 class="text-gray-900 fw-bold fs-2">{{ followUp.title }}</h1>
          <p class="text-muted mb-0">
            <span v-if="followUp.planName">{{ followUp.planName }}</span>
            <span v-if="followUp.visitDate"> · {{ followUp.visitDate }}</span>
          </p>
        </div>
        <div class="d-flex gap-2 align-items-center">
          <span :class="statusBadge(followUp.status) + ' fs-7 px-4 py-2'">{{ statusLabel(followUp.status) }}</span>

          <!-- Edit (pending or in_progress) -->
          <RouterLink
            v-if="(followUp.status === 'pending' || followUp.status === 'in_progress') && hasPermission('follow_up:update')"
            :to="`/consultancy/follow-ups/${followUp.id}/edit`"
            class="btn btn-sm btn-light"
          >
            <i class="ki-outline ki-pencil fs-4 me-1"></i>{{ $t('common.edit') }}
          </RouterLink>

          <!-- Start (pending → in_progress) -->
          <button
            v-if="followUp.status === 'pending' && hasPermission('follow_up:start')"
            class="btn btn-sm btn-info"
            :disabled="store.saving"
            @click="doStart"
          >
            <i class="ki-outline ki-arrow-right fs-4 me-1"></i>{{ $t('followUp.start') }}
          </button>

          <!-- Complete (in_progress → completed) -->
          <button
            v-if="followUp.status === 'in_progress' && hasPermission('follow_up:complete')"
            class="btn btn-sm btn-success"
            @click="showCompleteModal = true"
          >
            <i class="ki-outline ki-check fs-4 me-1"></i>{{ $t('followUp.markCompleted') }}
          </button>

          <!-- Cancel -->
          <button
            v-if="(followUp.status === 'pending' || followUp.status === 'in_progress') && hasPermission('follow_up:cancel')"
            class="btn btn-sm btn-light-danger"
            :disabled="store.saving"
            @click="doCancel"
          >
            {{ $t('followUp.cancel') }}
          </button>
        </div>
      </div>

      <div class="row g-6">
        <!-- Main Info -->
        <div class="col-xl-8">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('followUp.detail.info') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="row g-4">
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('followUp.dueDate') }}</div>
                  <div :class="isOverdue ? 'fw-bold text-danger' : 'fw-semibold'">
                    {{ followUp.dueDate ?? '—' }}
                  </div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('followUp.assignedTo') }}</div>
                  <div class="fw-semibold">{{ followUp.assignedTo ?? '—' }}</div>
                </div>
                <div v-if="followUp.description" class="col-12">
                  <div class="text-muted fs-7 mb-1">{{ $t('followUp.fields.description') }}</div>
                  <div class="text-gray-700">{{ followUp.description }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Completion Notes -->
          <div v-if="followUp.status === 'completed' && followUp.notes" class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold text-success">
                <i class="ki-outline ki-check-circle fs-2 text-success me-2"></i>
                {{ $t('followUp.completionNote') }}
              </h3>
            </div>
            <div class="card-body pt-0">
              <p class="text-gray-700 mb-0">{{ followUp.notes }}</p>
              <div v-if="followUp.completedAt" class="text-muted fs-7 mt-2">
                {{ $t('followUp.detail.completedAt') }}: {{ formatDatetime(followUp.completedAt) }}
              </div>
            </div>
          </div>

          <!-- Status Timeline -->
          <div class="card">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('followUp.detail.timeline') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="timeline">
                <!-- Created -->
                <div class="timeline-item mb-4">
                  <div class="timeline-line w-40px"></div>
                  <div class="timeline-icon symbol symbol-circle symbol-40px">
                    <div class="symbol-label bg-light-primary">
                      <i class="ki-outline ki-plus fs-2 text-primary"></i>
                    </div>
                  </div>
                  <div class="timeline-content mb-5 mt-n2 ps-4">
                    <div class="fw-semibold">{{ $t('followUp.detail.created') }}</div>
                    <div class="text-muted fs-7">{{ formatDatetime(followUp.createdAt) }}</div>
                  </div>
                </div>
                <!-- Completed -->
                <div v-if="followUp.completedAt" class="timeline-item mb-4">
                  <div class="timeline-line w-40px"></div>
                  <div class="timeline-icon symbol symbol-circle symbol-40px">
                    <div class="symbol-label bg-light-success">
                      <i class="ki-outline ki-check fs-2 text-success"></i>
                    </div>
                  </div>
                  <div class="timeline-content mb-5 mt-n2 ps-4">
                    <div class="fw-semibold text-success">{{ $t('followUp.completed') }}</div>
                    <div class="text-muted fs-7">{{ formatDatetime(followUp.completedAt) }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Sidebar: Source + Metadata -->
        <div class="col-xl-4">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('followUp.detail.source') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div v-if="followUp.planName" class="mb-3">
                <div class="text-muted fs-7 mb-1">{{ $t('followUp.fields.plan') }}</div>
                <RouterLink
                  v-if="followUp.consultancyPlanId"
                  :to="`/consultancy/plans/${followUp.consultancyPlanId}`"
                  class="fw-semibold text-primary"
                >
                  {{ followUp.planName }}
                </RouterLink>
              </div>
              <div v-if="followUp.visitDate" class="mb-3">
                <div class="text-muted fs-7 mb-1">{{ $t('followUp.fields.visitDate') }}</div>
                <div class="fw-semibold">{{ followUp.visitDate }}</div>
              </div>
              <div v-if="followUp.observationRecordId" class="mb-3">
                <div class="text-muted fs-7 mb-1">{{ $t('followUp.fields.observation') }}</div>
                <span class="badge badge-light-secondary">{{ $t('followUp.detail.linkedObservation') }}</span>
              </div>
            </div>
          </div>

          <div class="card">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('followUp.detail.metadata') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="mb-3">
                <span class="text-muted fs-7">{{ $t('common.createdAt') }}:</span>
                <span class="fw-semibold ms-2">{{ formatDate(followUp.createdAt) }}</span>
              </div>
              <div class="mb-3">
                <span class="text-muted fs-7">{{ $t('common.updatedAt') }}:</span>
                <span class="fw-semibold ms-2">{{ formatDate(followUp.updatedAt) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Complete Modal -->
    <div v-if="showCompleteModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('followUp.markCompleted') }}</h5>
            <button class="btn-close" @click="showCompleteModal = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label">{{ $t('followUp.completionNote') }}</label>
            <textarea v-model="completionNotes" class="form-control" rows="4" :placeholder="$t('followUp.completionNotePlaceholder')"></textarea>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showCompleteModal = false">{{ $t('common.cancel') }}</button>
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
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const authStore = useAuthStore()
const id = route.params.id as string
const followUp = computed(() => store.currentFollowUp)
const showCompleteModal = ref(false)
const completionNotes = ref('')

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }
function formatDatetime(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

const isOverdue = computed(() => {
  const f = followUp.value
  if (!f?.dueDate || f.status === 'completed' || f.status === 'cancelled') return false
  return new Date(f.dueDate) < new Date()
})

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

async function doStart() {
  await store.startFollowUp(id)
}

async function doComplete() {
  if (!followUp.value) return
  await store.completeFollowUp(id, {
    notes: completionNotes.value || undefined,
    rowVersion: followUp.value.rowVersion,
  })
  showCompleteModal.value = false
  completionNotes.value = ''
}

async function doCancel() {
  if (!confirm(t('followUp.cancelConfirm'))) return
  await store.cancelFollowUp(id)
}

onMounted(() => {
  store.currentFollowUp = null
  store.fetchFollowUp(id)
})
</script>
