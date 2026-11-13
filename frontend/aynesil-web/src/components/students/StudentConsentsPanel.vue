<script setup lang="ts">
/**
 * Öğrenci detayı → Rızalar: öğrencinin rıza kayıtları, rıza verme ve geri alma.
 *
 * Kamera izleme ilişkisi (backend, Cameras/Commands/ViewingCommands.cs):
 *  - Veliye izleme yetkisi (ViewingAuthorization) verilirken, o veli adına verilmiş, geçerli
 *    `camera_viewing` rızası aranır (öğrenci + veli eşleşmeli). Bu yüzden kamera rızası veli seçilerek verilmelidir.
 *  - İzleme başlatılırken rıza yeniden kontrol edilir; rıza geri alınmışsa/süresi dolmuşsa izleme başlamaz.
 *    Geri almak açık bir yetki kaydını silmez; yetki kayıtlı kalır ama izleme engellenir.
 * Geri alınan rıza değişmez; yeniden rıza için yeni kayıt oluşturulur.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { legalService } from '@/services/legal.service'
import { useConsentStore } from '@/stores/consent.store'
import { useRefDataStore } from '@/stores/refdata.store'
import type { RefValueItem } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import { Permissions } from '@/constants/permissions'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { StudentGuardianDto } from '@/types/student.types'
import type { ConsentTemplateListItemDto, StudentConsentListItemDto } from '@/types/legal.types'

const CAMERA_CONSENT = 'camera_viewing'

const props = defineProps<{ studentId: string; guardians: StudentGuardianDto[] }>()

const { t } = useI18n()
const { can } = usePermission()
const consentStore = useConsentStore()
const refData = useRefDataStore()

const rows = ref<StudentConsentListItemDto[]>([])
const loading = ref(false)
const listError = ref('')

const types = ref<RefValueItem[]>([])
const templates = ref<ConsentTemplateListItemDto[]>([])

const showGrant = ref(false)
const grantError = ref('')
const form = reactive({ consentTypeId: '', templateId: '', guardianId: '', validUntil: '' })

const withdrawTarget = ref<StudentConsentListItemDto | null>(null)
const withdrawError = ref('')

const typeLabel = (id?: string, code?: string) =>
  types.value.find((v) => v.id === id)?.label ?? code ?? '—'

const selectedType = computed(() => types.value.find((v) => v.id === form.consentTypeId))
const isCameraConsent = computed(() => selectedType.value?.code === CAMERA_CONSENT)

async function load() {
  loading.value = true
  listError.value = ''
  try {
    const res = await legalService.listConsents({ studentId: props.studentId, pageSize: 100 })
    if (res.success && res.data) rows.value = res.data.items
  } catch (e: unknown) {
    listError.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  types.value = await refData.getValues('consent_type')
  if (can(Permissions.ConsentTemplates.Read)) {
    try {
      const res = await legalService.listConsentTemplates({ isCurrent: true, pageSize: 100 })
      if (res.success && res.data) templates.value = res.data.items
    } catch { /* şablon listesi yoksa tür seçimiyle devam edilir */ }
  }
  await load()
})
watch(() => props.studentId, load)

// Şablon seçilince türü şablondan al.
watch(() => form.templateId, (id) => {
  const tpl = templates.value.find((x) => x.id === id)
  if (tpl?.consentTypeId) form.consentTypeId = tpl.consentTypeId
})

function openGrant() {
  Object.assign(form, { consentTypeId: '', templateId: '', guardianId: '', validUntil: '' })
  grantError.value = ''
  showGrant.value = true
}

async function submitGrant() {
  grantError.value = ''
  if (!form.consentTypeId && !form.templateId) { grantError.value = t('student.consent.typeRequired'); return }
  if (isCameraConsent.value && !form.guardianId) { grantError.value = t('student.consent.cameraGuardianRequired'); return }
  try {
    await consentStore.grantConsent({
      studentId: props.studentId,
      guardianId: form.guardianId || undefined,
      templateId: form.templateId || undefined,
      consentTypeId: form.consentTypeId || undefined,
      validUntil: form.validUntil || undefined,
    })
    showGrant.value = false
    await load()
  } catch (e: unknown) {
    grantError.value = (e as Error).message
  }
}

async function confirmWithdraw() {
  if (!withdrawTarget.value) return
  withdrawError.value = ''
  try {
    await consentStore.withdrawConsent(withdrawTarget.value.id)
    withdrawTarget.value = null
    await load()
  } catch (e: unknown) {
    withdrawError.value = (e as Error).message
  }
}

