<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSchedulingStore } from '@/stores/scheduling.store'
import { useStudentStore } from '@/stores/student.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { campusService } from '@/services/campus.service'
import { schedulingService } from '@/services/scheduling.service'
import { programService, studentProgramService } from '@/services/program.service'
import { educatorService } from '@/services/educator.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'
import type { CampusListItemDto } from '@/types/campus.types'
import type { StudentListItemDto } from '@/types/student.types'
import type { StudentProgramListItemDto, ProgramServiceDto } from '@/types/program.types'
import type { EducatorListItemDto } from '@/types/educator.types'
import type {
  RoomListItemDto,
  RecurrenceFrequency,
  EducatorRole,
  RecurringScheduleEducator,
} from '@/types/scheduling.types'

const { t } = useI18n()
const router = useRouter()
const store = useSchedulingStore()
const studentStore = useStudentStore()
const refData = useRefDataStore()

const sessionTypes = ref<RefValueItem[]>([])
const campuses = ref<CampusListItemDto[]>([])
const students = ref<StudentListItemDto[]>([])
const studentPrograms = ref<StudentProgramListItemDto[]>([])
const programServices = ref<ProgramServiceDto[]>([])
const rooms = ref<RoomListItemDto[]>([])
const educators = ref<EducatorListItemDto[]>([])

const SUPPORT_ROLES: EducatorRole[] = ['assistant', 'observer', 'supervisor']

// Displayed Monday-first; values follow the API convention (0 = Sunday … 6 = Saturday).
const WEEKDAYS: { key: string; value: number }[] = [
  { key: 'monday', value: 1 },
  { key: 'tuesday', value: 2 },
  { key: 'wednesday', value: 3 },
  { key: 'thursday', value: 4 },
  { key: 'friday', value: 5 },
  { key: 'saturday', value: 6 },
  { key: 'sunday', value: 0 },
]

const form = reactive({
  studentId: '',
  studentProgramId: '',
  programServiceId: '',
  leadEducatorId: '',
  supportEducators: [] as RecurringScheduleEducator[],
  campusId: '',
  roomId: '',
  sessionTypeId: '',
  frequency: 'weekly' as RecurrenceFrequency,
  intervalCount: 1,
  byWeekday: [] as number[],
  startTime: '09:00',
  durationMinutes: 45,
  rangeStart: '',
  rangeEnd: '',
  maxOccurrences: '' as string | number,
})

const errors = reactive<Record<string, string>>({})

const usesWeekdays = computed(() => form.frequency === 'weekly' || form.frequency === 'biweekly')

onMounted(async () => {
  await Promise.all([
    refData.getValues('session_type').then((v) => { sessionTypes.value = v }),
    campusService.list({ page: 1, pageSize: 200 }).then((res) => {
      if (res.success && res.data) campuses.value = res.data.items
    }),
    studentStore.fetchList({ page: 1, pageSize: 200 }).then(() => {
      students.value = studentStore.list.items
    }),
    educatorService.listEducators({ isActive: true, page: 1, pageSize: 200 }).then((res) => {
      if (res.success && res.data) educators.value = res.data.items
    }),
    loadRooms(),
  ])
})

watch(() => form.studentId, async (studentId) => {
  form.studentProgramId = ''
  studentPrograms.value = []
  if (!studentId) return
  const res = await studentProgramService.listStudentPrograms({
    studentId,
    status: 'active',
    page: 1,
    pageSize: 50,
  })
  if (res.success && res.data) studentPrograms.value = res.data.items
  if (studentPrograms.value.length === 1) form.studentProgramId = studentPrograms.value[0].id
})

watch(() => form.studentProgramId, async (spId) => {
  form.programServiceId = ''
  programServices.value = []
  const sp = studentPrograms.value.find((p) => p.id === spId)
  if (!sp) return
  if (sp.campusId && !form.campusId) form.campusId = sp.campusId
  const res = await programService.getProgram(sp.programId)
  if (res.success && res.data) {
    programServices.value = [...res.data.services].sort((a, b) => a.sortOrder - b.sortOrder)
  }
  if (programServices.value.length === 1) form.programServiceId = programServices.value[0].id
})

