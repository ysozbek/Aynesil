<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/consultancy/institutions" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>
    <div class="card mw-750px mx-auto">
      <div class="card-header border-0 pt-6">
        <h2 class="card-title fw-bold">{{ isEdit ? $t('consultancy.institution.form.editTitle') : $t('consultancy.institution.form.newTitle') }}</h2>
      </div>
      <div class="card-body">
        <form @submit.prevent="handleSubmit">
          <div class="row g-4">
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('consultancy.institution.fields.name') }}</label>
              <input v-model="form.name" type="text" class="form-control" required />
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancy.institution.fields.type') }}</label>
              <select v-model="form.institutionTypeId" class="form-select">
                <option value="">{{ $t('common.select') }}</option>
                <option v-for="t in institutionTypes" :key="t.id" :value="t.id">{{ t.label || t.code }}</option>
              </select>
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancy.institution.fields.city') }}</label>
              <input v-model="form.city" type="text" class="form-control" />
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancy.institution.fields.district') }}</label>
              <input v-model="form.district" type="text" class="form-control" />
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancy.institution.fields.contactName') }}</label>
              <input v-model="form.contactName" type="text" class="form-control" />
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancy.institution.fields.contactPhone') }}</label>
              <input v-model="form.contactPhone" type="tel" class="form-control" />
            </div>
            <div class="col-12">
              <label class="form-label">{{ $t('consultancy.institution.fields.contactEmail') }}</label>
              <input v-model="form.contactEmail" type="email" class="form-control" />
            </div>
          </div>
          <div v-if="errorMsg" class="alert alert-danger mt-5">{{ errorMsg }}</div>
          <div class="d-flex justify-content-end gap-3 mt-6">
            <RouterLink to="/consultancy/institutions" class="btn btn-light">{{ $t('common.cancel') }}</RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="consultancyStore.saving">
              <span v-if="consultancyStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'

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
  name: '', institutionTypeId: '', city: '', district: '',
  contactName: '', contactPhone: '', contactEmail: '',
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
  } catch (e: unknown) { errorMsg.value = (e as Error).message }
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
