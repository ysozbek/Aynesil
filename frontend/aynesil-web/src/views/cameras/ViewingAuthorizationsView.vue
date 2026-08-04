<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { ViewingAuthorizationDto } from '@/types/camera.types'

const { t } = useI18n()
const cameraStore = useCameraStore()
const auth = useAuthStore()
const { can } = usePermission()

const showCreateModal = ref(false)
const showRevokeConfirm = ref(false)
const revokeTargetId = ref<string | null>(null)

const filters = reactive({
  isRevoked: undefined as boolean | undefined,
  isCurrentlyValid: undefined as boolean | undefined,
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

function authStatusClass(a: ViewingAuthorizationDto) {
  if (a.isRevoked) return 'bg-red-100 text-red-700'
  if (a.isCurrentlyValid) return 'bg-green-100 text-green-700'
  return 'bg-amber-100 text-amber-700'
}

function authStatusLabel(a: ViewingAuthorizationDto) {
  if (a.isRevoked) return t('camera.auth.revoked')
  if (a.isCurrentlyValid) return t('camera.auth.active')
  return t('camera.auth.expired')
}

async function doFetch() {
  await cameraStore.fetchAuthorizations({
    corporationId: auth.user?.corporationId,
    isRevoked: filters.isRevoked,
    isCurrentlyValid: filters.isCurrentlyValid,
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
        v-if="can('viewing_authorization:grant')"
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
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select
          v-model="filters.isRevoked"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option :value="undefined">{{ t('common.allStatuses') }}</option>
          <option :value="false">{{ t('camera.auth.active') }}</option>
          <option :value="true">{{ t('camera.auth.revoked') }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('camera.auth.currentlyValid') }}</label>
        <select
          v-model="filters.isCurrentlyValid"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option :value="undefined">{{ t('common.allStatuses') }}</option>
          <option :value="true">{{ t('common.active') }}</option>
          <option :value="false">{{ t('common.passive') }}</option>
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
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', authStatusClass(row)]">
          {{ authStatusLabel(row) }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            v-if="!row.isRevoked && row.isCurrentlyValid && can('viewing_authorization:revoke')"
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
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.auth.guardianId') }} *</label>
          <input
            v-model="createForm.guardianId"
            type="text"
            required
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.auth.studentId') }} *</label>
          <input
            v-model="createForm.studentId"
            type="text"
            required
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
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