watch(() => form.programServiceId, (serviceId) => {
  const service = programServices.value.find((s) => s.id === serviceId)
  if (service?.defaultDurationMinutes) form.durationMinutes = service.defaultDurationMinutes
})

function addSupportEducator() {
  form.supportEducators.push({ educatorId: '', role: 'assistant' })
}

function removeSupportEducator(index: number) {
  form.supportEducators.splice(index, 1)
}

function buildEducatorAssignments(): RecurringScheduleEducator[] {
  const assignments: RecurringScheduleEducator[] = []
  if (form.leadEducatorId) assignments.push({ educatorId: form.leadEducatorId, role: 'lead' })
  for (const e of form.supportEducators) {
    if (e.educatorId) assignments.push({ educatorId: e.educatorId, role: e.role })
  }
  return assignments
}

watch(() => form.campusId, async () => {
  form.roomId = ''
  await loadRooms()
})

async function loadRooms() {
  const res = await schedulingService.listRooms({
    campusId: form.campusId || undefined,
    isActive: true,
    page: 1,
    pageSize: 200,
  })
  rooms.value = res.success && res.data ? res.data.items : []
}

function toggleWeekday(value: number) {
  const idx = form.byWeekday.indexOf(value)
  if (idx >= 0) form.byWeekday.splice(idx, 1)
  else form.byWeekday.push(value)
}

