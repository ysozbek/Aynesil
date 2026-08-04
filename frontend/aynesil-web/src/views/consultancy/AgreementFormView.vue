<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/consultancy/agreements" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div class="card mw-750px mx-auto">
      <div class="card-header border-0 pt-6">
        <h2 class="card-title fw-bold">
          {{ isEdit ? $t('consultancyContract.form.editTitle') : $t('consultancyContract.form.newTitle') }}
        </h2>
      </div>
      <div class="card-body">
        <form @submit.prevent="handleSubmit" novalidate>
          <div class="row g-4">
            <!-- Title -->
            <div class="col-12">
              <label class="form-label required">{{ $t('consultancyContract.fields.title') }}</label>
              <input
                v-model="form.title"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.title }"
                required
              />
              <div v-if="errors.title" class="invalid-feedback">{{ errors.title }}</div>
            </div>

            <!-- Agreement Type (from ref_type = agreement_type) -->
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancyContract.fields.type') }}</label>
              <select v-model="form.agreementTypeId" class="form-select">
                <option value="">{{ $t('common.select') }}</option>
                <option v-for="t in agreementTypes" :key="t.id" :value="t.id">{{ t.label || t.code }}</option>
              </select>
            </div>

            <!-- Consultancy Plan (required for create, pre-filled if from plan context) -->
            <div v-if="!isEdit" class="col-sm-6">
              <label class="form-label required">{{ $t('consultancyContract.fields.plan') }} ID</label>
              <input
                v-model="form.consultancyPlanId"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.consultancyPlanId }"
                :placeholder="$t('consultancyContract.form.planIdPlaceholder')"
                required
              />
              <div v-if="errors.consultancyPlanId" class="invalid-feedback">{{ errors.consultancyPlanId }}</div>
            </div>

            <!-- Institution ID (for create, pre-filled) -->
            <div v-if="!isEdit" class="col-sm-6">
              <label class="form-label required">{{ $t('consultancyContract.fields.institution') }} ID</label>
              <input
                v-model="form.institutionId"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.institutionId }"
                required
              />
              <div v-if="errors.institutionId" class="invalid-feedback">{{ errors.institutionId }}</div>
            </div>

            <!-- Start Date -->
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancyContract.fields.startDate') }}</label>
              <input v-model="form.startDate" type="date" class="form-control" />
            </div>

            <!-- End Date -->
            <div class="col-sm-6">
              <label class="form-label">{{ $t('consultancyContract.fields.endDate') }}</label>
              <input v-model="form.endDate" type="date" class="form-control" />
            </div>

            <!-- Description -->
            <div class="col-12">
              <label class="form-label">{{ $t('consultancyContract.fields.description') }}</label>
              <textarea v-model="form.description" class="form-control" rows="4"></textarea>
            </div>
          </div>

          <!-- Error message -->
          <div v-if="errorMsg" class="alert alert-danger mt-5">{{ errorMsg }}</div>

          <!-- Actions -->
          <div class="d-flex justify-content-end gap-3 mt-6">
            <RouterLink to="/consultancy/agreements" class="btn btn-light">{{ $t('common.cancel') }}</RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="store.saving">
              <span v-if="store.saving" class="spinner-border spinner-border-sm me-2"></span>
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
const store = useConsultancyStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const agreementTypes = computed(() => refDataStore.getByCategory?.('agreement_type') ?? [])

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
  await refDataStore.fetchCategory?.('agreement_type')
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
