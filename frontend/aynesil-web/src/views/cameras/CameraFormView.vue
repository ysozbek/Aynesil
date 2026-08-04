<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore, type RefValueItem } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const cameraStore = useCameraStore()
const auth = useAuthStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const cameraTypes = ref<RefValueItem[]>([])

const form = reactive({ code: '', name: '', cameraTypeId: '', streamRef: '' })

async function handleSubmit() {
  errorMsg.value = ''
  const corp = auth.user?.corporationId ?? ''
  try {
    if (isEdit.value && id) {
      const cur = cameraStore.currentCamera
      if (!cur) return
      await cameraStore.updateCamera(id, {
        code: form.code,
        name: form.name,
        cameraTypeId: form.cameraTypeId || undefined,
        streamRef: form.streamRef || undefined,
        rowVersion: cur.rowVersion,
      })
      router.push({ name: 'camera-detail', params: { id } })
    } else {
      const result = await cameraStore.createCamera({
        corporationId: corp,
        code: form.code,
        name: form.name,
        cameraTypeId: form.cameraTypeId || undefined,
        streamRef: form.streamRef || undefined,
      })
      router.push({ name: 'camera-detail', params: { id: result.id } })
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  cameraTypes.value = await refData.getValues('camera_type')
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

<template>
  <div>
    <PageHeader
      :title="isEdit ? t('camera.form.editTitle') : t('camera.form.newTitle')"
    >
      <button
        @click="router.push({ name: 'cameras' })"
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
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.fields.code') }} *</label>
          <input
            v-model="form.code"
            type="text"
            required
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.fields.name') }} *</label>
          <input
            v-model="form.name"
            type="text"
            required
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.fields.type') }}</label>
          <select
            v-model="form.cameraTypeId"
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent"
          >
            <option value="">{{ t('common.select') }}</option>
            <option v-for="ct in cameraTypes" :key="ct.id" :value="ct.id">{{ ct.label || ct.code }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.fields.streamRef') }}</label>
          <input
            v-model="form.streamRef"
            type="text"
            placeholder="rtsp://..."
            class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
      </div>

      <p v-if="errorMsg" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ errorMsg }}</p>

      <div class="flex justify-end gap-2 pt-2">
        <button
          type="button"
          @click="router.push({ name: 'cameras' })"
          class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="cameraStore.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ cameraStore.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
