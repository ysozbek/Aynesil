<script setup lang="ts">
/**
 * Öğrenci detayı → Bakım Ekibi: öğrenciye bakan eğitmenleri listeler; ekleme, rol/tarih/birincil düzenleme ve
 * ekipten çıkarma (atama sona erer, silinmez). Uçlar: CareTeamController (care_team:read / care_team:assign).
 * Kural: öğrenci başına aynı anda yalnızca bir birincil atama (sunucu reddederse mesaj gösterilir).
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { careTeamService } from '@/services/careTeam.service'
import { useRefDataStore } from '@/stores/refdata.store'
import type { RefValueItem } from '@/stores/refdata.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import { toLocalDateString } from '@/utils/datetime'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import EducatorPicker from '@/components/pickers/EducatorPicker.vue'
import type { CareTeamAssignmentListItemDto } from '@/types/careTeam.types'

const props = defineProps<{ studentId: string }>()

const { t } = useI18n()
const refData = useRefDataStore()
const { visible } = useModule(Modules.CareTeam)
const canAssign = computed(() => visible(Permissions.CareTeam.Assign))

const rows = ref<CareTeamAssignmentListItemDto[]>([])
const roles = ref<RefValueItem[]>([])
const loading = ref(false)
const saving = ref(false)
const listError = ref('')
const showEnded = ref(false)

const showForm = ref(false)
const editing = ref<CareTeamAssignmentListItemDto | null>(null)
const formError = ref('')
const form = reactive({ educatorId: '', roleId: '', isPrimary: false, activeFrom: '', activeTo: '', reason: '' })

const removeTarget = ref<CareTeamAssignmentListItemDto | null>(null)
const removeError = ref('')

const roleLabel = (id: string, code?: string | null) =>
  roles.value.find((r) => r.id === id)?.label ?? code ?? '—'

async function load() {
  loading.value = true
  listError.value = ''
  try {
    const res = await careTeamService.list(props.studentId, !showEnded.value)
    rows.value = res.success && res.data ? res.data : []
  } catch (e: unknown) {
    rows.value = []
    listError.value = (e as Error).message || t('careTeam.unavailable')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  roles.value = await refData.getValues('care_team_role')
  await load()
})
watch(() => [props.studentId, showEnded.value], load)

function openAdd() {
  editing.value = null
  Object.assign(form, {
    educatorId: '',
    roleId: roles.value.find((r) => r.isDefault)?.id ?? roles.value[0]?.id ?? '',
    isPrimary: false,
    activeFrom: toLocalDateString(),
    activeTo: '',
    reason: '',
  })
  formError.value = ''
  showForm.value = true
}

function openEdit(a: CareTeamAssignmentListItemDto) {
  editing.value = a
  Object.assign(form, {
    educatorId: a.educatorId,
    roleId: a.roleId,
    isPrimary: a.isPrimary,
    activeFrom: a.activeFrom,
    activeTo: a.activeTo ?? '',
    reason: a.reason ?? '',
  })
  formError.value = ''
  showForm.value = true
}

async function submit() {
  formError.value = ''
  if (!editing.value && !form.educatorId) { formError.value = t('validation.required', { field: t('careTeam.educator') }); return }
  if (!form.roleId) { formError.value = t('validation.required', { field: t('careTeam.role') }); return }
  if (!form.activeFrom) { formError.value = t('validation.required', { field: t('careTeam.activeFrom') }); return }
  if (form.activeTo && form.activeTo <= form.activeFrom) { formError.value = t('scheduling.recurring.rangeEndAfterStart'); return }
  saving.value = true
  try {
    if (editing.value) {
      await careTeamService.update(props.studentId, editing.value.id, {
        roleId: form.roleId,
        isPrimary: form.isPrimary,
        activeFrom: form.activeFrom,
        activeTo: form.activeTo || null,
        reason: form.reason.trim() || null,
        rowVersion: editing.value.rowVersion,
      })
    } else {
      await careTeamService.assign(props.studentId, {
        educatorId: form.educatorId,
        roleId: form.roleId,
        isPrimary: form.isPrimary,
        activeFrom: form.activeFrom,
        activeTo: form.activeTo || null,
        reason: form.reason.trim() || null,
      })
    }
    showForm.value = false
    await load()
    emit('changed')
  } catch (e: unknown) {
    formError.value = (e as Error).message
  } finally {
    saving.value = false
  }
}

async function confirmRemove() {
  if (!removeTarget.value) return
  removeError.value = ''
  saving.value = true
  try {
    await careTeamService.remove(props.studentId, removeTarget.value.id, removeTarget.value.rowVersion)
    removeTarget.value = null
    await load()
    emit('changed')
  } catch (e: unknown) {
    removeError.value = (e as Error).message
  } finally {
    saving.value = false
  }
}

const emit = defineEmits<{ changed: [] }>()

function formatDate(val?: string | null) {
  return val ? new Date(`${val}T00:00:00`).toLocaleDateString('tr-TR') : '—'
}
const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4 gap-4">
      <p class="text-sm text-muted-foreground">{{ t('careTeam.tabDescription') }}</p>
      <button
        v-if="canAssign"
        class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 shrink-0"
        @click="openAdd"
      >
        {{ t('careTeam.add') }}
      </button>
    </div>

    <label class="mb-3 flex items-center gap-2 text-sm text-muted-foreground">
      <input v-model="showEnded" type="checkbox" />
      {{ t('careTeam.showEnded') }}
    </label>

    <p v-if="listError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ listError }}</p>
    <div v-if="loading" class="py-8 text-center text-sm text-muted-foreground">{{ t('common.loading') }}</div>
    <div v-else-if="!rows.length && !listError" class="text-center py-12 text-muted-foreground">{{ t('careTeam.empty') }}</div>
    <div v-else class="space-y-3">
      <div
        v-for="a in rows"
        :key="a.id"
        class="rounded-xl border border-border bg-card p-4 shadow-sm flex items-start justify-between gap-4"
      >
        <div>
          <p class="font-medium text-foreground">
            {{ a.educatorName }}
            <span
              v-if="a.isPrimary"
              class="ml-2 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700"
            >{{ t('careTeam.primary') }}</span>
            <StatusBadge entity="careAssignment" :status="a.status" class="ml-2" />
          </p>
          <p class="text-xs text-muted-foreground mt-1">
            {{ roleLabel(a.roleId, a.roleCode) }}
            · {{ formatDate(a.activeFrom) }} – {{ a.activeTo ? formatDate(a.activeTo) : '…' }}
            <span v-if="a.reason"> · {{ a.reason }}</span>
          </p>
        </div>
        <div v-if="canAssign && a.status !== 'ended'" class="flex gap-2 shrink-0">
          <button class="px-3 py-1.5 text-xs rounded-lg border border-border hover:bg-accent" @click="openEdit(a)">
            {{ t('common.edit') }}
          </button>
          <button
            class="px-3 py-1.5 text-xs rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
            @click="removeTarget = a; removeError = ''"
          >
            {{ t('careTeam.remove') }}
          </button>
        </div>
      </div>
    </div>

    <FormModal
      :open="showForm"
      :title="editing ? t('careTeam.edit') : t('careTeam.add')"
      :saving="saving"
      @submit="submit"
      @close="showForm = false"
    >
      <div class="space-y-4">
        <p v-if="formError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ formError }}</p>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('careTeam.educator') }} *</label>
          <p v-if="editing" class="text-sm text-foreground">{{ editing.educatorName }}</p>
          <EducatorPicker v-else v-model="form.educatorId" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('careTeam.role') }} *</label>
          <select v-model="form.roleId" :class="inputClass">
            <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.label }}</option>
          </select>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('careTeam.activeFrom') }} *</label>
            <input v-model="form.activeFrom" type="date" :class="inputClass" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('careTeam.activeTo') }}</label>
            <input v-model="form.activeTo" type="date" :class="inputClass" />
          </div>
        </div>
        <div>
          <label class="flex items-center gap-2 text-sm text-foreground">
            <input v-model="form.isPrimary" type="checkbox" />
            {{ t('careTeam.isPrimary') }}
          </label>
          <p class="mt-1 text-xs text-muted-foreground">{{ t('careTeam.primaryHint') }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('careTeam.reason') }}</label>
          <input v-model="form.reason" type="text" :class="inputClass" />
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!removeTarget"
      :title="t('careTeam.remove')"
      :message="removeError || t('careTeam.removeConfirm')"
      :confirm-label="t('careTeam.remove')"
      :loading="saving"
      @confirm="confirmRemove"
      @cancel="removeTarget = null"
    />
  </div>
</template>
