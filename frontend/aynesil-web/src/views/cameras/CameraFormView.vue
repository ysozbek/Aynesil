<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/cameras" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>
    <div class="card mw-750px mx-auto">
      <div class="card-header border-0 pt-6">
        <h2 class="card-title fw-bold">{{ isEdit ? $t('camera.form.editTitle') : $t('camera.form.newTitle') }}</h2>
      </div>
      <div class="card-body">
        <form @submit.prevent="handleSubmit">
          <div class="row g-4">
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('camera.fields.code') }}</label>
              <input v-model="form.code" type="text" class="form-control" required />
            </div>
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('camera.fields.name') }}</label>
              <input v-model="form.name" type="text" class="form-control" required />
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('camera.fields.type') }}</label>
              <select v-model="form.cameraTypeId" class="form-select">
                <option value="">{{ $t('common.select') }}</option>
                <option v-for="ct in cameraTypes" :key="ct.id" :value="ct.id">{{ ct.label || ct.code }}</option>
              </select>
            </div>
            <div class="col-sm-6">
              <label class="form-label">{{ $t('camera.fields.streamRef') }}</label>
              <input v-model="form.streamRef" type="text" class="form-control" placeholder="rtsp://..." />
            </div>
          </div>
          <div v-if="errorMsg" class="alert alert-danger mt-5">{{ errorMsg }}</div>
          <div class="d-flex justify-content-end gap-3 mt-6">
            <RouterLink to="/cameras" class="btn btn-light">{{ $t('common.cancel') }}</RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="cameraStore.saving">
              <span v-if="cameraStore.saving" class="spinner-border spinner-border-sm me-2"></span>
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
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'

const route = useRoute()
const router = useRouter()
const cameraStore = useCameraStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()
const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const cameraTypes = computed(() => refDataStore.getByCategory?.('camera_type') ?? [])

const form = reactive({ code: '', name: '', cameraTypeId: '', streamRef: '' })

async function handleSubmit() {
  errorMsg.value = ''
  const corp = authStore.user?.corporationId ?? ''
  try {
    if (isEdit.value && id) {
      const cur = cameraStore.currentCamera
      if (!cur) return
      await cameraStore.updateCamera(id, {
        code: form.code, name: form.name,
        cameraTypeId: form.cameraTypeId || undefined,
        streamRef: form.streamRef || undefined,
        rowVersion: cur.rowVersion,
      })
      router.push(`/cameras/${id}`)
    } else {
      const result = await cameraStore.createCamera({
        corporationId: corp, code: form.code, name: form.name,
        cameraTypeId: form.cameraTypeId || undefined,
        streamRef: form.streamRef || undefined,
      })
      router.push(`/cameras/${result.id}`)
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('camera_type')
  if (isEdit.value && id) {
    await cameraStore.fetchCamera(id)
    const c = cameraStore.currentCamera
    if (c) {
      form.code = c.code
      form.name = c.name
      form.cameraTypeId = c.cameraTypeId ?? ''
      form.streamRef = c.streamRef ?? ''
    }
  }
})
</script>
