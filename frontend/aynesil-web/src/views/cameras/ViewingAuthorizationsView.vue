<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { usePermission } from '@/composables/usePermission'
import { ViewingAuthorizationState } from '@/constants/statuses'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import GuardianPicker from '@/components/pickers/GuardianPicker.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ViewingAuthorizationDto } from '@/types/camera.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const cameraStore = useCameraStore()
const { can } = usePermission()

const showCreateModal = ref(false)
const showRevokeConfirm = ref(false)
const revokeTargetId = ref<string | null>(null)

// Backend yalnızca `activeOnly` (iptal edilmemiş ve geçerlilik penceresi içinde) süzer.
const filters = reactive({
  activeOnly: undefined as boolean | undefined,
})

const createForm = reactive({ guardianId: '', studentId: '', validFrom: '', validTo: '' })

const columns: Column<ViewingAuthorizationDto>[] = [
  { key: 'guardianFullName', label: t('camera.auth.guardian') },
  { key: 'studentFullName', label: t('camera.auth.student') },
  { key: 'validFrom', label: t('camera.auth.validFrom'), width: '150px' },
  { key: 'validTo', label: t('camera.auth.validTo'), width: '150px' },
  { key: 'accessTypeCode', label: t('camera.auth.accessType'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '120px' },
]

function formatDate(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

function authState(a: ViewingAuthorizationDto): ViewingAuthorizationState {
  if (a.isRevoked) return ViewingAuthorizationState.Revoked
  if (a.isCurrentlyValid) return ViewingAuthorizationState.Active
  return ViewingAuthorizationState.Expired
}

async function doFetch() {
  await cameraStore.fetchAuthorizations({
    activeOnly: filters.activeOnly,
  })
}

function promptRevoke(id: string) {
  revokeTargetId.value = id
  showRevokeConfirm.value = true
}

async function doRevoke() {
  if (!revokeTargetId.value) return
  await cameraStore.revokeAuthorization(revokeTargetId.value)
  showRevokeConfirm.value = false
  revokeTargetId.value = null
  await doFetch()
}

async function doCreate() {
  if (!createForm.guardianId || !createForm.studentId || !createForm.validFrom || !createForm.validTo) return
  await cameraStore.createAuthorization({
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

<template>
  <div>
    <PageHeader :title="t('camera.auth.title')" :description="t('camera.auth.subtitle')">
      <button
        v-if="can(Permissions.ViewingAuthorization.Grant)"
        @click="showCreateModal = true"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('camera.auth.new') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('camera.auth.currentlyValid') }}</label>
        <select
          v-model="filters.activeOnly"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option :value="undefined">{{ t('common.allStatuses') }}</option>
          <option :value="true">{{ t('common.active') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="cameraStore.authorizations.items"
      :loading="cameraStore.loading"
      :empty-text="t('camera.auth.noData')"
    >
      <template #cell-guardianFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-studentFullName="{ value }">
        <span class="text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-validFrom="{ row }">
        <span class="text-muted-foreground text-xs">{{ formatDate(row.validFrom) }}</span>
      </template>
      <template #cell-validTo="{ row }">
        <span class="text-muted-foreground text-xs">{{ formatDate(row.validTo) }}</span>
      </template>
      <template #cell-accessTypeCode="{ value }">
        <span class="text-muted-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-status="{ row }">
        <StatusBadge entity="viewingAuthorization" :status="authState(row)" />
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="!row.isRevoked && row.isCurrentlyValid && can(Permissions.ViewingAuthorization.Revoke)"
            @click="promptRevoke(row.id)"
            class="px-2.5 py-1 text-xs rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          >
            {{ t('camera.auth.revoke') }}
          </button>
        </div>
      </template>
    </DataTable>

    <FormModal
      :open="showCreateModal"
      :title="t('camera.auth.new')"
      :saving="cameraStore.saving"
      @close="showCreateModal = false"
      @submit="doCreate"
    >
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.auth.guardian') }} *</label>
          <GuardianPicker v-model="createForm.guardianId" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.auth.student') }} *</label>
          <StudentPicker v-model="createForm.studentId" />
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.auth.validFrom') }} *</label>
            <input
              v-model="createForm.validFrom"
              type="datetime-local"
              required
              class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.auth.validTo') }} *</label>
            <input
              v-model="createForm.validTo"
              type="datetime-local"
              required
              class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="showRevokeConfirm"
      :title="t('camera.auth.revoke')"
      message="Bu yetki iptal edilecek. Onaylıyor musunuz?"
      :confirm-label="t('camera.auth.revoke')"
      :loading="cameraStore.saving"
      @confirm="doRevoke"
      @cancel="showRevokeConfirm = false"
    />
  </div>
</template>
