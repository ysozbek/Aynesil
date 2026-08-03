<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useStudentStore } from '@/stores/student.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useStudentStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const genders = ref<RefValueItem[]>([])
const statuses = ref<RefValueItem[]>([])

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  firstName: '',
  lastName: '',
  studentNo: '',
  nationalId: '',
  birthDate: '',
  gender: '',
  primaryCampusId: '',
  statusId: '',
  notes: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  await Promise.all([
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('GENDER').then(v => { genders.value = v }),
    refData.getValues('STUDENT_STATUS').then(v => { statuses.value = v }),
  ])

  if (isEdit.value && id.value) {
    await store.fetchOne(id.value)
    const s = store.current
    if (s) {
      form.firstName = s.firstName
      form.lastName = s.lastName
      form.studentNo = s.studentNo ?? ''
      form.nationalId = s.nationalId ?? ''
      form.birthDate = s.birthDate ?? ''
      form.gender = s.gender ?? ''
      form.primaryCampusId = s.primaryCampusId ?? ''
      form.notes = s.notes ?? ''
      form.rowVersion = s.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.firstName.trim()) {
    errors.firstName = t('validation.required', { field: t('student.firstName') })
    valid = false
  }
  if (!form.lastName.trim()) {
    errors.lastName = t('validation.required', { field: t('student.lastName') })
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
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        studentNo: form.studentNo.trim() || null,
        nationalId: form.nationalId.trim() || null,
        birthDate: form.birthDate || null,
        gender: form.gender || null,
        primaryCampusId: form.primaryCampusId || null,
        notes: form.notes.trim() || null,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'student-detail', params: { id: id.value } })
    } else {
      const result = await store.create({
        corporationId: form.corporationId,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        studentNo: form.studentNo.trim() || null,
        nationalId: form.nationalId.trim() || null,
        birthDate: form.birthDate || null,
        gender: form.gender || null,
        primaryCampusId: form.primaryCampusId || null,
        statusId: form.statusId || null,
        leadId: null,
        notes: form.notes.trim() || null,
      })
      router.push({ name: 'student-detail', params: { id: result.id } })
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
      :title="isEdit ? t('student.edit') : t('student.create')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Personal Info -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('student.fullName') }}</h3>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.firstName') }} *</label>
            <input
              v-model="form.firstName"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.firstName ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.firstName" class="mt-1 text-xs text-red-600">{{ errors.firstName }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.lastName') }} *</label>
            <input
              v-model="form.lastName"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.lastName ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.lastName" class="mt-1 text-xs text-red-600">{{ errors.lastName }}</p>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.studentNo') }}</label>
            <input
              v-model="form.studentNo"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.nationalId') }}</label>
            <input
              v-model="form.nationalId"
              type="text"
              maxlength="11"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.birthDate') }}</label>
            <input
              v-model="form.birthDate"
              type="date"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.gender') }}</label>
            <select
              v-model="form.gender"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="g in genders" :key="g.id" :value="g.code">{{ g.label }}</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Campus & Status -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('student.primaryCampus') }}</h3>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.primaryCampus') }}</label>
            <select
              v-model="form.primaryCampusId"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div v-if="!isEdit">
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('student.status') }}</label>
            <select
              v-model="form.statusId"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">{{ t('common.select') }}</option>
              <option v-for="s in statuses" :key="s.id" :value="s.id">{{ s.label }}</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Notes -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('student.notes') }}</h3>
        <textarea
          v-model="form.notes"
          rows="4"
          class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none"
        />
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
