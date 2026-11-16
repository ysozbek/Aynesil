<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCampStore } from '@/stores/camp.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import { educatorService } from '@/services/educator.service'
import { toLocalDateString } from '@/utils/datetime'
import { CampEducatorRoles, blocksDeactivation, periodHasStudents, periodPhase } from '@/utils/campRules'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { CampPeriodListItemDto, CampEducatorDto } from '@/types/camp.types'
import type { EducatorListItemDto } from '@/types/educator.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const campStore = useCampStore()
const { visible } = useModule(Modules.Camps)
const id = route.params.id as string
const camp = computed(() => campStore.currentCamp)
const today = toLocalDateString()
const actionError = ref('')
const notice = ref('')

const inputCls = 'w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
const labelCls = 'block text-xs font-medium text-muted-foreground mb-1'

function run(fn: () => Promise<unknown>, success?: string) {
  actionError.value = ''; notice.value = ''
  return fn().then(
    () => { if (success) notice.value = success; return true },
    (e: unknown) => { actionError.value = (e as Error).message; return false },
  )
}

// ── Kamp: etkinleştir / pasifleştir / sil ─────────────────────────────────────
const campAction = ref<'activate' | 'deactivate' | 'delete' | null>(null)
const deactivationHint = computed(() => !!camp.value && blocksDeactivation(camp.value.periods, today))

async function confirmCampAction() {
  const action = campAction.value
  if (!action || !camp.value) return
  campAction.value = null
  if (action === 'delete') {
    if (await run(() => campStore.deleteCamp(id))) router.replace({ name: 'camps' })
  } else {
    await run(() => (action === 'activate' ? campStore.activateCamp(id) : campStore.deactivateCamp(id)))
  }
}

// ── Dönem: ekle / düzenle / sil ───────────────────────────────────────────────
const periodModal = ref(false)
const editingPeriodId = ref<string | null>(null)
const periodError = ref('')
const periodForm = reactive({ name: '', startDate: '', endDate: '', capacity: undefined as number | undefined })

function openPeriod(p?: CampPeriodListItemDto) {
  editingPeriodId.value = p?.id ?? null
  periodError.value = ''
  Object.assign(periodForm, p
    ? { name: p.name, startDate: p.startDate, endDate: p.endDate, capacity: p.capacity ?? undefined }
    : { name: '', startDate: '', endDate: '', capacity: undefined })
  periodModal.value = true
}

async function savePeriod() {
  if (!camp.value) return
  periodError.value = ''
  const payload = { ...periodForm, capacity: periodForm.capacity || undefined }
  try {
    if (editingPeriodId.value) await campStore.updatePeriod(camp.value.id, editingPeriodId.value, payload)
    else await campStore.createPeriod(camp.value.id, payload)
    periodModal.value = false
  } catch (e: unknown) {
    periodError.value = (e as Error).message
  }
}

const periodToDelete = ref<CampPeriodListItemDto | null>(null)
async function confirmDeletePeriod() {
  const p = periodToDelete.value
  periodToDelete.value = null
  if (p && camp.value) await run(() => campStore.deletePeriod(camp.value!.id, p.id))
}

// ── Eğitmen: ata / rol değiştir / kaldır ──────────────────────────────────────
const educatorModal = ref(false)
const educatorError = ref('')
const educatorOptions = ref<EducatorListItemDto[]>([])
const educatorForm = reactive({ educatorId: '', role: 'lead' })
const availableEducators = computed(() => {
  const assigned = new Set(campStore.educators.filter((e) => !e.campPeriodId && !e.campActivityId).map((e) => e.educatorId))
  return educatorOptions.value.filter((e) => !assigned.has(e.id))
})

async function openEducatorModal() {
  educatorError.value = ''
  Object.assign(educatorForm, { educatorId: '', role: 'lead' })
  educatorModal.value = true
  try {
    const res = await educatorService.listEducators({ page: 1, pageSize: 100, isActive: true })
    educatorOptions.value = res.data?.items ?? []
  } catch (e: unknown) { educatorError.value = (e as Error).message }
}

