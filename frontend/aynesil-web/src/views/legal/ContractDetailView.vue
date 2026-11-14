<script setup lang="ts">
import { ContractStatus } from '@/constants/statuses'
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { SignContractPayload } from '@/types/legal.types'
import type { StudentGuardianDto } from '@/types/student.types'
import { studentService } from '@/services/student.service'
import { useContractStore } from '@/stores/contract.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const contractStore = useContractStore()
const { can } = usePermission()

const id = route.params.id as string
const contract = computed(() => contractStore.currentContract)
const showSignModal = ref(false)
const showTerminateConfirm = ref(false)
const showActivateConfirm = ref(false)
const showExpireConfirm = ref(false)
const actionError = ref('')
const showEdit = ref(false)
const showDeleteConfirm = ref(false)
const editError = ref('')
const editGuardians = ref<StudentGuardianDto[]>([])
const editForm = reactive({ guardianId: '', startsOn: '', endsOn: '' })
const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
const signForm = reactive<{ method: SignContractPayload['signatureMethod']; signedByName: string }>({ method: 'wet', signedByName: '' })

function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('tr-TR')
}

function formatDatetime(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

/** Durum geçişi hatasını (ör. geçersiz geçiş) sayfada göster. */
async function run(action: () => Promise<void>) {
  actionError.value = ''
  try {
    await action()
  } catch (e: unknown) {
    actionError.value = (e as Error).message
  }
}

async function doSend() {
  await run(() => contractStore.sendContract(id))
}

async function doActivate() {
  await run(() => contractStore.activateContract(id))
  showActivateConfirm.value = false
}

async function doExpire() {
  await run(() => contractStore.expireContract(id))
  showExpireConfirm.value = false
}

async function doSign() {
  if (!contract.value || !signForm.signedByName.trim()) return
  const current = contract.value
  await run(() => contractStore.signContract(id, {
    signedByName: signForm.signedByName.trim(),
    signatureMethod: signForm.method,
    rowVersion: current.rowVersion,
  }))
  showSignModal.value = false
}

// Yalnızca taslak sözleşme düzenlenir/silinir (sunucu da gönderilmiş/imzalı sözleşmeyi reddeder).
async function openEdit() {
  if (!contract.value) return
  Object.assign(editForm, {
    guardianId: contract.value.guardianId ?? '',
    startsOn: contract.value.startsOn ?? '',
    endsOn: contract.value.endsOn ?? '',
  })
  editError.value = ''
  editGuardians.value = []
  showEdit.value = true
  try {
    const res = await studentService.getGuardians(contract.value.studentId)
    if (res.success && res.data) editGuardians.value = res.data
  } catch { /* veli listesi yüklenemezse mevcut veli korunur */ }
}

async function submitEdit() {
  if (!contract.value) return
  editError.value = ''
  if (editForm.startsOn && editForm.endsOn && editForm.endsOn < editForm.startsOn) {
    editError.value = t('legal.contract.form.dateOrder'); return
  }
  try {
    await contractStore.updateContract(id, {
      guardianId: editForm.guardianId || null,
      startsOn: editForm.startsOn || null,
      endsOn: editForm.endsOn || null,
      rowVersion: contract.value.rowVersion,
    })
    showEdit.value = false
  } catch (e: unknown) {
    editError.value = (e as Error).message
  }
}

async function doDelete() {
  actionError.value = ''
  try {
    await contractStore.deleteContract(id)
    showDeleteConfirm.value = false
    router.replace({ name: 'contracts' })
  } catch (e: unknown) {
    showDeleteConfirm.value = false
    actionError.value = (e as Error).message
  }
}

async function doTerminate() {
  await run(() => contractStore.terminateContract(id))
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
          <StatusBadge entity="contract" :status="contract.status" />
          <button
            v-if="contract.status === ContractStatus.Draft && can(Permissions.StudentContracts.Update)"
            :disabled="contractStore.saving"
            @click="openEdit"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent disabled:opacity-50"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="contract.status === ContractStatus.Draft && can(Permissions.StudentContracts.Delete)"
            :disabled="contractStore.saving"
            @click="showDeleteConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50 disabled:opacity-50"
          >
            {{ t('common.delete') }}
          </button>
          <button
            v-if="contract.status === ContractStatus.Draft && can(Permissions.StudentContracts.Send)"
            :disabled="contractStore.saving"
            @click="doSend"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.send') }}
          </button>
          <button
            v-if="contract.status === ContractStatus.Sent && can(Permissions.StudentContracts.Sign)"
            :disabled="contractStore.saving"
            @click="showSignModal = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.sign') }}
          </button>
          <button
            v-if="contract.status === ContractStatus.Signed && can(Permissions.StudentContracts.Activate)"
            :disabled="contractStore.saving"
            @click="showActivateConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.activate') }}
          </button>
          <button
            v-if="contract.status === ContractStatus.Active && can(Permissions.StudentContracts.Expire)"
            :disabled="contractStore.saving"
            @click="showExpireConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent disabled:opacity-50"
          >
            {{ t('legal.contract.actions.expire') }}
          </button>
          <button
            v-if="contract.status === ContractStatus.Active && can(Permissions.StudentContracts.Terminate)"
            :disabled="contractStore.saving"
            @click="showTerminateConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50 disabled:opacity-50"
          >
            {{ t('legal.contract.actions.terminate') }}
          </button>
        </div>
      </PageHeader>

      <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ actionError }}</p>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 rounded-xl border border-border bg-card shadow-sm p-5">
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

        <div class="rounded-xl border border-border bg-card shadow-sm p-5 h-fit">
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
      :open="showEdit"
      :title="t('legal.contract.detail.editTitle')"
      :saving="contractStore.saving"
      @submit="submitEdit"
      @close="showEdit = false"
    >
      <p v-if="editError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ editError }}</p>
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.form.guardian') }}</label>
          <select v-model="editForm.guardianId" :class="inputClass">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="g in editGuardians" :key="g.guardianId" :value="g.guardianId">{{ g.guardianFullName }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.startsOn') }}</label>
            <input v-model="editForm.startsOn" type="date" :class="inputClass" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.endsOn') }}</label>
            <input v-model="editForm.endsOn" type="date" :class="inputClass" />
          </div>
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="showDeleteConfirm"
      :title="t('legal.contract.detail.deleteTitle')"
      :message="t('legal.contract.detail.deleteConfirm')"
      :confirm-label="t('common.delete')"
      :loading="contractStore.saving"
      @confirm="doDelete"
      @cancel="showDeleteConfirm = false"
    />

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
          <option value="wet">{{ t('legal.signature.manual') }}</option>
          <option value="e_sign">{{ t('legal.signature.electronic') }}</option>
          <option value="click_wrap">{{ t('legal.signature.clickWrap') }}</option>
        </select>
      </div>
      <div class="mt-4">
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.contract.fields.signedByName') }} <span class="text-red-500">*</span></label>
        <input v-model="signForm.signedByName" type="text" maxlength="200" required class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent" />
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
      :open="showActivateConfirm"
      :title="t('legal.contract.actions.activate')"
      :message="t('legal.contract.detail.activateConfirm')"
      :confirm-label="t('legal.contract.actions.activate')"
      confirm-class="bg-primary text-primary-foreground hover:opacity-90"
      :loading="contractStore.saving"
      @confirm="doActivate"
      @cancel="showActivateConfirm = false"
    />

    <ConfirmModal
      :open="showExpireConfirm"
      :title="t('legal.contract.actions.expire')"
      :message="t('legal.contract.detail.expireConfirm')"
      :confirm-label="t('legal.contract.actions.expire')"
      :loading="contractStore.saving"
      @confirm="doExpire"
      @cancel="showExpireConfirm = false"
    />

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