function guardianName(id?: string) {
  return props.guardians.find((g) => g.guardianId === id)?.guardianFullName ?? '—'
}
function formatDate(val?: string) {
  return val ? new Date(val).toLocaleDateString('tr-TR') : '—'
}
const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4 gap-4">
      <p class="text-sm text-muted-foreground">{{ t('student.consent.description') }}</p>
      <button
        v-if="can(Permissions.StudentConsents.Grant)"
        class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 shrink-0"
        @click="openGrant"
      >
        {{ t('student.consent.grant') }}
      </button>
    </div>

    <p v-if="listError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ listError }}</p>
    <div v-if="loading" class="py-8 text-center text-sm text-muted-foreground">{{ t('common.loading') }}</div>
    <div v-else-if="!rows.length" class="text-center py-12 text-muted-foreground">{{ t('common.noData') }}</div>
    <div v-else class="space-y-3">
      <div v-for="c in rows" :key="c.id" class="rounded-xl border border-border bg-card p-4 shadow-sm flex items-start justify-between gap-4">
        <div>
          <p class="font-medium text-foreground">
            {{ typeLabel(c.consentTypeId, c.consentTypeCode) }}
            <StatusBadge entity="consent" :status="c.state" class="ml-2" />
          </p>
          <p class="text-xs text-muted-foreground mt-1">
            {{ t('student.consent.guardian') }}: {{ guardianName(c.guardianId) }}
            · {{ t('legal.consent.fields.grantedAt') }}: {{ formatDate(c.grantedAt) }}
            <span v-if="c.validUntil"> · {{ t('legal.consent.fields.validUntil') }}: {{ formatDate(c.validUntil) }}</span>
            <span v-if="c.templateCode"> · {{ c.templateCode }} v{{ c.templateVersion }}</span>
            <span v-if="c.withdrawnAt"> · {{ t('student.consent.withdrawnAt') }}: {{ formatDate(c.withdrawnAt) }}</span>
          </p>
          <p v-if="c.consentTypeCode === CAMERA_CONSENT && c.state === 'granted'" class="text-xs text-indigo-600 mt-1">
            {{ t('student.consent.cameraNote') }}
          </p>
        </div>
        <button
          v-if="c.state === 'granted' && can(Permissions.StudentConsents.Withdraw)"
          class="px-3 py-1.5 text-xs rounded-lg border border-red-200 text-red-600 hover:bg-red-50 shrink-0"
          @click="withdrawTarget = c; withdrawError = ''"
        >
          {{ t('student.consent.withdraw') }}
        </button>
      </div>
    </div>

    <FormModal
      :open="showGrant"
      :title="t('student.consent.grant')"
      :saving="consentStore.saving"
      @submit="submitGrant"
      @close="showGrant = false"
    >
      <div class="space-y-4">
        <p v-if="grantError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ grantError }}</p>

        <div v-if="templates.length">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.consent.fields.template') }}</label>
          <select v-model="form.templateId" :class="inputClass">
            <option value="">—</option>
            <option v-for="tpl in templates" :key="tpl.id" :value="tpl.id">{{ tpl.code }} v{{ tpl.version }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.consent.fields.consentType') }} *</label>
          <select v-model="form.consentTypeId" :class="inputClass">
            <option value="">—</option>
            <option v-for="v in types" :key="v.id" :value="v.id">{{ v.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">
            {{ t('student.consent.guardian') }}<span v-if="isCameraConsent"> *</span>
          </label>
          <select v-model="form.guardianId" :class="inputClass">
            <option value="">—</option>
            <option v-for="g in guardians" :key="g.guardianId" :value="g.guardianId">{{ g.guardianFullName }}</option>
          </select>
          <p v-if="isCameraConsent" class="mt-1 text-xs text-muted-foreground">{{ t('student.consent.cameraHint') }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.consent.fields.validUntil') }}</label>
          <input v-model="form.validUntil" type="date" :class="inputClass" />
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!withdrawTarget"
      :title="t('student.consent.withdraw')"
      :message="withdrawError || t('student.consent.withdrawConfirm')"
      :confirm-label="t('student.consent.withdraw')"
      :loading="consentStore.saving"
      @confirm="confirmWithdraw"
      @cancel="withdrawTarget = null"
    />
  </div>
</template>
