<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/camps" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>
    <div class="card mw-750px mx-auto">
      <div class="card-header border-0 pt-6">
        <h2 class="card-title fw-bold">{{ isEdit ? $t('camp.form.editTitle') : $t('camp.form.newTitle') }}</h2>
      </div>
      <div class="card-body">
        <form @submit.prevent="handleSubmit">
          <div class="row g-4">
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('camp.fields.code') }}</label>
              <input v-model="form.code" type="text" class="form-control" required :disabled="isEdit" />
            </div>
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('camp.fields.name') }}</label>
              <input v-model="form.name" type="text" class="form-control" required />
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('camp.fields.type') }}</label>
              <select v-model="form.campTypeId" class="form-select">
                <option value="">{{ $t('common.select') }}</option>
                <option v-for="ct in campTypes" :key="ct.id" :value="ct.id">{{ ct.label || ct.code }}</option>
              </select>
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('camp.fields.capacity') }}</label>
              <input v-model.number="form.capacity" type="number" min="1" class="form-control" />
            </div>
            <div class="col-12">
              <label class="form-label">{{ $t('camp.fields.location') }}</label>
              <input v-model="form.location" type="text" class="form-control" />
            </div>
            <div class="col-12">
              <label class="form-label">{{ $t('camp.fields.description') }}</label>
              <textarea v-model="form.description" class="form-control" rows="3"></textarea>
            </div>
          </div>
          <div v-if="errorMsg" class="alert alert-danger mt-5">{{ errorMsg }}</div>
          <div class="d-flex justify-content-end gap-3 mt-6">
            <RouterLink to="/camps" class="btn btn-light">{{ $t('common.cancel') }}</RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="campStore.saving">
              <span v-if="campStore.saving" class="spinner-border spinner-border-sm me-2"></span>
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
import { useCampStore } from '@/stores/camp.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'

const route = useRoute()
const router = useRouter()
const campStore = useCampStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const campTypes = computed(() => refDataStore.getByCategory?.('camp_type') ?? [])
const form = reactive({ code: '', name: '', campTypeId: '', capacity: undefined as number | undefined, location: '', description: '' })

async function handleSubmit() {
  errorMsg.value = ''
  const corp = authStore.user?.corporationId ?? ''
  try {
    if (isEdit.value && id) {
      const cur = campStore.currentCamp
      if (!cur) return
      await campStore.updateCamp(id, {
        name: form.name, campTypeId: form.campTypeId || undefined,
        capacity: form.capacity, location: form.location || undefined,
        description: form.description || undefined,
        rowVersion: cur.rowVersion,
      })
      router.push(`/camps/${id}`)
    } else {
      const result = await campStore.createCamp({
        corporationId: corp, code: form.code, name: form.name,
        campTypeId: form.campTypeId || undefined,
        capacity: form.capacity, location: form.location || undefined,
        description: form.description || undefined,
      })
      router.push(`/camps/${result.id}`)
    }
  } catch (e: unknown) { errorMsg.value = (e as Error).message }
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('camp_type')
  if (isEdit.value && id) {
    await campStore.fetchCamp(id)
    const c = campStore.currentCamp
    if (c) {
      form.code = c.code; form.name = c.name
      form.campTypeId = c.campTypeId ?? ''
      form.capacity = c.capacity
      form.location = c.location ?? ''
      form.description = c.description ?? ''
    }
  }
})
</script>
