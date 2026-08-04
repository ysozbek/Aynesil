<template>
  <div class="p-6 space-y-6 max-w-4xl mx-auto">
    <div class="flex items-center gap-3">
      <button class="btn btn-ghost btn-sm" @click="$router.back()">
        ← {{ $t('common.back') }}
      </button>
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isEdit ? $t('notification.templates.edit') : $t('notification.templates.create') }}
      </h1>
    </div>

    <div v-if="store.loading && isEdit" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <form v-else @submit.prevent="submit" class="space-y-6">
      <!-- Basic info -->
      <div class="card bg-base-100 shadow">
        <div class="card-body space-y-4">
          <h2 class="font-semibold text-base">{{ $t('notification.templates.basicInfo') }}</h2>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="form-control">
              <label class="label"><span class="label-text">{{ $t('notification.templates.code') }} *</span></label>
              <input
                v-model="form.code"
                type="text"
                :class="['input input-bordered', errors.code ? 'input-error' : '']"
                :placeholder="$t('notification.templates.codePlaceholder')"
              />
              <label v-if="errors.code" class="label"><span class="label-text-alt text-error">{{ errors.code }}</span></label>
            </div>

            <div class="form-control">
              <label class="label"><span class="label-text">{{ $t('notification.templates.status') }}</span></label>
              <label class="label cursor-pointer justify-start gap-3">
                <input v-model="form.isActive" type="checkbox" class="toggle toggle-primary" />
                <span class="label-text">{{ form.isActive ? $t('common.active') : $t('common.passive') }}</span>
              </label>
            </div>
          </div>
        </div>
      </div>

      <!-- Translations -->
      <div class="card bg-base-100 shadow">
        <div class="card-body space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="font-semibold text-base">{{ $t('notification.templates.translations') }}</h2>
            <button type="button" class="btn btn-ghost btn-sm" @click="addTranslation">
              + {{ $t('notification.templates.addTranslation') }}
            </button>
          </div>

          <div
            v-for="(tr, idx) in form.translations"
            :key="idx"
            class="border rounded-lg p-4 space-y-3"
          >
            <div class="flex items-center justify-between">
              <div class="form-control w-24">
                <label class="label"><span class="label-text text-xs">{{ $t('notification.templates.locale') }}</span></label>
                <input v-model="tr.locale" type="text" class="input input-bordered input-sm" placeholder="tr" maxlength="5" />
              </div>
              <button v-if="form.translations.length > 1" type="button" class="btn btn-ghost btn-xs text-error" @click="removeTranslation(idx)">
                {{ $t('common.delete') }}
              </button>
            </div>
            <div class="form-control">
              <label class="label"><span class="label-text text-xs">{{ $t('notification.templates.subject') }}</span></label>
              <input v-model="tr.subject" type="text" class="input input-bordered input-sm" :placeholder="$t('notification.templates.subjectPlaceholder')" />
            </div>
            <div class="form-control">
              <label class="label"><span class="label-text text-xs">{{ $t('notification.templates.body') }} *</span></label>
              <textarea
                v-model="tr.body"
                class="textarea textarea-bordered"
                rows="4"
                :placeholder="$t('notification.templates.bodyPlaceholder')"
              ></textarea>
            </div>
          </div>
        </div>
      </div>

      <!-- Error alert -->
      <div v-if="submitError" class="alert alert-error">
        <span>{{ submitError }}</span>
      </div>

      <!-- Actions -->
      <div class="flex justify-end gap-3">
        <button type="button" class="btn btn-ghost" @click="$router.back()">{{ $t('common.cancel') }}</button>
        <button type="submit" class="btn btn-primary" :disabled="store.saving">
          <span v-if="store.saving" class="loading loading-spinner loading-sm"></span>
          {{ store.saving ? $t('common.saving') : $t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useNotificationTemplateStore } from '@/stores/notificationTemplate.store'

const route = useRoute()
const router = useRouter()
const store = useNotificationTemplateStore()

const isEdit = computed(() => !!route.params.id)
const submitError = ref<string | null>(null)

const form = ref({
  code: '',
  isActive: true,
  translations: [{ locale: 'tr', subject: '', body: '' }],
})
const errors = ref<Record<string, string>>({})

function addTranslation() {
  form.value.translations.push({ locale: '', subject: '', body: '' })
}
function removeTranslation(idx: number) {
  form.value.translations.splice(idx, 1)
}

function validate(): boolean {
  errors.value = {}
  if (!form.value.code.trim()) errors.value.code = 'Kod zorunludur.'
  return Object.keys(errors.value).length === 0
}

async function submit() {
  if (!validate()) return
  submitError.value = null
  try {
    if (isEdit.value) {
      const tpl = store.currentTemplate!
      await store.updateTemplate(route.params.id as string, {
        code: form.value.code,
        isActive: form.value.isActive,
        translations: form.value.translations,
        rowVersion: tpl.rowVersion,
      })
    } else {
      await store.createTemplate({
        code: form.value.code,
        isActive: form.value.isActive,
        translations: form.value.translations,
      })
    }
    router.push({ name: 'notification-templates' })
  } catch (e: unknown) {
    submitError.value = (e as Error).message
  }
}

onMounted(async () => {
  if (isEdit.value) {
    await store.fetchTemplate(route.params.id as string)
    if (store.currentTemplate) {
      form.value.code = store.currentTemplate.code
      form.value.isActive = store.currentTemplate.isActive
      form.value.translations = store.currentTemplate.translations.map(t => ({
        locale: t.locale,
        subject: t.subject ?? '',
        body: t.body,
      }))
    }
  }
})
</script>
