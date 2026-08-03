<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useGuardianStore } from '@/stores/guardian.store'
import { usePermission } from '@/composables/usePermission'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { StudentGuardianDto, GuardianPortalAccessDto, GrantPortalAccessPayload } from '@/types/student.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useGuardianStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const guardian = computed(() => store.current)
const activeTab = ref<'overview' | 'students' | 'portal-access'>('overview')

onMounted(async () => {
  await store.fetchOne(id.value)
})

onUnmounted(() => {
  store.clearCurrent()
})

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

// ── Portal Access Modal ────────────────────────────────────────────────────
const showPortalModal = ref(false)
const portalTarget = ref<StudentGuardianDto | null>(null)
const portalForm = reactive<GrantPortalAccessPayload>({
  canViewSessions: false,
  canViewAttendance: false,
  canViewReports: false,
  canViewPlan: false,
  canViewFinance: false,
  canViewCamera: false,
})
const portalError = ref('')
const portalMode = ref<'grant' | 'update'>('grant')

function openPortalModal(link: StudentGuardianDto, mode: 'grant' | 'update', existing?: GuardianPortalAccessDto) {
  portalTarget.value = link
  portalMode.value = mode
  portalError.value = ''
  if (existing) {
    portalForm.canViewSessions = existing.canViewSessions
    portalForm.canViewAttendance = existing.canViewAttendance
    portalForm.canViewReports = existing.canViewReports
    portalForm.canViewPlan = existing.canViewPlan
    portalForm.canViewFinance = existing.canViewFinance
    portalForm.canViewCamera = existing.canViewCamera
  } else {
    portalForm.canViewSessions = false
    portalForm.canViewAttendance = false
    portalForm.canViewReports = false
    portalForm.canViewPlan = false
    portalForm.canViewFinance = false
    portalForm.canViewCamera = false
  }
  showPortalModal.value = true
}

async function submitPortalAccess() {
  if (!portalTarget.value) return
  portalError.value = ''
  try {
    if (portalMode.value === 'grant') {
      await store.grantPortalAccess(id.value, portalTarget.value.linkId, { ...portalForm })
    } else {
      await store.updatePortalPermissions(id.value, portalTarget.value.linkId, { ...portalForm })
    }
    showPortalModal.value = false
    await store.fetchOne(id.value)
  } catch (e: unknown) {
    portalError.value = (e as Error).message
  }
}

// ── Revoke Portal ─────────────────────────────────────────────────────────
const revokeTarget = ref<StudentGuardianDto | null>(null)
const revokeLoading = ref(false)

async function doRevoke() {
  if (!revokeTarget.value) return
  revokeLoading.value = true
  try {
    await store.revokePortalAccess(id.value, revokeTarget.value.linkId)
    revokeTarget.value = null
    await store.fetchOne(id.value)
  } finally {
    revokeLoading.value = false
  }
}

// ── Delete Guardian ───────────────────────────────────────────────────────
const showDelete = ref(false)
const deleteLoading = ref(false)

