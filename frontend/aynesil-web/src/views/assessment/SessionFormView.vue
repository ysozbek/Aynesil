<script setup lang="ts">
/**
 * Assessment Session Create / Edit form.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useAssessmentStore } from '@/stores/assessment.store'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { useBranchStore } from '@/stores/branch.store'
import { useUserStore } from '@/stores/user.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useAssessmentStore()
const templateStore = useAssessmentTemplateStore()
const branchStore = useBranchStore()
const userStore = useUserStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  templateId: '',
  leadId: '',
  studentId: '',
  campusId: '',
  assessorId: '',
  scheduledAt: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const generalError = ref('')
const saving = ref(false)

onMounted(async () => {
  await Promise.all([
    templateStore.fetchList({ isActive: true, pageSize: 200 }),
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    userStore.list.items.length === 0 ? userStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
  ])

  if (isEdit.value && id.value) {
    await store.fetchOne(id.value)
    const s = store.current
    if (s) {
      form.templateId = s.templateId
      form.leadId = s.leadId ?? ''
      form.studentId = s.studentId ?? ''
      form.campusId = s.campusId ?? ''
      form.assessorId = s.assessorId ?? ''
      form.scheduledAt = s.scheduledAt ? new Date(s.scheduledAt).toISOString().slice(0, 16) : ''
      form.rowVersion = s.rowVersion
    }
  }

  // Pre-fill from query params (e.g., coming from Lead Detail)
  if (route.query.leadId) form.leadId = String(route.query.leadId)
  if (route.query.studentId) form.studentId = String(route.query.studentId)
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.templateId) { errors.templateId = t('validation.required', { field: t('assessment.session.template') }); valid = false }
  if (!form.corporationId) { errors.corporationId = t('validation.required', { field: t('crm.lead.corporation') }); valid = false }
  if (!form.leadId && !form.studentId) {
    errors.subject = t('assessment.session.subjectRequired')
    valid = false
  }
  return valid
}

async function submit() {
  if (!validate()) return
  saving.value = true
  generalError.value = ''
  try {
    if (isEdit.value && id.value) {
      await store.update(id.value, {
        scheduledAt: form.scheduledAt || undefined,
        assessorId: form.assessorId || undefined,
        campusId: form.campusId || undefined,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'assessment-session-detail', params: { id: id.value } })
    } else {
      const result = await store.create({
        corporationId: form.corporationId,
        templateId: form.templateId,
        leadId: form.leadId || undefined,
        studentId: form.studentId || undefined,
        campusId: form.campusId || undefined,
        assessorId: form.assessorId || undefined,
        scheduledAt: form.scheduledAt || undefined,
      })
      router.push({ name: 'assessment-session-detail', params: { id: result.id } })
    }
  } catch (e: unknown) {
    generalError.value = (e as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl mx-auto">
    <PageHeader
      :title="isEdit ? t('assessment.session.edit') : t('assessment.session.create')"
      :description="isEdit ? t('assessment.session.editDescription') : t('assessment.session.createDescription')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <!-- Template -->
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.session.template') }} *</label>
          <select v-model="form.templateId" :disabled="isEdit"
            class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
            :class="errors.templateId ? 'border-red-400' : 'border-border'">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="tpl in templateStore.list.items" :key="tpl.id" :value="tpl.id">
              {{ tpl.name }} (v{{ tpl.version }})
            </option>
          </select>
          <p v-if="errors.templateId" class="mt-1 text-xs text-red-600">{{ errors.templateId }}</p>
        </div>

        <!-- Subject (lead XOR student) -->
        <div v-if="!isEdit">
          <p class="text-sm font-medium text-foreground mb-2">{{ t('assessment.session.subject') }} *</p>
          <p v-if="errors.subject" class="mb-2 text-xs text-red-600">{{ errors.subject }}</p>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs text-muted-foreground mb-1">{{ t('assessment.session.leadId') }}</label>
              <input v-model="form.leadId" type="text" :placeholder="t('assessment.session.leadIdPlaceholder')"
                :disabled="!!form.studentId"
                class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50" />
            </div>
            <div>
              <label class="block text-xs text-muted-foreground mb-1">{{ t('assessment.session.studentId') }}</label>
              <input v-model="form.studentId" type="text" :placeholder="t('assessment.session.studentIdPlaceholder')"
                :disabled="!!form.leadId"
                class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50" />
            </div>
          </div>
        </div>

        <!-- Logistics -->
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('campus.title') }}</label>
            <select v-model="form.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.session.assessor') }}</label>
            <select v-model="form.assessorId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="u in userStore.list.items" :key="u.id" :value="u.id">{{ u.fullName }}</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.session.scheduledAt') }}</label>
          <input v-model="form.scheduledAt" type="datetime-local"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>

      <div class="flex items-center justify-end gap-3">
        <button type="button" @click="router.back()"
          class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="saving"
          class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60">
          <svg v-if="saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          {{ saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
