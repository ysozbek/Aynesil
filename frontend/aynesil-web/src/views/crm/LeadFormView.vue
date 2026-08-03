<script setup lang="ts">
/**
 * Lead Create / Edit form.
 * Route: /crm/leads/new  (create)
 *        /crm/leads/:id/edit (edit)
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useLeadStore } from '@/stores/lead.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { useUserStore } from '@/stores/user.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useLeadStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()
const userStore = useUserStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const statuses = ref<RefValueItem[]>([])
const sources = ref<RefValueItem[]>([])
const stages = ref<RefValueItem[]>([])

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  contactName: '',
  campusId: '',
  sourceId: '',
  statusId: '',
  pipelineStageId: '',
  childName: '',
  childBirthDate: '',
  contactPhone: '',
  contactEmail: '',
  presentingNeed: '',
  referralDetail: '',
  assignedToId: '',
  score: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  await Promise.all([
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('LEAD_STATUS').then(v => { statuses.value = v }),
    refData.getValues('LEAD_SOURCE').then(v => { sources.value = v }),
    refData.getValues('LEAD_PIPELINE_STAGE').then(v => { stages.value = v }),
    userStore.list.items.length === 0 ? userStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
  ])

  if (isEdit.value && id.value) {
    await store.fetchOne(id.value)
    const lead = store.current
    if (lead) {
      form.contactName = lead.contactName
      form.campusId = lead.campusId ?? ''
      form.sourceId = lead.sourceId ?? ''
      form.statusId = lead.statusId ?? ''
      form.pipelineStageId = lead.pipelineStageId ?? ''
      form.childName = lead.childName ?? ''
      form.childBirthDate = lead.childBirthDate ?? ''
      form.contactPhone = lead.contactPhone ?? ''
      form.contactEmail = lead.contactEmail ?? ''
      form.presentingNeed = lead.presentingNeed ?? ''
      form.referralDetail = lead.referralDetail ?? ''
      form.assignedToId = lead.assignedToId ?? ''
      form.score = lead.score !== null && lead.score !== undefined ? String(lead.score) : ''
      form.rowVersion = lead.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.contactName.trim()) {
    errors.contactName = t('validation.required', { field: t('crm.lead.contactName') })
    valid = false
  }
  if (!form.corporationId) {
    errors.corporationId = t('validation.required', { field: t('crm.lead.corporation') })
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
        contactName: form.contactName,
        campusId: form.campusId || undefined,
        sourceId: form.sourceId || undefined,
        childName: form.childName || undefined,
        childBirthDate: form.childBirthDate || undefined,
        contactPhone: form.contactPhone || undefined,
        contactEmail: form.contactEmail || undefined,
        presentingNeed: form.presentingNeed || undefined,
        referralDetail: form.referralDetail || undefined,
        assignedToId: form.assignedToId || undefined,
        score: form.score ? parseInt(form.score) : undefined,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'lead-detail', params: { id: id.value } })
    } else {
      const result = await store.create({
        corporationId: form.corporationId,
        contactName: form.contactName,
        campusId: form.campusId || undefined,
        sourceId: form.sourceId || undefined,
        statusId: form.statusId || undefined,
        pipelineStageId: form.pipelineStageId || undefined,
        childName: form.childName || undefined,
        childBirthDate: form.childBirthDate || undefined,
        contactPhone: form.contactPhone || undefined,
        contactEmail: form.contactEmail || undefined,
        presentingNeed: form.presentingNeed || undefined,
        referralDetail: form.referralDetail || undefined,
        assignedToId: form.assignedToId || undefined,
        score: form.score ? parseInt(form.score) : undefined,
      })
      router.push({ name: 'lead-detail', params: { id: result.id } })
    }
  } catch (e: unknown) {
    generalError.value = (e as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto">
    <PageHeader
      :title="isEdit ? t('crm.lead.edit') : t('crm.lead.create')"
      :description="isEdit ? t('crm.lead.editDescription') : t('crm.lead.createDescription')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Contact Info -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('crm.lead.contactInfo') }}</h3>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.contactName') }} *</label>
          <input v-model="form.contactName" type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.contactName ? 'border-red-400' : 'border-border'" />
          <p v-if="errors.contactName" class="mt-1 text-xs text-red-600">{{ errors.contactName }}</p>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.phone') }}</label>
            <input v-model="form.contactPhone" type="tel" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.email') }}</label>
            <input v-model="form.contactEmail" type="email" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.campus') }}</label>
          <select v-model="form.campusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
      </div>

      <!-- Child Info -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('crm.lead.childInfo') }}</h3>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.childName') }}</label>
            <input v-model="form.childName" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.childBirthDate') }}</label>
            <input v-model="form.childBirthDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>

      <!-- CRM Classification -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('crm.lead.classification') }}</h3>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.source') }}</label>
            <select v-model="form.sourceId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="s in sources" :key="s.id" :value="s.id">{{ s.label }}</option>
            </select>
          </div>
          <div v-if="!isEdit">
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.status') }}</label>
            <select v-model="form.statusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="s in statuses" :key="s.id" :value="s.id">{{ s.label }}</option>
            </select>
          </div>
          <div v-if="!isEdit">
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.pipelineStage') }}</label>
            <select v-model="form.pipelineStageId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="s in stages" :key="s.id" :value="s.id">{{ s.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.assignedTo') }}</label>
            <select v-model="form.assignedToId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="u in userStore.list.items" :key="u.id" :value="u.id">{{ u.fullName }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.score') }}</label>
            <input v-model="form.score" type="number" min="0" max="100"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>

      <!-- Clinical Notes -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('crm.lead.clinicalInfo') }}</h3>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.presentingNeed') }}</label>
          <textarea v-model="form.presentingNeed" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('crm.lead.referralDetail') }}</label>
          <textarea v-model="form.referralDetail" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>

      <!-- Actions -->
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
