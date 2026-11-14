<script setup lang="ts">
import { SessionStatus } from '@/constants/statuses'
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session.store'
import { useAttendanceStore } from '@/stores/attendance.store'
import { usePermission } from '@/composables/usePermission'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import EducatorPicker from '@/components/pickers/EducatorPicker.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import { goalService } from '@/services/goal.service'
import type { StudentGoalListItemDto } from '@/types/goal.types'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useSessionStore()
const attendanceStore = useAttendanceStore()
const refData = useRefDataStore()
const { can } = usePermission()

const sessionId = route.params.id as string
const activeTab = ref<'overview' | 'participants' | 'attendance' | 'notes' | 'goals'>('overview')
const attendanceReasons = ref<RefValueItem[]>([])

// Modal state
const cancelModal = ref(false)
const cancelReason = ref('')
const completeModal = ref(false)
const noteModal = ref(false)
const noteError = ref('')
/** Doluysa not modalı düzenleme modunda. */
const editingNote = ref<{ id: string; rowVersion: number } | null>(null)
const removeGoalTarget = ref<{ studentGoalId: string; goalStatement: string } | null>(null)
const goalError = ref('')
const newNoteBody = ref('')
const newNoteParentVisible = ref(false)
const newNoteStudentId = ref('')
/** Grup seansı: veliye görünür not için öğrenci seçimi zorunlu (sunucu da doğrular). */
const isGroupSession = computed(() => (session.value?.participants.length ?? 0) > 1)
const addParticipantId = ref('')
const addEducatorId = ref('')

// Add goal to session: active goals of the session's participants not yet linked
const participantGoals = ref<(StudentGoalListItemDto & { studentFullName: string })[]>([])
const addGoalId = ref('')
const goalsLoaded = ref(false)

onMounted(async () => {
  await Promise.all([
    store.fetchSession(sessionId),
    attendanceStore.fetchSessionAttendance(sessionId),
    refData.getValues('ATTENDANCE_REASON').then(v => { attendanceReasons.value = v }),
  ])
})

const session = computed(() => store.currentSession)

async function loadParticipantGoals() {
  const s = store.currentSession
  if (!s) return
  const results = await Promise.all(
    s.participants.map(p =>
      goalService
        .listStudentGoals({ studentId: p.studentId, status: 'active', page: 1, pageSize: 100 })
        .then(res => (res.success && res.data ? res.data.items : []).map(g => ({ ...g, studentFullName: p.studentFullName })))
        .catch(() => []),
    ),
  )
  participantGoals.value = results.flat()
  goalsLoaded.value = true
}

const availableGoals = computed(() => {
  const linked = new Set((session.value?.goals ?? []).map(g => g.studentGoalId))
  return participantGoals.value.filter(g => !linked.has(g.id))
})

const showStudentNameInGoals = computed(() => (session.value?.participants.length ?? 0) > 1)

watch(
  () => [activeTab.value, session.value?.participants.map(p => p.studentId).join(',')],
  () => { if (activeTab.value === 'goals') loadParticipantGoals() },
)

async function addGoalToSession() {
  if (!addGoalId.value) return
  await store.updateSessionGoal(sessionId, addGoalId.value, { workedOn: true })
  addGoalId.value = ''
}

