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
const consultancyStore = useConsultancyStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()
const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const institutionTypes = computed(() => refDataStore.getByCategory?.('institution_type') ?? [])

const form = reactive({
  name: '',
  institutionTypeId: '',
  city: '',
  district: '',
  contactName: '',
  contactPhone: '',
  contactEmail: '',
})

async function handleSubmit() {
  errorMsg.value = ''
  const corp = authStore.user?.corporationId ?? ''
  try {
    const payload = {
      name: form.name,
      institutionTypeId: form.institutionTypeId || undefined,
      city: form.city || undefined,
      district: form.district || undefined,
      contactName: form.contactName || undefined,
      contactPhone: form.contactPhone || undefined,
      contactEmail: form.contactEmail || undefined,
    }
    if (isEdit.value && id) {
      const cur = consultancyStore.currentInstitution
      if (!cur) return
      await consultancyStore.updateInstitution(id, { ...payload, rowVersion: cur.rowVersion })
      router.push(`/consultancy/institutions/${id}`)
    } else {
      const result = await consultancyStore.createInstitution({ ...payload, corporationId: corp })
      router.push(`/consultancy/institutions/${result.id}`)
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('institution_type')
  if (isEdit.value && id) {
    await consultancyStore.fetchInstitution(id)
    const inst = consultancyStore.currentInstitution
    if (inst) {
      form.name = inst.name
      form.institutionTypeId = inst.institutionTypeId ?? ''
      form.city = inst.city ?? ''
      form.district = inst.district ?? ''
      form.contactName = inst.contactName ?? ''
      form.contactPhone = inst.contactPhone ?? ''
      form.contactEmail = inst.contactEmail ?? ''
    }
  }
})
</script>

<template>
  <div>
    <PageHeader :title="isEdit ? t('consultancy.institution.form.editTitle') : t('consultancy.institution.form.newTitle')">
      <button
        @click="router.push('/consultancy/institutions')"
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
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.name') }} *</label>
          <input v-model="form.name" type="text" required class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.type') }}</label>
          <select v-model="form.institutionTypeId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="type in institutionTypes" :key="type.id" :value="type.id">{{ type.label || type.code }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.city') }}</label>
          <input v-model="form.city" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.district') }}</label>
          <input v-model="form.district" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.contactName') }}</label>
          <input v-model="form.contactName" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.contactPhone') }}</label>
          <input v-model="form.contactPhone" type="tel" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('consultancy.institution.fields.contactEmail') }}</label>
          <input v-model="form.contactEmail" type="email" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>

      <p v-if="errorMsg" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ errorMsg }}</p>

      <div class="flex justify-end gap-2 pt-2">
        <button type="button" @click="router.push('/consultancy/institutions')" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent">
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="consultancyStore.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ consultancyStore.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
