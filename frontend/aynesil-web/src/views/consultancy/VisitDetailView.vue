<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useRefDataStore } from '@/stores/refdata.store'
import type { RefValueItem } from '@/stores/refdata.store'
import { useModule } from '@/composables/useModule'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import EducatorPicker from '@/components/pickers/EducatorPicker.vue'
import { Permissions } from '@/constants/permissions'
import { Modules } from '@/constants/modules'
import { SchoolVisitStatus } from '@/constants/statuses'
import type { ObservationRecordDto } from '@/types/consultancy.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const refData = useRefDataStore()
const { visible } = useModule(Modules.Consultancy)

const id = computed(() => String(route.params.id))
const visit = computed(() => (store.currentVisit?.id === id.value ? store.currentVisit : null))
const isPlanned = computed(() => visit.value?.status === SchoolVisitStatus.Planned)
const canAddObservation = computed(
  () => !!visit.value && visit.value.status !== SchoolVisitStatus.Cancelled && visible(Permissions.Observations.Create),
)

const observationTypes = ref<RefValueItem[]>([])
const pageError = ref('')

function typeLabel(code?: string | null) {
  if (!code) return null
  return observationTypes.value.find((o) => o.code === code)?.label ?? code
}

function formatDate(val?: string | null) {
  return val ? new Date(`${val}T00:00:00`).toLocaleDateString('tr-TR') : '—'
}
function formatDateTime(val?: string | null) {
  return val ? new Date(val).toLocaleString('tr-TR') : '—'
}

async function load() {
  pageError.value = ''
  store.currentVisit = null
  await store.fetchVisit(id.value)
  if (!store.currentVisit) pageError.value = store.error ?? ''
}

onMounted(async () => {
  observationTypes.value = await refData.getValues('observation_type').catch(() => [])
  await load()
})

// ── Ziyaret: düzenle ──────────────────────────────────────────────────────────
const showEdit = ref(false)
const editError = ref('')
const editForm = reactive({ visitDate: '', purpose: '', visitorId: '', consultancyPlanId: '' })
const activePlans = computed(() =>
  store.plans.items.filter((p) => p.institutionId === visit.value?.institutionId),
)

async function openEdit() {
  if (!visit.value) return
  Object.assign(editForm, {
    visitDate: visit.value.visitDate,
    purpose: visit.value.purpose ?? '',
    visitorId: visit.value.visitorId ?? '',
    consultancyPlanId: visit.value.consultancyPlanId ?? '',
  })
  editError.value = ''
  showEdit.value = true
  await store.fetchPlans({ page: 1, pageSize: 200, institutionId: visit.value.institutionId, status: 'active' })
}

async function submitEdit() {
  if (!editForm.visitDate) {
    editError.value = t('validation.required', { field: t('consultancy.visit.fields.visitDate') })
    return
  }
  editError.value = ''
  try {
    await store.updateVisit(id.value, {
      visitDate: editForm.visitDate,
      purpose: editForm.purpose.trim() || undefined,
      visitorId: editForm.visitorId || undefined,
      consultancyPlanId: editForm.consultancyPlanId || undefined,
    })
    showEdit.value = false
  } catch (e: unknown) {
    editError.value = (e as Error).message
  }
}

// ── Ziyaret: tamamla / iptal / sil ────────────────────────────────────────────
const action = ref<'complete' | 'cancel' | 'delete' | null>(null)
const actionError = ref('')

async function runAction() {
  if (!action.value) return
  actionError.value = ''
  try {
    if (action.value === 'complete') await store.completeVisit(id.value)
    else if (action.value === 'cancel') await store.cancelVisit(id.value)
    else {
      await store.deleteVisit(id.value)
      router.replace({ name: 'consultancy-visits' })
    }
    action.value = null
  } catch (e: unknown) {
    // Silme engeli (gözlem/rapor/takip) gibi iş kuralı hatası: sunucunun mesajı olduğu gibi gösterilir.
    actionError.value = (e as Error).message
    action.value = null
  }
}