async function doDelete() {
  deleteLoading.value = true
  try {
    await store.remove(id.value)
    router.push({ name: 'guardians' })
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <div>
    <!-- Loading skeleton -->
    <div v-if="store.loading && !guardian" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Error / Not found -->
    <div v-else-if="store.error && !guardian" class="text-center py-24">
      <p class="text-muted-foreground">{{ store.error }}</p>
      <button @click="router.push({ name: 'guardians' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('guardian.backToList') }}
      </button>
    </div>
    <div v-else-if="!guardian && !store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'guardians' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('guardian.backToList') }}
      </button>
    </div>

    <template v-else-if="guardian">
      <!-- Header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'guardians' })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('guardian.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ guardian.fullName }}</h1>
          <div class="flex items-center gap-2 mt-1 flex-wrap">
            <span
              :class="['px-2 py-0.5 rounded-full text-xs font-medium', guardian.hasPortalAccount ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']"
            >
              {{ t('guardian.hasPortalAccount') }}: {{ guardian.hasPortalAccount ? t('common.yes') : t('common.no') }}
            </span>
          </div>
        </div>
        <div class="flex items-center gap-2 flex-wrap">
          <button
            v-if="can('guardian:update')"
            @click="router.push({ name: 'guardian-edit', params: { id: guardian.id } })"
            class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="can('guardian:delete')"
            @click="showDelete = true"
            class="px-3 py-2 text-sm rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors"
          >
            {{ t('common.delete') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-4 border-b border-border">
        <nav class="-mb-px flex gap-6">
          <button
            v-for="tab in ['overview', 'students', 'portal-access']"
            :key="tab"
            @click="activeTab = tab as typeof activeTab"
            :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground']"
          >
            {{ t(`guardian.tab.${tab === 'portal-access' ? 'portalAccess' : tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] p-5 space-y-3 shadow-sm">
          <h3 class="font-semibold text-foreground">{{ t('guardian.title') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.fullName') }}</dt>
              <dd class="font-medium text-foreground">{{ guardian.fullName }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.nationalId') }}</dt>
              <dd class="font-medium text-foreground">{{ guardian.nationalId ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.email') }}</dt>
              <dd class="font-medium text-foreground">{{ guardian.email ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.phone') }}</dt>
              <dd class="font-medium text-foreground">{{ guardian.phone ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.occupation') }}</dt>
              <dd class="font-medium text-foreground">{{ guardian.occupation ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.addressLine') }}</dt>
              <dd class="font-medium text-foreground text-right max-w-[200px]">{{ guardian.addressLine ?? '—' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('guardian.hasPortalAccount') }}</dt>
              <dd>
                <span
                  :class="['px-2 py-0.5 rounded-full text-xs font-medium', guardian.hasPortalAccount ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']"
                >
                  {{ guardian.hasPortalAccount ? t('common.yes') : t('common.no') }}
                </span>
              </dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-muted-foreground">{{ t('common.createdAt') }}</dt>
              <dd class="font-medium text-foreground">{{ formatDate(guardian.createdAt) }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Students Tab -->
      <div v-else-if="activeTab === 'students'">
        <div v-if="guardian.students.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="link in guardian.students"
            :key="link.linkId"
            class="rounded-xl border border-border bg-[--color-card] p-4 shadow-sm"
          >
            <div class="flex items-start justify-between">
              <div class="space-y-1">
                <p class="text-sm font-semibold text-foreground">
                  {{ link.guardianFullName }}
                </p>
                <p class="text-xs text-muted-foreground">
                  {{ link.relationshipLabel ?? '—' }}
                </p>
              </div>
              <div class="flex items-center gap-2 flex-wrap justify-end">
                <span
                  v-if="link.isPrimary"
                  class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700"
                >
                  {{ t('common.primary') }}
                </span>
                <span
                  v-if="link.hasCustody"
                  class="px-2 py-0.5 rounded-full text-xs font-medium bg-violet-100 text-violet-700"
                >
                  {{ t('common.custody') }}
                </span>
                <span
                  :class="['px-2 py-0.5 rounded-full text-xs font-medium', link.portalAccess ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']"
                >
                  Portal: {{ link.portalAccess ? t('common.active') : t('common.inactive') }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Portal Access Tab -->
      <div v-else-if="activeTab === 'portal-access'">
        <div v-if="guardian.students.length === 0" class="text-center py-12 text-muted-foreground">
          {{ t('common.noData') }}
        </div>
        <div v-else class="space-y-4">
          <div
            v-for="link in guardian.students"
            :key="link.linkId"
            class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm"
          >
            <div class="flex items-center justify-between mb-4">
              <div>
                <p class="text-sm font-semibold text-foreground">{{ link.guardianFullName }}</p>
                <p class="text-xs text-muted-foreground">{{ link.relationshipLabel ?? '—' }}</p>
              </div>
              <div v-if="can('guardian:manage_portal')" class="flex gap-2">
                <button
                  v-if="!link.portalAccess"
                  @click="openPortalModal(link, 'grant')"
                  class="px-3 py-1.5 text-xs rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
                >
                  {{ t('guardian.portalAccess.grant') }}
                </button>
                <template v-else>
                  <button
                    @click="openPortalModal(link, 'update')"
                    class="px-3 py-1.5 text-xs rounded-lg border border-border hover:bg-accent transition-colors"
                  >
                    {{ t('common.edit') }}
                  </button>
                  <button
                    @click="revokeTarget = link"
                    class="px-3 py-1.5 text-xs rounded-lg hover:bg-red-50 text-red-600 border border-red-200 transition-colors"
                  >
                    {{ t('guardian.portalAccess.revoke') }}
                  </button>
                </template>
              </div>
            </div>

            <div v-if="link.portalAccess" class="grid grid-cols-2 sm:grid-cols-3 gap-3">
              <div
                v-for="perm in ['canViewSessions', 'canViewAttendance', 'canViewReports', 'canViewPlan', 'canViewFinance', 'canViewCamera'] as const"
                :key="perm"
                class="flex items-center gap-2 text-sm"
              >
                <span class="w-4 h-4 flex-shrink-0 text-emerald-600">
                  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </span>
                <span class="text-foreground">{{ t(`guardian.portalAccess.${perm}`) }}</span>
              </div>
            </div>
            <p v-else class="text-sm text-muted-foreground">{{ t('common.noAccess') }}</p>
          </div>
        </div>
      </div>
    </template>

    <!-- Portal Access Modal -->
    <FormModal
      :open="showPortalModal"
      :title="portalMode === 'grant' ? t('guardian.portalAccess.grant') : t('guardian.portalAccess.grant')"
      :saving="store.saving"
      @submit="submitPortalAccess"
      @close="showPortalModal = false"
    >
      <div class="space-y-3">
        <p v-if="portalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ portalError }}</p>
        <p class="text-sm text-muted-foreground">{{ portalTarget?.guardianFullName }}</p>
        <div class="grid grid-cols-2 gap-3">
          <label v-for="perm in ['canViewSessions', 'canViewAttendance', 'canViewReports', 'canViewPlan', 'canViewFinance', 'canViewCamera'] as const" :key="perm"
            class="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" v-model="portalForm[perm]" class="rounded border-border" />
            {{ t(`guardian.portalAccess.${perm}`) }}
          </label>
        </div>
      </div>
    </FormModal>

    <!-- Revoke Confirm -->
    <ConfirmModal
      :open="!!revokeTarget"
      :title="t('guardian.portalAccess.revoke')"
      :message="t('common.confirmAction')"
      :confirm-label="t('guardian.portalAccess.revoke')"
      :loading="revokeLoading"
      @confirm="doRevoke"
      @cancel="revokeTarget = null"
    />

    <!-- Delete Confirm -->
    <ConfirmModal
      :open="showDelete"
      :title="t('common.deleteConfirmTitle')"
      :message="t('common.deleteConfirmMessage', { name: guardian?.fullName })"
      :confirm-label="t('common.delete')"
      :loading="deleteLoading"
      @confirm="doDelete"
      @cancel="showDelete = false"
    />
  </div>
</template>
