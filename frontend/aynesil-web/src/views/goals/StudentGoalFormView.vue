<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useGoalStore } from '@/stores/goal.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useGoalStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const categories = ref<RefValueItem[]>([])
const developmentAreas = ref<RefValueItem[]>([])

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  studentId: '',
  statement: '',
  horizon: 'long_term',
  templateId: '',
  categoryId: '',
  developmentAreaId: '',
  parentGoalId: '',
  masteryCriteria: '',
  baseline: '',
  targetValue: '',
  startDate: '',
  targetDate: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  await Promise.all([
    refData.getValues('GOAL_CATEGORY').then(v => { categories.value = v }),
    refData.getValues('DEVELOPMENT_AREA').then(v => { developmentAreas.value = v }),
  ])

  if (isEdit.value && id.value) {
    await store.fetchStudentGoal(id.value)
    const g = store.currentStudentGoal
    if (g) {
      form.statement = g.statement
      form.horizon = g.horizon
      form.templateId = g.templateId ?? ''
      form.categoryId = g.categoryId ?? ''
      form.developmentAreaId = g.developmentAreaId ?? ''
      form.parentGoalId = g.parentGoalId ?? ''
      form.masteryCriteria = g.masteryCriteria ?? ''
      form.baseline = g.baseline ?? ''
      form.targetValue = g.targetValue !== null && g.targetValue !== undefined ? String(g.targetValue) : ''
      form.startDate = g.startDate ?? ''
      form.targetDate = g.targetDate ?? ''
      form.rowVersion = g.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.statement.trim()) {
    errors.statement = t('validation.required', { field: t('goal.studentGoal.statement') })
    valid = false
  }
  if (!form.horizon) {
    errors.horizon = t('validation.required', { field: t('goal.studentGoal.horizon') })
    valid = false
  }
  if (!isEdit.value && !form.studentId.trim()) {
    errors.studentId = t('validation.required', { field: t('bep.studentName') })
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
      await store.updateStudentGoal(id.value, {
        statement: form.statement,
        categoryId: form.categoryId || null,
        developmentAreaId: form.developmentAreaId || null,
        masteryCriteria: form.masteryCriteria || null,
        baseline: form.baseline || null,
        targetValue: form.targetValue ? parseFloat(form.targetValue) : null,
        startDate: form.startDate || null,
        targetDate: form.targetDate || null,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'student-goal-detail', params: { id: id.value } })
    } else {
      const result = await store.createStudentGoal({
        corporationId: form.corporationId,
        studentId: form.studentId,
        statement: form.statement,
        horizon: form.horizon,
        templateId: form.templateId || null,
        categoryId: form.categoryId || null,
        developmentAreaId: form.developmentAreaId || null,
        parentGoalId: form.parentGoalId || null,
        masteryCriteria: form.masteryCriteria || null,
        baseline: form.baseline || null,
        targetValue: form.targetValue ? parseFloat(form.targetValue) : null,
        startDate: form.startDate || null,
        targetDate: form.targetDate || null,
      })
      router.push({ name: 'student-goal-detail', params: { id: result.id } })
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
      :title="isEdit ? t('common.edit') : t('goal.studentGoal.create')"
      :description="t('goal.studentGoal.title')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Core fields -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">Hedef Bilgileri</h3>

        <div v-if="!isEdit">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.studentName') }} ID *</label>
          <input
            v-model="form.studentId"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.studentId ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.studentId" class="mt-1 text-xs text-red-600">{{ errors.studentId }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.statement') }} *</label>
          <textarea
            v-model="form.statement"
            rows="4"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary resize-none"
            :class="errors.statement ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.statement" class="mt-1 text-xs text-red-600">{{ errors.statement }}</p>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.horizon') }} *</label>
            <select
              v-model="form.horizon"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary bg-transparent"
              :class="errors.horizon ? 'border-red-400' : 'border-border'"
            >
              <option value="long_term">{{ t('goal.studentGoal.horizon.longTerm') }}</option>
              <option value="short_term">{{ t('goal.studentGoal.horizon.shortTerm') }}</option>
            </select>
            <p v-if="errors.horizon" class="mt-1 text-xs text-red-600">{{ errors.horizon }}</p>
          </div>
          <div v-if="!isEdit">
            <label class="block text-sm font-medium text-foreground mb-1">Şablon ID</label>
            <input v-model="form.templateId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.category') }}</label>
            <select v-model="form.categoryId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.developmentArea') }}</label>
            <select v-model="form.developmentAreaId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="a in developmentAreas" :key="a.id" :value="a.id">{{ a.label }}</option>
            </select>
          </div>
        </div>

        <div v-if="!isEdit">
          <label class="block text-sm font-medium text-foreground mb-1">Üst Hedef ID</label>
          <input v-model="form.parentGoalId" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>

      <!-- Criteria & measurements -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">Kriter ve Ölçümler</h3>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.masteryCriteria') }}</label>
          <textarea v-model="form.masteryCriteria" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.baseline') }}</label>
            <input v-model="form.baseline" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.targetValue') }}</label>
            <input v-model="form.targetValue" type="number" step="0.01" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.startDate') }}</label>
            <input v-model="form.startDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.studentGoal.targetDate') }}</label>
            <input v-model="form.targetDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3">
        <button type="button" @click="router.back()" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="saving" class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60">
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