function validate(): boolean {
  Object.keys(errors).forEach((k) => delete errors[k])
  if (form.studentId && !form.studentProgramId) {
    errors.studentProgramId = t('validation.required', { field: t('scheduling.recurring.studentProgram') })
  }
  if (!form.sessionTypeId) errors.sessionTypeId = t('validation.required', { field: t('scheduling.session.type') })
  if (!form.startTime) errors.startTime = t('validation.required', { field: t('scheduling.recurring.startTime') })
  if (!form.rangeStart) errors.rangeStart = t('validation.required', { field: t('scheduling.recurring.from') })
  if (form.rangeEnd && form.rangeStart && form.rangeEnd <= form.rangeStart) {
    errors.rangeEnd = t('scheduling.recurring.rangeEndAfterStart')
  }
  if (usesWeekdays.value && form.byWeekday.length === 0) errors.byWeekday = t('scheduling.recurring.selectDay')
  const educatorIds = buildEducatorAssignments().map((e) => e.educatorId)
  if (new Set(educatorIds).size !== educatorIds.length) errors.educators = t('scheduling.recurring.duplicateEducator')
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    await store.createRecurringSchedule({
      campusId: form.campusId || undefined,
      studentProgramId: form.studentProgramId || undefined,
      programServiceId: form.programServiceId || undefined,
      educators: buildEducatorAssignments(),
      roomId: form.roomId || undefined,
      sessionTypeId: form.sessionTypeId,
      frequency: form.frequency,
      intervalCount: form.frequency === 'weekly' ? form.intervalCount : 1,
      byWeekday: usesWeekdays.value ? [...form.byWeekday].sort() : undefined,
      startTime: form.startTime.length === 5 ? `${form.startTime}:00` : form.startTime,
      durationMinutes: form.durationMinutes,
      rangeStart: form.rangeStart,
      rangeEnd: form.rangeEnd || undefined,
      maxOccurrences: form.maxOccurrences ? Number(form.maxOccurrences) : undefined,
    })
    router.push({ name: 'recurring-schedules' })
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div>
    <PageHeader
      :title="t('scheduling.recurring.create')"
      :description="t('scheduling.recurring.createDescription')"
    />

    <div class="max-w-2xl space-y-6">
      <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
        {{ errors.submit }}
      </div>

      <!-- Who -->
      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">
        <div>
          <h3 class="font-semibold text-foreground">{{ t('scheduling.recurring.sectionWho') }}</h3>
          <p class="text-xs text-muted-foreground mt-1">{{ t('scheduling.recurring.sectionWhoHint') }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.session.student') }}</label>
            <select v-model="form.studentId" :class="[inputClass, 'border-border']">
              <option value="">{{ t('common.none') }}</option>
              <option v-for="s in students" :key="s.id" :value="s.id">{{ s.fullName }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('scheduling.recurring.studentProgram') }}
              <span v-if="form.studentId" class="text-red-500">*</span>
            </label>
            <select
              v-model="form.studentProgramId"
              :disabled="!form.studentId"
              :class="[inputClass, errors.studentProgramId ? 'border-red-400' : 'border-border', !form.studentId ? 'opacity-50' : '']"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="sp in studentPrograms" :key="sp.id" :value="sp.id">{{ sp.programName }}</option>
            </select>
            <p v-if="errors.studentProgramId" class="text-xs text-red-500 mt-1">{{ errors.studentProgramId }}</p>
            <p v-else-if="form.studentId && studentPrograms.length === 0" class="text-xs text-amber-600 mt-1">
              {{ t('scheduling.recurring.noActivePrograms') }}
            </p>
          </div>
        </div>

        <div v-if="form.studentProgramId">
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.programService') }}</label>
          <select v-model="form.programServiceId" :class="[inputClass, 'border-border']">
            <option value="">{{ t('common.none') }}</option>
            <option v-for="s in programServices" :key="s.id" :value="s.id">
              {{ s.name }}<template v-if="s.defaultDurationMinutes"> · {{ s.defaultDurationMinutes }} dk</template>
            </option>
          </select>
          <p v-if="programServices.length === 0" class="text-xs text-muted-foreground mt-1">
            {{ t('scheduling.recurring.noProgramServices') }}
          </p>
        </div>
      </div>

      <!-- Educators -->
      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">
        <div>
          <h3 class="font-semibold text-foreground">{{ t('scheduling.recurring.sectionEducators') }}</h3>
          <p class="text-xs text-muted-foreground mt-1">{{ t('scheduling.recurring.sectionEducatorsHint') }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.leadEducator') }}</label>
          <select v-model="form.leadEducatorId" :class="[inputClass, 'border-border']">
            <option value="">{{ t('common.none') }}</option>
            <option v-for="e in educators" :key="e.id" :value="e.id">{{ e.fullName }}</option>
          </select>
        </div>

        <div v-for="(support, idx) in form.supportEducators" :key="idx" class="flex items-end gap-3">
          <div class="flex-1">
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.supportEducator') }}</label>
            <select v-model="support.educatorId" :class="[inputClass, 'border-border']">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="e in educators" :key="e.id" :value="e.id">{{ e.fullName }}</option>
            </select>
          </div>
          <div class="w-40">
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.educatorRole') }}</label>
            <select v-model="support.role" :class="[inputClass, 'border-border']">
              <option v-for="r in SUPPORT_ROLES" :key="r" :value="r">{{ t(`scheduling.recurring.role.${r}`) }}</option>
            </select>
          </div>
          <button
            type="button"
            @click="removeSupportEducator(idx)"
            class="h-9 px-3 text-sm border border-border rounded-lg hover:bg-accent text-muted-foreground"
          >{{ t('common.delete') }}</button>
        </div>

        <button
          type="button"
          @click="addSupportEducator"
          class="text-sm text-primary hover:underline"
        >+ {{ t('scheduling.recurring.addSupportEducator') }}</button>

        <p v-if="errors.educators" class="text-xs text-red-500">{{ errors.educators }}</p>
      </div>

      <!-- Where & what -->
      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">
        <h3 class="font-semibold text-foreground">{{ t('scheduling.recurring.sectionWhere') }}</h3>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.session.campus') }}</label>
            <select v-model="form.campusId" :class="[inputClass, 'border-border']">
              <option value="">{{ t('common.none') }}</option>
              <option v-for="c in campuses" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.session.room') }}</label>
            <select v-model="form.roomId" :class="[inputClass, 'border-border']">
              <option value="">{{ t('scheduling.session.noRoom') }}</option>
              <option v-for="r in rooms" :key="r.id" :value="r.id">{{ r.name }} ({{ r.code }})</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.type') }} <span class="text-red-500">*</span>
          </label>
          <select v-model="form.sessionTypeId" :class="[inputClass, errors.sessionTypeId ? 'border-red-400' : 'border-border']">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="st in sessionTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
          </select>
          <p v-if="errors.sessionTypeId" class="text-xs text-red-500 mt-1">{{ errors.sessionTypeId }}</p>
        </div>
      </div>

      <!-- When -->
      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">
        <h3 class="font-semibold text-foreground">{{ t('scheduling.recurring.sectionWhen') }}</h3>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.frequency.label') }} <span class="text-red-500">*</span></label>
            <select v-model="form.frequency" :class="[inputClass, 'border-border']">
              <option value="weekly">{{ t('scheduling.recurring.frequency.weekly') }}</option>
              <option value="biweekly">{{ t('scheduling.recurring.frequency.biweekly') }}</option>
              <option value="monthly">{{ t('scheduling.recurring.frequency.monthly') }}</option>
            </select>
          </div>
          <div v-if="form.frequency === 'weekly'">
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.interval') }}</label>
            <input v-model.number="form.intervalCount" type="number" min="1" :class="[inputClass, 'border-border']" />
          </div>
        </div>

        <div v-if="usesWeekdays">
          <label class="block text-sm font-medium text-foreground mb-2">{{ t('scheduling.recurring.weekdays') }} <span class="text-red-500">*</span></label>
          <div class="flex gap-1 flex-wrap">
            <button
              v-for="day in WEEKDAYS"
              :key="day.key"
              type="button"
              @click="toggleWeekday(day.value)"
              :class="[
                'w-9 h-9 rounded-lg text-xs font-medium border transition-colors',
                form.byWeekday.includes(day.value)
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-border text-muted-foreground hover:bg-accent'
              ]"
            >
              {{ t(`scheduling.recurring.weekday.${day.key}`).slice(0, 2) }}
            </button>
          </div>
          <p v-if="errors.byWeekday" class="text-xs text-red-500 mt-1">{{ errors.byWeekday }}</p>
        </div>
        <p v-else class="text-xs text-muted-foreground">{{ t('scheduling.recurring.monthlyHint') }}</p>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.startTime') }} <span class="text-red-500">*</span></label>
            <input v-model="form.startTime" type="time" :class="[inputClass, errors.startTime ? 'border-red-400' : 'border-border']" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.duration') }}</label>
            <input v-model.number="form.durationMinutes" type="number" min="15" step="15" :class="[inputClass, 'border-border']" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.from') }} <span class="text-red-500">*</span></label>
            <input v-model="form.rangeStart" type="date" :class="[inputClass, errors.rangeStart ? 'border-red-400' : 'border-border']" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.to') }}</label>
            <input v-model="form.rangeEnd" type="date" :class="[inputClass, errors.rangeEnd ? 'border-red-400' : 'border-border']" />
            <p v-if="errors.rangeEnd" class="text-xs text-red-500 mt-1">{{ errors.rangeEnd }}</p>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.maxOccurrences') }}</label>
          <input v-model="form.maxOccurrences" type="number" min="1" :placeholder="t('scheduling.recurring.maxOccurrencesHint')" :class="[inputClass, 'border-border']" />
        </div>
      </div>

      <div class="flex justify-end gap-3">
        <button @click="router.back()" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
        <button @click="submit" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
          {{ store.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </div>
  </div>
</template>
