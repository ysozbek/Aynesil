<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { campusService } from '@/services/campus.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import { localInputToIso, isoToLocalInput } from '@/utils/datetime'
import RoomPicker from '@/components/pickers/RoomPicker.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import EducatorPicker from '@/components/pickers/EducatorPicker.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type { CampusListItemDto } from '@/types/campus.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useSessionStore()
const refData = useRefDataStore()

const sessionId = route.params.id as string | undefined
const isEdit = !!sessionId

const sessionTypes = ref<RefValueItem[]>([])
const campuses = ref<CampusListItemDto[]>([])

const form = reactive({
  campusId: '',
  roomId: '',
  sessionTypeId: '',
  title: '',
  startsAt: '',
  endsAt: '',
  studentId: '',
  educatorId: '',
})

const errors = reactive<Record<string, string>>({})

watch(() => form.campusId, (_, previous) => {
  if (previous !== undefined && previous !== '') form.roomId = ''
})

onMounted(async () => {
  await Promise.all([
    refData.getValues('SESSION_TYPE').then(v => { sessionTypes.value = v }),
    campusService.list({ page: 1, pageSize: 200 }).then((res) => {
      if (res.success && res.data) campuses.value = res.data.items
    }),
  ])
  if (isEdit && sessionId) {
    await store.fetchSession(sessionId)
    const s = store.currentSession
    if (s) {
      form.campusId = s.campusId ?? ''
      form.roomId = s.roomId ?? ''
      form.sessionTypeId = s.sessionTypeId
      form.title = s.title ?? ''
      form.startsAt = isoToLocalInput(s.startsAt)
      form.endsAt = isoToLocalInput(s.endsAt)
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete (errors as Record<string, string>)[k])
  if (!form.title.trim()) errors.title = t('validation.required', { field: t('scheduling.session.titleField') })
  if (!form.sessionTypeId) errors.sessionTypeId = t('validation.required', { field: t('scheduling.session.type') })
  if (!form.startsAt) errors.startsAt = t('validation.required', { field: t('scheduling.session.startsAt') })
  if (!form.endsAt) errors.endsAt = t('validation.required', { field: t('scheduling.session.endsAt') })
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    if (isEdit && sessionId) {
      // Backend `RescheduleSessionRequest` expects `NewStartsAt` / `NewEndsAt`
      // and `RowVersion` as int — see src/Aynesil.Api/Controllers/SchedulingController.cs
      // and src/Aynesil.Application/Features/Scheduling/Commands/SessionCommands.cs.
      await store.rescheduleSession(sessionId, {
        newStartsAt: localInputToIso(form.startsAt),
        newEndsAt: localInputToIso(form.endsAt),
        roomId: form.roomId || undefined,
        rowVersion: store.currentSession!.rowVersion,
      })
      router.push({ name: 'session-detail', params: { id: sessionId } })
    } else {
      const created = await store.createSession({
        campusId: form.campusId || undefined,
        roomId: form.roomId || undefined,
        sessionTypeId: form.sessionTypeId,
        title: form.title,
        startsAt: localInputToIso(form.startsAt),
        endsAt: localInputToIso(form.endsAt),
        isMakeup: false,
        participantStudentIds: form.studentId ? [form.studentId] : [],
        educatorAssignments: form.educatorId ? [{ educatorId: form.educatorId, role: 'lead' }] : [],
      })
      router.push({ name: 'session-detail', params: { id: created.id } })
    }
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="isEdit ? t('scheduling.session.edit') : t('scheduling.session.create')"
      :description="isEdit ? t('scheduling.session.editDescription') : t('scheduling.session.createDescription')"
    />

    <div class="max-w-2xl">
      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">

        <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
          {{ errors.submit }}
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.titleField') }} <span class="text-red-500">*</span>
          </label>
          <input
            v-model="form.title"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.title ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.title" class="text-xs text-red-500 mt-1">{{ errors.title }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.type') }} <span class="text-red-500">*</span>
          </label>
          <select
            v-model="form.sessionTypeId"
            class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.sessionTypeId ? 'border-red-400' : 'border-border'"
          >
            <option value="">{{ t('common.select') }}</option>
            <option v-for="st in sessionTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
          </select>
          <p v-if="errors.sessionTypeId" class="text-xs text-red-500 mt-1">{{ errors.sessionTypeId }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('scheduling.session.startsAt') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.startsAt"
              type="datetime-local"
              class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.startsAt ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.startsAt" class="text-xs text-red-500 mt-1">{{ errors.startsAt }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('scheduling.session.endsAt') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.endsAt"
              type="datetime-local"
              class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.endsAt ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.endsAt" class="text-xs text-red-500 mt-1">{{ errors.endsAt }}</p>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('campus.name') }}</label>
            <select
              v-model="form.campusId"
              :disabled="isEdit"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
            >
              <option value="">{{ t('common.allCampuses') }}</option>
              <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.session.room') }}</label>
            <RoomPicker v-model="form.roomId" :campus-id="form.campusId" />
          </div>
        </div>

        <div v-if="!isEdit" class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.session.student') }}</label>
            <StudentPicker v-model="form.studentId" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.leadEducator') }}</label>
            <EducatorPicker v-model="form.educatorId" />
          </div>
          <p class="col-span-2 text-xs text-muted-foreground -mt-2">{{ t('scheduling.session.participantsLaterHint') }}</p>
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button
            @click="router.back()"
            class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent"
          >{{ t('common.cancel') }}</button>
          <button
            @click="submit"
            :disabled="store.saving"
            class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
          >
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
