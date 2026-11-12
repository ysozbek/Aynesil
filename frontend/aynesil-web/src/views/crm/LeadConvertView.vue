<script setup lang="ts">
/**
 * Lead Conversion screen.
 *
 * Two ways to convert a lead:
 *  - "new":      create the student (and optionally the guardian) from the lead's
 *                data, then mark the lead converted, all in one step.
 *  - "existing": link the lead to a student record that already exists.
 *
 * Uses existing endpoints only:
 *   POST /api/students                    (leadId set to this lead)
 *   POST /api/guardians                   (optional)
 *   POST /api/students/{id}/guardians     (optional, links guardian)
 *   POST /api/leads/{id}/convert          { studentId, rowVersion }
 *
 * The calls are not atomic. If a later step fails, the student that was
 * already created is kept and the form switches to "existing" with that
 * student pre-selected, so retrying never creates a duplicate.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCrmLabels } from '@/composables/useCrmLabels'
import { useLeadStore } from '@/stores/lead.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import { studentService, guardianService } from '@/services/student.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import StudentPicker from '@/components/pickers/StudentPicker.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const crm = useCrmLabels()
const route = useRoute()
const router = useRouter()
const store = useLeadStore()
const refData = useRefDataStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)
const lead = computed(() => store.current)

const canCreateStudent = computed(() => can('student:create'))
const canCreateGuardian = computed(() => can('guardian:create'))

const mode = ref<'new' | 'existing'>(canCreateStudent.value ? 'new' : 'existing')
const studentId = ref('')
const error = ref('')
const notice = ref('')
const saving = ref(false)
const relationships = ref<RefValueItem[]>([])

const student = reactive({ firstName: '', lastName: '', birthDate: '' })
const guardian = reactive({
  create: true,
  firstName: '',
  lastName: '',
  phone: '',
  email: '',
  relationshipId: '',
})

/** "Ali Can Yılmaz" → { first: "Ali Can", last: "Yılmaz" } */
function splitName(full: string | null | undefined): { first: string; last: string } {
  const parts = (full ?? '').trim().split(/\s+/).filter(Boolean)
  if (parts.length <= 1) return { first: parts[0] ?? '', last: '' }
  return { first: parts.slice(0, -1).join(' '), last: parts[parts.length - 1] }
}

onMounted(async () => {
  await Promise.all([
    store.fetchOne(id.value),
    crm.load('lead_status'),
    refData.getValues('GUARDIAN_RELATIONSHIP').then(v => { relationships.value = v }).catch(() => {}),
  ])
  const l = lead.value
  if (!l) return
  const child = splitName(l.childName)
  student.firstName = child.first
  student.lastName = child.last
  student.birthDate = l.childBirthDate?.slice(0, 10) ?? ''
  const contact = splitName(l.contactName)
  guardian.firstName = contact.first
  guardian.lastName = contact.last || child.last
  guardian.phone = l.contactPhone ?? ''
  guardian.email = l.contactEmail ?? ''
  guardian.create = canCreateGuardian.value
})

function required(field: string): string {
  return t('validation.required', { field })
}

function validateNew(): boolean {
  if (!student.firstName.trim()) { error.value = required(t('student.firstName')); return false }
  if (!student.lastName.trim()) { error.value = required(t('student.lastName')); return false }
  if (guardian.create) {
    if (!guardian.firstName.trim()) { error.value = required(t('crm.lead.convertGuardianFirstName')); return false }
    if (!guardian.lastName.trim()) { error.value = required(t('crm.lead.convertGuardianLastName')); return false }
  }
  return true
}

async function convertTo(targetStudentId: string) {
  if (!lead.value) return
  await store.convert(id.value, { studentId: targetStudentId, rowVersion: lead.value.rowVersion })
  router.push({ name: 'lead-detail', params: { id: id.value } })
}

