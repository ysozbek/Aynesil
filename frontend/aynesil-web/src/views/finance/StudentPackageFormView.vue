<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { usePackageStore } from '@/stores/package.store'
import { useStudentStore } from '@/stores/student.store'
import { financeService } from '@/services/finance.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { StudentListItemDto } from '@/types/student.types'
import type { PackageDefinitionListItemDto } from '@/types/finance.types'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const store = usePackageStore()
const studentStore = useStudentStore()

const students = ref<StudentListItemDto[]>([])
const definitions = ref<PackageDefinitionListItemDto[]>([])

const today = new Date().toISOString().slice(0, 10)

const form = reactive({
  studentId: typeof route.query.studentId === 'string' ? route.query.studentId : '',
  packageDefinitionId: '',
  totalCredits: '' as string | number,
  price: '' as string | number,
  currency: 'TRY',
  expiresOn: '',
})

const errors = reactive<Record<string, string>>({})

onMounted(async () => {
  await Promise.all([
    studentStore.fetchList({ page: 1, pageSize: 200 }).then(() => {
      students.value = studentStore.list.items
    }),
    financeService
      .listPackageDefinitions({ isActive: true, page: 1, pageSize: 100 })
      .then((res) => {
        if (res.success && res.data) definitions.value = res.data.items
      }),
  ])
})

function addDays(isoDate: string, days: number): string {
  const d = new Date(`${isoDate}T00:00:00`)
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}

watch(() => form.packageDefinitionId, async (id) => {
  if (!id) return
  const res = await financeService.getPackageDefinition(id)
  if (!res.success || !res.data) return
  const def = res.data
  form.totalCredits = def.totalCredits ?? ''
  form.price = def.listPrice
  form.currency = def.currency
  form.expiresOn = def.validityDays ? addDays(today, def.validityDays) : ''
})

function validate(): boolean {
  Object.keys(errors).forEach((k) => delete errors[k])
  if (!form.studentId) errors.studentId = t('validation.required', { field: t('scheduling.session.student') })
  if (!form.totalCredits || Number(form.totalCredits) <= 0) errors.totalCredits = t('finance.studentPackage.creditsPositive')
  if (form.price === '' || Number(form.price) < 0) errors.price = t('finance.studentPackage.priceNonNegative')
  if (form.expiresOn && form.expiresOn < today) errors.expiresOn = t('finance.studentPackage.expiresInPast')
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    await store.createStudentPackage({
      studentId: form.studentId,
      packageDefinitionId: form.packageDefinitionId || undefined,
      totalCredits: Number(form.totalCredits),
      price: Number(form.price),
      currency: form.currency,
      expiresOn: form.expiresOn || undefined,
    })
    router.push({ name: 'student-packages' })
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <div>
    <PageHeader
      :title="t('finance.studentPackage.assign')"
      :description="t('finance.studentPackage.assignDescription')"
    />

    <div class="max-w-2xl space-y-6">
      <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
        {{ errors.submit }}
      </div>

      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.student') }} <span class="text-red-500">*</span>
          </label>
          <select v-model="form.studentId" :class="[inputClass, errors.studentId ? 'border-red-400' : 'border-border']">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="s in students" :key="s.id" :value="s.id">{{ s.fullName }}</option>
          </select>
          <p v-if="errors.studentId" class="text-xs text-red-500 mt-1">{{ errors.studentId }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.studentPackage.definition') }}</label>
          <select v-model="form.packageDefinitionId" :class="[inputClass, 'border-border']">
            <option value="">{{ t('finance.studentPackage.customPackage') }}</option>
            <option v-for="d in definitions" :key="d.id" :value="d.id">
              {{ d.name }}<template v-if="d.totalCredits"> · {{ d.totalCredits }}</template>
            </option>
          </select>
          <p class="text-xs text-muted-foreground mt-1">{{ t('finance.studentPackage.definitionHint') }}</p>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-card shadow-sm p-6 space-y-5">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('finance.studentPackage.totalCredits') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.totalCredits"
              type="number"
              min="1"
              step="1"
              :class="[inputClass, errors.totalCredits ? 'border-red-400' : 'border-border']"
            />
            <p v-if="errors.totalCredits" class="text-xs text-red-500 mt-1">{{ errors.totalCredits }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.studentPackage.expiresOn') }}</label>
            <input
              v-model="form.expiresOn"
              type="date"
              :min="today"
              :class="[inputClass, errors.expiresOn ? 'border-red-400' : 'border-border']"
            />
            <p v-if="errors.expiresOn" class="text-xs text-red-500 mt-1">{{ errors.expiresOn }}</p>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('finance.studentPackage.price') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.price"
              type="number"
              min="0"
              step="0.01"
              :class="[inputClass, errors.price ? 'border-red-400' : 'border-border']"
            />
            <p v-if="errors.price" class="text-xs text-red-500 mt-1">{{ errors.price }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('finance.package.currency') }}</label>
            <input v-model="form.currency" type="text" maxlength="3" disabled :class="[inputClass, 'border-border opacity-60']" />
          </div>
        </div>

        <p class="text-xs text-muted-foreground">{{ t('finance.studentPackage.paymentHint') }}</p>
      </div>

      <div class="flex justify-end gap-3">
        <button @click="router.back()" class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent">{{ t('common.cancel') }}</button>
        <button @click="submit" :disabled="store.saving" class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50">
          {{ store.saving ? t('common.saving') : t('finance.studentPackage.assign') }}
        </button>
      </div>
    </div>
  </div>
</template>
