<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/legal/contracts" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div v-if="contractStore.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <div v-else-if="!contract" class="text-center py-20 text-muted">{{ $t('errors.notFound') }}</div>

    <div v-else>
      <div class="d-flex align-items-center justify-content-between mb-6">
        <div>
          <h1 class="text-gray-900 fw-bold fs-2">{{ $t('legal.contract.detail.title') }}</h1>
          <p class="text-muted mb-0">{{ contract.studentFullName }}</p>
        </div>
        <div class="d-flex gap-2">
          <span :class="statusBadge(contract.status) + ' fs-7 px-4 py-2'">{{ contract.status }}</span>
          <button
            v-if="contract.status === 'Draft' && hasPermission('student_contract:send')"
            class="btn btn-sm btn-primary"
            :disabled="contractStore.saving"
            @click="doSend"
          >
            {{ $t('legal.contract.actions.send') }}
          </button>
          <button
            v-if="contract.status === 'Sent' && hasPermission('student_contract:sign')"
            class="btn btn-sm btn-success"
            :disabled="contractStore.saving"
            @click="showSignModal = true"
          >
            {{ $t('legal.contract.actions.sign') }}
          </button>
          <button
            v-if="contract.status === 'Active' && hasPermission('student_contract:terminate')"
            class="btn btn-sm btn-light-danger"
            :disabled="contractStore.saving"
            @click="doTerminate"
          >
            {{ $t('legal.contract.actions.terminate') }}
          </button>
        </div>
      </div>

      <div class="row g-6">
        <div class="col-xl-8">
          <div class="card">
            <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('legal.contract.detail.info') }}</h3></div>
            <div class="card-body pt-0">
              <div class="row g-4">
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.student') }}</div>
                  <div class="fw-semibold">{{ contract.studentFullName ?? '—' }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.template') }}</div>
                  <div class="fw-semibold">{{ contract.templateCode ? `${contract.templateCode} v${contract.templateVersion}` : '—' }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.startsOn') }}</div>
                  <div class="fw-semibold">{{ contract.startsOn ?? '—' }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.endsOn') }}</div>
                  <div class="fw-semibold">{{ contract.endsOn ?? '—' }}</div>
                </div>
                <div v-if="contract.signedAt" class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.signedAt') }}</div>
                  <div class="fw-semibold">{{ formatDatetime(contract.signedAt) }}</div>
                </div>
                <div v-if="contract.signedByName" class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.signedBy') }}</div>
                  <div class="fw-semibold">{{ contract.signedByName }}</div>
                </div>
                <div v-if="contract.signatureMethod" class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('legal.contract.fields.signatureMethod') }}</div>
                  <div class="fw-semibold">{{ contract.signatureMethod }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-xl-4">
          <div class="card">
            <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('legal.contract.detail.metadata') }}</h3></div>
            <div class="card-body pt-0">
              <div class="mb-3"><span class="text-muted fs-7">{{ $t('common.createdAt') }}:</span> <span class="fw-semibold ms-2">{{ formatDate(contract.createdAt) }}</span></div>
              <div class="mb-3"><span class="text-muted fs-7">{{ $t('common.updatedAt') }}:</span> <span class="fw-semibold ms-2">{{ formatDate(contract.updatedAt) }}</span></div>
              <div v-if="contract.signedFileId" class="mb-3">
                <span class="text-muted fs-7">{{ $t('legal.contract.fields.signedFile') }}:</span>
                <span class="badge badge-light-success ms-2">{{ $t('legal.contract.fields.fileAttached') }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Sign Modal -->
    <div v-if="showSignModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('legal.contract.actions.sign') }}</h5>
            <button class="btn-close" @click="showSignModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="mb-4 p-4 rounded bg-light-warning text-warning fs-7">
              <i class="ki-outline ki-information-5 fs-3 me-2"></i>
              {{ $t('legal.contract.detail.signWarning') }}
            </div>
            <label class="form-label">{{ $t('legal.contract.fields.signatureMethod') }}</label>
            <select v-model="signForm.method" class="form-select">
              <option value="Manual">{{ $t('legal.signature.manual') }}</option>
              <option value="Electronic">{{ $t('legal.signature.electronic') }}</option>
            </select>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showSignModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-success" :disabled="contractStore.saving" @click="doSign">
              <span v-if="contractStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('legal.contract.actions.sign') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useContractStore } from '@/stores/contract.store'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const contractStore = useContractStore()
const authStore = useAuthStore()
const id = route.params.id as string
const contract = computed(() => contractStore.currentContract)
const showSignModal = ref(false)
const signForm = reactive({ method: 'Manual' })

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }
function formatDatetime(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

function statusBadge(s: string) {
  const map: Record<string, string> = {
    Draft: 'badge badge-light-secondary', Sent: 'badge badge-light-warning',
    Active: 'badge badge-light-success', Expired: 'badge badge-light-dark',
    Terminated: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

async function doSend() {
  await contractStore.sendContract(id)
}

async function doSign() {
  if (!contract.value) return
  await contractStore.signContract(id, {
    signatureMethod: signForm.method,
    rowVersion: contract.value.rowVersion,
  })
  showSignModal.value = false
}

async function doTerminate() {
  if (confirm('Bu sözleşme feshedilecek. Onaylıyor musunuz?')) {
    await contractStore.terminateContract(id)
  }
}

onMounted(() => {
  contractStore.clearCurrent()
  contractStore.fetchContract(id)
})
</script>