function formatDateTime(val: string): string {
  return new Date(val).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

// Backend session.status values (Aynesil.Domain.Modules.Scheduling.Entities.Session):
//   scheduled, in_progress, rescheduled, completed, cancelled, no_show
/** Lifecycle actions are available while a session is `scheduled` or `rescheduled`. */
const isLiveSession = (status: string | undefined) =>
  status === SessionStatus.Scheduled || status === SessionStatus.Rescheduled

async function doCancel() {
  if (!session.value) return
  await store.cancelSession(sessionId, { reason: cancelReason.value, rowVersion: session.value.rowVersion })
  cancelModal.value = false
  cancelReason.value = ''
}

async function doComplete() {
  if (!session.value) return
  await store.completeSession(sessionId, { rowVersion: session.value.rowVersion })
  completeModal.value = false
}

async function doNoShow() {
  if (!session.value) return
  await store.noShowSession(sessionId, session.value.rowVersion)
}

function openAddNote() {
  editingNote.value = null
  newNoteBody.value = ''
  newNoteParentVisible.value = false
  newNoteStudentId.value = 
  noteError.value = ''
  noteModal.value = true
}

function openEditNote(note: { id: string; body: string; parentVisible: boolean; rowVersion: number; studentId?: string | null }) {
  editingNote.value = { id: note.id, rowVersion: note.rowVersion }
  newNoteBody.value = note.body
  newNoteParentVisible.value = note.parentVisible
  newNoteStudentId.value = note.studentId ?? ''
  noteError.value = ''
  noteModal.value = true
}

/** Metni düzenlenmiş mi? (V55 edited_at; yalnızca metin değişince dolar). Yazar kontrolünü sunucu yapar. */
function isEdited(note: { editedAt?: string | null }) {
  return !!note.editedAt
}

async function doSaveNote() {
  if (!newNoteBody.value.trim()) return
  noteError.value = ''
  if (isGroupSession.value && newNoteParentVisible.value && !newNoteStudentId.value) {
    noteError.value = t('scheduling.session.noteStudentRequired')
    return
  }
  const studentId = newNoteStudentId.value || null
  try {
    if (editingNote.value) {
      await store.updateNote(sessionId, editingNote.value.id, {
        body: newNoteBody.value,
        parentVisible: newNoteParentVisible.value,
        studentId,
        rowVersion: editingNote.value.rowVersion,
      })
    } else {
      await store.addNote(sessionId, { body: newNoteBody.value, parentVisible: newNoteParentVisible.value, studentId })
    }
    noteModal.value = false
    editingNote.value = null
    newNoteBody.value = ''
    newNoteParentVisible.value = false
  } catch (e: unknown) {
    noteError.value = (e as Error).message
  }
}

async function deleteNote(noteId: string) {
  await store.deleteNote(sessionId, noteId)
}

async function doRemoveGoal() {
  if (!removeGoalTarget.value) return
  goalError.value = ''
  try {
    await store.removeSessionGoal(sessionId, removeGoalTarget.value.studentGoalId)
  } catch (e: unknown) {
    goalError.value = (e as Error).message
  } finally {
    removeGoalTarget.value = null
  }
}

async function removeParticipant(studentId: string) {
  await store.removeParticipant(sessionId, studentId)
}

async function removeEducator(educatorId: string) {
  await store.removeEducator(sessionId, educatorId)
}

async function doRecordAttendance(studentId: string, status: string, reasonId?: string) {
  await attendanceStore.recordAttendance(sessionId, { studentId, status, reasonId })
}

function getAttendance(studentId: string) {
  return attendanceStore.sessionAttendance.find(a => a.studentId === studentId)
}
</script>

<template>
  <div>
    <PageHeader
      :title="session?.title ?? session?.sessionTypeName ?? t('scheduling.session.detail')"
      :description="session ? formatDateTime(session.startsAt) + ' – ' + formatDateTime(session.endsAt) : ''"
    >
      <div v-if="session" class="flex items-center gap-2">
        <StatusBadge entity="session" :status="session.status" />
        <template v-if="isLiveSession(session.status)">
          <!-- Granular backend permissions:
               session:reschedule, session:complete, session:cancel
               (see src/Aynesil.Api/Controllers/SchedulingController.cs). -->
          <button
            v-if="can(Permissions.Sessions.Reschedule)"
            @click="router.push({ name: 'session-edit', params: { id: sessionId } })"
            class="px-3 py-1.5 border border-border rounded-lg text-sm hover:bg-accent"
          >{{ t('common.edit') }}</button>
          <button
            v-if="can(Permissions.Sessions.Complete)"
            @click="completeModal = true"
            class="px-3 py-1.5 bg-green-600 text-white rounded-lg text-sm hover:bg-green-700"
          >{{ t('scheduling.session.complete') }}</button>
          <button
            v-if="can(Permissions.Sessions.Cancel)"
            @click="cancelModal = true"
            class="px-3 py-1.5 bg-red-50 text-red-600 border border-red-200 rounded-lg text-sm hover:bg-red-100"
          >{{ t('scheduling.session.cancel') }}</button>
        </template>
        <button @click="router.back()" class="px-3 py-1.5 border border-border rounded-lg text-sm hover:bg-accent">
          {{ t('common.back') }}
        </button>
      </div>
    </PageHeader>

    <div v-if="store.loading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-20 rounded-xl bg-accent animate-pulse" />
    </div>

    <div v-else-if="!session" class="text-center py-16 text-muted-foreground">
      {{ t('errors.notFound') }}
    </div>

    <template v-else>
      <!-- Tabs -->
      <div class="flex gap-1 mb-6 border-b border-border">
        <button
          v-for="tab in ['overview', 'participants', 'attendance', 'notes', 'goals']"
          :key="tab"
          @click="activeTab = tab as typeof activeTab"
          :class="[
            'px-4 py-2 text-sm font-medium transition-colors border-b-2 -mb-px',
            activeTab === tab
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          ]"
        >
          {{ t(`scheduling.session.tab.${tab}`) }}
        </button>
      </div>

      <!-- Overview Tab -->
      <div v-if="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="rounded-xl border border-border bg-card shadow-sm p-5 space-y-3">
          <h3 class="font-semibold text-foreground">{{ t('scheduling.session.info') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex gap-2"><dt class="text-muted-foreground w-32 shrink-0">{{ t('scheduling.session.type') }}:</dt><dd>{{ session.sessionTypeName ?? '—' }}</dd></div>
            <div class="flex gap-2"><dt class="text-muted-foreground w-32 shrink-0">{{ t('scheduling.session.room') }}:</dt><dd>{{ session.roomName ?? t('scheduling.session.noRoom') }}</dd></div>
            <div class="flex gap-2"><dt class="text-muted-foreground w-32 shrink-0">{{ t('scheduling.session.campus') }}:</dt><dd>{{ session.campusName ?? '—' }}</dd></div>
            <div class="flex gap-2"><dt class="text-muted-foreground w-32 shrink-0">{{ t('scheduling.session.startsAt') }}:</dt><dd>{{ formatDateTime(session.startsAt) }}</dd></div>
            <div class="flex gap-2"><dt class="text-muted-foreground w-32 shrink-0">{{ t('scheduling.session.endsAt') }}:</dt><dd>{{ formatDateTime(session.endsAt) }}</dd></div>
            <div class="flex gap-2">
              <dt class="text-muted-foreground w-32 shrink-0">{{ t('scheduling.session.isMakeup') }}:</dt>
              <dd>
                <span v-if="session.isMakeup" class="px-1.5 py-0.5 rounded text-xs bg-amber-100 text-amber-700">{{ t('scheduling.session.makeup') }}</span>
                <span v-else class="text-muted-foreground">{{ t('common.no') }}</span>
              </dd>
            </div>
          </dl>
        </div>

        <div class="rounded-xl border border-border bg-card shadow-sm p-5">
          <h3 class="font-semibold text-foreground mb-3">{{ t('scheduling.session.participants') }}</h3>
          <div v-if="session.participants.length === 0" class="text-sm text-muted-foreground">{{ t('scheduling.session.noParticipants') }}</div>
          <ul v-else class="space-y-1">
            <li v-for="p in session.participants" :key="p.studentId" class="flex items-center justify-between text-sm">
              <span>{{ p.studentFullName }}</span>
              <span v-if="session.status === SessionStatus.Completed && p.creditsConsumed > 0" class="text-xs px-2 py-0.5 rounded-full bg-green-100 text-green-700">
                {{ t('scheduling.session.creditConsumed', { n: p.creditsConsumed }) }}
              </span>
              <span
                v-else-if="session.status === SessionStatus.Completed"
                class="text-xs px-2 py-0.5 rounded-full bg-amber-100 text-amber-700 cursor-help"
                :title="t('scheduling.session.creditNotConsumedHint')"
              >
                {{ t('scheduling.session.creditNotConsumed') }}
              </span>
              <span v-else class="text-xs text-muted-foreground">{{ p.role }}</span>
            </li>
          </ul>
          <h3 class="font-semibold text-foreground mb-3 mt-5">{{ t('scheduling.session.educators') }}</h3>
          <div v-if="session.educators.length === 0" class="text-sm text-muted-foreground">{{ t('scheduling.session.noEducators') }}</div>
          <ul v-else class="space-y-1">
            <li v-for="e in session.educators" :key="e.educatorId" class="flex items-center justify-between text-sm">
              <span>{{ e.educatorFullName }}</span>
              <span class="text-xs text-muted-foreground">{{ e.role }}</span>
            </li>
          </ul>
        </div>
      </div>

      <!-- Participants Tab -->
      <div v-if="activeTab === 'participants'" class="space-y-6">
        <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
          <div class="flex items-center justify-between p-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('scheduling.session.participants') }}</h3>
            <div v-if="can(Permissions.Sessions.ManageParticipants) && isLiveSession(session.status)" class="flex gap-2 items-center">
              <div class="w-64">
                <StudentPicker v-model="addParticipantId" size="sm" />
              </div>
              <button
                @click="store.addParticipant(sessionId, { studentId: addParticipantId }).then(() => { addParticipantId = '' })"
                :disabled="!addParticipantId"
                class="px-3 h-9 text-xs bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
              >{{ t('common.add') }}</button>
            </div>
          </div>
          <div class="divide-y divide-border">
            <div v-if="session.participants.length === 0" class="py-8 text-center text-muted-foreground text-sm">
              {{ t('scheduling.session.noParticipants') }}
            </div>
            <div
              v-for="p in session.participants"
              :key="p.studentId"
              class="flex items-center justify-between px-4 py-3"
            >
              <div>
                <p class="text-sm font-medium text-foreground">{{ p.studentFullName }}</p>
                <p class="text-xs text-muted-foreground">{{ p.role }}</p>
              </div>
              <button
                v-if="can(Permissions.Sessions.ManageParticipants) && isLiveSession(session.status)"
                @click="removeParticipant(p.studentId)"
                class="text-xs text-red-600 hover:underline"
              >{{ t('common.delete') }}</button>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
          <div class="flex items-center justify-between p-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('scheduling.session.educators') }}</h3>
            <div v-if="can(Permissions.Sessions.ManageEducators) && isLiveSession(session.status)" class="flex gap-2 items-center">
              <div class="w-64">
                <EducatorPicker v-model="addEducatorId" size="sm" />
              </div>
              <button
                @click="store.addEducator(sessionId, { educatorId: addEducatorId }).then(() => { addEducatorId = '' })"
                :disabled="!addEducatorId"
                class="px-3 h-9 text-xs bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
              >{{ t('common.add') }}</button>
            </div>
          </div>
          <div class="divide-y divide-border">
            <div v-if="session.educators.length === 0" class="py-8 text-center text-muted-foreground text-sm">
              {{ t('scheduling.session.noEducators') }}
            </div>
            <div
              v-for="e in session.educators"
              :key="e.educatorId"
              class="flex items-center justify-between px-4 py-3"
            >
              <div>
                <p class="text-sm font-medium text-foreground">{{ e.educatorFullName }}</p>
                <p class="text-xs text-muted-foreground">{{ e.role }}</p>
              </div>
              <button
                v-if="can(Permissions.Sessions.ManageEducators) && isLiveSession(session.status)"
                @click="removeEducator(e.educatorId)"
                class="text-xs text-red-600 hover:underline"
              >{{ t('common.delete') }}</button>
            </div>
          </div>
        </div>
      </div>

      <!-- Attendance Tab -->
      <div v-if="activeTab === 'attendance'" class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
        <div class="p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('scheduling.attendance.title') }}</h3>
        </div>
        <div v-if="session.participants.length === 0" class="py-8 text-center text-muted-foreground text-sm">
          {{ t('scheduling.session.noParticipants') }}
        </div>
        <table v-else class="w-full text-sm">
          <thead>
            <tr class="border-b border-border bg-accent/50">
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.session.student') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.attendance.status.label') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase">{{ t('scheduling.attendance.reason') }}</th>
              <th class="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase" v-if="can(Permissions.Attendance.Record)">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="p in session.participants"
              :key="p.studentId"
              class="border-b border-border last:border-0"
            >
              <td class="px-4 py-3 font-medium">{{ p.studentFullName }}</td>
              <td class="px-4 py-3">
                <StatusBadge
                  v-if="getAttendance(p.studentId)"
                  entity="attendance"
                  :status="getAttendance(p.studentId)!.status"
                />
                <span v-else class="text-muted-foreground text-xs">{{ t('scheduling.attendance.notRecorded') }}</span>
              </td>
              <td class="px-4 py-3 text-muted-foreground text-xs">
                {{ getAttendance(p.studentId)?.reasonLabel ?? '—' }}
              </td>
              <td class="px-4 py-3" v-if="can(Permissions.Attendance.Record)">
                <div class="flex gap-1">
                  <button
                    @click="doRecordAttendance(p.studentId, 'present')"
                    class="px-2 py-0.5 rounded text-xs bg-green-50 text-green-700 border border-green-200 hover:bg-green-100"
                  >{{ t('scheduling.attendance.present') }}</button>
                  <button
                    @click="doRecordAttendance(p.studentId, 'absent')"
                    class="px-2 py-0.5 rounded text-xs bg-red-50 text-red-700 border border-red-200 hover:bg-red-100"
                  >{{ t('scheduling.attendance.absent') }}</button>
                  <button
                    @click="doRecordAttendance(p.studentId, 'late')"
                    class="px-2 py-0.5 rounded text-xs bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100"
                  >{{ t('scheduling.attendance.late') }}</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Notes Tab -->
      <div v-if="activeTab === 'notes'">
        <div class="flex justify-end mb-4">
          <button
            v-if="can(Permissions.SessionNotes.Write)"
            @click="openAddNote"
            class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm hover:opacity-90"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ t('scheduling.session.addNote') }}
          </button>
        </div>

        <div v-if="session.notes.length === 0" class="py-10 text-center text-muted-foreground text-sm">
          {{ t('scheduling.session.noNotes') }}
        </div>

        <div v-else class="space-y-3">
          <div
            v-for="note in session.notes"
            :key="note.id"
            class="rounded-xl border border-border bg-card shadow-sm p-4"
          >
            <div class="flex items-start justify-between gap-4">
              <p class="text-sm text-foreground flex-1">{{ note.body }}</p>
              <button
                v-if="can(Permissions.SessionNotes.Write)"
                @click="openEditNote(note)"
                class="shrink-0 text-muted-foreground hover:text-foreground transition-colors"
                :title="t('common.edit')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button
                v-if="can(Permissions.SessionNotes.Delete)"
                @click="deleteNote(note.id)"
                class="shrink-0 text-muted-foreground hover:text-red-600 transition-colors"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
            <div class="mt-2 flex items-center gap-3 text-xs text-muted-foreground">
              <span>{{ note.authorName ?? '—' }}</span>
              <span>{{ new Date(note.createdAt).toLocaleString('tr-TR') }}</span>
              <span
                v-if="isEdited(note)"
                class="px-1.5 py-0.5 rounded bg-amber-100 text-amber-700"
                :title="t('scheduling.session.noteEditedAt', { at: new Date(note.editedAt!).toLocaleString('tr-TR') })"
              >
                {{ t('scheduling.session.noteEdited') }}
              </span>
              <span v-if="note.studentName" class="px-1.5 py-0.5 rounded bg-gray-100 text-gray-700">
                {{ note.studentName }}
              </span>
              <span v-if="note.parentVisible" class="px-1.5 py-0.5 rounded bg-blue-100 text-blue-700">
                {{ t('scheduling.session.parentVisible') }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Goals Tab -->
      <div v-if="activeTab === 'goals'">
        <div v-if="can(Permissions.Sessions.ManageGoals)" class="rounded-xl border border-border bg-card shadow-sm p-4 mb-4">
          <div v-if="session.participants.length === 0" class="text-sm text-muted-foreground">
            {{ t('scheduling.session.goalNeedsParticipant') }}
          </div>
          <div v-else-if="goalsLoaded && availableGoals.length === 0" class="text-sm text-muted-foreground">
            {{ t('scheduling.session.noAvailableGoals') }}
            <RouterLink
              v-if="session.participants.length === 1"
              :to="{ name: 'student-goal-new', query: { studentId: session.participants[0].studentId } }"
              class="text-primary hover:underline ml-1"
            >{{ t('scheduling.session.createStudentGoal') }}</RouterLink>
          </div>
          <div v-else class="flex flex-wrap gap-2 items-center">
            <select
              v-model="addGoalId"
              class="flex-1 min-w-0 h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('scheduling.session.selectGoal') }}</option>
              <option v-for="g in availableGoals" :key="g.id" :value="g.id">
                {{ showStudentNameInGoals ? `${g.studentFullName} · ` : '' }}{{ g.statement }}
              </option>
            </select>
            <button
              @click="addGoalToSession"
              :disabled="!addGoalId || store.saving"
              class="px-3 h-9 text-xs bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
            >{{ t('scheduling.session.addGoal') }}</button>
          </div>
        </div>
        <p v-if="goalError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ goalError }}</p>
        <div v-if="session.goals.length === 0" class="py-10 text-center text-muted-foreground text-sm">
          {{ t('scheduling.session.noGoals') }}
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="goal in session.goals"
            :key="goal.studentGoalId"
            class="rounded-xl border border-border bg-card shadow-sm p-4"
          >
            <div class="flex items-center gap-3 mb-2">
              <input
                type="checkbox"
                :checked="goal.workedOn"
                class="w-4 h-4 rounded border-border text-primary"
                :disabled="!can(Permissions.Sessions.ManageGoals)"
                @change="store.updateSessionGoal(sessionId, goal.studentGoalId, { workedOn: !goal.workedOn, progressNote: goal.progressNote, measuredValue: goal.measuredValue })"
              />
              <p class="text-sm font-medium text-foreground flex-1">{{ goal.goalStatement }}</p>
              <button
                v-if="can(Permissions.Sessions.ManageGoals)"
                @click="removeGoalTarget = { studentGoalId: goal.studentGoalId, goalStatement: goal.goalStatement }"
                class="shrink-0 text-muted-foreground hover:text-red-600 transition-colors"
                :title="t('scheduling.session.removeGoal')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div v-if="goal.progressNote" class="text-sm text-muted-foreground ml-7">{{ goal.progressNote }}</div>
            <div v-if="goal.measuredValue !== null && goal.measuredValue !== undefined" class="text-xs text-muted-foreground ml-7 mt-1">
              {{ t('scheduling.session.measuredValue') }}: {{ goal.measuredValue }}
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Cancel Modal -->
    <div v-if="cancelModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-card rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-4">{{ t('scheduling.session.cancelTitle') }}</h3>
        <textarea
          v-model="cancelReason"
          :placeholder="t('scheduling.session.cancelReason')"
          rows="3"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary resize-none"
        />
        <div class="flex justify-end gap-2 mt-4">
          <button @click="cancelModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doCancel" :disabled="store.saving" class="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('scheduling.session.cancel') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Complete Modal -->
    <ConfirmModal
      :open="completeModal"
      :title="t('scheduling.session.completeTitle')"
      :message="t('scheduling.session.completeMessage')"
      :confirm-label="t('scheduling.session.complete')"
      :loading="store.saving"
      @confirm="doComplete"
      @cancel="completeModal = false"
    />

    <ConfirmModal
      :open="!!removeGoalTarget"
      :title="t('scheduling.session.removeGoalTitle')"
      :message="t('scheduling.session.removeGoalMessage', { goal: removeGoalTarget?.goalStatement })"
      :confirm-label="t('scheduling.session.removeGoal')"
      :loading="store.saving"
      @confirm="doRemoveGoal"
      @cancel="removeGoalTarget = null"
    />

    <!-- Note Modal -->
    <div v-if="noteModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div class="bg-card rounded-xl shadow-xl p-6 w-full max-w-md border border-border">
        <h3 class="font-semibold text-foreground mb-4">{{ editingNote ? t('scheduling.session.editNote') : t('scheduling.session.addNote') }}</h3>
        <p v-if="noteError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ noteError }}</p>
        <textarea
          v-model="newNoteBody"
          :placeholder="t('scheduling.session.notePlaceholder')"
          rows="4"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary resize-none"
        />
        <label class="flex items-center gap-2 mt-3 text-sm">
          <input type="checkbox" v-model="newNoteParentVisible" class="rounded" />
          {{ t('scheduling.session.parentVisible') }}
        </label>
        <div v-if="isGroupSession" class="mt-3">
          <label class="block text-sm mb-1">
            {{ t('scheduling.session.noteStudent') }}<span v-if="newNoteParentVisible" class="text-red-500"> *</span>
          </label>
          <select v-model="newNoteStudentId" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('scheduling.session.noteStudentNone') }}</option>
            <option v-for="p in session?.participants ?? []" :key="p.studentId" :value="p.studentId">{{ p.studentFullName }}</option>
          </select>
          <p class="mt-1 text-xs text-muted-foreground">{{ t('scheduling.session.noteStudentHint') }}</p>
        </div>
        <div class="flex justify-end gap-2 mt-4">
          <button @click="noteModal = false" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="doSaveNote" :disabled="!newNoteBody.trim() || store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
