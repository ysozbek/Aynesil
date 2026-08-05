<script setup lang="ts">
import { computed, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { OpenFollowUpReportItemDto } from '@/types/consultancy.types'

const { t } = useI18n()
const router = useRouter()
const store = useConsultancyStore()
const authStore = useAuthStore()
const { can } = usePermission()

const completeModal = reactive({ show: false, followUpId: '', notes: '' })

const overdueItems = computed(() => store.openFollowUps.filter(f => f.isOverdue))
const upcomingItems = computed(() => store.openFollowUps.filter(f => !f.isOverdue))

const totalOpen = computed(() => store.openFollowUps.length)
const overdueCount = computed(() => overdueItems.value.length)
const inProgressCount = computed(() => store.openFollowUps.filter(f => f.status === 'in_progress').length)
const pendingCount = computed(() => store.openFollowUps.filter(f => f.status === 'pending').length)

const reportColumns: Column<OpenFollowUpReportItemDto>[] = [
  { key: 'title', label: t('followUp.fields.title') },
  { key: 'planName', label: t('followUp.fields.plan') },
  { key: 'dueDate', label: t('followUp.dueDate'), width: '100px' },
  { key: 'assignedTo', label: t('followUp.assignedTo'), width: '120px' },
  { key: 'status', label: t('common.status'), width: '110px' },
]

function statusClass(s: string) {
  const map: Record<string, string> = {
    pending: 'bg-amber-100 text-amber-700',
    in_progress: 'bg-sky-100 text-sky-700',
  }
  return map[s] ?? 'bg-gray-100 text-gray-600'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    pending: t('followUp.pending'),
    in_progress: t('followUp.inProgress'),
  }
  return map[s] ?? s
}

async function quickStart(id: string) {
  await store.startFollowUp(id)
  await reload()
}

function openCompleteModal(id: string) {
  completeModal.followUpId = id
  completeModal.notes = ''
  completeModal.show = true
}

async function doComplete() {
  await store.fetchFollowUp(completeModal.followUpId)
  const rv = store.currentFollowUp?.rowVersion ?? 1
  await store.completeFollowUp(completeModal.followUpId, {
    notes: completeModal.notes || undefined,
    rowVersion: rv,
  })
  completeModal.show = false
  await reload()
}

async function reload() {
  await store.fetchOpenFollowUps({ corporationId: authStore.user?.corporationId })
}

onMounted(reload)
</script>

<template>
  <div>
    <PageHeader :title="t('followUp.openReportLabel')" :description="t('followUp.openReportSubtitle')">
      <button
        @click="router.push('/consultancy/follow-ups')"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
        <p class="text-2xl font-bold text-amber-600">{{ totalOpen }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('followUp.stats.openTotal') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
        <p class="text-2xl font-bold text-red-600">{{ overdueCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('followUp.stats.overdue') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
        <p class="text-2xl font-bold text-sky-600">{{ inProgressCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('followUp.inProgress') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm text-center">
        <p class="text-2xl font-bold text-primary">{{ pendingCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('followUp.pending') }}</p>
      </div>
    </div>

    <div v-if="store.loading" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>

    <div v-else-if="store.openFollowUps.length === 0" class="rounded-xl border border-border bg-[--color-card] shadow-sm py-16 text-center">
      <svg class="w-12 h-12 text-green-500 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <p class="font-semibold text-green-700">{{ t('followUp.report.allDone') }}</p>
      <p class="text-sm text-muted-foreground mt-1">{{ t('followUp.report.noOpenItems') }}</p>
    </div>

    <div v-else class="space-y-6">
      <div v-if="overdueItems.length > 0" class="rounded-xl border border-red-200 bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center gap-2 px-4 py-3 border-b border-red-200 bg-red-50/50">
          <svg class="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <h3 class="font-semibold text-red-700">{{ t('followUp.overdue') }}</h3>
          <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">{{ overdueItems.length }}</span>
        </div>
        <DataTable
          :columns="reportColumns"
          :rows="overdueItems"
          :loading="false"
          :row-key="'activityId'"
          @row-click="(row) => router.push(`/consultancy/follow-ups/${row.activityId}`)"
        >
          <template #cell-title="{ value }">
            <span class="font-medium text-red-700">{{ value }}</span>
          </template>
          <template #cell-planName="{ value }">
            <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
          </template>
          <template #cell-dueDate="{ value }">
            <span class="font-bold text-red-600 text-xs">{{ value ?? '—' }}</span>
          </template>
          <template #cell-assignedTo="{ value }">
            <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
          </template>
          <template #cell-status="{ value }">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
              {{ statusLabel(String(value)) }}
            </span>
          </template>
          <template #actions="{ row }">
            <div class="flex items-center justify-end gap-1" @click.stop>
              <button
                v-if="row.status === 'in_progress' && can('follow_up:complete')"
                @click="openCompleteModal(row.activityId)"
                class="p-1.5 rounded-lg hover:bg-accent text-green-600"
                :title="t('followUp.markCompleted')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </button>
            </div>
          </template>
        </DataTable>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="px-4 py-3 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('followUp.report.upcoming') }}</h3>
        </div>
        <div v-if="upcomingItems.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('followUp.report.noUpcoming') }}
        </div>
        <DataTable
          v-else
          :columns="reportColumns"
          :rows="upcomingItems"
          :loading="false"
          :row-key="'activityId'"
          @row-click="(row) => router.push(`/consultancy/follow-ups/${row.activityId}`)"
        >
          <template #cell-title="{ value }">
            <span class="font-medium text-foreground">{{ value }}</span>
          </template>
          <template #cell-planName="{ value }">
            <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
          </template>
          <template #cell-dueDate="{ value }">
            <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
          </template>
          <template #cell-assignedTo="{ value }">
            <span class="text-muted-foreground text-xs">{{ value ?? '—' }}</span>
          </template>
          <template #cell-status="{ value }">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(String(value))]">
              {{ statusLabel(String(value)) }}
            </span>
          </template>
          <template #actions="{ row }">
            <div class="flex items-center justify-end gap-1" @click.stop>
              <button
                v-if="row.status === 'pending' && can('follow_up:start')"
                @click="quickStart(row.activityId)"
                class="p-1.5 rounded-lg hover:bg-accent text-sky-600"
                :title="t('followUp.start')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
                </svg>
              </button>
              <button
                v-if="row.status === 'in_progress' && can('follow_up:complete')"
                @click="openCompleteModal(row.activityId)"
                class="p-1.5 rounded-lg hover:bg-accent text-green-600"
                :title="t('followUp.markCompleted')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </button>
            </div>
          </template>
        </DataTable>
      </div>
    </div>

    <FormModal
      :open="completeModal.show"
      :title="t('followUp.markCompleted')"
      :saving="store.saving"
      @submit="doComplete"
      @close="completeModal.show = false"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.completionNote') }}</label>
        <textarea
          v-model="completeModal.notes"
          rows="3"
          :placeholder="t('followUp.completionNotePlaceholder')"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent"
        />
      </div>
    </FormModal>
  </div>
</template>