// ── Gözlem: ekle / düzenle / sil ──────────────────────────────────────────────
const showObs = ref(false)
const editingObs = ref<ObservationRecordDto | null>(null)
const obsError = ref('')
const obsForm = reactive({ observationTypeId: '', subject: '', observation: '', recommendations: '' })

function openObs(o?: ObservationRecordDto) {
  editingObs.value = o ?? null
  Object.assign(obsForm, {
    observationTypeId: o?.observationTypeId ?? '',
    subject: o?.subject ?? '',
    observation: o?.observation ?? '',
    recommendations: o?.recommendations ?? '',
  })
  obsError.value = ''
  showObs.value = true
}

async function submitObs() {
  if (!obsForm.observation.trim()) {
    obsError.value = t('validation.required', { field: t('consultancy.observation.fields.observation') })
    return
  }
  obsError.value = ''
  const payload = {
    observationTypeId: obsForm.observationTypeId || undefined,
    subject: obsForm.subject.trim() || undefined,
    observation: obsForm.observation.trim(),
    recommendations: obsForm.recommendations.trim() || undefined,
  }
  try {
    if (editingObs.value) await store.updateObservation(id.value, editingObs.value.id, payload)
    else await store.addObservation(id.value, payload)
    showObs.value = false
  } catch (e: unknown) {
    obsError.value = (e as Error).message
  }
}

const obsToDelete = ref<ObservationRecordDto | null>(null)
const obsDeleteError = ref('')
async function deleteObs() {
  if (!obsToDelete.value) return
  obsDeleteError.value = ''
  try {
    await store.deleteObservation(id.value, obsToDelete.value.id)
  } catch (e: unknown) {
    obsDeleteError.value = (e as Error).message
  }
  obsToDelete.value = null
}
</script>

