<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useGoalStore } from '@/stores/goal.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useGoalStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = computed(() => route.params.id as string | undefined)

const categories = ref<RefValueItem[]>([])
const developmentAreas = ref<RefValueItem[]>([])

const form = reactive({
  corporationId: auth.user?.corporationId ?? '',
  libraryId: '',
  categoryId: '',
  developmentAreaId: '',
  code: '',
  statement: '',
  defaultCriteria: '',
  rowVersion: 0,
})

const errors = reactive<Record<string, string>>({})
const saving = ref(false)
const generalError = ref('')

onMounted(async () => {
  await Promise.all([
    store.libraryList.items.length === 0 ? store.fetchLibraries({ corporationId: form.corporationId, pageSize: 200 }) : Promise.resolve(),
    refData.getValues('GOAL_CATEGORY').then(v => { categories.value = v }),
    refData.getValues('DEVELOPMENT_AREA').then(v => { developmentAreas.value = v }),
  ])

  if (isEdit.value && id.value) {
    await store.fetchTemplate(id.value)
    const tmpl = store.currentTemplate
    if (tmpl) {
      form.libraryId = tmpl.libraryId ?? ''
      form.categoryId = tmpl.categoryId ?? ''
      form.developmentAreaId = tmpl.developmentAreaId ?? ''
      form.code = tmpl.code ?? ''
      form.statement = tmpl.statement
      form.defaultCriteria = tmpl.defaultCriteria ?? ''
      form.rowVersion = tmpl.rowVersion
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete errors[k])
  let valid = true
  if (!form.statement.trim()) {
    errors.statement = t('validation.required', { field: t('goal.template.statement') })
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
      await store.updateTemplate(id.value, {
        libraryId: form.libraryId || null,
        categoryId: form.categoryId || null,
        developmentAreaId: form.developmentAreaId || null,
        code: form.code || null,
        statement: form.statement,
        defaultCriteria: form.defaultCriteria || null,
        rowVersion: form.rowVersion,
      })
      router.push({ name: 'goal-template-detail', params: { id: id.value } })
    } else {
      const result = await store.createTemplate({
        corporationId: form.corporationId || null,
        libraryId: form.libraryId || null,
        categoryId: form.categoryId || null,
        developmentAreaId: form.developmentAreaId || null,
        code: form.code || null,
        statement: form.statement,
        defaultCriteria: form.defaultCriteria || null,
      })
      router.push({ name: 'goal-template-detail', params: { id: result.id } })
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
      :title="isEdit ? t('common.edit') : t('goal.template.create')"
      :description="t('goal.template.title')"
    />

    <form @submit.prevent="submit" class="space-y-6">
      <p v-if="generalError" class="text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{{ generalError }}</p>

      <!-- Classification -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">Sınıflandırma</h3>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.library') }}</label>
            <select v-model="form.libraryId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="lib in store.libraryList.items" :key="lib.id" :value="lib.id">{{ lib.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.category') }}</label>
            <select v-model="form.categoryId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.developmentArea') }}</label>
            <select v-model="form.developmentAreaId" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('common.select') }}</option>
              <option v-for="a in developmentAreas" :key="a.id" :value="a.id">{{ a.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.code') }}</label>
            <input v-model="form.code" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm space-y-4">
        <h3 class="font-semibold text-foreground">İçerik</h3>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.statement') }} *</label>
          <textarea
            v-model="form.statement"
            rows="4"
            class="w-full px-3 py-2 text-sm rounded-lg border focus:outline-none focus:ring-1 focus:ring-primary resize-none"
            :class="errors.statement ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.statement" class="mt-1 text-xs text-red-600">{{ errors.statement }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('goal.template.defaultCriteria') }}</label>
          <textarea v-model="form.defaultCriteria" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none" />
        </div>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3">
        <button type="button" @click="router.back()" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="saving" class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-60">
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
