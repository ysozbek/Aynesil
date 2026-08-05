<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useContractStore } from '@/stores/contract.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const contractStore = useContractStore()
const { can } = usePermission()

const id = route.params.id as string
const contract = computed(() => contractStore.currentContract)
const showSignModal = ref(false)
const showTerminateConfirm = ref(false)
const signForm = reactive({ method: 'Manual' })

function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('tr-TR')
}

function formatDatetime(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

function statusClass(s: string) {
  const map: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-600',
    Sent: 'bg-amber-100 text-amber-700',
    Active: 'bg-green-100 text-green-700',
    Expired: 'bg-gray-100 text-gray-700',
    Terminated: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function statusLabel(s: string) {
  return t(`legal.contract.status.${s.toLowerCase()}`, s)
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
  await contractStore.terminateContract(id)
  showTerminateConfirm.value = false
}

onMounted(() => {
  contractStore.clearCurrent()
  contractStore.fetchContract(id)
})
</script>

<template>
  <div>
    <div v-if="contractStore.loading" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!contract" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
    <template v-else>
      <PageHeader :title="t('legal.contract.detail.title')" :description="contract.studentFullName">
        <div class="flex flex-wrap items-center gap-2">
          <span :class="['px-2.5 py-1 rounded-full text-xs font-medium', statusClass(contract.status)]">
            {{ statusLabel(contract.status) }}
          </span>
          <button
            v-if="contract.status === 'Draft' && can('student_contract:send')"
            :disabled="contractStore.saving"
            @click="doSend"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.send') }}
          </button>
          <button
            v-if="contract.status === 'Sent' && can('student_contract:sign')"
            :disabled="contractStore.saving"
            @click="showSignModal = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.sign') }}
          </button>
          <button
            v-if="contract.status === 'Active' && can('student_contract:terminate')"
            :disabled="contractStore.saving"
            @click="showTerminateConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.terminate') }}
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
          <h3 class="font-semibold text-foreground mb-4">{{ t('legal.contract.detail.info') }}</h3>
          <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
            <div>
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.student') }}</dt>
              <dd class="font-medium text-foreground">{{ contract.studentFullName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.template') }}</dt>
              <dd class="font-medium text-foreground">
                {{ contract.templateCode ? `${contract.templateCode} v${contract.templateVersion}` : '—' }}
              </dd>
            </div>
            <div>
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.startsOn') }}</dt>
              <dd class="font-medium text-foreground">{{ contract.startsOn ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.endsOn') }}</dt>
              <dd class="font-medium text-foreground">{{ contract.endsOn ?? '—' }}</dd>
            </div>
            <div v-if="contract.signedAt">
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.signedAt') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDatetime(contract.signedAt) }}</dd>
            </div>
            <div v-if="contract.signedByName">
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.signedBy') }}</dt>
              <dd class="font-medium text-foreground">{{ contract.signedByName }}</dd>
            </div>
            <div v-if="contract.signatureMethod">
              <dt class="text-muted-foreground mb-0.5">{{ t('legal.contract.fields.signatureMethod') }}</dt>
              <dd class="font-medium text-foreground">{{ contract.signatureMethod }}</dd>
            </div>
          </dl>
        </div>

        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5 h-fit">
          <h3 class="font-semibold text-foreground mb-4">{{ t('legal.contract.detail.metadata') }}</h3>
          <dl class="space-y-3 text-sm">
            <div>
              <dt class="text-muted-foreground">{{ t('common.createdAt') }}</dt>
              <dd class="font-medium text-foreground mt-0.5">{{ formatDate(contract.createdAt) }}</dd>
            </div>
            <div>
              <dt class="text-muted-foreground">{{ t('common.updatedAt') }}</dt>
              <dd class="font-medium text-foreground mt-0.5">{{ formatDate(contract.updatedAt) }}</dd>
            </div>
            <div v-if="contract.signedFileId">
              <dt class="text-muted-foreground">{{ t('legal.contract.fields.signedFile') }}</dt>
              <dd class="mt-1">
                <span class="inline-flex px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                  {{ t('legal.contract.fields.fileAttached') }}
                </span>
              </dd>
            </div>
          </dl>
          <button
            @click="router.push({ name: 'contracts' })"
            class="mt-4 w-full px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.back') }}
          </button>
        </div>
      </div>
    </template>

    <FormModal
      :open="showSignModal"
      :title="t('legal.contract.actions.sign')"
      :saving="contractStore.saving"
      @submit="doSign"
      @close="showSignModal = false"
    >
      <div class="mb-4 p-3 rounded-lg bg-amber-50 border border-amber-200 text-amber-800 text-sm">
        {{ t('legal.contract.detail.signWarning') }}
      </div>
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.signatureMethod') }}</label>
        <select v-model="signForm.method" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
          <option value="Manual">{{ t('legal.signature.manual') }}</option>
          <option value="Electronic">{{ t('legal.signature.electronic') }}</option>
        </select>
      </div>
      <template #footer>
        <button
          @click="showSignModal = false"
          :disabled="contractStore.saving"
          class="px-4 py-2 rounded-lg text-sm font-medium border border-border hover:bg-accent transition-colors disabled:opacity-50"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          @click="doSign"
          :disabled="contractStore.saving"
          class="px-4 py-2 rounded-lg text-sm font-medium bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
        >
          <span v-if="contractStore.saving" class="flex items-center gap-2">
            <svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            {{ t('common.loading') }}
          </span>
          <span v-else>{{ t('legal.contract.actions.sign') }}</span>
        </button>
      </template>
    </FormModal>

    <ConfirmModal
      :open="showTerminateConfirm"
      :title="t('legal.contract.actions.terminate')"
      :message="t('legal.contract.detail.terminateConfirm')"
      :confirm-label="t('legal.contract.actions.terminate')"
      :loading="contractStore.saving"
      @confirm="doTerminate"
      @cancel="showTerminateConfirm = false"
    />
  </div>
</template>
