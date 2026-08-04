<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('consultancy.visit.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('consultancy.visit.list.subtitle') }}</p>
      </div>
      <button
        v-if="hasPermission('school_visit:create')"
        class="btn btn-primary"
        @click="showCreateModal = true"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('consultancy.visit.new') }}
      </button>
    </div>

    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.status" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option value="Scheduled">{{ $t('consultancy.visit.status.scheduled') }}</option>
              <option value="Completed">{{ $t('consultancy.visit.status.completed') }}</option>
              <option value="Cancelled">{{ $t('consultancy.visit.status.cancelled') }}</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.from') }}</label>
            <input v-model="filters.from" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.to') }}</label>
            <input v-model="filters.to" type="date" class="form-control form-control-sm" @change="doFetch" />
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="consultancyStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="consultancyStore.visits.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('consultancy.visit.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('consultancy.institution.fields.name') }}</th>
                <th>{{ $t('consultancy.visit.fields.plan') }}</th>
                <th>{{ $t('consultancy.visit.fields.visitDate') }}</th>
                <th>{{ $t('consultancy.visit.fields.purpose') }}</th>
                <th class="text-center">{{ $t('consultancy.visit.fields.observations') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="v in consultancyStore.visits.items" :key="v.id">
                <td class="ps-4 fw-semibold">{{ v.institutionName }}</td>
                <td class="text-muted">{{ v.planName ?? '—' }}</td>
                <td class="text-muted fs-7">{{ v.visitDate }}</td>
                <td class="text-muted" style="max-width:200px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;">{{ v.purpose ?? '—' }}</td>
                <td class="text-center">{{ v.observationCount }}</td>
                <td>
                  <span :class="visitStatusBadge(v.status)">{{ v.status }}</span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/consultancy/visits/${v.id}`" class="btn btn-sm btn-light-primary">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Create Visit Modal -->
    <div v-if="showCreateModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('consultancy.visit.new') }}</h5>
            <button class="btn-close" @click="showCreateModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="row g-3">
              <div class="col-12">
                <label class="form-label required">{{ $t('consultancy.institution.fields.name') }} ID</label>
                <input v-model="createForm.institutionId" type="text" class="form-control" />
              </div>
              <div class="col-12">
                <label class="form-label required">{{ $t('consultancy.visit.fields.visitDate') }}</label>
                <input v-model="createForm.visitDate" type="date" class="form-control" />
              </div>
              <div class="col-12">
                <label class="form-label">{{ $t('consultancy.visit.fields.purpose') }}</label>
                <textarea v-model="createForm.purpose" class="form-control" rows="3"></textarea>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showCreateModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="consultancyStore.saving" @click="doCreate">
              <span v-if="consultancyStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import type { VisitListQuery } from '@/types/consultancy.types'

const router = useRouter()
const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()
const showCreateModal = ref(false)

const filters = reactive<VisitListQuery>({
  page: 1, pageSize: 20, status: '', from: '', to: '',
  corporationId: authStore.user?.corporationId,
})
const createForm = reactive({ institutionId: '', visitDate: '', purpose: '' })

function hasPermission(p: string) { return authStore.hasPermission(p) }

function visitStatusBadge(s: string) {
  const map: Record<string, string> = {
    Scheduled: 'badge badge-light-primary', Completed: 'badge badge-light-success',
    Cancelled: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

async function doFetch() {
  filters.page = 1
  await consultancyStore.fetchVisits(filters)
}

async function doCreate() {
  const result = await consultancyStore.createVisit({
    corporationId: authStore.user?.corporationId ?? '',
    institutionId: createForm.institutionId,
    visitDate: createForm.visitDate,
    purpose: createForm.purpose || undefined,
  })
  showCreateModal.value = false
  router.push(`/consultancy/visits/${result.id}`)
}

onMounted(doFetch)
</script>