<template>
  <div>
    <div v-if="store.loading && !visit" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!visit" class="py-16 text-center text-sm">
      <p class="text-muted-foreground">{{ t('consultancy.visit.detail.notFound') }}</p>
      <p v-if="pageError" class="mt-2 text-red-600">{{ pageError }}</p>
    </div>

    <template v-else>
      <PageHeader
        :title="`${visit.institutionName} · ${formatDate(visit.visitDate)}`"
        :description="visit.planName ?? t('consultancy.visit.fields.noPlan')"
      >
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
            @click="router.push({ name: 'consultancy-visits' })"
          >
            {{ t('common.back') }}
          </button>
          <button
            v-if="isPlanned && visible(Permissions.SchoolVisits.Update)"
            type="button"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
            @click="openEdit"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="isPlanned && visible(Permissions.SchoolVisits.Complete)"
            type="button"
            :disabled="store.saving"
            class="px-3 py-1.5 text-sm rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-50"
            @click="action = 'complete'"
          >
            {{ t('consultancy.visit.actions.complete') }}
          </button>
          <button
            v-if="isPlanned && visible(Permissions.SchoolVisits.Cancel)"
            type="button"
            :disabled="store.saving"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-700 hover:bg-red-50 disabled:opacity-50"
            @click="action = 'cancel'"
          >
            {{ t('consultancy.visit.actions.cancel') }}
          </button>
          <button
            v-if="isPlanned && visible(Permissions.SchoolVisits.Delete)"
            type="button"
            :disabled="store.saving"
            class="px-3 py-1.5 text-sm rounded-lg border border-red-200 text-red-700 hover:bg-red-50 disabled:opacity-50"
            @click="action = 'delete'"
          >
            {{ t('consultancy.visit.actions.delete') }}
          </button>
        </div>
      </PageHeader>

      <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ actionError }}</p>

      <div class="bg-white rounded-xl border border-border p-6 space-y-4 mb-6">
        <div class="flex items-center gap-2">
          <StatusBadge entity="schoolVisit" :status="visit.status" />
          <span v-if="!isPlanned" class="text-xs text-muted-foreground">
            {{ t('consultancy.visit.detail.onlyPlannedEditable') }}
          </span>
        </div>
        <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
          <div>
            <dt class="text-muted-foreground">{{ t('consultancy.visit.fields.institution') }}</dt>
            <dd class="font-medium mt-0.5">{{ visit.institutionName }}</dd>
          </div>
          <div>
            <dt class="text-muted-foreground">{{ t('consultancy.visit.fields.plan') }}</dt>
            <dd class="font-medium mt-0.5">
              <button
                v-if="visit.consultancyPlanId"
                type="button"
                class="text-primary hover:underline"
                @click="router.push({ name: 'consultancy-plan-detail', params: { id: visit.consultancyPlanId } })"
              >
                {{ visit.planName }}
              </button>
              <span v-else>{{ t('consultancy.visit.fields.noPlan') }}</span>
            </dd>
          </div>
          <div>
            <dt class="text-muted-foreground">{{ t('consultancy.visit.fields.visitDate') }}</dt>
            <dd class="font-medium mt-0.5">{{ formatDate(visit.visitDate) }}</dd>
          </div>
          <div class="sm:col-span-2">
            <dt class="text-muted-foreground">{{ t('consultancy.visit.fields.purpose') }}</dt>
            <dd class="mt-0.5 whitespace-pre-wrap">{{ visit.purpose || '—' }}</dd>
          </div>
        </dl>

        <div class="flex flex-wrap gap-3 pt-2 border-t border-border">
          <button
            v-if="visible(Permissions.ConsultancyReports.Create) && visit.status !== SchoolVisitStatus.Cancelled"
            type="button"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
            @click="router.push({ name: 'consultancy-reports', query: { visitId: visit.id, new: '1' } })"
          >
            {{ t('consultancy.visit.actions.addReport') }}
          </button>
          <button
            v-if="visible(Permissions.ConsultancyReports.Read)"
            type="button"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
            @click="router.push({ name: 'consultancy-reports', query: { visitId: visit.id } })"
          >
            {{ t('consultancy.visit.actions.viewReports') }}
          </button>
          <button
            v-if="visible(Permissions.FollowUps.Create) && visit.status !== SchoolVisitStatus.Cancelled"
            type="button"
            class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
            @click="router.push({ path: '/consultancy/follow-ups/new', query: { visitId: visit.id } })"
          >
            {{ t('consultancy.visit.actions.addFollowUp') }}
          </button>
        </div>
      </div>

      <!-- Gözlemler -->
      <div class="bg-white rounded-xl border border-border p-6">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-base font-semibold">
            {{ t('consultancy.observation.title') }} ({{ visit.observations.length }})
          </h2>
          <button
            v-if="canAddObservation"
            type="button"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
            @click="openObs()"
          >
            {{ t('consultancy.observation.new') }}
          </button>
        </div>
        <p v-if="obsDeleteError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ obsDeleteError }}</p>
        <p v-if="visit.status === SchoolVisitStatus.Cancelled" class="mb-3 text-xs text-muted-foreground">
          {{ t('consultancy.observation.cannotAdd') }}
        </p>

        <p v-if="!visit.observations.length" class="py-8 text-center text-sm text-muted-foreground">
          {{ t('consultancy.observation.empty') }}
        </p>
        <ul v-else class="space-y-3">
          <li v-for="o in visit.observations" :key="o.id" class="rounded-lg border border-border p-4">
            <div class="flex flex-wrap items-start justify-between gap-2">
              <div class="min-w-0">
                <p class="text-sm font-medium">
                  {{ o.subject || t('consultancy.observation.fields.observation') }}
                  <span v-if="typeLabel(o.observationTypeCode)" class="ml-2 text-xs font-normal text-muted-foreground">
                    · {{ typeLabel(o.observationTypeCode) }}
                  </span>
                </p>
                <p class="text-xs text-muted-foreground">{{ formatDateTime(o.createdAt) }}</p>
              </div>
              <div class="flex gap-2 shrink-0">
                <button
                  v-if="visible(Permissions.FollowUps.Create) && visit.status !== SchoolVisitStatus.Cancelled"
                  type="button"
                  class="px-2 py-1 text-xs rounded border border-border hover:bg-accent"
                  @click="router.push({ path: '/consultancy/follow-ups/new', query: { visitId: visit.id, observationId: o.id } })"
                >
                  {{ t('consultancy.visit.actions.addFollowUp') }}
                </button>
                <button
                  v-if="visible(Permissions.Observations.Update)"
                  type="button"
                  class="px-2 py-1 text-xs rounded border border-border hover:bg-accent"
                  @click="openObs(o)"
                >
                  {{ t('common.edit') }}
                </button>
                <button
                  v-if="visible(Permissions.Observations.Delete)"
                  type="button"
                  class="px-2 py-1 text-xs rounded border border-red-200 text-red-700 hover:bg-red-50"
                  @click="obsDeleteError = ''; obsToDelete = o"
                >
                  {{ t('common.delete') }}
                </button>
              </div>
            </div>
            <p class="mt-2 text-sm whitespace-pre-wrap">{{ o.observation }}</p>
            <p v-if="o.recommendations" class="mt-2 text-sm text-muted-foreground whitespace-pre-wrap">
              <span class="font-medium">{{ t('consultancy.observation.fields.recommendations') }}:</span>
              {{ o.recommendations }}
            </p>
          </li>
        </ul>
      </div>
    </template>

    <!-- Ziyaret düzenle -->
    <FormModal
      :open="showEdit"
      :title="t('common.edit')"
      :saving="store.saving"
      @close="showEdit = false"
      @submit="submitEdit"
    >
      <div class="space-y-4">
        <p v-if="editError" class="text-sm text-red-600">{{ editError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.visit.fields.visitDate') }} *</label>
          <input v-model="editForm.visitDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.visit.fields.plan') }}</label>
          <select v-model="editForm.consultancyPlanId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('consultancy.visit.fields.noPlan') }}</option>
            <option v-for="p in activePlans" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.visit.fields.visitor') }}</label>
          <EducatorPicker v-model="editForm.visitorId" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.visit.fields.purpose') }}</label>
          <textarea v-model="editForm.purpose" rows="3" maxlength="500" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <!-- Gözlem ekle / düzenle -->
    <FormModal
      :open="showObs"
      :title="editingObs ? t('consultancy.observation.edit') : t('consultancy.observation.new')"
      :saving="store.saving"
      @close="showObs = false"
      @submit="submitObs"
    >
      <div class="space-y-4">
        <p v-if="obsError" class="text-sm text-red-600">{{ obsError }}</p>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.observation.fields.type') }}</label>
          <select v-model="obsForm.observationTypeId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="o in observationTypes" :key="o.id" :value="o.id">{{ o.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.observation.fields.subject') }}</label>
          <input v-model="obsForm.subject" type="text" maxlength="300" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.observation.fields.observation') }} *</label>
          <textarea v-model="obsForm.observation" rows="5" maxlength="4000" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('consultancy.observation.fields.recommendations') }}</label>
          <textarea v-model="obsForm.recommendations" rows="3" maxlength="4000" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!action"
      :title="action ? t(`consultancy.visit.actions.${action}`) : ''"
      :message="action ? t(`consultancy.visit.actions.${action}Confirm`) : ''"
      :loading="store.saving"
      @confirm="runAction"
      @cancel="action = null"
    />
    <ConfirmModal
      :open="!!obsToDelete"
      :title="t('common.delete')"
      :message="t('consultancy.observation.deleteConfirm')"
      :loading="store.saving"
      @confirm="deleteObs"
      @cancel="obsToDelete = null"
    />
  </div>
</template>
