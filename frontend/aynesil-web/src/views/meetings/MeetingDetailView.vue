<script setup lang="ts">
import { MeetingFollowUpStatus, MeetingStatus } from '@/constants/statuses'
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMeetingStore } from '@/stores/meeting.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { MeetingParticipantDto, MeetingOutcomeDto } from '@/types/meeting.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useMeetingStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const meeting = computed(() => store.currentMeeting)

const showOutcomeModal = ref(false)
const showFollowUpModal = ref(false)
const showCancelConfirm = ref(false)
const openParticipantMenu = ref<string | null>(null)

const outcomeForm = ref({ summary: '', decisions: '' })
const editingOutcomeId = ref<string | null>(null)
const outcomeError = ref('')
const followUpForm = ref({ action: '', dueDate: '' })

const canEditMeeting = computed(() => {
  const s = meeting.value?.status
  return s !== 'cancelled' && s !== 'completed' && can(Permissions.Meetings.Update)
})

const canManageOutcomes = computed(() =>
  canEditMeeting.value && can(Permissions.Meetings.RecordOutcome)
)
const canManageFollowUps = computed(() =>
  canEditMeeting.value && can(Permissions.Meetings.ManageFollowUps)
)
const canManageParticipants = computed(() =>
  canEditMeeting.value && can(Permissions.Meetings.ManageParticipants)
)
const canRecordAttendance = computed(() =>
  canEditMeeting.value && can(Permissions.Meetings.RecordAttendance)
)

function formatDateTime(d: string) {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}
function formatDate(d: string) {
  return new Date(d).toLocaleDateString('tr-TR')
}

function participantLabel(p: MeetingParticipantDto) {
  return p.externalName ?? `${p.participantType} ${p.userId ?? p.guardianId ?? p.leadId ?? ''}`
}

function participantInitial(p: MeetingParticipantDto) {
  return participantLabel(p).slice(0, 2).toUpperCase()
}

function toggleParticipantMenu(participantId: string) {
  openParticipantMenu.value = openParticipantMenu.value === participantId ? null : participantId
}

async function complete() {
  await store.completeMeeting(id.value)
}

async function cancel() {
  await store.cancelMeeting(id.value)
  showCancelConfirm.value = false
}

async function setAttendance(participantId: string, attendance: string) {
  openParticipantMenu.value = null
  await store.updateParticipantAttendance(id.value, participantId, { attendance })
}

async function removeParticipant(participantId: string) {
  openParticipantMenu.value = null
  await store.removeParticipant(id.value, participantId)
}

function openAddOutcome() {
  editingOutcomeId.value = null
  outcomeError.value = ''
  outcomeForm.value = { summary: '', decisions: '' }
  showOutcomeModal.value = true
}

function openEditOutcome(o: MeetingOutcomeDto) {
  editingOutcomeId.value = o.id
  outcomeError.value = ''
  outcomeForm.value = { summary: o.summary ?? '', decisions: o.decisions ?? '' }
  showOutcomeModal.value = true
}

async function saveOutcome() {
  outcomeError.value = ''
  // Sunucu da doğrular: özet ya da karardan en az biri dolu olmalı.
  if (!outcomeForm.value.summary.trim() && !outcomeForm.value.decisions.trim()) {
    outcomeError.value = t('meeting.outcomes.atLeastOne')
    return
  }
  try {
    if (editingOutcomeId.value) await store.updateOutcome(id.value, editingOutcomeId.value, outcomeForm.value)
    else await store.addOutcome(id.value, outcomeForm.value)
    outcomeForm.value = { summary: '', decisions: '' }
    showOutcomeModal.value = false
  } catch (e: unknown) {
    outcomeError.value = (e as Error).message
  }
}

async function saveFollowUp() {
  await store.addFollowUp(id.value, {
    action: followUpForm.value.action,
    dueDate: followUpForm.value.dueDate || undefined,
  })
  followUpForm.value = { action: '', dueDate: '' }
  showFollowUpModal.value = false
}

async function markFollowUpDone(followUpId: string) {
  await store.updateFollowUpStatus(id.value, followUpId, { status: MeetingFollowUpStatus.Done })
}

onMounted(async () => {
  store.clearCurrent()
  await store.fetchMeeting(id.value)
})
</script>

