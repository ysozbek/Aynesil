<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camera.auth.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('camera.auth.subtitle') }}</p>
      </div>
      <button
        v-if="hasPermission('viewing_authorization:grant')"
        class="btn btn-primary"
        @click="showCreateModal = true"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('camera.auth.new') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.isRevoked" class="form-select form-select-sm" @change="doFetch">
              <option :value="undefined">{{ $t('common.allStatuses') }}</option>
              <option :value="false">{{ $t('camera.auth.active') }}</option>
              <option :value="true">{{ $t('camera.auth.revoked') }}</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('camera.auth.currentlyValid') }}</label>
            <select v-model="filters.isCurrentlyValid" class="form-select form-select-sm" @change="doFetch">
              <option :value="undefined">{{ $t('common.allStatuses') }}</option>
              <option :value="true">{{ $t('common.active') }}</option>
              <option :value="false">{{ $t('common.passive') }}</option>
            </select>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="card">
      <div class="card-body py-3">
        <div v-if="cameraStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="cameraStore.authorizations.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('camera.auth.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('camera.auth.guardian') }}</th>
                <th>{{ $t('camera.auth.student') }}</th>
                <th>{{ $t('camera.auth.validFrom') }}</th>
                <th>{{ $t('camera.auth.validTo') }}</th>
                <th>{{ $t('camera.auth.accessType') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="a in cameraStore.authorizations.items" :key="a.id">
                <td class="ps-4 fw-semibold">{{ a.guardianFullName ?? '—' }}</td>
                <td>{{ a.studentFullName ?? '—' }}</td>
                <td class="text-muted fs-7">{{ formatDate(a.validFrom) }}</td>
                <td class="text-muted fs-7">{{ formatDate(a.validTo) }}</td>
                <td>{{ a.accessTypeCode ?? '—' }}</td>
                <td>
                  <span v-if="a.isRevoked" class="badge badge-light-danger">{{ $t('camera.auth.revoked') }}</span>
                  <span v-else-if="a.isCurrentlyValid" class="badge badge-light-success">{{ $t('camera.auth.active') }}</span>
                  <span v-else class="badge badge-light-warning">{{ $t('camera.auth.expired') }}</span>
                </td>
                <td class="text-end pe-4">
                  <button
                    v-if="!a.isRevoked && a.isCurrentlyValid && hasPermission('viewing_authorization:revoke')"
                    class="btn btn-sm btn-light-danger"
                    @click="doRevoke(a.id)"
                  >
                    {{ $t('camera.auth.revoke') }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreateModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('camera.auth.new') }}</h5>
            <button class="btn-close" @click="showCreateModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="row g-3">
              <div class="col-12">
                <label class="form-label required">{{ $t('camera.auth.guardianId') }}</label>
                <input v-model="createForm.guardianId" type="text" class="form-control" />
              </div>
              <div class="col-12">
                <label class="form-label required">{{ $t('camera.auth.studentId') }}</label>
                <input v-model="createForm.studentId" type="text" class="form-control" />
              </div>
              <div class="col-6">
                <label class="form-label required">{{ $t('camera.auth.validFrom') }}</label>
                <input v-model="createForm.validFrom" type="datetime-local" class="form-control" />
              </div>
              <div class="col-6">
                <label class="form-label required">{{ $t('camera.auth.validTo') }}</label>
                <input v-model="createForm.validTo" type="datetime-local" class="form-control" />
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showCreateModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="cameraStore.saving" @click="doCreate">
              <span v-if="cameraStore.saving" class="spinner-border spinner-border-sm me-2"></span>
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
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'

const cameraStore = useCameraStore()
const authStore = useAuthStore()
const showCreateModal = ref(false)

const filters = reactive({ isRevoked: undefined as boolean | undefined, isCurrentlyValid: undefined as boolean | undefined })
const createForm = reactive({ guardianId: '', studentId: '', validFrom: '', validTo: '' })

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

async function doFetch() {
  await cameraStore.fetchAuthorizations({
    corporationId: authStore.user?.corporationId,
    isRevoked: filters.isRevoked,
    isCurrentlyValid: filters.isCurrentlyValid,
  })
}

async function doRevoke(id: string) {
  if (confirm('Bu yetki iptal edilecek. Onaylıyor musunuz?')) {
    await cameraStore.revokeAuthorization(id)
  }
}

async function doCreate() {
  await cameraStore.createAuthorization({
    corporationId: authStore.user?.corporationId ?? '',
    guardianId: createForm.guardianId,
    studentId: createForm.studentId,
    validFrom: new Date(createForm.validFrom).toISOString(),
    validTo: new Date(createForm.validTo).toISOString(),
  })
  showCreateModal.value = false
  Object.assign(createForm, { guardianId: '', studentId: '', validFrom: '', validTo: '' })
  await doFetch()
}

onMounted(doFetch)
</script>
