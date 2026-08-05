<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const agreementTypes = ref<Awaited<ReturnType<typeof refDataStore.getValues>>>([])

const form = reactive({
  title: '',
  agreementTypeId: '',
  consultancyPlanId: (route.query.planId as string) ?? '',
  institutionId: (route.query.institutionId as string) ?? '',
  startDate: '',
  endDate: '',
  description: '',
})

const errors = reactive({
  title: '',
  consultancyPlanId: '',
  institutionId: '',
})

function validate(): boolean {
  errors.title = ''
  errors.consultancyPlanId = ''
  errors.institutionId = ''
  let valid = true
  if (!form.title.trim()) { errors.title = 'Başlık zorunludur.'; valid = false }
  if (!isEdit.value && !form.consultancyPlanId.trim()) { errors.consultancyPlanId = 'Danışmanlık planı zorunludur.'; valid = false }
  if (!isEdit.value && !form.institutionId.trim()) { errors.institutionId = 'Kurum zorunludur.'; valid = false }
  return valid
}

async function handleSubmit() {
  if (!validate()) return
  errorMsg.value = ''
  try {
    if (isEdit.value && id) {
      const cur = store.currentAgreement
      if (!cur) return
      await store.updateAgreement(id, {
        agreementTypeId: form.agreementTypeId || undefined,
        title: form.title,
        description: form.description || undefined,
        startDate: form.startDate || undefined,
        endDate: form.endDate || undefined,
        rowVersion: cur.rowVersion,
      })
      router.push(`/consultancy/agreements/${id}`)
    } else {
      const result = await store.createAgreement({
        corporationId: authStore.user?.corporationId ?? '',
        consultancyPlanId: form.consultancyPlanId,
        institutionId: form.institutionId,
        agreementTypeId: form.agreementTypeId || undefined,
        title: form.title,
        description: form.description || undefined,
        startDate: form.startDate || undefined,
        endDate: form.endDate || undefined,
      })
      router.push(`/consultancy/agreements/${result.id}`)
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  agreementTypes.value = await refDataStore.getValues('agreement_type')
  if (isEdit.value && id) {
    await store.fetchAgreement(id)
    const a = store.currentAgreement
    if (a) {
      form.title = a.title
      form.agreementTypeId = a.agreementTypeId ?? ''
      form.startDate = a.startDate ?? ''
      form.endDate = a.endDate ?? ''
      form.description = a.description ?? ''
    }
  }
})
</script>

<template>
  <div>
    <PageHeader :title="isEdit ? t('consultancyContract.form.editTitle') : t('consultancyContract.form.newTitle')">
      <button
        @click="router.push('/consultancy/agreements')"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <form
      class="max-w-2xl rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-4"
      @submit.prevent="handleSubmit"
    >
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.title') }} *</label>
          <input
            v-model="form.title"
            type="text"
            required
            :class="['w-full h-10 px-3 text-sm rounded-lg border bg-transparent', errors.title ? 'border-red-500' : 'border-border']"
          />
          <p v-if="errors.title" class="text-xs text-red-600 mt-1">{{ errors.title }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.type') }}</label>
          <select v-model="form.agreementTypeId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="type in agreementTypes" :key="type.id" :value="type.id">{{ type.label || type.code }}</option>
          </select>
        </div>
        <div v-if="!isEdit">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.plan') }} ID *</label>
          <input
            v-model="form.consultancyPlanId"
            type="text"
            required
            :placeholder="t('consultancyContract.form.planIdPlaceholder')"
            :class="['w-full h-10 px-3 text-sm rounded-lg border bg-transparent', errors.consultancyPlanId ? 'border-red-500' : 'border-border']"
          />
          <p v-if="errors.consultancyPlanId" class="text-xs text-red-600 mt-1">{{ errors.consultancyPlanId }}</p>
        </div>
        <div v-if="!isEdit">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.institution') }} ID *</label>
          <input
            v-model="form.institutionId"
            type="text"
            required
            :class="['w-full h-10 px-3 text-sm rounded-lg border bg-transparent', errors.institutionId ? 'border-red-500' : 'border-border']"
          />
          <p v-if="errors.institutionId" class="text-xs text-red-600 mt-1">{{ errors.institutionId }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.startDate') }}</label>
          <input v-model="form.startDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.endDate') }}</label>
          <input v-model="form.endDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancyContract.fields.description') }}</label>
          <textarea v-model="form.description" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>

      <p v-if="errorMsg" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ errorMsg }}</p>

      <div class="flex justify-end gap-2 pt-2">
        <button type="button" @click="router.push('/consultancy/agreements')" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent">
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="store.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ store.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