async function submitNew() {
  const l = lead.value
  if (!l || !validateNew()) return
  // Step 1: student
  const created = await studentService.createStudent({
    firstName: student.firstName.trim(),
    lastName: student.lastName.trim(),
    studentNo: null,
    nationalId: null,
    birthDate: student.birthDate || null,
    gender: null,
    primaryCampusId: l.campusId,
    statusId: null,
    leadId: l.id,
    notes: l.presentingNeed,
  })
  if (!created.success || !created.data) throw new Error(created.message || t('errors.serverError'))
  const newStudentId = created.data.id

  try {
    // Step 2: guardian (optional)
    if (guardian.create) {
      const g = await guardianService.createGuardian({
        firstName: guardian.firstName.trim(),
        lastName: guardian.lastName.trim(),
        nationalId: null,
        email: guardian.email.trim() || null,
        phone: guardian.phone.trim() || null,
        occupation: null,
        addressLine: null,
      })
      if (!g.success || !g.data) throw new Error(g.message || t('errors.serverError'))
      await studentService.linkGuardian(newStudentId, {
        guardianId: g.data.id,
        relationshipId: guardian.relationshipId || null,
        isPrimary: true,
        hasCustody: true,
        portalAccess: false,
        financialResponsible: true,
      })
    }
    // Step 3: mark the lead converted
    await convertTo(newStudentId)
  } catch (e: unknown) {
    // Student exists now; never create it twice. Let the user retry by linking.
    mode.value = 'existing'
    studentId.value = newStudentId
    notice.value = t('crm.lead.convertPartialFailure')
    throw e
  }
}

async function submit() {
  error.value = ''
  notice.value = ''
  if (mode.value === 'existing' && !studentId.value) {
    error.value = required(t('scheduling.session.student'))
    return
  }
  saving.value = true
  try {
    if (mode.value === 'new') await submitNew()
    else await convertTo(studentId.value)
  } catch (e: unknown) {
    error.value = (e as Error).message
  } finally {
    saving.value = false
  }
}

