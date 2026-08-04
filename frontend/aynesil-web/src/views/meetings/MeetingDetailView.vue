<template>
  <div class="p-6 space-y-6">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <button class="btn btn-ghost btn-sm" @click="$router.back()">← {{ $t('common.back') }}</button>
        <h1 v-if="store.currentMeeting" class="text-xl font-bold">{{ store.currentMeeting.title }}</h1>
      </div>
      <div v-if="store.currentMeeting" class="flex gap-2">
        <router-link
          v-if="store.currentMeeting.status === 'scheduled' || store.currentMeeting.status === 'draft'"
          :to="{ name: 'meeting-edit', params: { id: store.currentMeeting.id } }"
          class="btn btn-outline btn-sm"
        >
          {{ $t('common.edit') }}
        </router-link>
        <button
          v-if="store.currentMeeting.status === 'scheduled'"
          class="btn btn-success btn-sm"
          :disabled="store.saving"
          @click="complete"
        >
          {{ $t('meeting.actions.complete') }}
        </button>
        <button
          v-if="store.currentMeeting.status !== 'completed' && store.currentMeeting.status !== 'cancelled'"
          class="btn btn-error btn-sm btn-outline"
          :disabled="store.saving"
          @click="cancel"
        >
          {{ $t('meeting.actions.cancel') }}
        </button>
      </div>
    </div>

    <div v-if="store.loading && !store.currentMeeting" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <template v-else-if="store.currentMeeting">
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Left: meeting info -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Info card -->
          <div class="card bg-base-100 shadow">
            <div class="card-body">
              <div class="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span class="text-gray-500">{{ $t('common.status') }}</span>
                  <span :class="['badge badge-sm ml-2', meetingStatusClass(store.currentMeeting.status)]">
                    {{ store.currentMeeting.status }}
                  </span>
                </div>
                <div>
                  <span class="text-gray-500">{{ $t('meeting.fields.type') }}:</span>
                  <span class="ml-1">{{ store.currentMeeting.meetingTypeCode ?? '-' }}</span>
                </div>
                <div v-if="store.currentMeeting.scheduledAt">
                  <span class="text-gray-500">{{ $t('meeting.fields.scheduledAt') }}:</span>
                  <span class="ml-1">{{ formatDateTime(store.currentMeeting.scheduledAt) }}</span>
                </div>
                <div v-if="store.currentMeeting.endsAt">
                  <span class="text-gray-500">{{ $t('meeting.fields.endsAt') }}:</span>
                  <span class="ml-1">{{ formatDateTime(store.currentMeeting.endsAt) }}</span>
                </div>
                <div v-if="store.currentMeeting.location">
                  <span class="text-gray-500">{{ $t('meeting.fields.location') }}:</span>
                  <span class="ml-1">{{ store.currentMeeting.location }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Outcomes -->
          <div class="card bg-base-100 shadow">
            <div class="card-header border-b px-5 py-4 flex items-center justify-between">
              <h2 class="font-semibold">{{ $t('meeting.outcomes.title') }}</h2>
              <button v-if="canEdit" class="btn btn-ghost btn-sm" @click="showOutcomeModal = true">+ {{ $t('meeting.outcomes.add') }}</button>
            </div>
            <div class="card-body p-0">
              <div v-if="!store.currentMeeting.outcomes.length" class="px-5 py-6 text-center text-gray-400">
                {{ $t('meeting.outcomes.none') }}
              </div>
              <div v-else class="divide-y">
                <div v-for="o in store.currentMeeting.outcomes" :key="o.id" class="px-5 py-4">
                  <p v-if="o.summary" class="font-medium">{{ o.summary }}</p>
                  <p v-if="o.decisions" class="text-sm text-gray-600 mt-1 whitespace-pre-wrap">{{ o.decisions }}</p>
                  <p class="text-xs text-gray-400 mt-2">{{ formatDateTime(o.createdAt) }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Follow-ups -->
          <div class="card bg-base-100 shadow">
            <div class="card-header border-b px-5 py-4 flex items-center justify-between">
              <h2 class="font-semibold">{{ $t('meeting.followUp.title') }}</h2>
              <button v-if="canEdit" class="btn btn-ghost btn-sm" @click="showFollowUpModal = true">+ {{ $t('meeting.followUp.add') }}</button>
            </div>
            <div class="card-body p-0">
              <div v-if="!store.currentMeeting.followUps.length" class="px-5 py-6 text-center text-gray-400">
                {{ $t('meeting.followUp.none') }}
              </div>
              <div v-else class="divide-y">
                <div v-for="fu in store.currentMeeting.followUps" :key="fu.id" class="px-5 py-3 flex items-center gap-3">
                  <span :class="['badge badge-sm', followUpStatusClass(fu.status)]">{{ fu.status }}</span>
                  <span class="flex-1 text-sm">{{ fu.action }}</span>
                  <span v-if="fu.dueDate" class="text-xs text-gray-500">{{ formatDate(fu.dueDate) }}</span>
                  <div v-if="canEdit" class="flex gap-1">
                    <button class="btn btn-xs btn-success" @click="markFollowUpDone(fu.id)" :disabled="fu.status === 'completed'">✓</button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Participants -->
        <div class="space-y-4">
          <div class="card bg-base-100 shadow">
            <div class="card-header border-b px-5 py-4 flex items-center justify-between">
              <h2 class="font-semibold">{{ $t('meeting.participants.title') }} ({{ store.currentMeeting.participants.length }})</h2>
              <button v-if="canEdit" class="btn btn-ghost btn-xs" @click="showParticipantModal = true">+</button>
            </div>
            <div class="card-body p-0">
              <div v-for="p in store.currentMeeting.participants" :key="p.id" class="px-5 py-3 border-b last:border-0 flex items-center gap-3">
                <div class="avatar placeholder">
                  <div class="bg-base-300 rounded-full w-8">
                    <span class="text-xs">{{ participantInitial(p) }}</span>
                  </div>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium truncate">{{ participantLabel(p) }}</p>
                  <p class="text-xs text-gray-400">{{ p.participantType }}</p>
                </div>
                <span v-if="p.attendance" :class="['badge badge-xs', attendanceClass(p.attendance)]">{{ p.attendance }}</span>
                <div v-if="canEdit" class="dropdown dropdown-end">
                  <button tabindex="0" class="btn btn-ghost btn-xs">⋮</button>
                  <ul tabindex="0" class="dropdown-content z-10 menu p-1 shadow bg-base-100 rounded-box w-32">
                    <li><button @click="setAttendance(p.id, 'attended')">{{ $t('meeting.participants.attended') }}</button></li>
                    <li><button @click="setAttendance(p.id, 'absent')">{{ $t('meeting.participants.absent') }}</button></li>
                    <li><button class="text-error" @click="removeParticipant(p.id)">{{ $t('common.delete') }}</button></li>
                  </ul>
                </div>
              </div>
              <div v-if="!store.currentMeeting.participants.length" class="px-5 py-4 text-center text-gray-400 text-sm">
                {{ $t('meeting.participants.none') }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Add Outcome Modal -->
    <dialog ref="outcomeModalRef" class="modal" :class="{ 'modal-open': showOutcomeModal }">
      <div class="modal-box">
        <h3 class="font-bold text-lg mb-4">{{ $t('meeting.outcomes.add') }}</h3>
        <div class="space-y-3">
          <div class="form-control">
            <label class="label label-text text-sm">{{ $t('meeting.outcomes.summary') }}</label>
            <input v-model="outcomeForm.summary" type="text" class="input input-bordered input-sm" />
          </div>
          <div class="form-control">
            <label class="label label-text text-sm">{{ $t('meeting.outcomes.decisions') }}</label>
            <textarea v-model="outcomeForm.decisions" class="textarea textarea-bordered" rows="3"></textarea>
          </div>
        </div>
        <div class="modal-action">
          <button class="btn btn-ghost" @click="showOutcomeModal = false">{{ $t('common.cancel') }}</button>
          <button class="btn btn-primary" :disabled="store.saving" @click="saveOutcome">{{ $t('common.save') }}</button>
        </div>
      </div>
      <div class="modal-backdrop" @click="showOutcomeModal = false"></div>
    </dialog>

    <!-- Add Follow-Up Modal -->
    <dialog class="modal" :class="{ 'modal-open': showFollowUpModal }">
      <div class="modal-box">
        <h3 class="font-bold text-lg mb-4">{{ $t('meeting.followUp.add') }}</h3>
        <div class="space-y-3">
          <div class="form-control">
            <label class="label label-text text-sm">{{ $t('meeting.followUp.action') }} *</label>
            <input v-model="followUpForm.action" type="text" class="input input-bordered input-sm" />
          </div>
          <div class="form-control">
            <label class="label label-text text-sm">{{ $t('meeting.followUp.dueDate') }}</label>
            <input v-model="followUpForm.dueDate" type="date" class="input input-bordered input-sm" />
          </div>
        </div>
        <div class="modal-action">
          <button class="btn btn-ghost" @click="showFollowUpModal = false">{{ $t('common.cancel') }}</button>
          <button class="btn btn-primary" :disabled="store.saving || !followUpForm.action" @click="saveFollowUp">{{ $t('common.save') }}</button>
        </div>
      </div>
      <div class="modal-backdrop" @click="showFollowUpModal = false"></div>
    </dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useMeetingStore } from '@/stores/meeting.store'
import type { MeetingParticipantDto } from '@/types/meeting.types'

const route = useRoute()
const store = useMeetingStore()
const id = computed(() => route.params.id as string)

const showOutcomeModal = ref(false)
const showFollowUpModal = ref(false)
const showParticipantModal = ref(false)
const outcomeModalRef = ref<HTMLDialogElement | null>(null)

const outcomeForm = ref({ summary: '', decisions: '' })
const followUpForm = ref({ action: '', dueDate: '' })

const canEdit = computed(() => {
  const s = store.currentMeeting?.status
  return s !== 'cancelled' && s !== 'completed'
})

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}
function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}
function meetingStatusClass(s: string): string {
  const map: Record<string, string> = { draft: 'badge-ghost', scheduled: 'badge-info', completed: 'badge-success', cancelled: 'badge-error' }
  return map[s] ?? 'badge-ghost'
}
function followUpStatusClass(s: string): string {
  const map: Record<string, string> = { pending: 'badge-warning', in_progress: 'badge-info', completed: 'badge-success', cancelled: 'badge-ghost' }
  return map[s] ?? 'badge-ghost'
}
function attendanceClass(s: string): string {
  return s === 'attended' ? 'badge-success' : s === 'absent' ? 'badge-error' : 'badge-ghost'
}
function participantLabel(p: MeetingParticipantDto): string {
  return p.externalName ?? `${p.participantType} ${p.userId ?? p.guardianId ?? p.leadId ?? ''}`
}
function participantInitial(p: MeetingParticipantDto): string {
  const label = participantLabel(p)
  return label.slice(0, 2).toUpperCase()
}

async function complete() {
  await store.completeMeeting(id.value)
}
async function cancel() {
  await store.cancelMeeting(id.value)
}
async function setAttendance(participantId: string, attendance: string) {
  await store.updateParticipantAttendance(id.value, participantId, { attendance })
}
async function removeParticipant(participantId: string) {
  await store.removeParticipant(id.value, participantId)
}
async function saveOutcome() {
  await store.addOutcome(id.value, outcomeForm.value)
  outcomeForm.value = { summary: '', decisions: '' }
  showOutcomeModal.value = false
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
  await store.updateFollowUpStatus(id.value, followUpId, { status: 'completed' })
}

onMounted(() => store.fetchMeeting(id.value))
</script>
