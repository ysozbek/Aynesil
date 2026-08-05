<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const { can } = usePermission()
const id = route.params.id as string
const agreement = computed(() => store.currentAgreement)
const showSignModal = ref(false)
const showExpireConfirm = ref(false)
const showCancelConfirm = ref(false)
const showDeleteConfirm = ref(false)
const signForm = reactive({ signedByName: '', signedDate: '' })

function formatDate(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

function statusClass(s: string) {
  const map: Record<string, string> = {
    draft: 'bg-gray-100 text-gray-600',
    sent: 'bg-blue-100 text-blue-700',
    signed: 'bg-green-100 text-green-700',
    expired: 'bg-gray-100 text-gray-800',
    cancelled: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
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
  if (!agreement.value || !signForm.signedByName || !signForm.signedDate) return
  await store.signAgreement(id, {
    signedByName: signForm.signedByName,
    signedDate: signForm.signedDate,
    rowVersion: agreement.value.rowVersion,
  })
  showSignModal.value = false
  Object.assign(signForm, { signedByName: '', signedDate: '' })
}

async function doExpire() {
  await store.expireAgreement(id)
  showExpireConfirm.value = false
}

async function doCancel() {
  await store.cancelAgreement(id)
  showCancelConfirm.value = false
}

async function doDelete() {
  await store.deleteAgreement(id)
  showDeleteConfirm.value = false
  router.push('/consultancy/agreements')
}

onMounted(() => {
  store.currentAgreement = null
  store.fetchAgreement(id)
})
</script>

<template>
  <div>
    <div v-if="store.loading" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!agreement" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
    <template v-else>
      <div
        v-if="agreement.status === 'signed'"
        class="mb-6 flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50/50 p-4"
      >
        <svg class="w-5 h-5 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        <div>
          <p class="font-semibold text-amber-800">{{ t('consultancyContract.immutableBanner') }}</p>
          <p class="text-sm text-muted-foreground mt-0.5">{{ t('consultancyContract.immutableDesc') }}</p>
        </div>
      </div>

      <PageHeader :title="agreement.title" :description="`${agreement.institutionName} · ${agreement.planName}`">
        <div class="flex flex-wrap items-center gap-2">
          <span :class="['px-2.5 py-1 rounded-full text-xs font-medium', statusClass(agreement.status)]">
            {{ statusLabel(agreement.status) }}
          </span>
          <button
            v-if="agreement.status === 'draft' && can('consultancy_agreement:update')"
            @click="router.push(`/consultancy/agreements/${agreement.id}/edit`)"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="agreement.status === 'draft' && can('consultancy_agreement:send')"
            :disabled="store.saving"
            @click="doSend"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {{ t('consultancyContract.send') }}
          </button>
          <button
            v-if="(agreement.status === 'draft' || agreement.status === 'sent') && can('consultancy_agreement:cancel')"
            :disabled="store.saving"
            @click="showCancelConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            v-if="agreement.status === 'sent' && can('consultancy_agreement:sign')"
            @click="showSignModal = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700"
          >
            {{ t('consultancyContract.markSigned') }}
          </button>
          <button
            v-if="agreement.status === 'signed' && can('consultancy_agreement:expire')"
            :disabled="store.saving"
            @click="showExpireConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-amber-200 text-amber-700 hover:bg-amber-50"
          >
            {{ t('consultancyContract.markExpired') }}
          </button>
          <button
            v-if="(agreement.status === 'draft' || agreement.status === 'sent') && can('consultancy_agreement:delete')"
            :disabled="store.saving"
            @click="showDeleteConfirm = true"
            class="p-1.5 rounded-lg hover:bg-red-50 text-red-600"
            :title="t('common.delete')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 space-y-6">
          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('consultancyContract.detail.info') }}</h3>
            <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.fields.institution') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.institutionName }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.fields.plan') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.planName }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.fields.type') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.agreementTypeCode ?? '—' }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.fields.startDate') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.startDate ?? '—' }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.fields.endDate') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.endDate ?? '—' }}</dd>
              </div>
              <div v-if="agreement.status === 'signed' || agreement.signedDate">
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.signatureDate') }}</dt>
                <dd class="font-medium text-green-700">{{ agreement.signedDate ?? '—' }}</dd>
              </div>
              <div v-if="agreement.signedByName">
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.signatory') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.signedByName }}</dd>
              </div>
              <div v-if="agreement.description" class="sm:col-span-2">
                <dt class="text-muted-foreground mb-0.5">{{ t('consultancyContract.fields.description') }}</dt>
                <dd class="font-medium text-foreground">{{ agreement.description }}</dd>
              </div>
            </dl>
          </div>

          <div v-if="agreement.fileId" class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('consultancyContract.detail.document') }}</h3>
            <div class="flex items-center gap-4 rounded-lg border border-border bg-accent/30 p-4">
              <svg class="w-8 h-8 text-primary shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <div class="flex-1 min-w-0">
                <p class="font-medium text-foreground">{{ t('consultancyContract.detail.contractDocument') }}</p>
                <p v-if="agreement.status === 'signed'" class="text-xs text-muted-foreground">
                  {{ t('consultancyContract.detail.signedDocument') }}
                </p>
              </div>
              <a
                v-if="can('consultancy_agreement:read')"
                :href="`/api/files/${agreement.fileId}`"
                target="_blank"
                class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent shrink-0"
              >
                {{ t('common.view') }}
              </a>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5 h-fit">
          <h3 class="font-semibold text-foreground mb-4">{{ t('consultancyContract.detail.metadata') }}</h3>
          <dl class="space-y-3 text-sm">
            <div>
              <dt class="text-muted-foreground">{{ t('common.createdAt') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(agreement.createdAt) }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground">{{ t('common.updatedAt') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(agreement.updatedAt) }}</dd>
            </div>
            <div v-if="agreement.createdBy">
              <dt class="text-muted-foreground">{{ t('consultancyContract.detail.createdBy') }}</dt>
              <dd class="font-medium text-muted-foreground text-xs">{{ agreement.createdBy }}</dd>
            </div>
            <div class="flex items-center gap-2">
              <span class="text-muted-foreground">{{ t('consultancyContract.detail.hasFile') }}:</span>
              <svg v-if="agreement.fileId" class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <svg v-else class="w-5 h-5 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </dl>
          <button
            @click="router.push('/consultancy/agreements')"
            class="mt-4 w-full px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.back') }}
          </button>
        </div>
      </div>
    </template>

    <FormModal
      :open="showSignModal"
      :title="t('consultancyContract.markSigned')"
      :saving="store.saving"
      @submit="doSign"
      @close="showSignModal = false"
    >
      <div class="space-y-4">
        <p class="text-sm text-amber-700 bg-amber-50 rounded-lg px-3 py-2">
          {{ t('consultancyContract.signConfirmWarning') }}
        </p>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.signatory') }} *</label>
          <input
            v-model="signForm.signedByName"
            type="text"
            :placeholder="t('consultancyContract.signatoryPlaceholder')"
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.signatureDate') }} *</label>
          <input v-model="signForm.signedDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="showExpireConfirm"
      :title="t('consultancyContract.markExpired')"
      :message="t('consultancyContract.expireConfirm')"
      :confirm-label="t('consultancyContract.markExpired')"
      confirm-class="bg-amber-600 hover:bg-amber-700 text-white"
      :loading="store.saving"
      @confirm="doExpire"
      @cancel="showExpireConfirm = false"
    />

    <ConfirmModal
      :open="showCancelConfirm"
      :title="t('common.cancel')"
      :message="t('consultancyContract.cancelConfirm')"
      :confirm-label="t('common.cancel')"
      :loading="store.saving"
      @confirm="doCancel"
      @cancel="showCancelConfirm = false"
    />

    <ConfirmModal
      :open="showDeleteConfirm"
      :title="t('common.delete')"
      :message="t('consultancyContract.deleteConfirm')"
      :confirm-label="t('common.delete')"
      :loading="store.saving"
      @confirm="doDelete"
      @cancel="showDeleteConfirm = false"
    />
  </div>
</template>
