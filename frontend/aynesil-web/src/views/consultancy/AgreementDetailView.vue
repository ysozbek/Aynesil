<template>
  <div class="container-xxl py-6">
    <!-- Back -->
    <div class="mb-5">
      <RouterLink to="/consultancy/agreements" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div v-if="store.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>
    <div v-else-if="!agreement" class="text-center py-20 text-muted">{{ $t('errors.notFound') }}</div>

    <div v-else>
      <!-- Immutability Banner -->
      <div v-if="agreement.status === 'signed'" class="alert alert-warning d-flex align-items-center mb-6">
        <i class="ki-outline ki-shield-tick fs-1 text-warning me-4"></i>
        <div>
          <div class="fw-bold fs-6">{{ $t('consultancyContract.immutableBanner') }}</div>
          <div class="text-muted fs-7">{{ $t('consultancyContract.immutableDesc') }}</div>
        </div>
      </div>

      <!-- Title Row -->
      <div class="d-flex align-items-center justify-content-between mb-6">
        <div>
          <h1 class="text-gray-900 fw-bold fs-2">{{ agreement.title }}</h1>
          <p class="text-muted mb-0">{{ agreement.institutionName }} · {{ agreement.planName }}</p>
        </div>
        <div class="d-flex gap-2 align-items-center flex-wrap">
          <span :class="statusBadge(agreement.status) + ' fs-7 px-4 py-2'">{{ statusLabel(agreement.status) }}</span>

          <!-- Draft: Edit + Send + Cancel -->
          <RouterLink
            v-if="agreement.status === 'draft' && hasPermission('consultancy_agreement:update')"
            :to="`/consultancy/agreements/${agreement.id}/edit`"
            class="btn btn-sm btn-light"
          >
            <i class="ki-outline ki-pencil fs-4 me-1"></i>{{ $t('common.edit') }}
          </RouterLink>
          <button
            v-if="agreement.status === 'draft' && hasPermission('consultancy_agreement:send')"
            class="btn btn-sm btn-primary"
            :disabled="store.saving"
            @click="doSend"
          >
            <span v-if="store.saving" class="spinner-border spinner-border-sm me-2"></span>
            {{ $t('consultancyContract.send') }}
          </button>
          <button
            v-if="(agreement.status === 'draft' || agreement.status === 'sent') && hasPermission('consultancy_agreement:cancel')"
            class="btn btn-sm btn-light-danger"
            :disabled="store.saving"
            @click="doCancel"
          >
            {{ $t('common.cancel') }}
          </button>

          <!-- Sent: Sign -->
          <button
            v-if="agreement.status === 'sent' && hasPermission('consultancy_agreement:sign')"
            class="btn btn-sm btn-success"
            :disabled="store.saving"
            @click="showSignModal = true"
          >
            <i class="ki-outline ki-check fs-4 me-1"></i>{{ $t('consultancyContract.markSigned') }}
          </button>

          <!-- Signed: Expire -->
          <button
            v-if="agreement.status === 'signed' && hasPermission('consultancy_agreement:expire')"
            class="btn btn-sm btn-light-warning"
            :disabled="store.saving"
            @click="doExpire"
          >
            {{ $t('consultancyContract.markExpired') }}
          </button>

          <!-- Draft/Sent: Delete -->
          <button
            v-if="(agreement.status === 'draft' || agreement.status === 'sent') && hasPermission('consultancy_agreement:delete')"
            class="btn btn-sm btn-light-danger"
            :disabled="store.saving"
            @click="doDelete"
          >
            <i class="ki-outline ki-trash fs-4"></i>
          </button>
        </div>
      </div>

      <div class="row g-6">
        <!-- Main Info Card -->
        <div class="col-xl-8">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('consultancyContract.detail.info') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="row g-4">
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.fields.institution') }}</div>
                  <div class="fw-semibold">{{ agreement.institutionName }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.fields.plan') }}</div>
                  <div class="fw-semibold">{{ agreement.planName }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.fields.type') }}</div>
                  <div class="fw-semibold">{{ agreement.agreementTypeCode ?? '—' }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.fields.startDate') }}</div>
                  <div class="fw-semibold">{{ agreement.startDate ?? '—' }}</div>
                </div>
                <div class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.fields.endDate') }}</div>
                  <div class="fw-semibold">{{ agreement.endDate ?? '—' }}</div>
                </div>
                <div v-if="agreement.status === 'signed' || agreement.signedDate" class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.signatureDate') }}</div>
                  <div class="fw-semibold text-success">{{ agreement.signedDate ?? '—' }}</div>
                </div>
                <div v-if="agreement.signedByName" class="col-sm-6">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.signatory') }}</div>
                  <div class="fw-semibold">{{ agreement.signedByName }}</div>
                </div>
                <div v-if="agreement.description" class="col-12">
                  <div class="text-muted fs-7 mb-1">{{ $t('consultancyContract.fields.description') }}</div>
                  <div class="text-gray-700">{{ agreement.description }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Document Section -->
          <div v-if="agreement.fileId" class="card">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('consultancyContract.detail.document') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="d-flex align-items-center p-4 rounded bg-light">
                <i class="ki-outline ki-document fs-2 text-primary me-4"></i>
                <div class="flex-grow-1">
                  <div class="fw-semibold">{{ $t('consultancyContract.detail.contractDocument') }}</div>
                  <div v-if="agreement.status === 'signed'" class="text-muted fs-7">
                    {{ $t('consultancyContract.detail.signedDocument') }}
                  </div>
                </div>
                <a
                  v-if="hasPermission('consultancy_agreement:read')"
                  :href="`/api/files/${agreement.fileId}`"
                  target="_blank"
                  class="btn btn-sm btn-light-primary"
                >
                  <i class="ki-outline ki-download fs-4 me-1"></i>{{ $t('common.view') }}
                </a>
              </div>
            </div>
          </div>
        </div>

        <!-- Sidebar: Metadata -->
        <div class="col-xl-4">
          <div class="card">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('consultancyContract.detail.metadata') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div class="mb-3">
                <span class="text-muted fs-7">{{ $t('common.createdAt') }}:</span>
                <span class="fw-semibold ms-2">{{ formatDate(agreement.createdAt) }}</span>
              </div>
              <div class="mb-3">
                <span class="text-muted fs-7">{{ $t('common.updatedAt') }}:</span>
                <span class="fw-semibold ms-2">{{ formatDate(agreement.updatedAt) }}</span>
              </div>
              <div v-if="agreement.createdBy" class="mb-3">
                <span class="text-muted fs-7">{{ $t('consultancyContract.detail.createdBy') }}:</span>
                <span class="fw-semibold ms-2 text-muted fs-7">{{ agreement.createdBy }}</span>
              </div>
              <div class="mb-3">
                <span class="text-muted fs-7">{{ $t('consultancyContract.detail.hasFile') }}:</span>
                <i v-if="agreement.fileId" class="ki-outline ki-check-circle fs-3 text-success ms-2"></i>
                <i v-else class="ki-outline ki-cross-circle fs-3 text-muted ms-2"></i>
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
            <h5 class="modal-title">{{ $t('consultancyContract.markSigned') }}</h5>
            <button class="btn-close" @click="showSignModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="alert alert-warning fs-7 mb-4">
              <i class="ki-outline ki-information-5 fs-3 me-2"></i>
              {{ $t('consultancyContract.signConfirmWarning') }}
            </div>
            <div class="mb-4">
              <label class="form-label required">{{ $t('consultancyContract.signatory') }}</label>
              <input v-model="signForm.signedByName" type="text" class="form-control" :placeholder="$t('consultancyContract.signatoryPlaceholder')" />
            </div>
            <div class="mb-4">
              <label class="form-label required">{{ $t('consultancyContract.signatureDate') }}</label>
              <input v-model="signForm.signedDate" type="date" class="form-control" />
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showSignModal = false">{{ $t('common.cancel') }}</button>
            <button
              class="btn btn-success"
              :disabled="store.saving || !signForm.signedByName || !signForm.signedDate"
              @click="doSign"
            >
              <span v-if="store.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('consultancyContract.markSigned') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
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
const agreement = computed(() => store.currentAgreement)
const showSignModal = ref(false)
const signForm = reactive({ signedByName: '', signedDate: '' })

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

function statusBadge(s: string) {
  const map: Record<string, string> = {
    draft: 'badge badge-light-secondary',
    sent: 'badge badge-light-primary',
    signed: 'badge badge-light-success',
    expired: 'badge badge-light-dark',
    cancelled: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    draft: t('consultancyContract.draft'),
    sent: t('consultancyContract.sent'),
    signed: t('consultancyContract.signed'),
    expired: t('consultancyContract.expired'),
    cancelled: t('consultancyContract.cancelled'),
  }
  return map[s] ?? s
}

async function doSend() {
  await store.sendAgreement(id)
}

async function doSign() {
  if (!agreement.value) return
  await store.signAgreement(id, {
    signedByName: signForm.signedByName,
    signedDate: signForm.signedDate,
    rowVersion: agreement.value.rowVersion,
  })
  showSignModal.value = false
  Object.assign(signForm, { signedByName: '', signedDate: '' })
}

async function doExpire() {
  if (!confirm(t('consultancyContract.expireConfirm'))) return
  await store.expireAgreement(id)
}

async function doCancel() {
  if (!confirm(t('consultancyContract.cancelConfirm'))) return
  await store.cancelAgreement(id)
}

async function doDelete() {
  if (!confirm(t('consultancyContract.deleteConfirm'))) return
  await store.deleteAgreement(id)
  router.push('/consultancy/agreements')
}

onMounted(() => {
  store.currentAgreement = null
  store.fetchAgreement(id)
})
</script>