<template>
  <div>
    <div v-if="store.loading && !meeting" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!meeting" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
    <template v-else>
      <PageHeader :title="meeting.title" :description="t('meeting.fields.scheduledAt')">
        <div class="flex flex-wrap items-center gap-2">
          <StatusBadge entity="meeting" :status="meeting.status" />
          <button
            v-if="meeting.status === MeetingStatus.Scheduled && can(Permissions.Meetings.Update)"
            @click="router.push({ name: 'meeting-edit', params: { id: meeting.id } })"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="meeting.status === MeetingStatus.Scheduled && can(Permissions.Meetings.Complete)"
            :disabled="store.saving"
            @click="complete"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
          >
            {{ t('meeting.actions.complete') }}
          </button>
          <button
            v-if="meeting.status !== MeetingStatus.Completed && meeting.status !== MeetingStatus.Cancelled && can(Permissions.Meetings.Cancel)"
            @click="showCancelConfirm = true"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          >
            {{ t('meeting.actions.cancel') }}
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 space-y-6">
          <div class="rounded-xl border border-border bg-card shadow-sm p-5">
            <h3 class="font-semibold text-foreground mb-4">{{ t('meeting.fields.title') }}</h3>
            <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div>
                <dt class="text-muted-foreground mb-0.5">{{ t('meeting.fields.type') }}</dt>
                <dd class="font-medium text-foreground">{{ meeting.meetingTypeCode ?? '—' }}</dd>
              </div>
              <div v-if="meeting.scheduledAt">
                <dt class="text-muted-foreground mb-0.5">{{ t('meeting.fields.scheduledAt') }}</dt>
                <dd class="font-medium text-foreground">{{ formatDateTime(meeting.scheduledAt) }}</dd>
              </div>
              <div v-if="meeting.endsAt">
                <dt class="text-muted-foreground mb-0.5">{{ t('meeting.fields.endsAt') }}</dt>
                <dd class="font-medium text-foreground">{{ formatDateTime(meeting.endsAt) }}</dd>
              </div>
              <div v-if="meeting.location">
                <dt class="text-muted-foreground mb-0.5">{{ t('meeting.fields.location') }}</dt>
                <dd class="font-medium text-foreground">{{ meeting.location }}</dd>
              </div>
            </dl>
          </div>

          <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
            <div class="flex items-center justify-between p-4 border-b border-border">
              <h3 class="font-semibold text-foreground">{{ t('meeting.outcomes.title') }}</h3>
              <button
                v-if="canManageOutcomes"
                @click="openAddOutcome"
                class="text-xs text-primary hover:underline"
              >
                + {{ t('meeting.outcomes.add') }}
              </button>
            </div>
            <div v-if="meeting.outcomes.length === 0" class="py-10 text-center text-sm text-muted-foreground">
              {{ t('meeting.outcomes.none') }}
            </div>
            <div v-else class="divide-y divide-border">
              <div v-for="o in meeting.outcomes" :key="o.id" class="px-4 py-4 flex items-start gap-3">
                <div class="flex-1 min-w-0">
                  <p v-if="o.summary" class="text-sm font-medium text-foreground">{{ o.summary }}</p>
                  <p v-if="o.decisions" class="text-sm text-muted-foreground mt-1 whitespace-pre-wrap">{{ o.decisions }}</p>
                  <p class="text-xs text-muted-foreground mt-2">{{ formatDateTime(o.createdAt) }}</p>
                </div>
                <button
                  v-if="canManageOutcomes"
                  @click="openEditOutcome(o)"
                  class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground shrink-0"
                  :title="t('common.edit')"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
            <div class="flex items-center justify-between p-4 border-b border-border">
              <h3 class="font-semibold text-foreground">{{ t('meeting.followUp.title') }}</h3>
              <button
                v-if="canManageFollowUps"
                @click="showFollowUpModal = true"
                class="text-xs text-primary hover:underline"
              >
                + {{ t('meeting.followUp.add') }}
              </button>
            </div>
            <div v-if="meeting.followUps.length === 0" class="py-10 text-center text-sm text-muted-foreground">
              {{ t('meeting.followUp.none') }}
            </div>
            <div v-else class="divide-y divide-border">
              <div
                v-for="fu in meeting.followUps"
                :key="fu.id"
                class="flex items-center gap-3 px-4 py-3"
              >
                <StatusBadge entity="meetingFollowUp" :status="fu.status" class="shrink-0" />
                <span class="flex-1 text-sm text-foreground">{{ fu.action }}</span>
                <span v-if="fu.dueDate" class="text-xs text-muted-foreground shrink-0">{{ formatDate(fu.dueDate) }}</span>
                <button
                  v-if="canManageFollowUps && fu.status !== MeetingFollowUpStatus.Done"
                  @click="markFollowUpDone(fu.id)"
                  class="px-2 py-1 text-xs rounded-lg bg-green-600 text-white hover:bg-green-700 shrink-0"
                >
                  {{ t('followUp.markCompleted') }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden h-fit">
          <div class="flex items-center justify-between p-4 border-b border-border">
            <h3 class="font-semibold text-foreground">
              {{ t('meeting.participants.title') }} ({{ meeting.participants.length }})
            </h3>
          </div>
          <div v-if="meeting.participants.length === 0" class="py-10 text-center text-sm text-muted-foreground">
            {{ t('meeting.participants.none') }}
          </div>
          <div v-else class="divide-y divide-border">
            <div
              v-for="p in meeting.participants"
              :key="p.id"
              class="relative flex items-center gap-3 px-4 py-3"
            >
              <div class="w-8 h-8 rounded-full bg-accent flex items-center justify-center shrink-0">
                <span class="text-xs font-medium text-muted-foreground">{{ participantInitial(p) }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-foreground truncate">{{ participantLabel(p) }}</p>
                <p class="text-xs text-muted-foreground">{{ p.participantType }}</p>
              </div>
              <StatusBadge v-if="p.attendance" entity="meetingAttendance" :status="p.attendance" class="shrink-0" />
              <div v-if="canManageParticipants || canRecordAttendance" class="relative shrink-0">
                <button
                  @click="toggleParticipantMenu(p.id)"
                  class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z" />
                  </svg>
                </button>
                <div
                  v-if="openParticipantMenu === p.id"
                  class="absolute right-0 top-full mt-1 z-10 w-36 rounded-lg border border-border bg-card shadow-lg py-1"
                >
                  <button
                    v-if="canRecordAttendance"
                    @click="setAttendance(p.id, 'attended')"
                    class="w-full text-left px-3 py-1.5 text-sm hover:bg-accent"
                  >
                    {{ t('meeting.participants.attended') }}
                  </button>
                  <button
                    v-if="canRecordAttendance"
                    @click="setAttendance(p.id, 'absent')"
                    class="w-full text-left px-3 py-1.5 text-sm hover:bg-accent"
                  >
                    {{ t('meeting.participants.absent') }}
                  </button>
                  <button
                    v-if="canManageParticipants"
                    @click="removeParticipant(p.id)"
                    class="w-full text-left px-3 py-1.5 text-sm text-red-600 hover:bg-red-50"
                  >
                    {{ t('common.delete') }}
                  </button>
                </div>
              </div>
            </div>
          </div>
          <div class="p-4 border-t border-border">
            <button
              @click="router.push({ name: 'meetings' })"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
            >
              {{ t('common.back') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <FormModal
      :open="showOutcomeModal"
      :title="editingOutcomeId ? t('meeting.outcomes.edit') : t('meeting.outcomes.add')"
      :saving="store.saving"
      @submit="saveOutcome"
      @close="showOutcomeModal = false"
    >
      <div v-if="outcomeError" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700 mb-4">{{ outcomeError }}</div>
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.outcomes.summary') }}</label>
          <input v-model="outcomeForm.summary" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.outcomes.decisions') }}</label>
          <textarea v-model="outcomeForm.decisions" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <FormModal
      :open="showFollowUpModal"
      :title="t('meeting.followUp.add')"
      :saving="store.saving"
      @submit="saveFollowUp"
      @close="showFollowUpModal = false"
    >
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.followUp.action') }} *</label>
          <input v-model="followUpForm.action" type="text" required class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.followUp.dueDate') }}</label>
          <input v-model="followUpForm.dueDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="showCancelConfirm"
      :title="t('meeting.actions.cancel')"
      :message="t('meeting.actions.cancel')"
      :confirm-label="t('meeting.actions.cancel')"
      :loading="store.saving"
      @confirm="cancel"
      @cancel="showCancelConfirm = false"
    />
  </div>
</template>
