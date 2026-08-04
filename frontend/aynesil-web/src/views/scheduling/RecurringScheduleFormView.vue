<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useSchedulingStore } from '@/stores/scheduling.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = useSchedulingStore()
const refData = useRefDataStore()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const sessionTypes = ref<RefValueItem[]>([])

const WEEKDAYS = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']
const WEEKDAY_CODES = ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU']

const form = reactive({
  campusId: '',
  roomId: '',
  sessionTypeId: '',
  frequency: 'weekly',
  intervalCount: 1,
  byWeekday: [] as string[],
  startTime: '09:00',
  durationMinutes: 60,
  rangeStart: '',
  rangeEnd: '',
  maxOccurrences: '' as string | number,
  educatorIds: '',
})

const errors = reactive<Record<string, string>>({})

onMounted(async () => {
  await refData.getValues('SESSION_TYPE').then(v => { sessionTypes.value = v })
})

function toggleWeekday(code: string) {
  const idx = form.byWeekday.indexOf(code)
  if (idx >= 0) form.byWeekday.splice(idx, 1)
  else form.byWeekday.push(code)
}

function validate(): boolean {
  Object.keys(errors).forEach(k => delete (errors as Record<string, string>)[k])
  if (!form.sessionTypeId) errors.sessionTypeId = t('validation.required', { field: t('scheduling.session.type') })
  if (!form.startTime) errors.startTime = t('validation.required', { field: t('scheduling.recurring.startTime') })
  if (!form.rangeStart) errors.rangeStart = t('validation.required', { field: t('scheduling.recurring.from') })
  if (form.frequency === 'weekly' && form.byWeekday.length === 0) errors.byWeekday = t('scheduling.recurring.selectDay')
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    const result = await store.createRecurringSchedule({
      corporationId: corporationId.value,
      campusId: form.campusId,
      roomId: form.roomId || undefined,
      sessionTypeId: form.sessionTypeId,
      frequency: form.frequency,
      intervalCount: form.intervalCount,
      byWeekday: form.frequency === 'weekly' ? form.byWeekday : undefined,
      startTime: form.startTime,
      durationMinutes: form.durationMinutes,
      rangeStart: form.rangeStart,
      rangeEnd: form.rangeEnd || undefined,
      maxOccurrences: form.maxOccurrences ? Number(form.maxOccurrences) : undefined,
      educatorIds: form.educatorIds ? form.educatorIds.split(',').map(s => s.trim()).filter(Boolean) : undefined,
    })
    router.push({ name: 'recurring-schedules' })
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="t('scheduling.recurring.create')"
      :description="t('scheduling.recurring.createDescription')"
    />

    <div class="max-w-2xl">
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-5">

        <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
          {{ errors.submit }}
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.type') }} <span class="text-red-500">*</span>
          </label>
          <select v-model="form.sessionTypeId" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.sessionTypeId ? 'border-red-400' : 'border-border'">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="st in sessionTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
          </select>
          <p v-if="errors.sessionTypeId" class="text-xs text-red-500 mt-1">{{ errors.sessionTypeId }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.frequency.label') }} <span class="text-red-500">*</span></label>
            <select v-model="form.frequency" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="daily">{{ t('scheduling.recurring.frequency.daily') }}</option>
              <option value="weekly">{{ t('scheduling.recurring.frequency.weekly') }}</option>
              <option value="monthly">{{ t('scheduling.recurring.frequency.monthly') }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.interval') }}</label>
            <input v-model.number="form.intervalCount" type="number" min="1" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <!-- Weekday picker -->
        <div v-if="form.frequency === 'weekly'">
          <label class="block text-sm font-medium text-foreground mb-2">{{ t('scheduling.recurring.weekdays') }} <span class="text-red-500">*</span></label>
          <div class="flex gap-1 flex-wrap">
            <button
              v-for="(day, idx) in WEEKDAYS"
              :key="day"
              type="button"
              @click="toggleWeekday(WEEKDAY_CODES[idx])"
              :class="[
                'w-9 h-9 rounded-lg text-xs font-medium border transition-colors',
                form.byWeekday.includes(WEEKDAY_CODES[idx])
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-border text-muted-foreground hover:bg-accent'
              ]"
            >
              {{ t(`scheduling.recurring.weekday.${day}`).slice(0, 2) }}
            </button>
          </div>
          <p v-if="errors.byWeekday" class="text-xs text-red-500 mt-1">{{ errors.byWeekday }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.startTime') }} <span class="text-red-500">*</span></label>
            <input v-model="form.startTime" type="time" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.startTime ? 'border-red-400' : 'border-border'" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.duration') }} (dk)</label>
            <input v-model.number="form.durationMinutes" type="number" min="15" step="15" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.from') }} <span class="text-red-500">*</span></label>
            <input v-model="form.rangeStart" type="date" class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" :class="errors.rangeStart ? 'border-red-400' : 'border-border'" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.to') }}</label>
            <input v-model="form.rangeEnd" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.recurring.maxOccurrences') }}</label>
          <input v-model="form.maxOccurrences" type="number" min="1" :placeholder="t('scheduling.recurring.maxOccurrencesHint')" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button @click="router.back()" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
          <button @click="submit" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
