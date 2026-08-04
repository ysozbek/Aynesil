<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const leaveStore = useLeaveStore()
const { can } = usePermission()

const id = route.params.id as string
const leave = computed(() => leaveStore.currentLeave)
const showApproveModal = ref(false)
const showRejectModal = ref(false)
const showCancelConfirm = ref(false)
const actionComment = ref('')

function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('tr-TR')
}
function formatDatetime(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

function statusClass(status: string) {
  const map: Record<string, string> = {
    Pending: 'bg-amber-100 text-amber-700',
    Approved: 'bg-green-100 text-green-700',
    Rejected: 'bg-red-100 text-red-700',
    Cancelled: 'bg-gray-100 text-gray-600',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}

async function doApprove() {
  if (!leave.value) return
  await leaveStore.approveLeave(id, { comment: actionComment.value, rowVersion: leave.value.rowVersion })
  showApproveModal.value = false
  actionComment.value = ''
}

async function doReject() {
  if (!leave.value) return
  await leaveStore.rejectLeave(id, { comment: actionComment.value, rowVersion: leave.value.rowVersion })
  showRejectModal.value = false
  actionComment.value = ''
}

async function doCancel() {
  if (!leave.value) return
  await leaveStore.cancelLeave(id, { rowVersion: leave.value.rowVersion })
  showCancelConfirm.value = false
  router.push({ name: 'leave-list' })
}

onMounted(async () => {
  leaveStore.clearCurrent()
  await leaveStore.fetchLeave(id)
  if (leaveStore.currentLeave) {
    await leaveStore.fetchSessionImpact(id)
  }
})
</script>

<template>
  <div>
    <div v-if="leaveStore.loading" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!leave" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
    <template v-else>
      <PageHeader :title="t('leave.detail.title')" :description="leave.educatorFullName">
        <div class="flex flex-wrap items-center gap-2">
          <span :class="['px-2.5 py-1 rounded-full text-xs font-medium', statusClass(leave.status)]">
            {{ t(`leave.status.${leave.status.toLowerCase()}`) }}
          </span>
          <button
            v-if="leave.status === 'Pending' && can('leave_request:update')"
            @click="router.push({ name: 'leave-edit', params: { id: leave.id } })"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="leave.status === 'Pending' && can('leave_request:approve')"
            @click="showApproveModal = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700"
          >
            {{ t('leave.actions.approve') }}
          </button>
          <button
            v-if="leave.status === 'Pending' && can('leave_request:approve')"
            @click="showRejectModal = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-red-600 text-white hover:bg-red-700"
          >
            {{ t('leave.actions.reject') }}
          </button>
          <button
            v-if="(leave.status === 'Pending' || leave.status === 'Approved') && can('leave_request:cancel')"
            @click="showCancelConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          >
            {{ t('leave.actions.cancel') }}
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 space-y-6">
          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('leave.detail.requestInfo') }}</h3>
            <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('leave.fields.leaveType') }}</dt>
                <dd class="font-medium text-foreground">{{ leave.leaveTypeCode ?? '—' }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('leave.fields.unit') }}</dt>
                <dd class="font-medium text-foreground">
                  {{ leave.unit === 'Day' ? t('leave.unit.day') : t('leave.unit.hour') }}
                  <span v-if="leave.quantity" class="text-muted-foreground ml-1">({{ leave.quantity }})</span>
                </dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('leave.fields.startsAt') }}</dt>
                <dd class="font-medium text-foreground">{{ formatDatetime(leave.startsAt) }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('leave.fields.endsAt') }}</dt>
                <dd class="font-medium text-foreground">{{ formatDatetime(leave.endsAt) }}</dd>
              </div>
              <div class="sm:col-span-2">
                <dt class="text-muted-foreground mb-0.5">{{ t('leave.fields.reason') }}</dt>
                <dd class="font-medium text-foreground">{{ leave.reason ?? '—' }}</dd>
              </div>
            </dl>
          </div>

          <div
            v-if="leaveStore.sessionImpact.length > 0"
            class="rounded-xl border border-amber-200 bg-amber-50/50 shadow-sm p-5"
          >
            <h3 class="font-semibold text-amber-800 mb-4">{{ t('leave.detail.sessionImpact') }}</h3>
            <div class="space-y-2">
              <div
                v-for="s in leaveStore.sessionImpact"
                :key="s.sessionId"
                class="flex items-center justify-between text-sm rounded-lg bg-white/80 border border-amber-100 px-3 py-2"
              >
                <div>
                  <p class="font-medium text-foreground">{{ s.sessionTitle ?? '—' }}</p>
                  <p class="text-xs text-muted-foreground">
                    {{ formatDatetime(s.sessionStartsAt) }} – {{ formatDatetime(s.sessionEndsAt) }}
                  </p>
                </div>
                <span class="text-xs px-2 py-0.5 rounded bg-gray-100 text-gray-600">{{ s.sessionStatus }}</span>
              </div>
            </div>
          </div>

          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('leave.detail.approvalHistory') }}</h3>
            <div v-if="leave.approvals.length === 0" class="text-sm text-muted-foreground text-center py-6">
              {{ t('leave.detail.noApprovals') }}
            </div>
            <div v-else class="space-y-3">
              <div
                v-for="a in leave.approvals"
                :key="a.id"
                class="rounded-lg border border-border p-3"
              >
                <p class="text-sm font-medium text-foreground">
                  {{ t('leave.detail.step') }} {{ a.stepNo }}: {{ a.decision }}
                </p>
                <p class="text-xs text-muted-foreground mt-0.5">{{ a.comment ?? '—' }}</p>
                <p v-if="a.decidedAt" class="text-xs text-muted-foreground mt-1">{{ formatDatetime(a.decidedAt) }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5 h-fit">
          <h3 class="font-semibold text-foreground mb-4">{{ t('leave.detail.educator') }}</h3>
          <p class="text-sm font-medium text-foreground">{{ leave.educatorFullName ?? '—' }}</p>
          <p class="text-xs text-muted-foreground mt-0.5">{{ t('leave.fields.educator') }}</p>
          <div class="mt-4 pt-4 border-t border-border grid grid-cols-2 gap-3 text-center">
            <div>
              <p class="text-sm font-semibold text-foreground">{{ formatDate(leave.createdAt) }}</p>
              <p class="text-xs text-muted-foreground">{{ t('common.createdAt') }}</p>
            </div>
            <div>
              <p class="text-sm font-semibold text-foreground">{{ formatDate(leave.updatedAt) }}</p>
              <p class="text-xs text-muted-foreground">{{ t('common.updatedAt') }}</p>
            </div>
          </div>
          <button
            @click="router.push({ name: 'leave-list' })"
            class="mt-4 w-full px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.back') }}
          </button>
        </div>
      </div>
    </template>

    <FormModal
      :open="showApproveModal"
      :title="t('leave.actions.approve')"
      :saving="leaveStore.saving"
      @submit="doApprove"
      @close="showApproveModal = false"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.detail.comment') }}</label>
        <textarea v-model="actionComment" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
      </div>
    </FormModal>

    <FormModal
      :open="showRejectModal"
      :title="t('leave.actions.reject')"
      :saving="leaveStore.saving"
      @submit="doReject"
      @close="showRejectModal = false"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.detail.rejectReason') }}</label>
        <textarea
          v-model="actionComment"
          rows="3"
          :placeholder="t('leave.detail.rejectReasonPlaceholder')"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent"
        />
      </div>
    </FormModal>

    <ConfirmModal
      :open="showCancelConfirm"
      :title="t('leave.actions.cancel')"
      :message="t('leave.actions.cancel')"
      :confirm-label="t('leave.actions.cancel')"
      :loading="leaveStore.saving"
      @confirm="doCancel"
      @cancel="showCancelConfirm = false"
    />
  </div>
</template>
