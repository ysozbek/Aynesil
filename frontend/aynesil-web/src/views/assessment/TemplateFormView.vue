<script setup lang="ts">
/**
 * Assessment Template Create / Edit form.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useAssessmentTemplateStore } from '@/stores/assessmentTemplate.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useAssessmentTemplateStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const types = ref<RefValueItem[]>([])
const categories = ref<RefValueItem[]>([])

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  code: '',
  name: '',
  typeId: '',
  categoryId: '',
  scoringModel: 'sum',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const generalError = ref('')
const saving = ref(false)

onMounted(async () => {
  await Promise.all([
    refData.getValues('ASSESSMENT_TYPE').then(v => { types.value = v }),
    refData.getValues('ASSESSMENT_CATEGORY').then(v => { categories.value = v }),
  ])

  if (isEdit.value && id.value) {
    await store.fetchOne(id.value)
    const tpl = store.current
    if (tpl) {
      form.code = tpl.code
      form.name = tpl.name
      form.typeId = tpl.typeId ?? ''
      form.categoryId = tpl.categoryId ?? ''
      form.scoringModel = tpl.scoringModel ?? 'sum'
      form.rowVersion = tpl.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.code.trim()) { errors.code = t('validation.required', { field: t('assessment.template.code') }); valid = false }
  if (!form.name.trim()) { errors.name = t('validation.required', { field: t('assessment.template.name') }); valid = false }
  return valid
}

async function submit() {
  if (!validate()) return
  saving.value = true
  generalError.value = ''
  try {
    if (isEdit.value && id.value) {
      await store.update(id.value, {
        name: form.name,
        typeId: form.typeId || undefined,
        categoryId: form.categoryId || undefined,
        scoringModel: form.scoringModel || undefined,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'assessment-template-detail', params: { id: id.value } })
    } else {
      const result = await store.create({
        corporationId: form.corporationId || undefined,
        code: form.code,
        name: form.name,
        typeId: form.typeId || undefined,
        categoryId: form.categoryId || undefined,
        scoringModel: form.scoringModel || undefined,
      })
      router.push({ name: 'assessment-template-detail', params: { id: result.id } })
    }
  } catch (e: unknown) {
    generalError.value = (e as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl mx-auto">
    <PageHeader
      :title="isEdit ? t('assessment.template.edit') : t('assessment.template.create')"
      :description="isEdit ? t('assessment.template.editDescription') : t('assessment.template.createDescription')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.template.code') }} *</label>
            <input v-model="form.code" type="text" :disabled="isEdit"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
              :class="errors.code ? 'border-red-400' : 'border-border'" />
            <p v-if="errors.code" class="mt-1 text-xs text-red-600">{{ errors.code }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.template.name') }} *</label>
            <input v-model="form.name" type="text"
              class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.name ? 'border-red-400' : 'border-border'" />
            <p v-if="errors.name" class="mt-1 text-xs text-red-600">{{ errors.name }}</p>
          </div>
        </div>

        <div class="grid grid-cols-3 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.template.type') }}</label>
            <select v-model="form.typeId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="type in types" :key="type.id" :value="type.id">{{ type.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.template.category') }}</label>
            <select v-model="form.categoryId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('assessment.template.scoringModel') }}</label>
            <select v-model="form.scoringModel" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="sum">{{ t('assessment.template.scoring.sum') }}</option>
              <option value="average">{{ t('assessment.template.scoring.average') }}</option>
              <option value="rubric">{{ t('assessment.template.scoring.rubric') }}</option>
              <option value="none">{{ t('assessment.template.scoring.none') }}</option>
            </select>
          </div>
        </div>
      </div>

      <div class="flex items-center justify-end gap-3">
        <button type="button" @click="router.back()"
          class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="saving"
          class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60">
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