const inputClass = 'w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div class="max-w-2xl mx-auto">
    <PageHeader :title="t('crm.lead.convertTitle')" :description="t('crm.lead.convertDescription')" />

    <div v-if="store.loading" class="h-32 rounded-xl bg-accent animate-pulse" />
    <div v-else-if="!lead" class="text-center py-12 text-muted-foreground">{{ t('errors.notFound') }}</div>

    <template v-else>
      <!-- Lead summary -->
      <div class="rounded-xl border border-border bg-card p-5 shadow-sm mb-6">
        <h3 class="font-semibold text-foreground mb-3">{{ t('crm.lead.convertLeadSummary') }}</h3>
        <dl class="space-y-2 text-sm">
          <div class="flex justify-between gap-4">
            <dt class="text-muted-foreground">{{ t('crm.lead.contactName') }}</dt>
            <dd class="font-medium text-foreground text-right">{{ lead.contactName }}</dd>
          </div>
          <div v-if="lead.childName" class="flex justify-between gap-4">
            <dt class="text-muted-foreground">{{ t('crm.lead.childName') }}</dt>
            <dd class="font-medium text-foreground text-right">{{ lead.childName }}</dd>
          </div>
          <div v-if="lead.statusCode" class="flex justify-between gap-4">
            <dt class="text-muted-foreground">{{ t('common.status') }}</dt>
            <dd class="font-medium text-foreground text-right">{{ crm.label('lead_status', lead.statusCode) }}</dd>
          </div>
        </dl>
      </div>

      <!-- Already converted -->
      <div v-if="lead.isConverted" class="rounded-xl border border-emerald-200 bg-emerald-50 p-5 text-emerald-800 text-sm">
        {{ t('crm.lead.alreadyConverted') }}
      </div>

      <form v-else @submit.prevent="submit" class="space-y-4">
        <p v-if="notice" class="text-sm text-amber-800 bg-amber-50 rounded-lg px-4 py-3">{{ notice }}</p>
        <p v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ error }}</p>

        <!-- Mode -->
        <div class="rounded-xl border border-border bg-card p-2 shadow-sm flex flex-col sm:flex-row gap-2">
          <label
            v-if="canCreateStudent"
            :class="['flex-1 flex items-start gap-2 rounded-lg p-3 cursor-pointer', mode === 'new' ? 'bg-accent' : '']"
          >
            <input v-model="mode" type="radio" value="new" class="mt-0.5" />
            <span>
              <span class="block text-sm font-medium text-foreground">{{ t('crm.lead.convertModeNew') }}</span>
              <span class="block text-xs text-muted-foreground">{{ t('crm.lead.convertModeNewHint') }}</span>
            </span>
          </label>
          <label :class="['flex-1 flex items-start gap-2 rounded-lg p-3 cursor-pointer', mode === 'existing' ? 'bg-accent' : '']">
            <input v-model="mode" type="radio" value="existing" class="mt-0.5" />
            <span>
              <span class="block text-sm font-medium text-foreground">{{ t('crm.lead.convertModeExisting') }}</span>
              <span class="block text-xs text-muted-foreground">{{ t('crm.lead.convertModeExistingHint') }}</span>
            </span>
          </label>
        </div>

        <!-- New student from lead -->
        <template v-if="mode === 'new'">
          <div class="rounded-xl border border-border bg-card p-5 shadow-sm space-y-3">
            <h3 class="font-semibold text-foreground">{{ t('crm.lead.convertStudentSection') }}</h3>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.firstName') }} *</label>
                <input v-model="student.firstName" :class="inputClass" />
              </div>
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.lastName') }} *</label>
                <input v-model="student.lastName" :class="inputClass" />
              </div>
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.childBirthDate') }}</label>
                <input v-model="student.birthDate" type="date" :class="inputClass" />
              </div>
            </div>
            <p class="text-xs text-muted-foreground">{{ t('crm.lead.convertStudentHint') }}</p>
          </div>

          <div v-if="canCreateGuardian" class="rounded-xl border border-border bg-card p-5 shadow-sm space-y-3">
            <label class="flex items-center gap-2">
              <input v-model="guardian.create" type="checkbox" class="w-4 h-4 rounded border-border" />
              <span class="font-semibold text-foreground">{{ t('crm.lead.convertGuardianSection') }}</span>
            </label>
            <div v-if="guardian.create" class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.convertGuardianFirstName') }} *</label>
                <input v-model="guardian.firstName" :class="inputClass" />
              </div>
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.convertGuardianLastName') }} *</label>
                <input v-model="guardian.lastName" :class="inputClass" />
              </div>
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.phone') }}</label>
                <input v-model="guardian.phone" type="tel" :class="inputClass" />
              </div>
              <div>
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.email') }}</label>
                <input v-model="guardian.email" type="email" :class="inputClass" />
              </div>
              <div v-if="relationships.length">
                <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.convertGuardianRelationship') }}</label>
                <select v-model="guardian.relationshipId" :class="inputClass">
                  <option value="">{{ t('common.select') }}</option>
                  <option v-for="r in relationships" :key="r.id" :value="r.id">{{ r.label }}</option>
                </select>
              </div>
            </div>
            <p v-if="guardian.create" class="text-xs text-muted-foreground">{{ t('crm.lead.convertGuardianHint') }}</p>
          </div>
        </template>

        <!-- Existing student -->
        <div v-else class="rounded-xl border border-border bg-card p-5 shadow-sm">
          <label class="block text-sm font-medium text-foreground mb-1">
            {{ t('scheduling.session.student') }} *
          </label>
          <StudentPicker v-model="studentId" />
          <p class="mt-1 text-xs text-muted-foreground">{{ t('crm.lead.convertPickStudentHint') }}</p>
        </div>

        <div class="flex items-center justify-end gap-3">
          <button type="button" @click="router.back()"
            class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
            {{ t('common.cancel') }}
          </button>
          <button type="submit" :disabled="saving"
            class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 transition-colors disabled:opacity-60">
            <svg v-if="saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            {{ saving ? t('common.saving') : mode === 'new' ? t('crm.lead.convertCreateAndConvert') : t('crm.lead.convertConfirm') }}
          </button>
        </div>
      </form>
    </template>
  </div>
</template>