async function saveEducator() {
  if (!camp.value || !educatorForm.educatorId) return
  educatorError.value = ''
  try {
    await campStore.assignEducator(camp.value.id, educatorForm.educatorId, educatorForm.role)
    educatorModal.value = false
  } catch (e: unknown) { educatorError.value = (e as Error).message }
}

function changeRole(e: CampEducatorDto, role: string) {
  if (camp.value && role !== e.role) run(() => campStore.changeEducatorRole(camp.value!.id, e.id, role))
}

const educatorToRemove = ref<CampEducatorDto | null>(null)
async function confirmRemoveEducator() {
  const e = educatorToRemove.value
  educatorToRemove.value = null
  if (e && camp.value) await run(() => campStore.removeEducator(camp.value!.id, e.id))
}

onMounted(async () => {
  campStore.clearCurrent()
  await campStore.fetchCamp(id)
  if (campStore.currentCamp) await campStore.fetchEducators(id)
})
</script>

<template>
  <div>
    <div v-if="campStore.loading && !camp" class="py-20 text-center text-muted-foreground">{{ t('common.loading') }}</div>
    <div v-else-if="!camp" class="py-20 text-center text-muted-foreground">{{ t('errors.notFound') }}</div>

    <template v-else>
      <PageHeader :title="camp.name" :description="`${camp.code} · ${camp.location ?? '—'}`">
        <StatusBadge :value="camp.isActive" :true-label="t('common.active')" :false-label="t('common.passive')" />
        <button type="button" class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="router.push({ name: 'camps' })">
          {{ t('common.back') }}
        </button>
        <button
          v-if="visible(Permissions.Camps.Update)" type="button"
          class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          @click="router.push({ name: 'camp-edit', params: { id: camp.id } })"
        >{{ t('common.edit') }}</button>
        <button
          v-if="visible(Permissions.Camps.Activate)" type="button"
          class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent disabled:opacity-50"
          :disabled="camp.isActive && deactivationHint"
          :title="camp.isActive && deactivationHint ? t('camp.actions.deactivateMessage', { name: camp.name }) : undefined"
          @click="campAction = camp.isActive ? 'deactivate' : 'activate'"
        >{{ camp.isActive ? t('camp.actions.deactivate') : t('camp.actions.activate') }}</button>
        <button
          v-if="visible(Permissions.Camps.Delete)" type="button"
          class="px-3 py-2 text-sm rounded-lg border border-red-200 text-red-600 hover:bg-red-50"
          @click="campAction = 'delete'"
        >{{ t('common.delete') }}</button>
      </PageHeader>

      <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ actionError }}</p>
      <p v-if="notice" class="mb-4 text-sm text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2">{{ notice }}</p>

      <div class="grid gap-5 lg:grid-cols-3 mb-6">
        <div class="rounded-xl border border-border bg-card p-5">
          <h3 class="font-semibold mb-3">{{ t('camp.detail.info') }}</h3>
          <dl class="space-y-2 text-sm">
            <div><dt class="inline text-muted-foreground">{{ t('camp.fields.type') }}:</dt> <dd class="inline font-medium ml-1">{{ camp.campTypeCode ?? '—' }}</dd></div>
            <div><dt class="inline text-muted-foreground">{{ t('camp.fields.capacity') }}:</dt> <dd class="inline font-medium ml-1">{{ camp.capacity ?? '—' }}</dd></div>
            <div><dt class="inline text-muted-foreground">{{ t('camp.fields.location') }}:</dt> <dd class="inline font-medium ml-1">{{ camp.location ?? '—' }}</dd></div>
            <div v-if="camp.description" class="pt-2">
              <dt class="text-muted-foreground text-xs mb-1">{{ t('camp.fields.description') }}</dt>
              <dd>{{ camp.description }}</dd>
            </div>
          </dl>
        </div>

        <div class="rounded-xl border border-border bg-card p-5 lg:col-span-2">
          <div class="flex items-center justify-between mb-3">
            <h3 class="font-semibold">{{ t('camp.detail.periods') }}</h3>
            <button
              v-if="visible(Permissions.Camps.ManagePeriods)" type="button"
              class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
              @click="openPeriod()"
            >{{ t('camp.detail.addPeriod') }}</button>
          </div>
          <p v-if="camp.periods.length === 0" class="py-6 text-center text-sm text-muted-foreground">{{ t('camp.detail.noPeriods') }}</p>
          <div v-else class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-left text-xs text-muted-foreground border-b border-border">
                  <th class="py-2 pr-3">{{ t('camp.fields.periodName') }}</th>
                  <th class="pr-3">{{ t('camp.fields.startDate') }}</th>
                  <th class="pr-3">{{ t('camp.fields.endDate') }}</th>
                  <th class="pr-3 text-center">{{ t('camp.fields.capacity') }}</th>
                  <th class="pr-3 text-center">{{ t('camp.fields.enrolled') }}</th>
                  <th class="pr-3 text-center">{{ t('camp.fields.waitlist') }}</th>
                  <th class="text-right">{{ t('common.actions') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="p in camp.periods" :key="p.id" class="border-b border-border/60">
                  <td class="py-2 pr-3 font-medium">
                    {{ p.name }}
                    <span class="ml-1 text-xs text-muted-foreground">{{ t(`camp.period.phase.${periodPhase(p, today)}`) }}</span>
                  </td>
                  <td class="pr-3 text-muted-foreground">{{ p.startDate }}</td>
                  <td class="pr-3 text-muted-foreground">{{ p.endDate }}</td>
                  <td class="pr-3 text-center">{{ p.capacity ?? '∞' }}</td>
                  <td class="pr-3 text-center text-emerald-700 font-semibold">{{ p.enrolledCount }}</td>
                  <td class="pr-3 text-center text-amber-600">{{ p.waitlistCount }}</td>
                  <td class="text-right whitespace-nowrap">
                    <div class="flex items-center justify-end gap-2">
                      <button
                        v-if="visible(Permissions.CampEnrollments.Read)" type="button" class="text-xs text-primary hover:underline"
                        @click="router.push({ name: 'camp-enrollment', params: { periodId: p.id } })"
                      >{{ t('camp.detail.enrollments') }}</button>
                      <button
                        v-if="visible(Permissions.Camps.ManagePeriods)" type="button" class="text-xs text-primary hover:underline"
                        @click="openPeriod(p)"
                      >{{ t('common.edit') }}</button>
                      <button
                        v-if="visible(Permissions.Camps.ManagePeriods)" type="button"
                        class="text-xs text-red-600 hover:underline disabled:opacity-40 disabled:no-underline"
                        :disabled="periodHasStudents(p)"
                        :title="periodHasStudents(p) ? t('camp.period.hasStudentsHint') : undefined"
                        @click="periodToDelete = p"
                      >{{ t('common.delete') }}</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Eğitmenler -->
      <div class="rounded-xl border border-border bg-card p-5">
        <div class="flex items-center justify-between mb-3">
          <h3 class="font-semibold">{{ t('camp.detail.educators') }}</h3>
          <button
            v-if="visible(Permissions.CampEducators.Manage)" type="button"
            class="px-3 py-1.5 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
            @click="openEducatorModal"
          >{{ t('camp.detail.assignEducator') }}</button>
        </div>
        <p v-if="campStore.educators.length === 0" class="py-6 text-center text-sm text-muted-foreground">{{ t('camp.detail.noEducators') }}</p>
        <ul v-else class="divide-y divide-border/60">
          <li v-for="e in campStore.educators" :key="e.id" class="flex items-center justify-between gap-3 py-2">
            <span class="font-medium text-sm">{{ e.educatorName ?? e.educatorId }}</span>
            <div class="flex items-center gap-3">
              <select
                v-if="visible(Permissions.CampEducators.Manage)"
                :value="e.role"
                class="h-8 px-2 text-sm rounded-lg border border-border bg-transparent"
                @change="changeRole(e, ($event.target as HTMLSelectElement).value)"
              >
                <option v-for="r in CampEducatorRoles" :key="r" :value="r">{{ t(`camp.educatorRole.${r}`) }}</option>
              </select>
              <span v-else class="text-xs text-muted-foreground">{{ t(`camp.educatorRole.${e.role}`) }}</span>
              <button
                v-if="visible(Permissions.CampEducators.Manage)" type="button" class="text-xs text-red-600 hover:underline"
                @click="educatorToRemove = e"
              >{{ t('camp.detail.removeEducator') }}</button>
            </div>
          </li>
        </ul>
      </div>
    </template>

    <!-- Dönem ekle / düzenle -->
    <FormModal
      :open="periodModal"
      :title="editingPeriodId ? t('camp.detail.editPeriod') : t('camp.detail.addPeriod')"
      :saving="campStore.saving"
      @submit="savePeriod"
      @close="periodModal = false"
    >
      <p v-if="periodError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ periodError }}</p>
      <div class="grid grid-cols-2 gap-3">
        <div class="col-span-2">
          <label :class="labelCls">{{ t('camp.fields.periodName') }} *</label>
          <input v-model="periodForm.name" type="text" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.fields.startDate') }} *</label>
          <input v-model="periodForm.startDate" type="date" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.fields.endDate') }} *</label>
          <input v-model="periodForm.endDate" type="date" :class="inputCls" />
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.fields.capacity') }}</label>
          <input v-model.number="periodForm.capacity" type="number" min="1" :class="inputCls" />
        </div>
      </div>
    </FormModal>

    <!-- Eğitmen ata -->
    <FormModal
      :open="educatorModal"
      :title="t('camp.detail.assignEducator')"
      :saving="campStore.saving"
      @submit="saveEducator"
      @close="educatorModal = false"
    >
      <p v-if="educatorError" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2" role="alert">{{ educatorError }}</p>
      <div class="space-y-3">
        <div>
          <label :class="labelCls">{{ t('camp.detail.selectEducator') }} *</label>
          <select v-model="educatorForm.educatorId" :class="inputCls">
            <option value="">—</option>
            <option v-for="e in availableEducators" :key="e.id" :value="e.id">{{ e.fullName }}</option>
          </select>
          <p v-if="availableEducators.length === 0" class="mt-1 text-xs text-muted-foreground">{{ t('camp.detail.noAvailableEducators') }}</p>
        </div>
        <div>
          <label :class="labelCls">{{ t('camp.detail.role') }}</label>
          <select v-model="educatorForm.role" :class="inputCls">
            <option v-for="r in CampEducatorRoles" :key="r" :value="r">{{ t(`camp.educatorRole.${r}`) }}</option>
          </select>
        </div>
      </div>
    </FormModal>

    <ConfirmModal
      :open="!!campAction"
      :title="campAction ? t(`camp.actions.${campAction}Title`) : ''"
      :message="campAction && camp ? t(`camp.actions.${campAction}Message`, { name: camp.name }) : ''"
      :confirm-label="campAction === 'delete' ? t('common.delete') : campAction ? t(`camp.actions.${campAction}`) : ''"
      :confirm-class="campAction === 'activate' ? 'bg-primary text-primary-foreground hover:opacity-90' : undefined"
      :loading="campStore.saving"
      @confirm="confirmCampAction"
      @cancel="campAction = null"
    />
    <ConfirmModal
      :open="!!periodToDelete"
      :title="t('camp.period.deleteTitle')"
      :message="t('camp.period.deleteMessage', { name: periodToDelete?.name ?? '' })"
      :confirm-label="t('common.delete')"
      :loading="campStore.saving"
      @confirm="confirmDeletePeriod"
      @cancel="periodToDelete = null"
    />
    <ConfirmModal
      :open="!!educatorToRemove"
      :title="t('camp.detail.removeEducator')"
      :message="educatorToRemove?.educatorName ?? ''"
      :confirm-label="t('camp.detail.removeEducator')"
      :loading="campStore.saving"
      @confirm="confirmRemoveEducator"
      @cancel="educatorToRemove = null"
    />
  </div>
</template>
