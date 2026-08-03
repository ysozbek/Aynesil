<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useEducatorStore } from '@/stores/educator.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useEducatorStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const titles = ref<RefValueItem[]>([])
const employmentTypes = ref<RefValueItem[]>([])

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  firstName: '',
  lastName: '',
  titleId: '',
  email: '',
  phone: '',
  employmentType: '',
  hireDate: '',
  primaryCampusId: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  await Promise.all([
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
    refData.getValues('EDUCATOR_TITLE').then(v => { titles.value = v }),
    refData.getValues('EMPLOYMENT_TYPE').then(v => { employmentTypes.value = v }),
  ])

  if (isEdit.value && id.value) {
    await store.fetchOne(id.value)
    const educator = store.current
    if (educator) {
      form.firstName = educator.firstName
      form.lastName = educator.lastName
      form.titleId = educator.titleId ?? ''
      form.email = educator.email ?? ''
      form.phone = educator.phone ?? ''
      form.employmentType = educator.employmentType ?? ''
      form.hireDate = educator.hireDate ?? ''
      form.primaryCampusId = educator.primaryCampusId ?? ''
      form.rowVersion = educator.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.firstName.trim()) {
    errors.firstName = t('validation.required', { field: t('educator.fullName') })
    valid = false
  }
  if (!form.lastName.trim()) {
    errors.lastName = t('validation.required', { field: t('educator.fullName') })
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
        firstName: form.firstName,
        lastName: form.lastName,
        titleId: form.titleId || null,
        email: form.email || null,
        phone: form.phone || null,
        employmentType: form.employmentType || null,
        hireDate: form.hireDate || null,
        primaryCampusId: form.primaryCampusId || null,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'educator-detail', params: { id: id.value } })
    } else {
      const result = await store.create({
        corporationId: form.corporationId,
        firstName: form.firstName,
        lastName: form.lastName,
        titleId: form.titleId || null,
        email: form.email || null,
        phone: form.phone || null,
        employmentType: form.employmentType || null,
        hireDate: form.hireDate || null,
        primaryCampusId: form.primaryCampusId || null,
      })
      router.push({ name: 'educator-detail', params: { id: result.id } })
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
      :title="isEdit ? t('educator.edit') : t('educator.create')"
      :description="t('educator.description')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Personal Info -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">{{ t('educator.title') }}</h3>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.firstName') }} *</label>
            <input
              v-model="form.firstName"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.firstName ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.firstName" class="mt-1 text-xs text-red-600">{{ errors.firstName }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('guardian.lastName') }} *</label>
            <input
              v-model="form.lastName"
              type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.lastName ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.lastName" class="mt-1 text-xs text-red-600">{{ errors.lastName }}</p>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.titleLabel') }}</label>
          <select v-model="form.titleId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="title in titles" :key="title.id" :value="title.id">{{ title.label }}</option>
          </select>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.email') }}</label>
            <input v-model="form.email" type="email" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.phone') }}</label>
            <input v-model="form.phone" type="tel" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.employmentType') }}</label>
            <select v-model="form.employmentType" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="et in employmentTypes" :key="et.id" :value="et.code">{{ et.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.hireDate') }}</label>
            <input v-model="form.hireDate" type="date" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('educator.primaryCampus') }}</label>
          <select v-model="form.primaryCampusId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
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
