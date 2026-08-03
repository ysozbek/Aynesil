<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useBepStore } from '@/stores/bep.store'
import { useBranchStore } from '@/stores/branch.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useBepStore()
const branchStore = useBranchStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  studentId: '',
  title: '',
  academicPeriodId: '',
  campusId: '',
  preparedBy: '',
  effectiveFrom: '',
  effectiveTo: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  await Promise.all([
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    store.periodList.items.length === 0 ? store.fetchPeriods({ corporationId: form.corporationId, pageSize: 200 }) : Promise.resolve(),
  ])

  if (isEdit.value && id.value) {
    await store.fetchPlan(id.value)
    const plan = store.currentPlan
    if (plan) {
      form.title = plan.title
      form.studentId = plan.studentId
      form.academicPeriodId = plan.academicPeriodId ?? ''
      form.campusId = plan.campusId ?? ''
      form.preparedBy = plan.preparedBy ?? ''
      form.effectiveFrom = plan.effectiveFrom ?? ''
      form.effectiveTo = plan.effectiveTo ?? ''
      form.rowVersion = plan.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.title.trim()) {
    errors.title = t('validation.required', { field: t('bep.title2') })
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
      await store.updatePlan(id.value, {
        title: form.title,
        academicPeriodId: form.academicPeriodId || null,
        campusId: form.campusId || null,
        preparedBy: form.preparedBy || null,
        effectiveFrom: form.effectiveFrom || null,
        effectiveTo: form.effectiveTo || null,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'bep-detail', params: { id: id.value } })
    } else {
      const result = await store.createPlan({
        corporationId: form.corporationId,
        studentId: form.studentId,
        title: form.title,
        academicPeriodId: form.academicPeriodId || null,
        campusId: form.campusId || null,
        preparedBy: form.preparedBy || null,
        effectiveFrom: form.effectiveFrom || null,
        effectiveTo: form.effectiveTo || null,
      })
      router.push({ name: 'bep-detail', params: { id: result.id } })
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
      :title="isEdit ? t('bep.edit') : t('bep.create')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Basic Info -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('bep.title2') }}</h3>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.title2') }} *</label>
          <input
            v-model="form.title"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.title ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.title" class="mt-1 text-xs text-red-600">{{ errors.title }}</p>
        </div>

        <div v-if="!isEdit">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.studentName') }} *</label>
          <input
            v-model="form.studentId"
            type="text"
            :placeholder="t('bep.studentName') + ' ID'"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.studentId ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.studentId" class="mt-1 text-xs text-red-600">{{ errors.studentId }}</p>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.academicPeriod') }}</label>
            <select
              v-model="form.academicPeriodId"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="p in store.periodList.items" :key="p.id" :value="p.id">{{ p.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('common.campus') }}</label>
            <select
              v-model="form.campusId"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.preparedBy') }}</label>
          <input
            v-model="form.preparedBy"
            type="text"
            :placeholder="t('bep.preparedBy') + ' ID'"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
      </div>

      <!-- Dates -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('bep.effectiveFrom') }} / {{ t('bep.effectiveTo') }}</h3>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.effectiveFrom') }}</label>
            <input
              v-model="form.effectiveFrom"
              type="date"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('bep.effectiveTo') }}</label>
            <input
              v-model="form.effectiveTo"
              type="date"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3">
        <button
          type="button"
          @click="router.back()"
          class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="saving"
          class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60"
        >
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
