<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
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
const followUp = computed(() => store.currentFollowUp)
const showCompleteModal = ref(false)
const showCancelConfirm = ref(false)
const completionNotes = ref('')

function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('tr-TR')
}
function formatDatetime(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

const isOverdue = computed(() => {
  const f = followUp.value
  if (!f?.dueDate || f.status === 'completed' || f.status === 'cancelled') return false
  return new Date(f.dueDate) < new Date()
})

const headerDescription = computed(() => {
  const f = followUp.value
  if (!f) return ''
  const parts: string[] = []
  if (f.planName) parts.push(f.planName)
  if (f.visitDate) parts.push(f.visitDate)
  return parts.join(' · ')
})

function statusClass(s: string) {
  const map: Record<string, string> = {
    pending: 'bg-amber-100 text-amber-700',
    in_progress: 'bg-sky-100 text-sky-700',
    completed: 'bg-green-100 text-green-700',
    cancelled: 'bg-red-100 text-red-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    pending: t('followUp.pending'),
    in_progress: t('followUp.inProgress'),
    completed: t('followUp.completed'),
    cancelled: t('followUp.cancelled'),
  }
  return map[s] ?? s
}

async function doStart() {
  await store.startFollowUp(id)
}

async function doComplete() {
  if (!followUp.value) return
  await store.completeFollowUp(id, {
    notes: completionNotes.value || undefined,
    rowVersion: followUp.value.rowVersion,
  })
  showCompleteModal.value = false
  completionNotes.value = ''
}

async function doCancel() {
  await store.cancelFollowUp(id)
  showCancelConfirm.value = false
}

onMounted(() => {
  store.currentFollowUp = null
  store.fetchFollowUp(id)
})
</script>

<template>
  <div>
    <div v-if="store.loading" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!followUp" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
    <template v-else>
      <div v-if="isOverdue" class="mb-6 flex items-center gap-3 rounded-xl border border-red-200 bg-red-50/50 p-4">
        <svg class="w-5 h-5 text-red-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="font-semibold text-red-700">{{ t('followUp.overdueAlert') }}</p>
      </div>

      <PageHeader :title="followUp.title" :description="headerDescription">
        <div class="flex flex-wrap items-center gap-2">
          <span :class="['px-2.5 py-1 rounded-full text-xs font-medium', statusClass(followUp.status)]">
            {{ statusLabel(followUp.status) }}
          </span>
          <button
            v-if="(followUp.status === 'pending' || followUp.status === 'in_progress') && can('follow_up:update')"
            @click="router.push(`/consultancy/follow-ups/${followUp.id}/edit`)"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="followUp.status === 'pending' && can('follow_up:start')"
            :disabled="store.saving"
            @click="doStart"
            class="px-3 py-1.5 text-sm rounded-lg bg-sky-600 text-white hover:bg-sky-700 disabled:opacity-50"
          >
            {{ t('followUp.start') }}
          </button>
          <button
            v-if="followUp.status === 'in_progress' && can('follow_up:complete')"
            @click="showCompleteModal = true"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700"
          >
            {{ t('followUp.markCompleted') }}
          </button>
          <button
            v-if="(followUp.status === 'pending' || followUp.status === 'in_progress') && can('follow_up:cancel')"
            :disabled="store.saving"
            @click="showCancelConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          >
            {{ t('followUp.cancel') }}
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 space-y-6">
          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('followUp.detail.info') }}</h3>
            <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('followUp.dueDate') }}</dt>
                <dd :class="isOverdue ? 'font-bold text-red-600' : 'font-medium text-foreground'">
                  {{ followUp.dueDate ?? '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('followUp.assignedTo') }}</dt>
                <dd class="font-medium text-foreground">{{ followUp.assignedTo ?? '—' }}</dd>
              </div>
              <div v-if="followUp.description" class="sm:col-span-2">
                <dt class="text-muted-foreground mb-0.5">{{ t('followUp.fields.description') }}</dt>
                <dd class="font-medium text-foreground">{{ followUp.description }}</dd>
              </div>
            </dl>
          </div>

          <div v-if="followUp.status === 'completed' && followUp.notes" class="rounded-xl border border-green-200 bg-green-50/50 shadow-sm p-5">
            <h3 class="font-semibold text-green-800 mb-3 flex items-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              {{ t('followUp.completionNote') }}
            </h3>
            <p class="text-sm text-foreground">{{ followUp.notes }}</p>
            <p v-if="followUp.completedAt" class="text-xs text-muted-foreground mt-2">
              {{ t('followUp.detail.completedAt') }}: {{ formatDatetime(followUp.completedAt) }}
            </p>
          </div>

          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('followUp.detail.timeline') }}</h3>
            <div class="space-y-4">
              <div class="flex gap-3">
                <div class="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
                  <svg class="w-4 h-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                  </svg>
                </div>
                <div>
                  <p class="text-sm font-medium text-foreground">{{ t('followUp.detail.created') }}</p>
                  <p class="text-xs text-muted-foreground">{{ formatDatetime(followUp.createdAt) }}</p>
                </div>
              </div>
              <div v-if="followUp.completedAt" class="flex gap-3">
                <div class="w-8 h-8 rounded-full bg-green-100 flex items-center justify-center shrink-0">
                  <svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <div>
                  <p class="text-sm font-medium text-green-700">{{ t('followUp.completed') }}</p>
                  <p class="text-xs text-muted-foreground">{{ formatDatetime(followUp.completedAt) }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="space-y-6">
          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('followUp.detail.source') }}</h3>
            <dl class="space-y-3 text-sm">
              <div v-if="followUp.planName">
                <dt class="text-muted-foreground mb-0.5">{{ t('followUp.fields.plan') }}</dt>
                <dd>
                  <RouterLink
                    v-if="followUp.consultancyPlanId"
                    :to="`/consultancy/plans/${followUp.consultancyPlanId}`"
                    class="font-medium text-primary hover:underline"
                  >
                    {{ followUp.planName }}
                  </RouterLink>
                </dd>
              </div>
              <div v-if="followUp.visitDate">
                <dt class="text-muted-foreground mb-0.5">{{ t('followUp.fields.visitDate') }}</dt>
                <dd class="font-medium text-foreground">{{ followUp.visitDate }}</dd>
              </div>
              <div v-if="followUp.observationRecordId">
                <dt class="text-muted-foreground mb-0.5">{{ t('followUp.fields.observation') }}</dt>
                <dd>
                  <span class="px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">
                    {{ t('followUp.detail.linkedObservation') }}
                  </span>
                </dd>
              </div>
            </dl>
          </div>

          <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('followUp.detail.metadata') }}</h3>
            <dl class="space-y-3 text-sm">
              <div>
                <dt class="text-muted-foreground">{{ t('common.createdAt') }}</dt>
                <dd class="font-medium text-foreground">{{ formatDate(followUp.createdAt) }}</dd>
              </div>
              <div>
                <dt class="text-muted-foreground">{{ t('common.updatedAt') }}</dt>
                <dd class="font-medium text-foreground">{{ formatDate(followUp.updatedAt) }}</dd>
              </div>
            </dl>
            <button @click="router.back()" class="mt-4 w-full px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent">
              {{ t('common.back') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <FormModal
      :open="showCompleteModal"
      :title="t('followUp.markCompleted')"
      :saving="store.saving"
      @submit="doComplete"
      @close="showCompleteModal = false"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.completionNote') }}</label>
        <textarea
          v-model="completionNotes"
          rows="4"
          :placeholder="t('followUp.completionNotePlaceholder')"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent"
        />
      </div>
    </FormModal>

    <ConfirmModal
      :open="showCancelConfirm"
      :title="t('followUp.cancel')"
      :message="t('followUp.cancelConfirm')"
      :confirm-label="t('followUp.cancel')"
      :loading="store.saving"
      @confirm="doCancel"
      @cancel="showCancelConfirm = false"
    />
  </div>
</template>
